using Avalonia.Controls;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ APPLY CHANGES DIALOG (v0.4.3f)                                           │
// │ Confirmation dialog with diff preview for applying code changes.        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Apply Changes confirmation dialog window.
/// Shows diff preview and allows user to confirm or cancel the apply operation.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3f.</para>
/// </remarks>
public partial class ApplyChangesDialog : Window
{
    /// <summary>
    /// Initializes a new instance of the ApplyChangesDialog.
    /// </summary>
    public ApplyChangesDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the Apply Changes dialog and returns the result.
    /// </summary>
    /// <param name="parent">The parent window.</param>
    /// <param name="codeBlock">The code block to apply.</param>
    /// <param name="originalContent">The original file content.</param>
    /// <param name="proposedContent">The proposed new content.</param>
    /// <param name="isNewFile">Whether this creates a new file.</param>
    /// <param name="workspacePath">The workspace path.</param>
    /// <param name="changeService">Service for applying changes.</param>
    /// <returns>The ApplyResult if applied, or null if cancelled.</returns>
    public static async Task<ApplyResult?> ShowAsync(
        Window parent,
        CodeBlock codeBlock,
        string originalContent,
        string proposedContent,
        bool isNewFile,
        string workspacePath,
        IFileChangeService changeService)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(codeBlock);
        ArgumentNullException.ThrowIfNull(changeService);

        var tcs = new TaskCompletionSource<ApplyResult?>();
        
        var dialog = new ApplyChangesDialog();
        
        var viewModel = new ApplyChangesDialogViewModel(
            changeService,
            codeBlock,
            workspacePath,
            originalContent ?? string.Empty,
            proposedContent,
            isNewFile,
            closeAction: result =>
            {
                dialog.Close(result);
            });

        dialog.DataContext = viewModel;

        // Handle dialog closed without result
        dialog.Closed += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetResult(null);
            }
        };

        var result = await dialog.ShowDialog<ApplyResult?>(parent);
        return result;
    }
}
