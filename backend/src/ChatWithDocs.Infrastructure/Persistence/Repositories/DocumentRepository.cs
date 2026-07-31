using ChatWithDocs.Application.Interfaces;
using ChatWithDocs.Domain.Entities;

namespace ChatWithDocs.Infrastructure.Persistence.Repositories;

public class DocumentRepository(AppDbContext db) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken) =>
        await db.Documents.AddAsync(document, cancellationToken);

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Documents.FindAsync([id], cancellationToken);
}
