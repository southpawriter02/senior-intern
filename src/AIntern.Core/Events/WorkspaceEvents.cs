using AIntern.Core.Models;

namespace AIntern.Core.Events;

/// <summary>Event args for workspace open/close/refresh events.</summary>
public sealed class WorkspaceChangedEventArgs : EventArgs
{
    /// <summary>Workspace before the change (null if opening first).</summary>
    public Workspace? PreviousWorkspace { get; init; }

    /// <summary>Workspace after the change (null if closing).</summary>
    public Workspace? CurrentWorkspace { get; init; }

    /// <summary>The type of change that occurred.</summary>
    public required WorkspaceChangeType ChangeType { get; init; }
}

/// <summary>Type of workspace change.</summary>
public enum WorkspaceChangeType
{
    /// <summary>A workspace was opened.</summary>
    Opened,

    /// <summary>The workspace was closed.</summary>
    Closed,

    /// <summary>The workspace was refreshed (files reloaded).</summary>
    Refreshed
}

/// <summary>Event args for workspace state changes.</summary>
public sealed class WorkspaceStateChangedEventArgs : EventArgs
{
    /// <summary>The workspace whose state changed.</summary>
    public required Workspace Workspace { get; init; }

    /// <summary>The type of state change.</summary>
    public required WorkspaceStateChangeType ChangeType { get; init; }
}

/// <summary>Type of workspace state change.</summary>
public enum WorkspaceStateChangeType
{
    /// <summary>Open files list changed.</summary>
    OpenFilesChanged,

    /// <summary>Active/focused file changed.</summary>
    ActiveFileChanged,

    /// <summary>Expanded folders in tree changed.</summary>
    ExpandedFoldersChanged
}
