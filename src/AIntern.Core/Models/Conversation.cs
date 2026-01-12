namespace AIntern.Core.Models;

/// <summary>
/// Represents a complete chat conversation containing multiple messages.
/// Tracks metadata such as creation time, title, and associated model.
/// </summary>
/// <remarks>
/// Conversations are the primary unit of state in the chat application.
/// Each conversation maintains its own message history and can be associated
/// with a specific model for context preservation.
/// </remarks>
public sealed class Conversation
{
    /// <summary>
    /// Gets the unique identifier for this conversation.
    /// Auto-generated on creation for database storage and reference.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display title for this conversation.
    /// Defaults to "New Conversation" and can be auto-generated from content.
    /// </summary>
    public string Title { get; set; } = "New Conversation";

    /// <summary>
    /// Gets the UTC timestamp when this conversation was created.
    /// Immutable after creation; used for sorting and display.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when this conversation was last modified.
    /// Updated whenever messages are added, edited, or removed.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the collection of messages in this conversation, ordered chronologically.
    /// Messages are added as the conversation progresses.
    /// </summary>
    public List<ChatMessage> Messages { get; init; } = new();

    /// <summary>
    /// Gets or sets the optional system prompt that provides context to the model.
    /// Prepended to the conversation when generating responses.
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Gets or sets the file path of the model used for this conversation.
    /// Stored to track which model generated the responses.
    /// </summary>
    public string? ModelPath { get; set; }
}
