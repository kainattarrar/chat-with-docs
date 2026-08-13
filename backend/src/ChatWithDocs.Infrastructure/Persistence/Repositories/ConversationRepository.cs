using ChatWithDocs.Application.Interfaces;
using ChatWithDocs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatWithDocs.Infrastructure.Persistence.Repositories;

public class ConversationRepository(AppDbContext db) : IConversationRepository
{
    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken) =>
        await db.Conversations.AddAsync(conversation, cancellationToken);

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Conversations.FindAsync([id], cancellationToken);

    public async Task<ConversationDetail?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await db.Conversations
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.CreatedAt,
                c.UpdatedAt,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (conversation is null)
            return null;

        var messages = await db.Messages
            .Where(m => m.ConversationId == id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(m.Id, m.Role.ToString(), m.Content, m.Sources, m.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ConversationDetail(conversation.Id, conversation.Title, conversation.CreatedAt, conversation.UpdatedAt, messages);
    }

    public Task<List<ConversationSummary>> GetAllAsync(CancellationToken cancellationToken) =>
        db.Conversations
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummary(c.Id, c.Title, c.UpdatedAt))
            .ToListAsync(cancellationToken);

    public Task RemoveAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        db.Conversations.Remove(conversation);
        return Task.CompletedTask;
    }
}
