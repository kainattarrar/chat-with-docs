using ChatWithDocs.Domain.Entities;

namespace ChatWithDocs.Application.Interfaces;

public sealed record DocumentSummary(Guid Id, string FileName, string Status, DateTime CreatedAt);

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken);

    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<DocumentSummary>> GetAllAsync(CancellationToken cancellationToken);

    Task RemoveAsync(Document document, CancellationToken cancellationToken);
}
