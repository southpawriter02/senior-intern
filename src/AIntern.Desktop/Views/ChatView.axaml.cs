using Avalonia.Controls;
using Avalonia.Input;
using SeniorIntern.Desktop.ViewModels;

namespace SeniorIntern.Desktop.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (DataContext is ChatViewModel viewModel)
            {
                viewModel.HandleEnterKey();
                e.Handled = true;
            }
        }
    }
}
