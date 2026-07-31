using ChatWithDocs.Application.Interfaces;
using ChatWithDocs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace ChatWithDocs.Infrastructure.Persistence.Repositories;

public class ChunkRepository(AppDbContext db) : IChunkRepository
{
    public Task<bool> AnyAsync(CancellationToken cancellationToken) =>
        db.Chunks.AnyAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Chunk> chunks, CancellationToken cancellationToken) =>
        await db.Chunks.AddRangeAsync(chunks, cancellationToken);

    public Task<List<ChunkSearchResult>> FindTopKByCosineDistanceAsync(
        float[] queryEmbedding,
        int topK,
        CancellationToken cancellationToken)
    {
        var queryVector = new Vector(queryEmbedding);

        return db.Chunks
            .Select(c => new
            {
                c.DocumentId,
                FileName = c.Document.FileName,
                c.ChunkIndex,
                c.Content,
                Distance = c.Embedding!.CosineDistance(queryVector),
            })
            .OrderBy(x => x.Distance)
            .Take(topK)
            .Select(x => new ChunkSearchResult(x.DocumentId, x.FileName, x.ChunkIndex, x.Content))
            .ToListAsync(cancellationToken);
    }
}
