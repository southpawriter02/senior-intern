namespace AIntern.Desktop.Views;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

/// <summary>
/// File explorer view with TreeView for workspace navigation.
/// </summary>
/// <remarks>Added in v0.3.2d.</remarks>
public partial class FileExplorerView : UserControl
{
    private ILogger? _logger;

    public FileExplorerView()
    {
        InitializeComponent();
    }

    /// <summary>Gets the ViewModel from DataContext.</summary>
    private FileExplorerViewModel? ViewModel => DataContext as FileExplorerViewModel;

    /// <summary>
    /// Handles double-tap on TreeView items.
    /// Files are opened, directories toggle expansion.
    /// </summary>
    private void OnTreeViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel?.SelectedItem is not { } item) return;

        if (item.IsFile)
        {
            _logger?.LogDebug("Double-tapped file: {Path}", item.Path);
            ViewModel.OpenFileCommand.Execute(item);
        }
        else if (item.IsDirectory)
        {
            _logger?.LogDebug("Double-tapped directory: {Path}, toggling expansion", item.Path);
            item.IsExpanded = !item.IsExpanded;
        }
    }

    /// <summary>
    /// Handles keyboard navigation in the TreeView.
    /// </summary>
    private void OnTreeViewKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel?.SelectedItem is not { } item) return;

        switch (e.Key)
        {
            case Key.Enter:
                if (item.IsFile) ViewModel.OpenFileCommand.Execute(item);
                else item.IsExpanded = !item.IsExpanded;
                e.Handled = true;
                break;

            case Key.F2:
                item.BeginRename();
                e.Handled = true;
                break;

            case Key.Delete:
                // Delete uses the ViewModel command which handles confirmation via event
                _logger?.LogDebug("Delete key pressed for: {Path}", item.Path);
                ViewModel.DeleteCommand.Execute(item);
                e.Handled = true;
                break;

            case Key.Right when item.IsDirectory && !item.IsExpanded:
                item.IsExpanded = true;
                e.Handled = true;
                break;

            case Key.Left when item.IsDirectory && item.IsExpanded:
                item.IsExpanded = false;
                e.Handled = true;
                break;

            case Key.C when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    ViewModel.CopyRelativePathCommand.Execute(item);
                else
                    ViewModel.CopyPathCommand.Execute(item);
                e.Handled = true;
                break;

            case Key.N when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    ViewModel.NewFolderCommand.Execute(item);
                else
                    ViewModel.NewFileCommand.Execute(item);
                e.Handled = true;
                break;

            case Key.R when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                ViewModel.RefreshCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Handles keyboard events in the rename TextBox.
    /// Enter commits, Escape cancels.
    /// </summary>
    private void OnRenameKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: FileTreeItemViewModel item }) return;

        if (e.Key == Key.Enter)
        {
            item.CommitRenameCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            item.CancelRenameCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles lost focus on rename TextBox - auto-commits the rename.
    /// </summary>
    private void OnRenameLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: FileTreeItemViewModel { IsRenaming: true } item })
            item.CommitRenameCommand.Execute(null);
    }

    /// <summary>
    /// Sets the logger for debugging (optional).
    /// </summary>
    public void SetLogger(ILogger logger) => _logger = logger;
}


