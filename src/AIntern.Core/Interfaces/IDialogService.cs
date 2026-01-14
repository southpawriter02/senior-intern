namespace AIntern.Core.Interfaces;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service for displaying dialogs to the user.
/// </summary>
/// <remarks>
/// <para>
/// Implemented by platform-specific dialog services (Avalonia, WPF, etc.).
/// Provides:
/// </para>
/// <list type="bullet">
///   <item><description>File open/save dialogs</description></item>
///   <item><description>Confirmation dialogs with multiple buttons</description></item>
///   <item><description>Error/message dialogs</description></item>
///   <item><description>Input dialogs (e.g., go-to-line)</description></item>
/// </list>
/// <para>Added in v0.3.3b.</para>
/// </remarks>
public interface IDialogService
{
    /// <summary>
    /// Shows an error dialog with a message.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Error message to display.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ShowErrorAsync(string title, string message, CancellationToken ct = default);

    /// <summary>
    /// Shows a confirmation dialog with multiple buttons.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Message to display.</param>
    /// <param name="buttons">Button labels (e.g., "Save", "Don't Save", "Cancel").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The text of the clicked button, or null if cancelled.</returns>
    Task<string?> ShowConfirmDialogAsync(
        string title,
        string message,
        string[] buttons,
        CancellationToken ct = default);

    /// <summary>
    /// Shows a save file dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="suggestedFileName">Default file name.</param>
    /// <param name="filters">File type filters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Selected file path, or null if cancelled.</returns>
    Task<string?> ShowSaveDialogAsync(
        string title,
        string suggestedFileName,
        IReadOnlyList<(string Name, string[] Extensions)>? filters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Shows an open file dialog.
    /// </summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="filters">File type filters.</param>
    /// <param name="allowMultiple">Whether to allow selecting multiple files.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Selected file path(s), or empty if cancelled.</returns>
    Task<IReadOnlyList<string>> ShowOpenDialogAsync(
        string title,
        IReadOnlyList<(string Name, string[] Extensions)>? filters = null,
        bool allowMultiple = false,
        CancellationToken ct = default);

    /// <summary>
    /// Shows a go-to-line dialog.
    /// </summary>
    /// <param name="maxLineNumber">Maximum valid line number.</param>
    /// <param name="currentLine">Current line number (default selection).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Selected line number, or null if cancelled.</returns>
    Task<int?> ShowGoToLineDialogAsync(
        int maxLineNumber,
        int currentLine = 1,
        CancellationToken ct = default);
}
