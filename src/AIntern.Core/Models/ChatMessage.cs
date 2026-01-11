namespace SeniorIntern.Core.Models;

public enum MessageRole
{
    System,
    User,
    Assistant
}

public sealed class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public MessageRole Role { get; init; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool IsComplete { get; set; } = true;
    public int? TokenCount { get; set; }
    public TimeSpan? GenerationTime { get; set; }
}
