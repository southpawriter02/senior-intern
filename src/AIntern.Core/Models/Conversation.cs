namespace SeniorIntern.Core.Models;

public sealed class Conversation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = "New Conversation";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ChatMessage> Messages { get; init; } = new();
    public string? SystemPrompt { get; set; }
    public string? ModelPath { get; set; }
}
