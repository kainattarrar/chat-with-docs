using ChatWithDocs.Application.Interfaces;
using MediatR;

namespace ChatWithDocs.Application.Documents.Commands;

public sealed record DeleteDocumentCommand(Guid Id) : IRequest<bool>;

public sealed class DeleteDocumentCommandHandler(
    IDocumentRepository documentRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteDocumentCommand, bool>
{
    public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await documentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (document is null)
            return false;

        await documentRepository.RemoveAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
