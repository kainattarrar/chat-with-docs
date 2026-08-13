using ChatWithDocs.Domain.Enums;

namespace ChatWithDocs.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;

    // Only ever populated for Assistant messages; empty for User messages
    // and for off-topic answers that cited nothing.
    public List<MessageSource> Sources { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
