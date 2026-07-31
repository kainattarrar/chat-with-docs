namespace ChatWithDocs.Application.Interfaces;

public interface IEmbeddingService
{
    Task<List<float[]>> EmbedDocumentsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);

    Task<float[]> EmbedQueryAsync(string text, CancellationToken cancellationToken);
}
