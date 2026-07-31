using System.Runtime.CompilerServices;
using ChatWithDocs.Application.Common;
using ChatWithDocs.Application.Interfaces;
using MediatR;

namespace ChatWithDocs.Application.Chat.Queries;

public sealed record AskQuestionQuery(string Question) : IStreamRequest<ChatStreamEvent>;

public sealed class AskQuestionQueryHandler(
    IChunkRepository chunkRepository,
    IEmbeddingService embeddingService,
    IChatService chatService) : IStreamRequestHandler<AskQuestionQuery, ChatStreamEvent>
{
    private const string NoDocumentsMessage =
        "No documents have been added yet, so there's nothing to search. Upload a document and ask again.";

    public async IAsyncEnumerable<ChatStreamEvent> Handle(
        AskQuestionQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var hasAnyChunks = await chunkRepository.AnyAsync(cancellationToken);
        if (!hasAnyChunks)
        {
            yield return new SourcesStreamEvent([]);
            yield return new TokenStreamEvent(NoDocumentsMessage);
            yield return new DoneStreamEvent();
            yield break;
        }

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

        var sources = citedIndices
            .Select(i => topChunks[i])
            .Select(c => new ChatSourceDto(c.DocumentId, c.FileName, c.ChunkIndex, Truncate(c.Content, Constants.SourceSnippetLength)))
            .ToList();

        yield return new SourcesStreamEvent(sources);
        yield return new DoneStreamEvent();
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
}
