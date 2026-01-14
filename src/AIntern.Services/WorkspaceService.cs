namespace AIntern.Services;

using Microsoft.Extensions.Logging;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Data.Repositories;
using System.Diagnostics;

/// <summary>
/// Service for workspace lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// Provides workspace open/close, state persistence, and event notifications.
/// Features auto-save every 30 seconds when state changes.
/// </para>
/// <para>Added in v0.3.1e.</para>
/// </remarks>
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

    /// <summary>
    /// Auto-save interval in milliseconds (30 seconds).
    /// </summary>
    private const int AutoSaveIntervalMs = 30_000;

    #region Properties

    /// <inheritdoc/>
    public Workspace? CurrentWorkspace => _currentWorkspace;

    /// <inheritdoc/>
    public bool HasOpenWorkspace => _currentWorkspace != null;

    #endregion

    #region Events

    /// <inheritdoc/>
    public event EventHandler<WorkspaceChangedEventArgs>? WorkspaceChanged;

    /// <inheritdoc/>
    public event EventHandler<WorkspaceStateChangedEventArgs>? StateChanged;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the WorkspaceService.
    /// </summary>
    public WorkspaceService(
        IWorkspaceRepository repository,
        IFileSystemService fileSystemService,
        ISettingsService settingsService,
        ILogger<WorkspaceService> logger)
    {
        _repository = repository;
        _fileSystemService = fileSystemService;
        _settingsService = settingsService;
        _logger = logger;

        // Auto-save workspace state every 30 seconds
        _autoSaveTimer = new System.Timers.Timer(AutoSaveIntervalMs);
        _autoSaveTimer.Elapsed += async (s, e) => await AutoSaveStateAsync();
        _autoSaveTimer.Start();

        _logger.LogDebug("[INIT] WorkspaceService created with {Interval}ms auto-save", AutoSaveIntervalMs);
    }

    #endregion

    #region Workspace Operations

    /// <inheritdoc/>
    public async Task<Workspace> OpenWorkspaceAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        var sw = Stopwatch.StartNew();

        // Normalize to absolute path
        folderPath = Path.GetFullPath(folderPath);
        _logger.LogInformation("[ENTRY] OpenWorkspaceAsync - Path: {Path}", folderPath);

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Close current workspace if any
            if (_currentWorkspace != null)
            {
                _logger.LogDebug("Closing current workspace before opening new one");
                await CloseWorkspaceInternalAsync();
            }

            // Check if workspace exists in recent
            var existing = await _repository.GetByPathAsync(folderPath, cancellationToken);

            Workspace workspace;
            if (existing != null)
            {
                workspace = existing;
                workspace.LastAccessedAt = DateTime.UtcNow;
                _logger.LogDebug("Restored existing workspace: {Name}", workspace.DisplayName);
            }
            else
            {
                workspace = new Workspace
                {
                    RootPath = folderPath,
                    OpenedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };
                _logger.LogDebug("Created new workspace for: {Path}", folderPath);
            }

            // Load .gitignore patterns
            var ignorePatterns = await _fileSystemService.LoadGitIgnorePatternsAsync(
                folderPath, cancellationToken);
            workspace.GitIgnorePatterns = ignorePatterns;
            _logger.LogDebug("Loaded {Count} ignore patterns", ignorePatterns.Count);

            // Save to repository
            await _repository.AddOrUpdateAsync(workspace, cancellationToken);

            // Start file watcher
            _fileWatcher = _fileSystemService.WatchDirectory(
                folderPath,
                OnFileSystemChange,
                includeSubdirectories: true);

            _currentWorkspace = workspace;
            _stateChanged = false;

            // Raise event
            WorkspaceChanged?.Invoke(this, new WorkspaceChangedEventArgs
            {
                PreviousWorkspace = null,
                CurrentWorkspace = workspace,
                ChangeType = WorkspaceChangeType.Opened
            });

            _logger.LogInformation("[EXIT] OpenWorkspaceAsync - Opened: {Name} in {Elapsed}ms",
                workspace.DisplayName, sw.ElapsedMilliseconds);

            return workspace;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task CloseWorkspaceAsync()
    {
        _logger.LogDebug("[ENTRY] CloseWorkspaceAsync");

        await _lock.WaitAsync();
        try
        {
            await CloseWorkspaceInternalAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Internal close without acquiring lock (caller must hold lock).
    /// </summary>
    private async Task CloseWorkspaceInternalAsync()
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
        _stateChanged = false;

        // Raise event
        WorkspaceChanged?.Invoke(this, new WorkspaceChangedEventArgs
        {
            PreviousWorkspace = previousWorkspace,
            CurrentWorkspace = null,
            ChangeType = WorkspaceChangeType.Closed
        });

        _logger.LogInformation("Workspace closed: {Name}", previousWorkspace.DisplayName);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Workspace>> GetRecentWorkspacesAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[ENTRY] GetRecentWorkspacesAsync - Count: {Count}", count);
        var sw = Stopwatch.StartNew();

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
            _logger.LogDebug("Cleaning up {Count} invalid workspace entries", invalidIds.Count);
            _ = Task.Run(async () =>
            {
                foreach (var id in invalidIds)
                {
                    try
                    {
                        await _repository.RemoveAsync(id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to remove invalid workspace: {Id}", id);
                    }
                }
            });
        }

        _logger.LogDebug("[EXIT] GetRecentWorkspacesAsync - Found: {Count} valid, Elapsed: {Elapsed}ms",
            validWorkspaces.Count, sw.ElapsedMilliseconds);

        return validWorkspaces;
    }

    /// <inheritdoc/>
    public async Task RemoveFromRecentAsync(Guid workspaceId)
    {
        _logger.LogInformation("[ACTION] RemoveFromRecentAsync - Id: {Id}", workspaceId);
        await _repository.RemoveAsync(workspaceId);
    }

    /// <inheritdoc/>
    public async Task ClearRecentWorkspacesAsync()
    {
        _logger.LogWarning("[ACTION] ClearRecentWorkspacesAsync - Clearing all");
        await _repository.ClearAllAsync();
    }

    /// <inheritdoc/>
    public async Task SetPinnedAsync(Guid workspaceId, bool isPinned)
    {
        _logger.LogInformation("[ACTION] SetPinnedAsync - Id: {Id}, IsPinned: {IsPinned}", workspaceId, isPinned);
        await _repository.SetPinnedAsync(workspaceId, isPinned);
    }

    /// <inheritdoc/>
    public async Task RenameWorkspaceAsync(Guid workspaceId, string newName)
    {
        _logger.LogInformation("[ACTION] RenameWorkspaceAsync - Id: {Id}, Name: {Name}", workspaceId, newName);
        await _repository.RenameAsync(workspaceId, newName);
    }

    #endregion

    #region State Management

    /// <inheritdoc/>
    public async Task SaveWorkspaceStateAsync()
    {
        _logger.LogDebug("[ENTRY] SaveWorkspaceStateAsync");

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

    /// <summary>
    /// Internal save without acquiring lock (caller must hold lock).
    /// </summary>
    private async Task SaveWorkspaceStateInternalAsync()
    {
        if (_currentWorkspace == null) return;

        await _repository.AddOrUpdateAsync(_currentWorkspace);
        _stateChanged = false;
        _logger.LogDebug("Saved workspace state: {Name}", _currentWorkspace.DisplayName);
    }

    /// <inheritdoc/>
    public async Task<Workspace?> RestoreLastWorkspaceAsync()
    {
        _logger.LogDebug("[ENTRY] RestoreLastWorkspaceAsync");
        var sw = Stopwatch.StartNew();

        // Load settings if not already loaded
        await _settingsService.LoadSettingsAsync();
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
        var restored = await OpenWorkspaceAsync(last.RootPath);

        _logger.LogInformation("[EXIT] RestoreLastWorkspaceAsync - Restored: {Name} in {Elapsed}ms",
            restored.DisplayName, sw.ElapsedMilliseconds);

        return restored;
    }

    /// <inheritdoc/>
    public void UpdateOpenFiles(IReadOnlyList<string> openFiles, string? activeFile)
    {
        if (_currentWorkspace == null)
        {
            _logger.LogWarning("UpdateOpenFiles called with no workspace open");
            return;
        }

        _logger.LogDebug("UpdateOpenFiles - Files: {Count}, Active: {Active}",
            openFiles.Count, activeFile ?? "(none)");

        _currentWorkspace.OpenFiles = openFiles;
        _currentWorkspace.ActiveFilePath = activeFile;
        _stateChanged = true;

        StateChanged?.Invoke(this, new WorkspaceStateChangedEventArgs
        {
            Workspace = _currentWorkspace,
            ChangeType = WorkspaceStateChangeType.OpenFilesChanged
        });
    }

    /// <inheritdoc/>
    public void UpdateExpandedFolders(IReadOnlyList<string> expandedFolders)
    {
        if (_currentWorkspace == null)
        {
            _logger.LogWarning("UpdateExpandedFolders called with no workspace open");
            return;
        }

        _logger.LogDebug("UpdateExpandedFolders - Count: {Count}", expandedFolders.Count);

        _currentWorkspace.ExpandedFolders = expandedFolders;
        _stateChanged = true;

        StateChanged?.Invoke(this, new WorkspaceStateChangedEventArgs
        {
            Workspace = _currentWorkspace,
            ChangeType = WorkspaceStateChangeType.ExpandedFoldersChanged
        });
    }

    /// <summary>
    /// Auto-save callback from timer.
    /// </summary>
    private async Task AutoSaveStateAsync()
    {
        if (!_stateChanged || _currentWorkspace == null) return;

        try
        {
            _logger.LogDebug("Auto-saving workspace state");
            await SaveWorkspaceStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-save workspace state");
        }
    }

    #endregion

    #region File Watcher Handler

    /// <summary>
    /// Handles file system change events from the watcher.
    /// </summary>
    private void OnFileSystemChange(FileSystemChangeEvent e)
    {
        _logger.LogDebug("File system change: {Type} - {Path}", e.ChangeType, e.Path);
        // Future: Could raise a FileSystemChanged event for UI file tree refresh
    }

    #endregion

    #region IDisposable

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogDebug("Disposing WorkspaceService");

        _autoSaveTimer.Stop();
        _autoSaveTimer.Dispose();
        _fileWatcher?.Dispose();
        _lock.Dispose();
    }

    #endregion
}
