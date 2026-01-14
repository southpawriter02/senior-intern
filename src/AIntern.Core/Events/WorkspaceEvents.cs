namespace AIntern.Core.Events;

using AIntern.Core.Models;

/// <summary>
/// Event args for workspace open/close/refresh events.
/// </summary>
/// <remarks>Added in v0.3.1e.</remarks>
public sealed class WorkspaceChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the workspace before the change (null if opening first workspace).
    /// </summary>
    public Workspace? PreviousWorkspace { get; init; }

    /// <summary>
    /// Gets the workspace after the change (null if closing).
    /// </summary>
    public Workspace? CurrentWorkspace { get; init; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public required WorkspaceChangeType ChangeType { get; init; }
}

/// <summary>
/// Type of workspace change.
/// </summary>
/// <remarks>Added in v0.3.1e.</remarks>
public enum WorkspaceChangeType
{
    /// <summary>A workspace was opened.</summary>
    Opened,

    /// <summary>The workspace was closed.</summary>
    Closed,

    /// <summary>The workspace content was refreshed (files reloaded).</summary>
    Refreshed
}

/// <summary>
/// Event args for workspace state changes.
/// </summary>
/// <remarks>Added in v0.3.1e.</remarks>
public sealed class WorkspaceStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the workspace whose state changed.
    /// </summary>
    public required Workspace Workspace { get; init; }

    /// <summary>
    /// Gets the type of state change.
    /// </summary>
    public required WorkspaceStateChangeType ChangeType { get; init; }
}

/// <summary>
/// Type of workspace state change.
/// </summary>
/// <remarks>Added in v0.3.1e.</remarks>
public enum WorkspaceStateChangeType
{
    /// <summary>The open files list changed.</summary>
    OpenFilesChanged,

    /// <summary>The active/focused file changed.</summary>
    ActiveFileChanged,

    /// <summary>The expanded folders in tree view changed.</summary>
    ExpandedFoldersChanged
}
