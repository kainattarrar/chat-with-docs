using ChatWithDocs.Application.Interfaces;
using ChatWithDocs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatWithDocs.Infrastructure.Persistence.Repositories;

public class DocumentRepository(AppDbContext db) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken) =>
        await db.Documents.AddAsync(document, cancellationToken);

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Documents.FindAsync([id], cancellationToken);

    public Task<List<DocumentSummary>> GetAllAsync(CancellationToken cancellationToken) =>
        db.Documents
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DocumentSummary(d.Id, d.FileName, d.Status.ToString(), d.CreatedAt))
            .ToListAsync(cancellationToken);

    public Task RemoveAsync(Document document, CancellationToken cancellationToken)
    {
        db.Documents.Remove(document);
        return Task.CompletedTask;
    }
}
