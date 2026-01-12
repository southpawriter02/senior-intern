using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Defines the contract for managing conversation state and message history.
/// </summary>
/// <remarks>
/// Implementation notes:
/// <list type="bullet">
/// <item>Maintains a single active conversation at a time</item>
/// <item>Messages are stored in chronological order</item>
/// <item>Fires events on any state change for UI binding</item>
/// </list>
/// Future enhancements may include multi-conversation support and persistence.
/// </remarks>
public interface IConversationService
{
    #region Properties

    /// <summary>
    /// Gets the current active conversation.
    /// Never null - a new conversation is created if none exists.
    /// </summary>
    Conversation CurrentConversation { get; }

    #endregion

    #region Message Operations

    /// <summary>
    /// Adds a message to the current conversation.
    /// Updates the conversation's UpdatedAt timestamp.
    /// </summary>
    /// <param name="message">The message to add (User, Assistant, or System).</param>
    void AddMessage(ChatMessage message);

    /// <summary>
    /// Updates an existing message in the current conversation.
    /// Used for streaming content updates during generation.
    /// </summary>
    /// <param name="messageId">The ID of the message to update.</param>
    /// <param name="updateAction">Action to apply to the message.</param>
    void UpdateMessage(Guid messageId, Action<ChatMessage> updateAction);

    /// <summary>
    /// Gets all messages in the current conversation.
    /// Returns a read-only view to prevent external modification.
    /// </summary>
    /// <returns>Enumerable of messages in chronological order.</returns>
    IEnumerable<ChatMessage> GetMessages();

    #endregion

    #region Conversation Management

    /// <summary>
    /// Clears all messages from the current conversation.
    /// Resets to an empty state but keeps the same conversation ID.
    /// </summary>
    void ClearConversation();

    /// <summary>
    /// Creates a new conversation and sets it as current.
    /// Generates a new ID and resets all message history.
    /// </summary>
    /// <returns>The newly created conversation.</returns>
    Conversation CreateNewConversation();

    #endregion

    #region Events

    /// <summary>
    /// Raised when the conversation changes (message added, updated, or cleared).
    /// Subscribe to this for reactive UI updates.
    /// </summary>
    event EventHandler? ConversationChanged;

    #endregion
}
