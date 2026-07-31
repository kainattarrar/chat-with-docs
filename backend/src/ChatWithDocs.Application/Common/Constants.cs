namespace ChatWithDocs.Application.Common;

public static class Constants
{
    // Must match the output_dimension requested from the embedding provider and the
    // pgvector column width — shared here so ingestion and retrieval can't drift apart.
    public const int EmbeddingDimensions = 1024;

    public const int TopK = 5;

    public const long MaxDocumentSizeBytes = 20 * 1024 * 1024;

    public const int SourceSnippetLength = 300;
}
