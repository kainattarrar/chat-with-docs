namespace ChatWithDocs.Application.Interfaces;

// Lets the upload command and the background worker save a Document + its Chunks
// together atomically across two repositories, exactly as the single AppDbContext
// SaveChanges call did before this was split into repositories.
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
