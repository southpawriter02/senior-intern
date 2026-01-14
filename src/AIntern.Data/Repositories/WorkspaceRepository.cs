namespace AIntern.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AIntern.Core.Entities;
using AIntern.Core.Models;
using System.Diagnostics;

/// <summary>
/// EF Core implementation of workspace repository.
/// </summary>
/// <remarks>Added in v0.3.1e.</remarks>
public sealed class WorkspaceRepository : IWorkspaceRepository
{
    private readonly AInternDbContext _context;
    private readonly ILogger<WorkspaceRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the WorkspaceRepository.
    /// </summary>
    public WorkspaceRepository(AInternDbContext context, ILogger<WorkspaceRepository> logger)
    {
        _context = context;
        _logger = logger;
        _logger.LogDebug("[INIT] WorkspaceRepository created");
    }

    /// <inheritdoc/>
    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ENTRY] GetByIdAsync - Id: {Id}", id);
        var sw = Stopwatch.StartNew();

        var entity = await _context.RecentWorkspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        _logger.LogDebug("[EXIT] GetByIdAsync - Found: {Found}, Elapsed: {Elapsed}ms",
            entity != null, sw.ElapsedMilliseconds);

        return entity?.ToWorkspace();
    }

    /// <inheritdoc/>
    public async Task<Workspace?> GetByPathAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _logger.LogDebug("[ENTRY] GetByPathAsync - Path: {Path}", rootPath);
        var sw = Stopwatch.StartNew();

        // Normalize path for comparison
        var normalizedPath = Path.GetFullPath(rootPath);

        var entity = await _context.RecentWorkspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.RootPath == normalizedPath, cancellationToken);

        _logger.LogDebug("[EXIT] GetByPathAsync - Found: {Found}, Elapsed: {Elapsed}ms",
            entity != null, sw.ElapsedMilliseconds);

        return entity?.ToWorkspace();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Workspace>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ENTRY] GetRecentAsync - Count: {Count}", count);
        var sw = Stopwatch.StartNew();

        var entities = await _context.RecentWorkspaces
            .AsNoTracking()
            .OrderByDescending(w => w.IsPinned)
            .ThenByDescending(w => w.LastAccessedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

        var workspaces = entities.Select(e => e.ToWorkspace()).ToList();

        _logger.LogDebug("[EXIT] GetRecentAsync - Found: {Count} workspaces, Elapsed: {Elapsed}ms",
            workspaces.Count, sw.ElapsedMilliseconds);

        return workspaces;
    }

    /// <inheritdoc/>
    public async Task AddOrUpdateAsync(Workspace workspace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        _logger.LogDebug("[ENTRY] AddOrUpdateAsync - Id: {Id}, Path: {Path}", workspace.Id, workspace.RootPath);
        var sw = Stopwatch.StartNew();

        var existing = await _context.RecentWorkspaces
            .FirstOrDefaultAsync(w => w.Id == workspace.Id, cancellationToken);

        if (existing != null)
        {
            existing.UpdateFrom(workspace);
            _logger.LogDebug("Updating existing workspace");
        }
        else
        {
            // Also check by path to prevent duplicates
            existing = await _context.RecentWorkspaces
                .FirstOrDefaultAsync(w => w.RootPath == workspace.RootPath, cancellationToken);

            if (existing != null)
            {
                existing.UpdateFrom(workspace);
                _logger.LogDebug("Updating existing workspace (matched by path)");
            }
            else
            {
                var entity = RecentWorkspaceEntity.FromWorkspace(workspace);
                _context.RecentWorkspaces.Add(entity);
                _logger.LogDebug("Adding new workspace");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("[EXIT] AddOrUpdateAsync - Elapsed: {Elapsed}ms", sw.ElapsedMilliseconds);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ENTRY] RemoveAsync - Id: {Id}", workspaceId);

        var entity = await _context.RecentWorkspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (entity != null)
        {
            _context.RecentWorkspaces.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed workspace: {Id}", workspaceId);
        }
    }

    /// <inheritdoc/>
    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("[ACTION] ClearAllAsync - Removing all recent workspaces");

        await _context.RecentWorkspaces.ExecuteDeleteAsync(cancellationToken);
        _logger.LogInformation("Cleared all recent workspaces");
    }

    /// <inheritdoc/>
    public async Task SetPinnedAsync(Guid workspaceId, bool isPinned, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ENTRY] SetPinnedAsync - Id: {Id}, IsPinned: {IsPinned}", workspaceId, isPinned);

        var entity = await _context.RecentWorkspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (entity != null)
        {
            entity.IsPinned = isPinned;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Set workspace {Id} pinned: {IsPinned}", workspaceId, isPinned);
        }
    }

    /// <inheritdoc/>
    public async Task RenameAsync(Guid workspaceId, string newName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        _logger.LogDebug("[ENTRY] RenameAsync - Id: {Id}, NewName: {Name}", workspaceId, newName);

        var entity = await _context.RecentWorkspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (entity != null)
        {
            entity.Name = newName;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Renamed workspace {Id} to: {Name}", workspaceId, newName);
        }
    }
}
