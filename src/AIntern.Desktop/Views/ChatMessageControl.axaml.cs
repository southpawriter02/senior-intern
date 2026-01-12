using Avalonia.Controls;

namespace AIntern.Desktop.Views;

/// <summary>
/// A reusable control for displaying a single chat message.
/// Renders differently based on message role (User vs Assistant).
/// </summary>
public partial class ChatMessageControl : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageControl"/> class.
    /// </summary>
    public ChatMessageControl()
    {
        InitializeComponent();
    }
}
