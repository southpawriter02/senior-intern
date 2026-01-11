using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

public partial class SystemPromptEditorWindow : Window
{
    public SystemPromptEditorWindow()
    {
        InitializeComponent();
        Loaded += OnWindowLoaded;
        KeyDown += OnWindowKeyDown;
        Closing += OnWindowClosing;
    }

    private SystemPromptEditorViewModel? ViewModel => DataContext as SystemPromptEditorViewModel;

    private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.LoadPromptsCommand.ExecuteAsync(null);
        }
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel == null)
            return;

        // Ctrl+S: Save
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (ViewModel.SavePromptCommand.CanExecute(null))
            {
                ViewModel.SavePromptCommand.Execute(null);
            }
            e.Handled = true;
        }
        // Ctrl+N: New Prompt
        else if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            ViewModel.CreateNewPromptCommand.Execute(null);
            e.Handled = true;
        }
        // Escape: Discard or Close
        else if (e.Key == Key.Escape)
        {
            if (ViewModel.IsDirty)
            {
                ViewModel.DiscardChangesCommand.Execute(null);
            }
            else
            {
                Close();
            }
            e.Handled = true;
        }
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (ViewModel?.IsDirty == true)
        {
            // Cancel the close temporarily
            e.Cancel = true;

            // Show confirmation dialog
            var result = await ShowUnsavedChangesDialogAsync();

            if (result == UnsavedChangesResult.Save)
            {
                if (ViewModel.SavePromptCommand.CanExecute(null))
                {
                    await ViewModel.SavePromptCommand.ExecuteAsync(null);
                }
                Close();
            }
            else if (result == UnsavedChangesResult.Discard)
            {
                ViewModel.DiscardChangesCommand.Execute(null);
                Close();
            }
            // Cancel: do nothing, window stays open
        }
    }

    private async Task<UnsavedChangesResult> ShowUnsavedChangesDialogAsync()
    {
        var result = UnsavedChangesResult.Cancel;

        var dialog = new Window
        {
            Title = "Unsaved Changes",
            Width = 400,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = new SolidColorBrush(Color.Parse("#1E1E1E"))
        };

        var panel = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 20
        };

        panel.Children.Add(new TextBlock
        {
            Text = "You have unsaved changes. What would you like to do?",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#CCCCCC"))
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(16, 8)
        };
        var discardButton = new Button
        {
            Content = "Discard",
            Padding = new Thickness(16, 8)
        };
        var saveButton = new Button
        {
            Content = "Save",
            Padding = new Thickness(16, 8),
            Background = new SolidColorBrush(Color.Parse("#007ACC")),
            Foreground = Brushes.White
        };

        cancelButton.Click += (s, e) =>
        {
            result = UnsavedChangesResult.Cancel;
            dialog.Close();
        };

        discardButton.Click += (s, e) =>
        {
            result = UnsavedChangesResult.Discard;
            dialog.Close();
        };

        saveButton.Click += (s, e) =>
        {
            result = UnsavedChangesResult.Save;
            dialog.Close();
        };

        buttonPanel.Children.Add(cancelButton);
        buttonPanel.Children.Add(discardButton);
        buttonPanel.Children.Add(saveButton);

        panel.Children.Add(buttonPanel);
        dialog.Content = panel;

        await dialog.ShowDialog(this);

        return result;
    }

    private enum UnsavedChangesResult
    {
        Save,
        Discard,
        Cancel
    }
}
