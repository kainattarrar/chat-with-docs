using System.Threading.Channels;
using ChatWithDocs.Application.Interfaces;

namespace ChatWithDocs.Infrastructure.Documents;

public class DocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly Channel<DocumentProcessingJob> _channel = Channel.CreateUnbounded<DocumentProcessingJob>();

    public void Enqueue(DocumentProcessingJob job) => _channel.Writer.TryWrite(job);

    public IAsyncEnumerable<DocumentProcessingJob> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
