using backend.Data;
using backend.Models;
using Pgvector;

namespace backend.Services;

// A BackgroundService is a singleton, so it must not capture the scoped AppDbContext
// directly — each job gets its own DI scope (and DbContext) instead.
public class DocumentProcessingWorker(
    IDocumentProcessingQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process document {DocumentId}", job.DocumentId);
                await MarkFailedAsync(job.DocumentId, stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(DocumentProcessingJob job, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var embeddingClient = scope.ServiceProvider.GetRequiredService<VoyageEmbeddingClient>();

        var text = PdfTextExtractor.ExtractText(job.PdfContent);
        var chunkTexts = TextChunker.Split(text);

        if (chunkTexts.Count == 0)
            throw new InvalidOperationException("No extractable text found in the PDF.");

        var embeddings = await embeddingClient.EmbedDocumentsAsync(chunkTexts, cancellationToken);

        var chunks = chunkTexts.Select((content, index) => new Chunk
        {
            Id = Guid.NewGuid(),
            DocumentId = job.DocumentId,
            ChunkIndex = index,
            Content = content,
            Embedding = new Vector(embeddings[index]),
        });

        db.Chunks.AddRange(chunks);

        var document = await db.Documents.FindAsync([job.DocumentId], cancellationToken)
            ?? throw new InvalidOperationException($"Document {job.DocumentId} not found.");
        document.Status = DocumentStatus.Ready;
        document.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkFailedAsync(Guid documentId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var document = await db.Documents.FindAsync([documentId], cancellationToken);
            if (document is null)
                return;

            document.Status = DocumentStatus.Failed;
            document.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark document {DocumentId} as Failed", documentId);
        }
    }
}
