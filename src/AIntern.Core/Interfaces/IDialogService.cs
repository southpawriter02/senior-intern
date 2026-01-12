namespace AIntern.Core.Interfaces;

/// <summary>
/// Service for displaying dialogs to the user.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows an error dialog to the user.
    /// </summary>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog with multiple options.
    /// </summary>
    /// <returns>The selected option text, or null if cancelled.</returns>
    Task<string?> ShowConfirmDialogAsync(string title, string message, IEnumerable<string> options);

    /// <summary>
    /// Shows a save file dialog.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowSaveDialogAsync(
        string title,
        string suggestedName,
        IReadOnlyList<(string Name, string[] Extensions)> filters);

    /// <summary>
    /// Shows a go-to-line dialog.
    /// </summary>
    /// <returns>The selected line number, or null if cancelled.</returns>
    Task<int?> ShowGoToLineDialogAsync(int maxLine, int currentLine);
}
