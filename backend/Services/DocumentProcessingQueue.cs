using System.Threading.Channels;

namespace backend.Services;

// The PDF bytes travel in-memory with the job rather than through disk/blob storage,
// since this phase has no separate file store yet. A queued job (and its bytes) is
// lost if the app restarts mid-processing — acceptable for this dev project.
public record DocumentProcessingJob(Guid DocumentId, byte[] PdfContent);

public interface IDocumentProcessingQueue
{
    void Enqueue(DocumentProcessingJob job);
    IAsyncEnumerable<DocumentProcessingJob> DequeueAllAsync(CancellationToken cancellationToken);
}

public class DocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly Channel<DocumentProcessingJob> _channel = Channel.CreateUnbounded<DocumentProcessingJob>();

    public void Enqueue(DocumentProcessingJob job) => _channel.Writer.TryWrite(job);

    public IAsyncEnumerable<DocumentProcessingJob> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
