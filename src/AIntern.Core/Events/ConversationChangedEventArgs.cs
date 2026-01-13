namespace AIntern.Core.Events;

using AIntern.Core.Models;

/// <summary>
/// Event args for conversation state changes.
/// </summary>
/// <remarks>
/// Fired by <c>IConversationService</c> when the current conversation
/// is created, loaded, modified, or saved.
/// </remarks>
public sealed class ConversationChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the affected conversation.
    /// </summary>
    public required Conversation Conversation { get; init; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public required ConversationChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the previous conversation ID (for load/create transitions).
    /// </summary>
    /// <remarks>
    /// Only populated when switching between conversations. Null for
    /// in-place changes like MessageAdded or Saved.
    /// </remarks>
    public Guid? PreviousConversationId { get; init; }
}

/// <summary>
/// Types of conversation changes.
/// </summary>
public enum ConversationChangeType
{
    /// <summary>
    /// A new conversation was created.
    /// </summary>
    Created,

    /// <summary>
    /// An existing conversation was loaded from the database.
    /// </summary>
    Loaded,

    /// <summary>
    /// A message was added to the conversation.
    /// </summary>
    MessageAdded,

    /// <summary>
    /// A message was updated (content or status change).
    /// </summary>
    MessageUpdated,

    /// <summary>
    /// A message was removed from the conversation.
    /// </summary>
    MessageRemoved,

    /// <summary>
    /// The conversation title was changed.
    /// </summary>
    TitleChanged,

    /// <summary>
    /// The conversation was saved to the database.
    /// </summary>
    Saved,

    /// <summary>
    /// All messages were cleared from the conversation.
    /// </summary>
    Cleared
}
