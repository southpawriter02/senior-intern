using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

public partial class FileExplorerView : UserControl
{
    public FileExplorerView() => InitializeComponent();

    private FileExplorerViewModel? ViewModel => DataContext as FileExplorerViewModel;

    private void OnTreeViewDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel?.SelectedItem is not { } item) return;

        if (item.IsFile)
            ViewModel.OpenFileCommand.Execute(item);
        else if (item.IsDirectory)
            item.IsExpanded = !item.IsExpanded;
    }

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

    private void OnRenameLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: FileTreeItemViewModel { IsRenaming: true } item })
            item.CommitRenameCommand.Execute(null);
    }

    private void OnDeleteMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedItem is { } item)
            ViewModel.DeleteCommand.Execute(item);
    }
}
