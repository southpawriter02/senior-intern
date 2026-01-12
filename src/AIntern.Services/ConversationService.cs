using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

/// <summary>
/// Manages the current conversation state including message history.
/// Provides methods to add, update, and clear messages within a conversation.
/// </summary>
/// <remarks>
/// This service maintains a single active conversation in memory.
/// Future versions may support multiple conversations and persistence.
/// </remarks>
public sealed class ConversationService : IConversationService
{
    // The current active conversation (in-memory, not persisted)
    private Conversation _currentConversation = new();

    /// <inheritdoc />
    public Conversation CurrentConversation => _currentConversation;

    /// <inheritdoc />
    public event EventHandler? ConversationChanged;

    /// <inheritdoc />
    public void AddMessage(ChatMessage message)
    {
        // Append the message to the conversation history
        _currentConversation.Messages.Add(message);
        
        // Update the modification timestamp
        _currentConversation.UpdatedAt = DateTime.UtcNow;
        
        // Notify subscribers (e.g., UI) that the conversation changed
        ConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void UpdateMessage(Guid messageId, Action<ChatMessage> updateAction)
    {
        // Find the message by ID
        var message = _currentConversation.Messages.FirstOrDefault(m => m.Id == messageId);
        
        if (message is not null)
        {
            // Apply the update action (e.g., append streaming content)
            updateAction(message);
            
            // Update modification timestamp
            _currentConversation.UpdatedAt = DateTime.UtcNow;
            
            // Notify subscribers of the change
            ConversationChanged?.Invoke(this, EventArgs.Empty);
        }
        // If message not found, silently ignore (defensive programming)
    }

    /// <inheritdoc />
    public void ClearConversation()
    {
        // Replace with a fresh conversation (resets all state)
        _currentConversation = new Conversation();
        
        // Notify subscribers that conversation was cleared
        ConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public IEnumerable<ChatMessage> GetMessages()
    {
        // Return read-only view to prevent external modification
        return _currentConversation.Messages.AsReadOnly();
    }

    /// <inheritdoc />
    public Conversation CreateNewConversation()
    {
        // Create and activate a new conversation
        _currentConversation = new Conversation();
        
        // Notify subscribers of the new conversation
        ConversationChanged?.Invoke(this, EventArgs.Empty);
        
        return _currentConversation;
    }
}
