namespace AIntern.Core.Models;

/// <summary>
/// Represents the saved state of a workspace session (v0.3.5h).
/// </summary>
public sealed class WorkspaceState
{
    /// <summary>
    /// The workspace this state belongs to.
    /// </summary>
    public Guid WorkspaceId { get; init; }

    /// <summary>
    /// List of open files with their states.
    /// </summary>
    public List<OpenFileState> OpenFiles { get; init; } = new();

    /// <summary>
    /// Index of the active (focused) file tab.
    /// </summary>
    public int ActiveFileIndex { get; init; }

    /// <summary>
    /// List of expanded folder paths in the file explorer.
    /// </summary>
    public List<string> ExpandedFolders { get; init; } = new();

    /// <summary>
    /// Timestamp when this state was saved.
    /// </summary>
    public DateTime SavedAt { get; init; } = DateTime.UtcNow;
}
