namespace AIntern.Desktop.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using AIntern.Desktop.Views.Dialogs;
using Microsoft.Extensions.Logging;

/// <summary>
/// Avalonia implementation of <see cref="IDialogService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses Avalonia's StorageProvider API for file/folder pickers and
/// custom dialog windows for go-to-line and message dialogs.
/// </para>
/// <para>Added in v0.3.3g.</para>
/// </remarks>
public sealed class DialogService : IDialogService
{
    private readonly ILogger<DialogService>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DialogService"/>.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public DialogService(ILogger<DialogService>? logger = null)
    {
        _logger = logger;
    }

    #region Private Helpers

    /// <summary>
    /// Gets the main window for dialog parenting.
    /// </summary>
    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        _logger?.LogWarning("[DIALOG] No main window available for dialog");
        return null;
    }

    #endregion

    #region Message Dialogs

    /// <inheritdoc />
    public async Task ShowErrorAsync(string title, string message)
    {
        _logger?.LogDebug("[DIALOG] ShowErrorAsync: {Title}", title);

        var window = GetMainWindow();
        if (window == null) return;

        var dialog = new MessageDialog
        {
            Title = title,
            Message = message,
            Icon = MessageDialogIcon.Error,
            Buttons = new[] { "OK" }
        };

        await dialog.ShowDialog(window);
    }

    /// <inheritdoc />
    public async Task ShowInfoAsync(string title, string message)
    {
        _logger?.LogDebug("[DIALOG] ShowInfoAsync: {Title}", title);

        var window = GetMainWindow();
        if (window == null) return;

        var dialog = new MessageDialog
        {
            Title = title,
            Message = message,
            Icon = MessageDialogIcon.Information,
            Buttons = new[] { "OK" }
        };

        await dialog.ShowDialog(window);
    }

    /// <inheritdoc />
    public async Task<string?> ShowConfirmDialogAsync(
        string title,
        string message,
        IEnumerable<string> options)
    {
        _logger?.LogDebug("[DIALOG] ShowConfirmDialogAsync: {Title}", title);

        var window = GetMainWindow();
        if (window == null) return null;

        var dialog = new MessageDialog
        {
            Title = title,
            Message = message,
            Icon = MessageDialogIcon.Question,
            Buttons = options.ToArray()
        };

        var result = await dialog.ShowDialog<string?>(window);
        _logger?.LogDebug("[DIALOG] Confirm result: {Result}", result ?? "(cancelled)");
        return result;
    }

    #endregion

    #region File Dialogs

    /// <inheritdoc />
    public async Task<string?> ShowSaveDialogAsync(
        string title,
        string defaultFileName,
        IReadOnlyList<(string Name, string[] Extensions)> filters)
    {
        _logger?.LogDebug("[DIALOG] ShowSaveDialogAsync: {Title}, DefaultName: {FileName}",
            title, defaultFileName);

        var window = GetMainWindow();
        if (window == null) return null;

        var storageProvider = window.StorageProvider;
        var fileTypes = filters.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Extensions.Select(e => $"*.{e}").ToArray()
        }).ToList();

        try
        {
            var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = defaultFileName,
                FileTypeChoices = fileTypes
            });

            var path = result?.Path.LocalPath;
            _logger?.LogDebug("[DIALOG] Save result: {Path}", path ?? "(cancelled)");
            return path;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[DIALOG] Error in save dialog");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> ShowOpenFileDialogAsync(
        string title,
        IReadOnlyList<(string Name, string[] Extensions)> filters,
        bool allowMultiple = false)
    {
        _logger?.LogDebug("[DIALOG] ShowOpenFileDialogAsync: {Title}", title);

        var window = GetMainWindow();
        if (window == null) return null;

        var storageProvider = window.StorageProvider;
        var fileTypes = filters.Select(f => new FilePickerFileType(f.Name)
        {
            Patterns = f.Extensions.Select(e => $"*.{e}").ToArray()
        }).ToList();

        try
        {
            var results = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = allowMultiple,
                FileTypeFilter = fileTypes
            });

            var path = results.FirstOrDefault()?.Path.LocalPath;
            _logger?.LogDebug("[DIALOG] Open result: {Path}", path ?? "(cancelled)");
            return path;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[DIALOG] Error in open dialog");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> ShowFolderPickerAsync(string title)
    {
        _logger?.LogDebug("[DIALOG] ShowFolderPickerAsync: {Title}", title);

        var window = GetMainWindow();
        if (window == null) return null;

        var storageProvider = window.StorageProvider;

        try
        {
            var results = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });

            var path = results.FirstOrDefault()?.Path.LocalPath;
            _logger?.LogDebug("[DIALOG] Folder result: {Path}", path ?? "(cancelled)");
            return path;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[DIALOG] Error in folder picker");
            return null;
        }
    }

    #endregion

    #region Custom Dialogs

    /// <inheritdoc />
    public async Task<int?> ShowGoToLineDialogAsync(int maxLine, int currentLine)
    {
        _logger?.LogDebug("[DIALOG] ShowGoToLineDialogAsync: MaxLine={Max}, CurrentLine={Current}",
            maxLine, currentLine);

        var window = GetMainWindow();
        if (window == null) return null;

        var dialog = new GoToLineDialog
        {
            MaxLine = maxLine,
            CurrentLine = currentLine
        };

        var result = await dialog.ShowDialog<int?>(window);
        _logger?.LogDebug("[DIALOG] GoToLine result: {Result}", result?.ToString() ?? "(cancelled)");
        return result;
    }

    #endregion
}
