namespace ChatWithDocs.Domain.Entities;

// A denormalized snapshot of a cited passage, stored as JSON on the Message
// itself rather than a foreign key — so a message still shows what it cited
// even if the source document is later deleted.
public record MessageSource(string FileName, string Snippet, int ChunkIndex);
