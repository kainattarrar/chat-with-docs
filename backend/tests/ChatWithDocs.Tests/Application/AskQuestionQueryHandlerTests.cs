using ChatWithDocs.Application.Chat;
using ChatWithDocs.Application.Chat.Queries;
using ChatWithDocs.Application.Common;
using ChatWithDocs.Application.Interfaces;
using ChatWithDocs.Domain.Entities;
using ChatWithDocs.Domain.Enums;
using NSubstitute;

namespace ChatWithDocs.Tests.Application;

public class AskQuestionQueryHandlerTests
{
    private readonly IChunkRepository _chunkRepository = Substitute.For<IChunkRepository>();
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly IConversationRepository _conversationRepository = Substitute.For<IConversationRepository>();
    private readonly IMessageRepository _messageRepository = Substitute.For<IMessageRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITitleGenerationService _titleGenerationService = Substitute.For<ITitleGenerationService>();

    private AskQuestionQueryHandler CreateHandler() => new(
        _chunkRepository,
        _embeddingService,
        _chatService,
        _conversationRepository,
        _messageRepository,
        _unitOfWork,
        _titleGenerationService);

    private static async Task<List<ChatStreamEvent>> CollectAsync(IAsyncEnumerable<ChatStreamEvent> events)
    {
        var list = new List<ChatStreamEvent>();
        await foreach (var e in events)
            list.Add(e);
        return list;
    }

    [Fact]
    public async Task Handle_NoDocumentsIngestedYet_SkipsRetrievalAndReturnsAFixedMessageWithNoSources()
    {
        _chunkRepository.AnyAsync(Arg.Any<CancellationToken>()).Returns(false);
        _titleGenerationService.GenerateTitleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("Generated Title");

        var handler = CreateHandler();
        var events = await CollectAsync(handler.Handle(new AskQuestionQuery("What's in my docs?", null), CancellationToken.None));

        Assert.IsType<ConversationStreamEvent>(events[0]);
        var sourcesEvent = Assert.Single(events.OfType<SourcesStreamEvent>());
        Assert.Empty(sourcesEvent.Sources);

        var tokenText = string.Concat(events.OfType<TokenStreamEvent>().Select(t => t.Text));
        Assert.Contains("No documents have been added yet", tokenText);

        await _embeddingService.DidNotReceive().EmbedQueryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _chatService.DidNotReceive().StreamAnswerAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ChunkSearchResult>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewConversation_CreatesItFirstAndEmitsConversationEventBeforeAnythingElse()
    {
        _chunkRepository.AnyAsync(Arg.Any<CancellationToken>()).Returns(false);
        _titleGenerationService.GenerateTitleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("New Title");

        var handler = CreateHandler();
        var events = await CollectAsync(handler.Handle(new AskQuestionQuery("Hello", null), CancellationToken.None));

        await _conversationRepository.Received(1).AddAsync(Arg.Any<Conversation>(), Arg.Any<CancellationToken>());
        var conversationEvent = Assert.IsType<ConversationStreamEvent>(events[0]);
        Assert.NotEqual(Guid.Empty, conversationEvent.ConversationId);
    }

    [Fact]
    public async Task Handle_NewConversation_GeneratesATitleAndReturnsItOnDone()
    {
        _chunkRepository.AnyAsync(Arg.Any<CancellationToken>()).Returns(false);
        _titleGenerationService.GenerateTitleAsync("Hello", Arg.Any<CancellationToken>()).Returns("Friendly Greeting");

        var handler = CreateHandler();
        var events = await CollectAsync(handler.Handle(new AskQuestionQuery("Hello", null), CancellationToken.None));

        var done = Assert.IsType<DoneStreamEvent>(events[^1]);
        Assert.Equal("Friendly Greeting", done.Title);
    }

    [Fact]
    public async Task Handle_ExistingConversationId_ReusesItAndNeverGeneratesATitle()
    {
        var existingId = Guid.NewGuid();
        var existingConversation = new Conversation { Id = existingId, Title = "Already Named", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _conversationRepository.GetByIdAsync(existingId, Arg.Any<CancellationToken>()).Returns(existingConversation);
        _chunkRepository.AnyAsync(Arg.Any<CancellationToken>()).Returns(false);

        var handler = CreateHandler();
        var events = await CollectAsync(handler.Handle(new AskQuestionQuery("Follow-up question", existingId), CancellationToken.None));

        await _conversationRepository.DidNotReceive().AddAsync(Arg.Any<Conversation>(), Arg.Any<CancellationToken>());
        await _titleGenerationService.DidNotReceive().GenerateTitleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());

        var conversationEvent = Assert.IsType<ConversationStreamEvent>(events[0]);
        Assert.Equal(existingId, conversationEvent.ConversationId);

        var done = Assert.IsType<DoneStreamEvent>(events[^1]);
        Assert.Null(done.Title);
    }

    [Fact]
    public async Task Handle_UnknownConversationId_Throws()
    {
        var missingId = Guid.NewGuid();
        _conversationRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((Conversation?)null);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CollectAsync(handler.Handle(new AskQuestionQuery("Anything", missingId), CancellationToken.None)));
    }

    [Fact]
    public async Task Handle_WithIngestedDocuments_EmbedsTheQuestionAndRetrievesTopKChunksForGeneration()
    {
        _chunkRepository.AnyAsync(Arg.Any<CancellationToken>()).Returns(true);
        var embedding = new float[] { 0.1f, 0.2f };
        _embeddingService.EmbedQueryAsync("Explain the SLA", Arg.Any<CancellationToken>()).Returns(embedding);
        _chunkRepository
            .FindTopKByCosineDistanceAsync(embedding, Constants.TopK, Arg.Any<CancellationToken>())
            .Returns([]);
        _chatService
            .StreamAnswerAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ChunkSearchResult>>(), Arg.Any<CancellationToken>())
            .Returns(EmptyChunks());
        _titleGenerationService.GenerateTitleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("Title");

        var handler = CreateHandler();
        await CollectAsync(handler.Handle(new AskQuestionQuery("Explain the SLA", null), CancellationToken.None));

        await _embeddingService.Received(1).EmbedQueryAsync("Explain the SLA", Arg.Any<CancellationToken>());
        await _chunkRepository.Received(1).FindTopKByCosineDistanceAsync(embedding, Constants.TopK, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StreamedAnswerChunks_AreForwardedAsTokenEventsInOrderAndConcatenateToTheFullAnswer()
    {
        _chunkRepository.AnyAsync(Arg.Any<CancellationToken>()).Returns(true);
        _embeddingService.EmbedQueryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new float[] { 1f });
        _chunkRepository
            .FindTopKByCosineDistanceAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _chatService
            .StreamAnswerAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ChunkSearchResult>>(), Arg.Any<CancellationToken>())
            .Returns(TextOnlyChunks("The ", "answer ", "streams ", "in."));

        var handler = CreateHandler();
        var events = await CollectAsync(handler.Handle(new AskQuestionQuery("Q", null), CancellationToken.None));

        var tokens = events.OfType<TokenStreamEvent>().Select(t => t.Text).ToList();
        Assert.Equal(["The ", "answer ", "streams ", "in."], tokens);
        Assert.Equal("The answer streams in.", string.Concat(tokens));
    }

    [Fact]
    public async Task Handle_CitedChunkIndices_AreMappedToSourcesInCitationOrderWithSnippetsFromTheRetrievedChunks()
    {
        var documentId = Guid.NewGuid();
        var retrievedChunks = new List<ChunkSearchResult>
        {
            new(documentId, "handbook.pdf", 0, "First relevant passage about vacation policy."),
            new(documentId, "handbook.pdf", 3, "Second relevant passage about sick leave."),
        };
        _chunkRepository.AnyAsync(Arg.Any<CancellationToken>()).Returns(true);
        _embeddingService.EmbedQueryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new float[] { 1f });
        _chunkRepository
            .FindTopKByCosineDistanceAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(retrievedChunks);
        _chatService
            .StreamAnswerAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ChunkSearchResult>>(), Arg.Any<CancellationToken>())
            .Returns(MixedChunks(
                new ChatTextChunk("Answer."),
                new ChatCitationChunk(1),
                new ChatCitationChunk(0)));

        var handler = CreateHandler();
        var events = await CollectAsync(handler.Handle(new AskQuestionQuery("Q", null), CancellationToken.None));

        var sourcesEvent = Assert.Single(events.OfType<SourcesStreamEvent>());
        Assert.Equal(2, sourcesEvent.Sources.Count);
        // Citation order (1 then 0), not retrieval order.
        Assert.Equal(3, sourcesEvent.Sources[0].ChunkIndex);
        Assert.Equal(0, sourcesEvent.Sources[1].ChunkIndex);
        Assert.All(sourcesEvent.Sources, s => Assert.Equal("handbook.pdf", s.FileName));
    }

    [Fact]
    public async Task Handle_DuplicateAndOutOfRangeCitations_AreIgnored()
    {
        var documentId = Guid.NewGuid();
        var retrievedChunks = new List<ChunkSearchResult> { new(documentId, "only.pdf", 0, "The only retrieved chunk.") };
        _chunkRepository.AnyAsync(Arg.Any<CancellationToken>()).Returns(true);
        _embeddingService.EmbedQueryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new float[] { 1f });
        _chunkRepository
            .FindTopKByCosineDistanceAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(retrievedChunks);
        _chatService
            .StreamAnswerAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ChunkSearchResult>>(), Arg.Any<CancellationToken>())
            .Returns(MixedChunks(
                new ChatCitationChunk(0),
                new ChatCitationChunk(0),   // duplicate of an already-seen index
                new ChatCitationChunk(-1),  // out of range
                new ChatCitationChunk(7))); // out of range

        var handler = CreateHandler();
        var events = await CollectAsync(handler.Handle(new AskQuestionQuery("Q", null), CancellationToken.None));

        var sourcesEvent = Assert.Single(events.OfType<SourcesStreamEvent>());
        var source = Assert.Single(sourcesEvent.Sources);
        Assert.Equal("only.pdf", source.FileName);
    }

    [Fact]
    public async Task Handle_PersistsTheUserQuestionAndTheAssistantAnswerWithItsSources()
    {
        var documentId = Guid.NewGuid();
        _chunkRepository.AnyAsync(Arg.Any<CancellationToken>()).Returns(true);
        _embeddingService.EmbedQueryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new float[] { 1f });
        _chunkRepository
            .FindTopKByCosineDistanceAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([new(documentId, "doc.pdf", 2, "Cited content.")]);
        _chatService
            .StreamAnswerAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ChunkSearchResult>>(), Arg.Any<CancellationToken>())
            .Returns(MixedChunks(new ChatTextChunk("Final answer."), new ChatCitationChunk(0)));

        var persisted = new List<Message>();
        _messageRepository
            .When(r => r.AddAsync(Arg.Any<Message>(), Arg.Any<CancellationToken>()))
            .Do(call => persisted.Add(call.Arg<Message>()));

        var handler = CreateHandler();
        await CollectAsync(handler.Handle(new AskQuestionQuery("What does it say?", null), CancellationToken.None));

        Assert.Equal(2, persisted.Count);

        var userMessage = persisted[0];
        Assert.Equal(MessageRole.User, userMessage.Role);
        Assert.Equal("What does it say?", userMessage.Content);
        Assert.Empty(userMessage.Sources);

        var assistantMessage = persisted[1];
        Assert.Equal(MessageRole.Assistant, assistantMessage.Role);
        Assert.Equal("Final answer.", assistantMessage.Content);
        var source = Assert.Single(assistantMessage.Sources);
        Assert.Equal("doc.pdf", source.FileName);
        Assert.Equal(2, source.ChunkIndex);
    }

    private static async IAsyncEnumerable<ChatCompletionChunk> EmptyChunks()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<ChatCompletionChunk> TextOnlyChunks(params string[] texts)
    {
        foreach (var text in texts)
        {
            await Task.Yield();
            yield return new ChatTextChunk(text);
        }
    }

    private static async IAsyncEnumerable<ChatCompletionChunk> MixedChunks(params ChatCompletionChunk[] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }
}
