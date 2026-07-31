namespace ChatWithDocs.Application.Interfaces;

public abstract record ChatCompletionChunk;

public sealed record ChatTextChunk(string Text) : ChatCompletionChunk;

public sealed record ChatCitationChunk(int DocumentIndex) : ChatCompletionChunk;

public interface IChatService
{
    IAsyncEnumerable<ChatCompletionChunk> StreamAnswerAsync(
        string question,
        IReadOnlyList<ChunkSearchResult> context,
        CancellationToken cancellationToken);
}
