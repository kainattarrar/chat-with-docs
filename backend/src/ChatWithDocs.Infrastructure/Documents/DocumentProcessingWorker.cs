using ChatWithDocs.Application.Interfaces;
using ChatWithDocs.Domain.Entities;
using ChatWithDocs.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace ChatWithDocs.Infrastructure.Documents;

// A BackgroundService is a singleton, so it must not capture any scoped dependency
// directly — each job gets its own DI scope instead.
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
        var chunkRepository = scope.ServiceProvider.GetRequiredService<IChunkRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        var text = PdfTextExtractor.ExtractText(job.PdfContent);
        var chunkTexts = TextChunker.Split(text);

        if (chunkTexts.Count == 0)
            throw new InvalidOperationException("No extractable text found in the PDF.");

        var embeddings = await embeddingService.EmbedDocumentsAsync(chunkTexts, cancellationToken);

        var chunks = chunkTexts.Select((content, index) => new Chunk
        {
            Id = Guid.NewGuid(),
            DocumentId = job.DocumentId,
            ChunkIndex = index,
            Content = content,
            Embedding = new Vector(embeddings[index]),
        });

        await chunkRepository.AddRangeAsync(chunks, cancellationToken);

        var document = await documentRepository.GetByIdAsync(job.DocumentId, cancellationToken)
            ?? throw new InvalidOperationException($"Document {job.DocumentId} not found.");
        document.Status = DocumentStatus.Ready;
        document.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkFailedAsync(Guid documentId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var document = await documentRepository.GetByIdAsync(documentId, cancellationToken);
            if (document is null)
                return;

            document.Status = DocumentStatus.Failed;
            document.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark document {DocumentId} as Failed", documentId);
        }
    }
}
