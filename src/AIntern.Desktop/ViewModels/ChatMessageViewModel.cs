using CommunityToolkit.Mvvm.ComponentModel;
using SeniorIntern.Core.Models;

namespace SeniorIntern.Desktop.ViewModels;

public partial class ChatMessageViewModel : ViewModelBase
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private MessageRole _role;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private DateTime _timestamp;

    public bool IsUser => Role == MessageRole.User;
    public bool IsAssistant => Role == MessageRole.Assistant;
    public string RoleLabel => Role switch
    {
        MessageRole.User => "You",
        MessageRole.Assistant => "Senior Intern",
        MessageRole.System => "System",
        _ => "Unknown"
    };

    public ChatMessageViewModel()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    public ChatMessageViewModel(ChatMessage message)
    {
        Id = message.Id;
        Content = message.Content;
        Role = message.Role;
        Timestamp = message.Timestamp;
        IsStreaming = !message.IsComplete;
    }

    /// <summary>
    /// Appends streamed content (for assistant messages).
    /// </summary>
    public void AppendContent(string token)
    {
        Content += token;
    }

    /// <summary>
    /// Called when streaming is complete.
    /// </summary>
    public void CompleteStreaming()
    {
        IsStreaming = false;
    }

    /// <summary>
    /// Marks the message as cancelled.
    /// </summary>
    public void MarkAsCancelled()
    {
        IsStreaming = false;
        if (!string.IsNullOrEmpty(Content) && !Content.EndsWith("..."))
        {
            Content += " [Cancelled]";
        }
    }

    public ChatMessage ToChatMessage() => new()
    {
        Id = Id,
        Content = Content,
        Role = Role,
        Timestamp = Timestamp,
        IsComplete = !IsStreaming
    };
}
