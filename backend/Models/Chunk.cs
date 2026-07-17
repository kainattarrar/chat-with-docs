using Pgvector;

namespace backend.Models;

public class Chunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public Vector? Embedding { get; set; }

    public Document Document { get; set; } = null!;
}
