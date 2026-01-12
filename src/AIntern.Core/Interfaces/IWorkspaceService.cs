using AIntern.Core.Events;
using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Manages workspace lifecycle and state.
/// </summary>
public interface IWorkspaceService
{
    #region Properties

    /// <summary>The currently open workspace, or null if none.</summary>
    Workspace? CurrentWorkspace { get; }

    /// <summary>Whether a workspace is currently open.</summary>
    bool HasOpenWorkspace { get; }

    #endregion

    #region Workspace Operations

    /// <summary>Opens a workspace from a folder path.</summary>
    Task<Workspace> OpenWorkspaceAsync(
        string folderPath,
        CancellationToken cancellationToken = default);

    /// <summary>Closes the current workspace.</summary>
    Task CloseWorkspaceAsync();

    /// <summary>Gets the list of recently opened workspaces.</summary>
    Task<IReadOnlyList<Workspace>> GetRecentWorkspacesAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a workspace from the recent list.</summary>
    Task RemoveFromRecentAsync(Guid workspaceId);

    /// <summary>Clears all recent workspaces.</summary>
    Task ClearRecentWorkspacesAsync();

    /// <summary>Pins or unpins a workspace in the recent list.</summary>
    Task SetPinnedAsync(Guid workspaceId, bool isPinned);

    /// <summary>Updates the workspace custom name.</summary>
    Task RenameWorkspaceAsync(Guid workspaceId, string newName);

    #endregion

    #region State Management

    /// <summary>Saves the current workspace state to database.</summary>
    Task SaveWorkspaceStateAsync();

    /// <summary>Restores the last opened workspace on startup.</summary>
    Task<Workspace?> RestoreLastWorkspaceAsync();

    /// <summary>Updates the list of open files in the current workspace.</summary>
    void UpdateOpenFiles(IReadOnlyList<string> openFiles, string? activeFile);

    /// <summary>Updates the list of expanded folders in the file tree.</summary>
    void UpdateExpandedFolders(IReadOnlyList<string> expandedFolders);

    #endregion

    #region Events

    /// <summary>Raised when workspace changes (opened, closed, refreshed).</summary>
    event EventHandler<WorkspaceChangedEventArgs>? WorkspaceChanged;

    /// <summary>Raised when workspace state changes (files, folders).</summary>
    event EventHandler<WorkspaceStateChangedEventArgs>? StateChanged;

    #endregion
}
