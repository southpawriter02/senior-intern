namespace AIntern.Core.Models;

/// <summary>
/// Categories of quick actions available on code blocks (v0.4.5g).
/// </summary>
public enum QuickActionType
{
    /// <summary>
    /// Apply the code block to the target file using default options.
    /// </summary>
    Apply,

    /// <summary>
    /// Copy the code content to the system clipboard.
    /// </summary>
    Copy,

    /// <summary>
    /// Open the diff viewer showing changes.
    /// </summary>
    ShowDiff,

    /// <summary>
    /// Open the target file in the default editor.
    /// </summary>
    OpenFile,

    /// <summary>
    /// Open the apply options popup for custom configuration.
    /// </summary>
    ApplyWithOptions,

    /// <summary>
    /// Mark the code block as rejected by the user.
    /// </summary>
    Reject,

    /// <summary>
    /// Execute a command block in the terminal.
    /// </summary>
    RunCommand,

    /// <summary>
    /// Insert code at the current editor cursor position.
    /// </summary>
    InsertAtCursor
}
