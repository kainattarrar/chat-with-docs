namespace ChatWithDocs.Application.Interfaces;

// The PDF bytes travel in-memory with the job rather than through disk/blob storage,
// since there's no separate file store. A queued job (and its bytes) is lost if the
// app restarts mid-processing — acceptable for this dev project.
public sealed record DocumentProcessingJob(Guid DocumentId, byte[] PdfContent);

public interface IDocumentProcessingQueue
{
    void Enqueue(DocumentProcessingJob job);

    IAsyncEnumerable<DocumentProcessingJob> DequeueAllAsync(CancellationToken cancellationToken);
}
