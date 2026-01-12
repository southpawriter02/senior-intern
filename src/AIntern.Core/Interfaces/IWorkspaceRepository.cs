using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Repository interface for workspace persistence operations.
/// </summary>
public interface IWorkspaceRepository
{
    /// <summary>Gets a workspace by its root path.</summary>
    Task<Workspace?> GetByPathAsync(string rootPath, CancellationToken ct = default);

    /// <summary>Gets the most recently accessed workspaces.</summary>
    Task<IReadOnlyList<Workspace>> GetRecentAsync(int count = 10, CancellationToken ct = default);

    /// <summary>Adds a new workspace or updates an existing one.</summary>
    Task AddOrUpdateAsync(Workspace workspace, CancellationToken ct = default);

    /// <summary>Removes a workspace from the recent list.</summary>
    Task RemoveAsync(Guid workspaceId, CancellationToken ct = default);

    /// <summary>Clears all recent workspaces.</summary>
    Task ClearAllAsync(CancellationToken ct = default);

    /// <summary>Sets the pinned status of a workspace.</summary>
    Task SetPinnedAsync(Guid workspaceId, bool isPinned, CancellationToken ct = default);

    /// <summary>Updates the custom name of a workspace.</summary>
    Task RenameAsync(Guid workspaceId, string newName, CancellationToken ct = default);
}
