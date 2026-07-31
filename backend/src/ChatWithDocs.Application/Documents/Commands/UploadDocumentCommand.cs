using ChatWithDocs.Application.Interfaces;
using ChatWithDocs.Domain.Entities;
using ChatWithDocs.Domain.Enums;
using MediatR;

namespace ChatWithDocs.Application.Documents.Commands;

public sealed record UploadDocumentCommand(string FileName, byte[] Content) : IRequest<UploadDocumentResult>;

public sealed record UploadDocumentResult(Guid Id, string Status);

public sealed class UploadDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IUnitOfWork unitOfWork,
    IDocumentProcessingQueue queue) : IRequestHandler<UploadDocumentCommand, UploadDocumentResult>
{
    public async Task<UploadDocumentResult> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = request.FileName,
            Status = DocumentStatus.Processing,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await documentRepository.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        queue.Enqueue(new DocumentProcessingJob(document.Id, request.Content));

        return new UploadDocumentResult(document.Id, document.Status.ToString());
    }
}
