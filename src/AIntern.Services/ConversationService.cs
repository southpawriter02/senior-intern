using SeniorIntern.Core.Interfaces;
using SeniorIntern.Core.Models;

namespace SeniorIntern.Services;

public sealed class ConversationService : IConversationService
{
    private Conversation _currentConversation = new();

    public Conversation CurrentConversation => _currentConversation;

    public event EventHandler? ConversationChanged;

    public void AddMessage(ChatMessage message)
    {
        _currentConversation.Messages.Add(message);
        _currentConversation.UpdatedAt = DateTime.UtcNow;
        ConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateMessage(Guid messageId, Action<ChatMessage> updateAction)
    {
        var message = _currentConversation.Messages.FirstOrDefault(m => m.Id == messageId);
        if (message is not null)
        {
            updateAction(message);
            _currentConversation.UpdatedAt = DateTime.UtcNow;
            ConversationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ClearConversation()
    {
        _currentConversation = new Conversation();
        ConversationChanged?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<ChatMessage> GetMessages() => _currentConversation.Messages.AsReadOnly();

    public Conversation CreateNewConversation()
    {
        _currentConversation = new Conversation();
        ConversationChanged?.Invoke(this, EventArgs.Empty);
        return _currentConversation;
    }
}
