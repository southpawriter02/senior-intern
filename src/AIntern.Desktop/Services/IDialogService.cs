namespace AIntern.Desktop.Services;

using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Service interface for displaying platform dialogs.
/// </summary>
/// <remarks>
/// <para>
/// Provides an abstraction for showing various types of dialogs, enabling
/// platform-independent dialog operations and testability via mocking.
/// </para>
/// <para>Added in v0.3.3g.</para>
/// </remarks>
public interface IDialogService
{
    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Error message to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Information message to display.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog with multiple options.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <param name="options">Button options (e.g., "Save", "Don't Save", "Cancel").</param>
    /// <returns>The selected option text, or null if cancelled/closed.</returns>
    Task<string?> ShowConfirmDialogAsync(string title, string message, IEnumerable<string> options);

    /// <summary>
    /// Shows a save file dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="defaultFileName">Suggested file name.</param>
    /// <param name="filters">File type filters as (Name, Extensions[]) tuples.</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowSaveDialogAsync(
        string title,
        string defaultFileName,
        IReadOnlyList<(string Name, string[] Extensions)> filters);

    /// <summary>
    /// Shows an open file dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="filters">File type filters as (Name, Extensions[]) tuples.</param>
    /// <param name="allowMultiple">Whether to allow selecting multiple files.</param>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowOpenFileDialogAsync(
        string title,
        IReadOnlyList<(string Name, string[] Extensions)> filters,
        bool allowMultiple = false);

    /// <summary>
    /// Shows a folder picker dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <returns>The selected folder path, or null if cancelled.</returns>
    Task<string?> ShowFolderPickerAsync(string title);

    /// <summary>
    /// Shows the go-to-line dialog.
    /// </summary>
    /// <param name="maxLine">Maximum line number allowed.</param>
    /// <param name="currentLine">Current line number (pre-filled in dialog).</param>
    /// <returns>The line number to navigate to, or null if cancelled.</returns>
    Task<int?> ShowGoToLineDialogAsync(int maxLine, int currentLine);
}
