using Pgvector;

namespace ChatWithDocs.Domain.Entities;

public class Chunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;

    // Pgvector.Vector is a dependency-free value type (just wraps a float array) rather
    // than an EF/ORM concern, so it stays on the entity to preserve the existing
    // vector(1024) column mapping and CosineDistance query path without a schema change.
    public Vector? Embedding { get; set; }

    public Document Document { get; set; } = null!;
}
