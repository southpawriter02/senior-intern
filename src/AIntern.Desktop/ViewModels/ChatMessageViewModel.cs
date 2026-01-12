using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel representing a single chat message in the UI.
/// Supports streaming content updates for assistant messages.
/// </summary>
/// <remarks>
/// This ViewModel wraps the <see cref="ChatMessage"/> domain model
/// and adds UI-specific properties like <see cref="IsStreaming"/>.
/// </remarks>
public partial class ChatMessageViewModel : ViewModelBase
{
    #region Observable Properties

    /// <summary>
    /// Gets or sets the unique identifier for this message.
    /// Matches the domain model ID for correlation.
    /// </summary>
    [ObservableProperty]
    private Guid _id;

    /// <summary>
    /// Gets or sets the text content of the message.
    /// Updated incrementally during streaming generation.
    /// </summary>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// Gets or sets the role of the message sender.
    /// Determines display styling (User vs Assistant).
    /// </summary>
    [ObservableProperty]
    private MessageRole _role;

    /// <summary>
    /// Gets or sets whether the message content is still being streamed.
    /// True while tokens are being generated, false when complete.
    /// </summary>
    [ObservableProperty]
    private bool _isStreaming;

    /// <summary>
    /// Gets or sets the UTC timestamp when this message was created.
    /// Used for display and sorting purposes.
    /// </summary>
    [ObservableProperty]
    private DateTime _timestamp;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether this message is from the user.
    /// Used for conditional styling in XAML.
    /// </summary>
    public bool IsUser => Role == MessageRole.User;

    /// <summary>
    /// Gets whether this message is from the assistant.
    /// Used for conditional styling in XAML.
    /// </summary>
    public bool IsAssistant => Role == MessageRole.Assistant;

    /// <summary>
    /// Gets the display label for the message sender role.
    /// Maps internal role enum to user-friendly display text.
    /// </summary>
    public string RoleLabel => Role switch
    {
        MessageRole.User => "You",           // Display name for user messages
        MessageRole.Assistant => "AIntern",  // App name for AI responses
        MessageRole.System => "System",      // System prompts (rarely shown)
        _ => "Unknown"                       // Fallback for safety
    };

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageViewModel"/> class
    /// with default values. Creates new ID and timestamp.
    /// </summary>
    public ChatMessageViewModel()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageViewModel"/> class
    /// from an existing <see cref="ChatMessage"/> domain model.
    /// </summary>
    /// <param name="message">The domain message to wrap.</param>
    public ChatMessageViewModel(ChatMessage message)
    {
        // Copy all properties from domain model
        Id = message.Id;
        Content = message.Content;
        Role = message.Role;
        Timestamp = message.Timestamp;
        
        // Invert IsComplete to get IsStreaming
        IsStreaming = !message.IsComplete;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Appends a token to the message content during streaming.
    /// Called for each token received from the LLM.
    /// </summary>
    /// <param name="token">The token text to append.</param>
    public void AppendContent(string token)
    {
        // Concatenate token to existing content
        Content += token;
    }

    /// <summary>
    /// Marks the message as complete (no longer streaming).
    /// Called when generation finishes successfully.
    /// </summary>
    public void CompleteStreaming()
    {
        // Remove streaming indicator
        IsStreaming = false;
    }

    /// <summary>
    /// Marks the message as cancelled by the user.
    /// Appends "[Cancelled]" to the content if not empty.
    /// </summary>
    public void MarkAsCancelled()
    {
        // Stop streaming indicator
        IsStreaming = false;
        
        // Add cancellation marker if there's content and it doesn't already trail off
        if (!string.IsNullOrEmpty(Content) && !Content.EndsWith("..."))
        {
            Content += " [Cancelled]";
        }
    }

    /// <summary>
    /// Converts this ViewModel back to a <see cref="ChatMessage"/> domain model.
    /// Used when saving to conversation history.
    /// </summary>
    /// <returns>A new <see cref="ChatMessage"/> instance with current values.</returns>
    public ChatMessage ToChatMessage() => new()
    {
        Id = Id,
        Content = Content,
        Role = Role,
        Timestamp = Timestamp,
        IsComplete = !IsStreaming  // Invert IsStreaming to get IsComplete
    };

    #endregion
}
