using SeniorIntern.Core.Models;

namespace SeniorIntern.Core.Interfaces;

public interface IConversationService
{
    /// <summary>
    /// Gets the current active conversation.
    /// </summary>
    Conversation CurrentConversation { get; }

    /// <summary>
    /// Adds a message to the current conversation.
    /// </summary>
    void AddMessage(ChatMessage message);

    /// <summary>
    /// Updates an existing message in the current conversation.
    /// </summary>
    void UpdateMessage(Guid messageId, Action<ChatMessage> updateAction);

    /// <summary>
    /// Clears all messages from the current conversation.
    /// </summary>
    void ClearConversation();

    /// <summary>
    /// Gets all messages in the current conversation.
    /// </summary>
    IEnumerable<ChatMessage> GetMessages();

    /// <summary>
    /// Creates a new conversation and sets it as current.
    /// </summary>
    Conversation CreateNewConversation();

    /// <summary>
    /// Raised when the conversation changes.
    /// </summary>
    event EventHandler? ConversationChanged;
}
