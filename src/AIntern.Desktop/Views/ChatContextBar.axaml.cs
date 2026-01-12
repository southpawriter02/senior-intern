using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

/// <summary>
/// Code-behind for ChatContextBar - displays attached file contexts as interactive pills.
/// </summary>
public partial class ChatContextBar : UserControl
{
    public ChatContextBar()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles pointer press on context pill.
    /// Left click → ShowPreview, Middle click → Remove.
    /// </summary>
    private void OnContextPillPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border { DataContext: FileContextViewModel context } border)
            return;

        var point = e.GetCurrentPoint(border);
        var properties = point.Properties;

        if (properties.IsLeftButtonPressed)
        {
            // Left click - show preview
            if (DataContext is ChatViewModel vm)
            {
                vm.ShowPreviewCommand.Execute(context);
            }
        }
        else if (properties.IsMiddleButtonPressed)
        {
            // Middle click - remove context
            if (DataContext is ChatViewModel vm)
            {
                vm.RemoveContextCommand.Execute(context);
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer entering a context pill - sets hover state.
    /// </summary>
    private void OnContextPillEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Border { DataContext: FileContextViewModel context })
        {
            context.IsHovered = true;
        }
    }

    /// <summary>
    /// Handles pointer exiting a context pill - clears hover state.
    /// </summary>
    private void OnContextPillExited(object? sender, PointerEventArgs e)
    {
        if (sender is Border { DataContext: FileContextViewModel context })
        {
            context.IsHovered = false;
        }
    }
}
