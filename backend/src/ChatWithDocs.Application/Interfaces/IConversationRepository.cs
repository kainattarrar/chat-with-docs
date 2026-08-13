using ChatWithDocs.Domain.Entities;

namespace ChatWithDocs.Application.Interfaces;

public sealed record ConversationSummary(Guid Id, string? Title, DateTime UpdatedAt);

public sealed record MessageDto(Guid Id, string Role, string Content, IReadOnlyList<MessageSource> Sources, DateTime CreatedAt);

public sealed record ConversationDetail(Guid Id, string? Title, DateTime CreatedAt, DateTime UpdatedAt, IReadOnlyList<MessageDto> Messages);

public interface IConversationRepository
{
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken);

    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ConversationDetail?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<ConversationSummary>> GetAllAsync(CancellationToken cancellationToken);

    Task RemoveAsync(Conversation conversation, CancellationToken cancellationToken);
}
