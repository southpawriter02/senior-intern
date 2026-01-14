namespace AIntern.Data.Repositories;

using AIntern.Core.Models;

/// <summary>
/// Repository for workspace persistence operations.
/// </summary>
/// <remarks>Added in v0.3.1e.</remarks>
public interface IWorkspaceRepository
{
    /// <summary>
    /// Gets a workspace by its ID.
    /// </summary>
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a workspace by its root path.
    /// </summary>
    Task<Workspace?> GetByPathAsync(string rootPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent workspaces ordered by last accessed (pinned first).
    /// </summary>
    Task<IReadOnlyList<Workspace>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a workspace.
    /// </summary>
    Task AddOrUpdateAsync(Workspace workspace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a workspace from the recent list.
    /// </summary>
    Task RemoveAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all recent workspaces.
    /// </summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the pinned status of a workspace.
    /// </summary>
    Task SetPinnedAsync(Guid workspaceId, bool isPinned, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renames a workspace (sets custom display name).
    /// </summary>
    Task RenameAsync(Guid workspaceId, string newName, CancellationToken cancellationToken = default);
}
