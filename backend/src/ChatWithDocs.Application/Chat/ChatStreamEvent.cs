namespace ChatWithDocs.Application.Chat;

public abstract record ChatStreamEvent;

public sealed record TokenStreamEvent(string Text) : ChatStreamEvent;

public sealed record SourcesStreamEvent(IReadOnlyList<ChatSourceDto> Sources) : ChatStreamEvent;

public sealed record DoneStreamEvent : ChatStreamEvent;

public sealed record ChatSourceDto(Guid DocumentId, string FileName, int ChunkIndex, string Snippet);
