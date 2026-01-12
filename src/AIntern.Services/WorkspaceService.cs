using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

/// <summary>
/// Manages workspace lifecycle, state persistence, and auto-save (v0.3.1e).
/// </summary>
public sealed class WorkspaceService : IWorkspaceService, IDisposable
{
    private readonly IWorkspaceRepository _repository;
    private readonly IFileSystemService _fileSystemService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<WorkspaceService> _logger;

    private Workspace? _currentWorkspace;
    private IDisposable? _fileWatcher;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly System.Timers.Timer _autoSaveTimer;
    private bool _stateChanged;
    private bool _disposed;

    public Workspace? CurrentWorkspace => _currentWorkspace;
    public bool HasOpenWorkspace => _currentWorkspace != null;

    public event EventHandler<WorkspaceChangedEventArgs>? WorkspaceChanged;
    public event EventHandler<WorkspaceStateChangedEventArgs>? StateChanged;

    public WorkspaceService(
        IWorkspaceRepository repository,
        IFileSystemService fileSystemService,
        ISettingsService settingsService,
        ILogger<WorkspaceService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Auto-save workspace state every 30 seconds
        _autoSaveTimer = new System.Timers.Timer(30_000);
        _autoSaveTimer.Elapsed += async (s, e) => await AutoSaveStateAsync();
        _autoSaveTimer.Start();
    }

    #region Workspace Operations

    public async Task<Workspace> OpenWorkspaceAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        folderPath = Path.GetFullPath(folderPath);

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Opening workspace: {Path}", folderPath);

            var previousWorkspace = _currentWorkspace;

            // Close current workspace if any
            if (_currentWorkspace != null)
                await CloseWorkspaceInternalAsync(raisEvent: false);

            // Check if workspace exists in recent
            var existing = await _repository.GetByPathAsync(folderPath, cancellationToken);

            Workspace workspace;
            if (existing != null)
            {
                workspace = existing;
                workspace.LastAccessedAt = DateTime.UtcNow;
            }
            else
            {
                workspace = new Workspace
                {
                    RootPath = folderPath,
                    OpenedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };
            }

            // Load .gitignore patterns
            var ignorePatterns = await _fileSystemService.LoadGitIgnorePatternsAsync(
                folderPath, cancellationToken);
            workspace.GitIgnorePatterns = ignorePatterns;

            // Save to recent
            await _repository.AddOrUpdateAsync(workspace, cancellationToken);

            // Start file watcher
            _fileWatcher = _fileSystemService.WatchDirectory(
                folderPath,
                OnFileSystemChange,
                includeSubdirectories: true);

            _currentWorkspace = workspace;

            // Raise event
            WorkspaceChanged?.Invoke(this, new WorkspaceChangedEventArgs
            {
                PreviousWorkspace = previousWorkspace,
                CurrentWorkspace = workspace,
                ChangeType = WorkspaceChangeType.Opened
            });

            _logger.LogInformation("Opened workspace: {Name} at {Path}",
                workspace.DisplayName, workspace.RootPath);

            return workspace;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task CloseWorkspaceAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await CloseWorkspaceInternalAsync(raisEvent: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task CloseWorkspaceInternalAsync(bool raisEvent)
    {
        if (_currentWorkspace == null) return;

        _logger.LogInformation("Closing workspace: {Name}", _currentWorkspace.DisplayName);

        // Save state before closing
        await SaveWorkspaceStateInternalAsync();

        // Stop file watcher
        _fileWatcher?.Dispose();
        _fileWatcher = null;

        var previousWorkspace = _currentWorkspace;
        _currentWorkspace = null;

        if (raisEvent)
        {
            // Raise event
            WorkspaceChanged?.Invoke(this, new WorkspaceChangedEventArgs
            {
                PreviousWorkspace = previousWorkspace,
                CurrentWorkspace = null,
                ChangeType = WorkspaceChangeType.Closed
            });
        }
    }

    public async Task<IReadOnlyList<Workspace>> GetRecentWorkspacesAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var workspaces = await _repository.GetRecentAsync(count, cancellationToken);

        // Filter out workspaces that no longer exist
        var validWorkspaces = workspaces
            .Where(w => Directory.Exists(w.RootPath))
            .ToList();

        // Clean up invalid entries in background
        var invalidIds = workspaces
            .Where(w => !Directory.Exists(w.RootPath))
            .Select(w => w.Id)
            .ToList();

        if (invalidIds.Count > 0)
        {
            _ = Task.Run(async () =>
            {
                foreach (var id in invalidIds)
                {
                    try
                    {
                        await _repository.RemoveAsync(id);
                        _logger.LogDebug("Cleaned up non-existent workspace: {Id}", id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to clean up workspace: {Id}", id);
                    }
                }
            });
        }

        return validWorkspaces;
    }

    public async Task RemoveFromRecentAsync(Guid workspaceId)
    {
        await _repository.RemoveAsync(workspaceId);
        _logger.LogInformation("Removed workspace from recent: {Id}", workspaceId);
    }

    public async Task ClearRecentWorkspacesAsync()
    {
        await _repository.ClearAllAsync();
        _logger.LogInformation("Cleared all recent workspaces");
    }

    public async Task SetPinnedAsync(Guid workspaceId, bool isPinned)
    {
        await _repository.SetPinnedAsync(workspaceId, isPinned);
        _logger.LogInformation("Set workspace {Id} pinned: {IsPinned}", workspaceId, isPinned);
    }

    public async Task RenameWorkspaceAsync(Guid workspaceId, string newName)
    {
        await _repository.RenameAsync(workspaceId, newName);
        _logger.LogInformation("Renamed workspace {Id} to: {Name}", workspaceId, newName);
    }

    #endregion

    #region State Management

    public async Task SaveWorkspaceStateAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await SaveWorkspaceStateInternalAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveWorkspaceStateInternalAsync()
    {
        if (_currentWorkspace == null) return;

        await _repository.AddOrUpdateAsync(_currentWorkspace);
        _stateChanged = false;
        _logger.LogDebug("Saved workspace state: {Name}", _currentWorkspace.DisplayName);
    }

    public async Task<Workspace?> RestoreLastWorkspaceAsync()
    {
        var settings = _settingsService.CurrentSettings;

        if (!settings.RestoreLastWorkspace)
        {
            _logger.LogDebug("Workspace restoration disabled in settings");
            return null;
        }

        var recent = await _repository.GetRecentAsync(1);
        if (recent.Count == 0)
        {
            _logger.LogDebug("No recent workspaces to restore");
            return null;
        }

        var last = recent[0];
        if (!Directory.Exists(last.RootPath))
        {
            _logger.LogWarning("Last workspace no longer exists: {Path}", last.RootPath);
            return null;
        }

        _logger.LogInformation("Restoring last workspace: {Path}", last.RootPath);
        return await OpenWorkspaceAsync(last.RootPath);
    }

    public void UpdateOpenFiles(IReadOnlyList<string> openFiles, string? activeFile)
    {
        if (_currentWorkspace == null) return;

        _currentWorkspace.OpenFiles = openFiles;
        _currentWorkspace.ActiveFilePath = activeFile;
        _stateChanged = true;

        StateChanged?.Invoke(this, new WorkspaceStateChangedEventArgs
        {
            Workspace = _currentWorkspace,
            ChangeType = WorkspaceStateChangeType.OpenFilesChanged
        });
    }

    public void UpdateExpandedFolders(IReadOnlyList<string> expandedFolders)
    {
        if (_currentWorkspace == null) return;

        _currentWorkspace.ExpandedFolders = expandedFolders;
        _stateChanged = true;

        StateChanged?.Invoke(this, new WorkspaceStateChangedEventArgs
        {
            Workspace = _currentWorkspace,
            ChangeType = WorkspaceStateChangeType.ExpandedFoldersChanged
        });
    }

    private async Task AutoSaveStateAsync()
    {
        if (!_stateChanged || _currentWorkspace == null) return;

        try
        {
            await SaveWorkspaceStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-save workspace state");
        }
    }

    #endregion

    #region File Watcher Handler

    private void OnFileSystemChange(FileSystemChangeEvent e)
    {
        _logger.LogDebug("File system change: {Type} - {Path}", e.ChangeType, e.Path);
        // Could raise an event here for UI file tree refresh
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _autoSaveTimer.Stop();
        _autoSaveTimer.Dispose();
        _fileWatcher?.Dispose();
        _lock.Dispose();
    }
}
