using ChatWithDocs.Domain.Entities;

namespace ChatWithDocs.Application.Interfaces;

public interface IMessageRepository
{
    Task AddAsync(Message message, CancellationToken cancellationToken);
}
