using ChatWithDocs.Application.Interfaces;
using MediatR;

namespace ChatWithDocs.Application.Documents.Queries;

public sealed record ListDocumentsQuery : IRequest<List<DocumentSummary>>;

public sealed class ListDocumentsQueryHandler(IDocumentRepository documentRepository)
    : IRequestHandler<ListDocumentsQuery, List<DocumentSummary>>
{
    public Task<List<DocumentSummary>> Handle(ListDocumentsQuery request, CancellationToken cancellationToken) =>
        documentRepository.GetAllAsync(cancellationToken);
}
