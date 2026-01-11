using Avalonia.Controls;
using Avalonia.Interactivity;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

public partial class SystemPromptSelector : UserControl
{
    public SystemPromptSelector()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SystemPromptSelectorViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
