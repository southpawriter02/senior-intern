namespace AIntern.Core.Models;

/// <summary>
/// Domain model for a conversation (in-memory representation).
/// Bridges between entities and ViewModels with persistence state tracking.
/// </summary>
/// <remarks>
/// <para>
/// Conversations are the primary unit of state in the chat application.
/// Each conversation maintains its own message history and can be associated
/// with a specific model and system prompt.
/// </para>
/// <para>
/// Persistence state is tracked via <see cref="IsPersisted"/> and
/// <see cref="HasUnsavedChanges"/> flags, enabling auto-save functionality
/// in the <c>DatabaseConversationService</c>.
/// </para>
/// </remarks>
public sealed class Conversation
{
    #region Identity

    /// <summary>
    /// Gets or sets the unique identifier for this conversation.
    /// Auto-generated on creation for database storage and reference.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display title for this conversation.
    /// Defaults to "New Conversation" and auto-generates from first user message.
    /// </summary>
    public string Title { get; set; } = "New Conversation";

    #endregion

    #region Timestamps

    /// <summary>
    /// Gets or sets the UTC timestamp when this conversation was created.
    /// Used for sorting and display.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when this conversation was last modified.
    /// Updated whenever messages are added, edited, or removed.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    #endregion

    #region Model Info

    /// <summary>
    /// Gets or sets the file path of the model used for this conversation.
    /// Stored to track which model generated the responses.
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// Gets or sets the human-readable model name for display.
    /// </summary>
    public string? ModelName { get; set; }

    #endregion

    #region System Prompt

    /// <summary>
    /// Gets or sets the ID of the associated system prompt.
    /// References a SystemPromptEntity in the database.
    /// </summary>
    public Guid? SystemPromptId { get; set; }

    /// <summary>
    /// Gets or sets the system prompt name for display purposes.
    /// Cached here to avoid extra database lookups.
    /// </summary>
    public string? SystemPromptName { get; set; }

    /// <summary>
    /// Gets or sets the system prompt content.
    /// Prepended to the conversation when generating responses.
    /// </summary>
    public string? SystemPrompt { get; set; }

    #endregion

    #region Flags

    /// <summary>
    /// Gets or sets whether the conversation is archived (soft deleted).
    /// Archived conversations are hidden from the main list.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// Gets or sets whether the conversation is pinned to the top of the list.
    /// </summary>
    public bool IsPinned { get; set; }

    #endregion

    #region Persistence State

    /// <summary>
    /// Gets or sets whether the conversation has been saved to the database.
    /// </summary>
    /// <remarks>
    /// A new conversation starts with IsPersisted = false.
    /// After the first save, this becomes true.
    /// </remarks>
    public bool IsPersisted { get; set; }

    /// <summary>
    /// Gets or sets whether there are unsaved changes.
    /// </summary>
    /// <remarks>
    /// Set to true when messages are added/removed/updated.
    /// Set to false after a successful save operation.
    /// </remarks>
    public bool HasUnsavedChanges { get; set; }

    #endregion

    #region Messages

    /// <summary>
    /// Internal storage for messages. Use <see cref="Messages"/> for read access.
    /// </summary>
    private readonly List<ChatMessage> _messages = [];

    /// <summary>
    /// Gets the ordered list of messages in this conversation.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages => _messages;

    /// <summary>
    /// Adds a message to the conversation.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <remarks>
    /// <para>
    /// Automatically assigns a SequenceNumber based on the current message count.
    /// Updates the UpdatedAt timestamp and sets HasUnsavedChanges to true.
    /// </para>
    /// </remarks>
    public void AddMessage(ChatMessage message)
    {
        // Assign sequence number for database ordering.
        // SequenceNumber is 1-based (first message = 1).
        message.SequenceNumber = _messages.Count + 1;

        _messages.Add(message);

        // Mark conversation as modified for auto-save detection.
        UpdatedAt = DateTime.UtcNow;
        HasUnsavedChanges = true;
    }

    /// <summary>
    /// Updates an existing message in the conversation.
    /// </summary>
    /// <param name="messageId">The ID of the message to update.</param>
    /// <param name="updateAction">Action to apply to the message.</param>
    /// <remarks>
    /// Used for streaming content updates during generation.
    /// If the message is not found, no action is taken.
    /// </remarks>
    public void UpdateMessage(Guid messageId, Action<ChatMessage> updateAction)
    {
        var message = _messages.FirstOrDefault(m => m.Id == messageId);

        if (message is not null)
        {
            updateAction(message);

            // Mark conversation as modified.
            UpdatedAt = DateTime.UtcNow;
            HasUnsavedChanges = true;
        }
    }

    /// <summary>
    /// Removes a message from the conversation.
    /// </summary>
    /// <param name="messageId">The ID of the message to remove.</param>
    /// <remarks>
    /// After removal, remaining messages are re-sequenced to maintain
    /// contiguous SequenceNumber values.
    /// </remarks>
    public void RemoveMessage(Guid messageId)
    {
        var message = _messages.FirstOrDefault(m => m.Id == messageId);

        if (message is not null)
        {
            _messages.Remove(message);

            // Re-sequence remaining messages to maintain contiguous ordering.
            // This ensures SequenceNumber always matches position in list.
            for (var i = 0; i < _messages.Count; i++)
            {
                _messages[i].SequenceNumber = i + 1;
            }

            UpdatedAt = DateTime.UtcNow;
            HasUnsavedChanges = true;
        }
    }

    /// <summary>
    /// Clears all messages from the conversation.
    /// </summary>
    public void ClearMessages()
    {
        _messages.Clear();
        UpdatedAt = DateTime.UtcNow;
        HasUnsavedChanges = true;
    }

    #endregion

    #region Internal Methods (for service use)

    /// <summary>
    /// Loads messages from database (internal use only).
    /// </summary>
    /// <param name="messages">Messages to load, ordered by SequenceNumber.</param>
    /// <remarks>
    /// This method bypasses normal mutation tracking and should only
    /// be called by the DatabaseConversationService when loading.
    /// </remarks>
    internal void LoadMessages(IEnumerable<ChatMessage> messages)
    {
        _messages.Clear();
        _messages.AddRange(messages.OrderBy(m => m.SequenceNumber));
    }

    /// <summary>
    /// Marks the conversation as saved (internal use only).
    /// </summary>
    /// <remarks>
    /// Called by the DatabaseConversationService after a successful save.
    /// Clears the HasUnsavedChanges flag and sets IsPersisted to true.
    /// </remarks>
    internal void MarkAsSaved()
    {
        HasUnsavedChanges = false;
        IsPersisted = true;
    }

    #endregion
}
