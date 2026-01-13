namespace AIntern.Core.Interfaces;

using AIntern.Core.Events;
using AIntern.Core.Models;

/// <summary>
/// Service for managing conversations with database persistence.
/// </summary>
/// <remarks>
/// <para>
/// This service maintains a single active conversation in memory while
/// providing full CRUD operations backed by SQLite storage.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item>Auto-save with 500ms debouncing</item>
/// <item>Title auto-generation from first user message</item>
/// <item>Event-driven updates for UI synchronization</item>
/// <item>Thread-safe save operations</item>
/// </list>
/// </para>
/// </remarks>
public interface IConversationService
{
    #region State Properties

    /// <summary>
    /// Gets the currently active conversation.
    /// Never null - a new conversation is created if none exists.
    /// </summary>
    Conversation CurrentConversation { get; }

    /// <summary>
    /// Gets whether there are unsaved changes in the current conversation.
    /// </summary>
    bool HasUnsavedChanges { get; }

    /// <summary>
    /// Gets whether a conversation is currently loaded (has been persisted or has messages).
    /// </summary>
    bool HasActiveConversation { get; }

    #endregion

    #region Message Operations

    /// <summary>
    /// Adds a message to the current conversation.
    /// </summary>
    /// <param name="message">The message to add (User, Assistant, or System).</param>
    /// <remarks>
    /// <para>
    /// Automatically assigns a SequenceNumber and updates timestamps.
    /// Triggers the auto-save timer (500ms debounce).
    /// </para>
    /// <para>
    /// If this is the first user message and the title is "New Conversation",
    /// the title is auto-generated from the message content.
    /// </para>
    /// </remarks>
    void AddMessage(ChatMessage message);

    /// <summary>
    /// Updates an existing message in the current conversation.
    /// </summary>
    /// <param name="messageId">The ID of the message to update.</param>
    /// <param name="updateAction">Action to apply to the message.</param>
    /// <remarks>
    /// Used for streaming content updates during generation.
    /// Triggers the auto-save timer.
    /// </remarks>
    void UpdateMessage(Guid messageId, Action<ChatMessage> updateAction);

    /// <summary>
    /// Removes a message from the current conversation.
    /// </summary>
    /// <param name="messageId">The ID of the message to remove.</param>
    /// <remarks>
    /// Remaining messages are re-sequenced to maintain contiguous ordering.
    /// </remarks>
    void RemoveMessage(Guid messageId);

    /// <summary>
    /// Gets all messages in the current conversation.
    /// </summary>
    /// <returns>Read-only list of messages in chronological order.</returns>
    IReadOnlyList<ChatMessage> GetMessages();

    /// <summary>
    /// Clears all messages from the current conversation.
    /// </summary>
    /// <remarks>
    /// Resets to an empty state but keeps the same conversation.
    /// The conversation's IsPersisted state is preserved.
    /// </remarks>
    void ClearConversation();

    #endregion

    #region Conversation CRUD

    /// <summary>
    /// Creates a new conversation and sets it as current.
    /// </summary>
    /// <param name="title">Optional title (defaults to "New Conversation").</param>
    /// <param name="systemPromptId">Optional system prompt to associate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created conversation.</returns>
    /// <remarks>
    /// If the current conversation has unsaved changes, it is saved first.
    /// </remarks>
    Task<Conversation> CreateNewConversationAsync(
        string? title = null,
        Guid? systemPromptId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Loads an existing conversation by ID and sets it as current.
    /// </summary>
    /// <param name="conversationId">The conversation ID to load.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The loaded conversation with all messages.</returns>
    /// <exception cref="InvalidOperationException">If conversation not found.</exception>
    /// <remarks>
    /// If the current conversation has unsaved changes, it is saved first.
    /// </remarks>
    Task<Conversation> LoadConversationAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Saves the current conversation to the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// <para>
    /// If the conversation has not been persisted yet, creates a new record.
    /// Otherwise, updates the existing record and synchronizes messages.
    /// </para>
    /// <para>
    /// This method is typically called automatically by the auto-save timer,
    /// but can be called manually for immediate persistence.
    /// </para>
    /// </remarks>
    Task SaveCurrentConversationAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes a conversation from the database.
    /// </summary>
    /// <param name="conversationId">The conversation ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// If deleting the current conversation, a new conversation is created.
    /// </remarks>
    Task DeleteConversationAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Renames a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID to rename.</param>
    /// <param name="newTitle">The new title.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RenameConversationAsync(Guid conversationId, string newTitle, CancellationToken ct = default);

    #endregion

    #region Conversation Flags

    /// <summary>
    /// Archives a conversation (soft delete).
    /// </summary>
    /// <param name="conversationId">The conversation ID to archive.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Archived conversations are hidden from the main list but not deleted.
    /// </remarks>
    Task ArchiveConversationAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Unarchives a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID to unarchive.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UnarchiveConversationAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Pins a conversation to the top of the list.
    /// </summary>
    /// <param name="conversationId">The conversation ID to pin.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PinConversationAsync(Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Unpins a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID to unpin.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UnpinConversationAsync(Guid conversationId, CancellationToken ct = default);

    #endregion

    #region List Operations

    /// <summary>
    /// Gets recent conversations for the sidebar.
    /// </summary>
    /// <param name="count">Maximum number of conversations to return.</param>
    /// <param name="includeArchived">Whether to include archived conversations.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of conversation summaries, ordered by UpdatedAt descending.</returns>
    Task<IReadOnlyList<ConversationSummary>> GetRecentConversationsAsync(
        int count = 50,
        bool includeArchived = false,
        CancellationToken ct = default);

    /// <summary>
    /// Searches conversations by title or message content.
    /// </summary>
    /// <param name="query">Search query string.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching conversation summaries.</returns>
    Task<IReadOnlyList<ConversationSummary>> SearchConversationsAsync(
        string query,
        CancellationToken ct = default);

    #endregion

    #region Events

    /// <summary>
    /// Fired when the current conversation changes (created, loaded, modified, saved).
    /// </summary>
    event EventHandler<ConversationChangedEventArgs>? ConversationChanged;

    /// <summary>
    /// Fired when the conversation list should be refreshed.
    /// </summary>
    event EventHandler<ConversationListChangedEventArgs>? ConversationListChanged;

    /// <summary>
    /// Fired when save state changes (saving started, completed, or failed).
    /// </summary>
    event EventHandler<SaveStateChangedEventArgs>? SaveStateChanged;

    #endregion
}
