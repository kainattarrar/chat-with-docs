using ChatWithDocs.Domain.Entities;

namespace ChatWithDocs.Application.Interfaces;

public sealed record ChunkSearchResult(Guid DocumentId, string FileName, int ChunkIndex, string Content);

public interface IChunkRepository
{
    Task<bool> AnyAsync(CancellationToken cancellationToken);

    Task AddRangeAsync(IEnumerable<Chunk> chunks, CancellationToken cancellationToken);

    Task<List<ChunkSearchResult>> FindTopKByCosineDistanceAsync(
        float[] queryEmbedding,
        int topK,
        CancellationToken cancellationToken);
}
