using Avalonia.Controls;
using Avalonia.Input;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

/// <summary>
/// The chat interface view displaying messages and input controls.
/// Handles keyboard shortcuts for message submission.
/// </summary>
public partial class ChatView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatView"/> class.
    /// </summary>
    public ChatView()
    {
        // Load the XAML-defined UI components
        InitializeComponent();
    }

    /// <summary>
    /// Handles the KeyDown event on the input TextBox.
    /// Submits the message when Enter is pressed (without Shift modifier).
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The key event arguments.</param>
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        // Check for Enter key without Shift modifier
        // Shift+Enter allows for multi-line input
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            // Access the ViewModel through DataContext binding
            if (DataContext is ChatViewModel viewModel)
            {
                // Delegate to ViewModel which checks CanSend and executes command
                viewModel.HandleEnterKey();
                
                // Mark event as handled to prevent default Enter behavior
                e.Handled = true;
            }
        }
    }
}
