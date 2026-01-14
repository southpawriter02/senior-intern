namespace AIntern.Core.Interfaces;

using AIntern.Core.Events;
using AIntern.Core.Models;

/// <summary>
/// Manages workspace lifecycle and state.
/// </summary>
/// <remarks>
/// <para>
/// This service provides:
/// </para>
/// <list type="bullet">
///   <item><description>Workspace open/close operations with event notifications</description></item>
///   <item><description>Recent workspaces management (pin, rename, remove)</description></item>
///   <item><description>Workspace state persistence (open files, expanded folders)</description></item>
///   <item><description>Auto-save functionality (30-second interval)</description></item>
///   <item><description>Workspace restoration on startup</description></item>
/// </list>
/// <para>Added in v0.3.1e.</para>
/// </remarks>
public interface IWorkspaceService
{
    #region Properties

    /// <summary>
    /// Gets the currently open workspace, or null if none.
    /// </summary>
    Workspace? CurrentWorkspace { get; }

    /// <summary>
    /// Gets whether a workspace is currently open.
    /// </summary>
    bool HasOpenWorkspace { get; }

    #endregion

    #region Workspace Operations

    /// <summary>
    /// Opens a workspace from a folder path.
    /// </summary>
    /// <param name="folderPath">Absolute path to the folder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The opened workspace.</returns>
    /// <remarks>
    /// <para>If a workspace is already open, it will be closed first.</para>
    /// <para>Raises <see cref="WorkspaceChanged"/> event with <see cref="WorkspaceChangeType.Opened"/>.</para>
    /// </remarks>
    Task<Workspace> OpenWorkspaceAsync(
        string folderPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the current workspace.
    /// </summary>
    /// <remarks>
    /// <para>Saves workspace state before closing.</para>
    /// <para>Raises <see cref="WorkspaceChanged"/> event with <see cref="WorkspaceChangeType.Closed"/>.</para>
    /// </remarks>
    Task CloseWorkspaceAsync();

    /// <summary>
    /// Gets the list of recently opened workspaces.
    /// </summary>
    /// <param name="count">Maximum number to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent workspaces, filtered for existing paths.</returns>
    Task<IReadOnlyList<Workspace>> GetRecentWorkspacesAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a workspace from the recent list.
    /// </summary>
    /// <param name="workspaceId">ID of the workspace to remove.</param>
    Task RemoveFromRecentAsync(Guid workspaceId);

    /// <summary>
    /// Clears all recent workspaces.
    /// </summary>
    Task ClearRecentWorkspacesAsync();

    /// <summary>
    /// Pins or unpins a workspace in the recent list.
    /// </summary>
    /// <param name="workspaceId">ID of the workspace.</param>
    /// <param name="isPinned">Whether to pin the workspace.</param>
    Task SetPinnedAsync(Guid workspaceId, bool isPinned);

    /// <summary>
    /// Updates the workspace custom display name.
    /// </summary>
    /// <param name="workspaceId">ID of the workspace.</param>
    /// <param name="newName">New display name.</param>
    Task RenameWorkspaceAsync(Guid workspaceId, string newName);

    #endregion

    #region State Management

    /// <summary>
    /// Saves the current workspace state to database.
    /// </summary>
    Task SaveWorkspaceStateAsync();

    /// <summary>
    /// Restores the last opened workspace on startup.
    /// </summary>
    /// <returns>The restored workspace, or null if none or disabled.</returns>
    /// <remarks>
    /// <para>Respects the RestoreLastWorkspace setting.</para>
    /// </remarks>
    Task<Workspace?> RestoreLastWorkspaceAsync();

    /// <summary>
    /// Updates the list of open files in the current workspace.
    /// </summary>
    /// <param name="openFiles">List of relative file paths.</param>
    /// <param name="activeFile">Active file path, or null.</param>
    /// <remarks>
    /// <para>Raises <see cref="StateChanged"/> event.</para>
    /// </remarks>
    void UpdateOpenFiles(IReadOnlyList<string> openFiles, string? activeFile);

    /// <summary>
    /// Updates the list of expanded folders in the file tree.
    /// </summary>
    /// <param name="expandedFolders">List of relative folder paths.</param>
    /// <remarks>
    /// <para>Raises <see cref="StateChanged"/> event.</para>
    /// </remarks>
    void UpdateExpandedFolders(IReadOnlyList<string> expandedFolders);

    #endregion

    #region Events

    /// <summary>
    /// Raised when a workspace is opened, closed, or refreshed.
    /// </summary>
    event EventHandler<WorkspaceChangedEventArgs>? WorkspaceChanged;

    /// <summary>
    /// Raised when workspace state changes (open files, expanded folders).
    /// </summary>
    event EventHandler<WorkspaceStateChangedEventArgs>? StateChanged;

    #endregion
}
