using ChatWithDocs.Application.Interfaces;
using ChatWithDocs.Domain.Entities;

namespace ChatWithDocs.Infrastructure.Persistence.Repositories;

public class MessageRepository(AppDbContext db) : IMessageRepository
{
    public async Task AddAsync(Message message, CancellationToken cancellationToken) =>
        await db.Messages.AddAsync(message, cancellationToken);
}
