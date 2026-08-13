namespace ChatWithDocs.Application.Chat;

public abstract record ChatStreamEvent;

// Emitted first, before retrieval/generation, so the client can update its
// sidebar/URL without waiting for the answer to finish.
public sealed record ConversationStreamEvent(Guid ConversationId) : ChatStreamEvent;

public sealed record TokenStreamEvent(string Text) : ChatStreamEvent;

public sealed record SourcesStreamEvent(IReadOnlyList<ChatSourceDto> Sources) : ChatStreamEvent;

// Title is non-null only when this exchange just generated one (i.e. it was
// the conversation's first exchange); the client already knows the title
// otherwise.
public sealed record DoneStreamEvent(Guid ConversationId, string? Title) : ChatStreamEvent;

public sealed record ChatSourceDto(Guid DocumentId, string FileName, int ChunkIndex, string Snippet);
