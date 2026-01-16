namespace AIntern.Core.Models;

/// <summary>
/// Represents the saved state of an open file (v0.3.5h).
/// </summary>
public sealed class OpenFileState
{
    /// <summary>
    /// Full path to the file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Caret line position (1-indexed).
    /// </summary>
    public int CaretLine { get; init; } = 1;

    /// <summary>
    /// Caret column position (1-indexed).
    /// </summary>
    public int CaretColumn { get; init; } = 1;

    /// <summary>
    /// Vertical scroll offset.
    /// </summary>
    public double ScrollOffsetY { get; init; }

    /// <summary>
    /// Whether the file had unsaved changes when state was saved.
    /// </summary>
    public bool HasUnsavedChanges { get; init; }
}
