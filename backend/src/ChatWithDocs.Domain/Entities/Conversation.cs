namespace ChatWithDocs.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; }

    // Null until the title-generation call completes after the first exchange.
    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Message> Messages { get; set; } = [];
}
