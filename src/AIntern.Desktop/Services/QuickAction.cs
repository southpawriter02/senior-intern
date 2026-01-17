using AIntern.Core.Models;
using AIntern.Desktop.Models;

namespace AIntern.Desktop.Services;

/// <summary>
/// Defines a quick action that can be performed on a code block (v0.4.5g).
/// </summary>
/// <param name="Id">Unique identifier for the action</param>
/// <param name="Type">Category of the action</param>
/// <param name="Label">Short display text</param>
/// <param name="Icon">Icon resource key</param>
/// <param name="Tooltip">Full description for tooltip</param>
/// <param name="Shortcut">Optional keyboard shortcut</param>
/// <param name="IsEnabled">Predicate determining if action is available for a block</param>
/// <param name="Priority">Display order (lower values appear first)</param>
public sealed record QuickAction(
    string Id,
    QuickActionType Type,
    string Label,
    string Icon,
    string Tooltip,
    KeyboardShortcut? Shortcut,
    Func<CodeBlock, bool> IsEnabled,
    int Priority = 100)
{
    // ═══════════════════════════════════════════════════════════════
    // Factory Methods for Default Actions
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates the standard "Apply" action.
    /// </summary>
    public static QuickAction Apply() => new(
        Id: "apply",
        Type: QuickActionType.Apply,
        Label: "Apply",
        Icon: "CheckIcon",
        Tooltip: "Apply this code to the target file (Ctrl+Enter)",
        Shortcut: KeyboardShortcut.Ctrl(Avalonia.Input.Key.Return),
        IsEnabled: block => block.IsApplicable &&
                           block.Status == CodeBlockStatus.Pending &&
                           !string.IsNullOrEmpty(block.TargetFilePath),
        Priority: 10);

    /// <summary>
    /// Creates the standard "Copy" action.
    /// </summary>
    public static QuickAction Copy() => new(
        Id: "copy",
        Type: QuickActionType.Copy,
        Label: "Copy",
        Icon: "CopyIcon",
        Tooltip: "Copy code to clipboard (Ctrl+C)",
        Shortcut: KeyboardShortcut.Ctrl(Avalonia.Input.Key.C),
        IsEnabled: block => !string.IsNullOrEmpty(block.Content),
        Priority: 20);

    /// <summary>
    /// Creates the standard "Show Diff" action.
    /// </summary>
    public static QuickAction ShowDiff() => new(
        Id: "diff",
        Type: QuickActionType.ShowDiff,
        Label: "Diff",
        Icon: "DiffIcon",
        Tooltip: "Show diff preview (Ctrl+D)",
        Shortcut: KeyboardShortcut.Ctrl(Avalonia.Input.Key.D),
        IsEnabled: block => block.IsApplicable &&
                           !string.IsNullOrEmpty(block.TargetFilePath),
        Priority: 30);

    /// <summary>
    /// Creates the standard "Open in Editor" action.
    /// </summary>
    public static QuickAction OpenInEditor() => new(
        Id: "open",
        Type: QuickActionType.OpenFile,
        Label: "Open",
        Icon: "ExternalLinkIcon",
        Tooltip: "Open target file in editor",
        Shortcut: null,
        IsEnabled: block => !string.IsNullOrEmpty(block.TargetFilePath),
        Priority: 40);

    /// <summary>
    /// Creates the standard "Apply with Options" action.
    /// </summary>
    public static QuickAction ApplyWithOptions() => new(
        Id: "options",
        Type: QuickActionType.ApplyWithOptions,
        Label: "Options...",
        Icon: "SettingsIcon",
        Tooltip: "Apply with custom options (Ctrl+Shift+Enter)",
        Shortcut: KeyboardShortcut.CtrlShift(Avalonia.Input.Key.Return),
        IsEnabled: block => block.IsApplicable &&
                           block.Status == CodeBlockStatus.Pending,
        Priority: 50);

    /// <summary>
    /// Creates the standard "Reject" action.
    /// </summary>
    public static QuickAction Reject() => new(
        Id: "reject",
        Type: QuickActionType.Reject,
        Label: "Reject",
        Icon: "CrossIcon",
        Tooltip: "Mark this code block as rejected",
        Shortcut: null,
        IsEnabled: block => block.Status == CodeBlockStatus.Pending,
        Priority: 60);

    /// <summary>
    /// Creates the "Run Command" action for command blocks.
    /// </summary>
    public static QuickAction RunCommand() => new(
        Id: "run",
        Type: QuickActionType.RunCommand,
        Label: "Run",
        Icon: "PlayIcon",
        Tooltip: "Execute this command in terminal",
        Shortcut: KeyboardShortcut.Ctrl(Avalonia.Input.Key.R),
        IsEnabled: block => block.BlockType == CodeBlockType.Command,
        Priority: 15);

    /// <summary>
    /// Creates the "Insert at Cursor" action.
    /// </summary>
    public static QuickAction InsertAtCursor() => new(
        Id: "insert",
        Type: QuickActionType.InsertAtCursor,
        Label: "Insert",
        Icon: "InsertIcon",
        Tooltip: "Insert code at editor cursor position",
        Shortcut: KeyboardShortcut.CtrlShift(Avalonia.Input.Key.I),
        IsEnabled: block => !string.IsNullOrEmpty(block.Content),
        Priority: 45);
}
