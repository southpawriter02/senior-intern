namespace AIntern.Core.Events;

/// <summary>
/// Event args for conversation list changes (for sidebar refresh).
/// </summary>
/// <remarks>
/// Fired by <c>IConversationService</c> when the conversation list
/// should be refreshed in the UI (e.g., after save, delete, or archive).
/// </remarks>
public sealed class ConversationListChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type of list change that occurred.
    /// </summary>
    public required ConversationListChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the affected conversation ID (if applicable).
    /// </summary>
    /// <remarks>
    /// Populated for targeted changes (Added, Removed, Updated).
    /// Null for full list refresh.
    /// </remarks>
    public Guid? AffectedConversationId { get; init; }
}

/// <summary>
/// Types of conversation list changes.
/// </summary>
public enum ConversationListChangeType
{
    /// <summary>
    /// A new conversation was added to the list.
    /// Triggered after first save of a new conversation.
    /// </summary>
    ConversationAdded,

    /// <summary>
    /// A conversation was removed from the list.
    /// Triggered after delete or archive.
    /// </summary>
    ConversationRemoved,

    /// <summary>
    /// A conversation in the list was updated (title, message count, etc.).
    /// Triggered after save updates or rename.
    /// </summary>
    ConversationUpdated,

    /// <summary>
    /// The entire list should be refreshed.
    /// Used for bulk operations or filter changes.
    /// </summary>
    ListRefreshed
}
