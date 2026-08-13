using System.Runtime.CompilerServices;
using System.Text;
using ChatWithDocs.Application.Common;
using ChatWithDocs.Application.Interfaces;
using ChatWithDocs.Domain.Entities;
using ChatWithDocs.Domain.Enums;
using MediatR;

namespace ChatWithDocs.Application.Chat.Queries;

public sealed record AskQuestionQuery(string Question, Guid? ConversationId) : IStreamRequest<ChatStreamEvent>;

public sealed class AskQuestionQueryHandler(
    IChunkRepository chunkRepository,
    IEmbeddingService embeddingService,
    IChatService chatService,
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IUnitOfWork unitOfWork,
    ITitleGenerationService titleGenerationService) : IStreamRequestHandler<AskQuestionQuery, ChatStreamEvent>
{
    private const string NoDocumentsMessage =
        "No documents have been added yet, so there's nothing to search. Upload a document and ask again.";

    public async IAsyncEnumerable<ChatStreamEvent> Handle(
        AskQuestionQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var isNewConversation = request.ConversationId is null;
        var conversation = await ResolveConversationAsync(request.ConversationId, cancellationToken);

        yield return new ConversationStreamEvent(conversation.Id);

        await PersistMessageAsync(conversation.Id, MessageRole.User, request.Question, [], cancellationToken);

        var hasAnyChunks = await chunkRepository.AnyAsync(cancellationToken);

        var assistantContent = new StringBuilder();
        List<ChatSourceDto> sources;

        if (!hasAnyChunks)
        {
            sources = [];
            yield return new SourcesStreamEvent(sources);

            assistantContent.Append(NoDocumentsMessage);
            yield return new TokenStreamEvent(NoDocumentsMessage);
        }
        else
        {
            var queryEmbedding = await embeddingService.EmbedQueryAsync(request.Question, cancellationToken);
            var topChunks = await chunkRepository.FindTopKByCosineDistanceAsync(queryEmbedding, Constants.TopK, cancellationToken);

            // Citations tell us which retrieved chunks Claude actually grounded its answer in —
            // that, not raw top-K retrieval, is what determines what we surface as "sources".
            var citedIndices = new List<int>();
            var seenIndices = new HashSet<int>();

            await foreach (var chunk in chatService.StreamAnswerAsync(request.Question, topChunks, cancellationToken))
            {
                switch (chunk)
                {
                    case ChatTextChunk textChunk:
                        assistantContent.Append(textChunk.Text);
                        yield return new TokenStreamEvent(textChunk.Text);
                        break;
                    case ChatCitationChunk citationChunk
                        when citationChunk.DocumentIndex >= 0
                            && citationChunk.DocumentIndex < topChunks.Count
                            && seenIndices.Add(citationChunk.DocumentIndex):
                        citedIndices.Add(citationChunk.DocumentIndex);
                        break;
                }
            }

            sources = citedIndices
                .Select(i => topChunks[i])
                .Select(c => new ChatSourceDto(c.DocumentId, c.FileName, c.ChunkIndex, Truncate(c.Content, Constants.SourceSnippetLength)))
                .ToList();

            yield return new SourcesStreamEvent(sources);
        }

        var messageSources = sources.Select(s => new MessageSource(s.FileName, s.Snippet, s.ChunkIndex)).ToList();
        await PersistMessageAsync(conversation.Id, MessageRole.Assistant, assistantContent.ToString(), messageSources, cancellationToken);

        string? title = null;
        if (isNewConversation)
        {
            title = await titleGenerationService.GenerateTitleAsync(request.Question, cancellationToken);
            conversation.Title = title;
        }

        conversation.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        yield return new DoneStreamEvent(conversation.Id, title);
    }

    private async Task<Conversation> ResolveConversationAsync(Guid? conversationId, CancellationToken cancellationToken)
    {
        if (conversationId is null)
        {
            var now = DateTime.UtcNow;
            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Title = null,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await conversationRepository.AddAsync(conversation, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return conversation;
        }

        return await conversationRepository.GetByIdAsync(conversationId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Conversation {conversationId} not found.");
    }

    private async Task PersistMessageAsync(
        Guid conversationId,
        MessageRole role,
        string content,
        List<MessageSource> sources,
        CancellationToken cancellationToken)
    {
        await messageRepository.AddAsync(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            Sources = sources,
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
}
