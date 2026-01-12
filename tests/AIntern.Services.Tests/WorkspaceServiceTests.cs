using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Events;
using AIntern.Core.Models;
using AIntern.Services;

namespace AIntern.Services.Tests;

/// <summary>
/// Unit tests for WorkspaceService (v0.3.1e).
/// </summary>
public class WorkspaceServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IWorkspaceRepository _mockRepository;
    private readonly IFileSystemService _mockFileSystemService;
    private readonly ISettingsService _mockSettingsService;
    private readonly ILogger<WorkspaceService> _logger;
    private readonly WorkspaceService _service;

    public WorkspaceServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"WorkspaceServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _mockRepository = Substitute.For<IWorkspaceRepository>();
        _mockFileSystemService = Substitute.For<IFileSystemService>();
        _mockSettingsService = Substitute.For<ISettingsService>();
        _logger = Substitute.For<ILogger<WorkspaceService>>();

        // Default settings
        _mockSettingsService.CurrentSettings.Returns(new AppSettings { RestoreLastWorkspace = true });
        _mockFileSystemService.LoadGitIgnorePatternsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([]));
        _mockFileSystemService.WatchDirectory(Arg.Any<string>(), Arg.Any<Action<FileSystemChangeEvent>>(), Arg.Any<bool>())
            .Returns(Substitute.For<IDisposable>());

        _service = new WorkspaceService(
            _mockRepository,
            _mockFileSystemService,
            _mockSettingsService,
            _logger);
    }

    public void Dispose()
    {
        _service.Dispose();
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch { /* Ignore cleanup errors */ }
    }

    private string CreateTempDirectory(string name = "workspace")
    {
        var path = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(path);
        return path;
    }

    #region OpenWorkspaceAsync Tests

    [Fact]
    public async Task OpenWorkspaceAsync_NewWorkspace_CreatesAndSaves()
    {
        var path = CreateTempDirectory();
        _mockRepository.GetByPathAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Workspace?>(null));

        var workspace = await _service.OpenWorkspaceAsync(path);

        Assert.NotNull(workspace);
        Assert.Equal(path, workspace.RootPath);
        await _mockRepository.Received(1).AddOrUpdateAsync(Arg.Any<Workspace>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenWorkspaceAsync_ExistingWorkspace_RestoresState()
    {
        var path = CreateTempDirectory();
        var existing = new Workspace
        {
            RootPath = path,
            OpenFiles = ["file.cs"],
            IsPinned = true
        };
        _mockRepository.GetByPathAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Workspace?>(existing));

        var workspace = await _service.OpenWorkspaceAsync(path);

        Assert.Single(workspace.OpenFiles);
        Assert.Equal("file.cs", workspace.OpenFiles[0]);
        Assert.True(workspace.IsPinned);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_SetsCurrent()
    {
        var path = CreateTempDirectory();
        Assert.Null(_service.CurrentWorkspace);
        Assert.False(_service.HasOpenWorkspace);

        await _service.OpenWorkspaceAsync(path);

        Assert.NotNull(_service.CurrentWorkspace);
        Assert.True(_service.HasOpenWorkspace);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_RaisesWorkspaceChangedEvent()
    {
        var path = CreateTempDirectory();
        WorkspaceChangedEventArgs? eventArgs = null;
        _service.WorkspaceChanged += (s, e) => eventArgs = e;

        await _service.OpenWorkspaceAsync(path);

        Assert.NotNull(eventArgs);
        Assert.Equal(WorkspaceChangeType.Opened, eventArgs.ChangeType);
        Assert.NotNull(eventArgs.CurrentWorkspace);
        Assert.Null(eventArgs.PreviousWorkspace);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_ThrowsForNonexistent()
    {
        var nonexistent = Path.Combine(_tempDir, "nonexistent");

        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _service.OpenWorkspaceAsync(nonexistent));
    }

    [Fact]
    public async Task OpenWorkspaceAsync_ThrowsForNullPath()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.OpenWorkspaceAsync(null!));
    }

    [Fact]
    public async Task OpenWorkspaceAsync_ThrowsForEmptyPath()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.OpenWorkspaceAsync(""));
    }

    [Fact]
    public async Task OpenWorkspaceAsync_ClosePreviousWorkspace()
    {
        var path1 = CreateTempDirectory("workspace1");
        var path2 = CreateTempDirectory("workspace2");

        await _service.OpenWorkspaceAsync(path1);
        var firstWorkspace = _service.CurrentWorkspace;

        WorkspaceChangedEventArgs? eventArgs = null;
        _service.WorkspaceChanged += (s, e) => eventArgs = e;

        await _service.OpenWorkspaceAsync(path2);

        Assert.NotNull(_service.CurrentWorkspace);
        Assert.Equal(path2, _service.CurrentWorkspace.RootPath);
        // Previous workspace state should have been saved
        await _mockRepository.Received().AddOrUpdateAsync(
            Arg.Is<Workspace>(w => w.RootPath == path1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenWorkspaceAsync_LoadsGitIgnorePatterns()
    {
        var path = CreateTempDirectory();
        var patterns = new List<string> { "node_modules/", "*.log" };
        _mockFileSystemService.LoadGitIgnorePatternsAsync(path, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(patterns));

        var workspace = await _service.OpenWorkspaceAsync(path);

        Assert.Equal(2, workspace.GitIgnorePatterns.Count);
        Assert.Contains("node_modules/", workspace.GitIgnorePatterns);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_StartsFileWatcher()
    {
        var path = CreateTempDirectory();

        await _service.OpenWorkspaceAsync(path);

        _mockFileSystemService.Received(1).WatchDirectory(
            path,
            Arg.Any<Action<FileSystemChangeEvent>>(),
            includeSubdirectories: true);
    }

    #endregion

    #region CloseWorkspaceAsync Tests

    [Fact]
    public async Task CloseWorkspaceAsync_SavesStateBeforeClosing()
    {
        var path = CreateTempDirectory();
        await _service.OpenWorkspaceAsync(path);
        _service.UpdateOpenFiles(["file.cs"], "file.cs");

        await _service.CloseWorkspaceAsync();

        await _mockRepository.Received().AddOrUpdateAsync(
            Arg.Is<Workspace>(w => w.OpenFiles.Contains("file.cs")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloseWorkspaceAsync_RaisesEvent()
    {
        var path = CreateTempDirectory();
        await _service.OpenWorkspaceAsync(path);
        
        WorkspaceChangedEventArgs? eventArgs = null;
        _service.WorkspaceChanged += (s, e) => eventArgs = e;

        await _service.CloseWorkspaceAsync();

        Assert.NotNull(eventArgs);
        Assert.Equal(WorkspaceChangeType.Closed, eventArgs.ChangeType);
        Assert.Null(eventArgs.CurrentWorkspace);
        Assert.NotNull(eventArgs.PreviousWorkspace);
    }

    [Fact]
    public async Task CloseWorkspaceAsync_ClearsCurrentWorkspace()
    {
        var path = CreateTempDirectory();
        await _service.OpenWorkspaceAsync(path);
        Assert.NotNull(_service.CurrentWorkspace);

        await _service.CloseWorkspaceAsync();

        Assert.Null(_service.CurrentWorkspace);
        Assert.False(_service.HasOpenWorkspace);
    }

    #endregion

    #region GetRecentWorkspacesAsync Tests

    [Fact]
    public async Task GetRecentWorkspacesAsync_FiltersNonExistentPaths()
    {
        var existingPath = CreateTempDirectory("exists");
        var workspaces = new List<Workspace>
        {
            new Workspace { RootPath = existingPath },
            new Workspace { RootPath = "/does-not-exist" }
        };
        _mockRepository.GetRecentAsync(10, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Workspace>>(workspaces));

        var result = await _service.GetRecentWorkspacesAsync();

        Assert.Single(result);
        Assert.Equal(existingPath, result[0].RootPath);
    }

    [Fact]
    public async Task GetRecentWorkspacesAsync_RespectsCount()
    {
        await _service.GetRecentWorkspacesAsync(5);

        await _mockRepository.Received(1).GetRecentAsync(5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecentWorkspacesAsync_ReturnsEmptyWhenNone()
    {
        _mockRepository.GetRecentAsync(10, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Workspace>>([]));

        var result = await _service.GetRecentWorkspacesAsync();

        Assert.Empty(result);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public async Task UpdateOpenFiles_RaisesStateChangedEvent()
    {
        var path = CreateTempDirectory();
        await _service.OpenWorkspaceAsync(path);

        WorkspaceStateChangedEventArgs? eventArgs = null;
        _service.StateChanged += (s, e) => eventArgs = e;

        _service.UpdateOpenFiles(["new.cs"], "new.cs");

        Assert.NotNull(eventArgs);
        Assert.Equal(WorkspaceStateChangeType.OpenFilesChanged, eventArgs.ChangeType);
    }

    [Fact]
    public async Task UpdateOpenFiles_UpdatesWorkspaceState()
    {
        var path = CreateTempDirectory();
        await _service.OpenWorkspaceAsync(path);

        _service.UpdateOpenFiles(["file1.cs", "file2.cs"], "file1.cs");

        Assert.Equal(2, _service.CurrentWorkspace!.OpenFiles.Count);
        Assert.Equal("file1.cs", _service.CurrentWorkspace.ActiveFilePath);
    }

    [Fact]
    public async Task UpdateExpandedFolders_RaisesStateChangedEvent()
    {
        var path = CreateTempDirectory();
        await _service.OpenWorkspaceAsync(path);

        WorkspaceStateChangedEventArgs? eventArgs = null;
        _service.StateChanged += (s, e) => eventArgs = e;

        _service.UpdateExpandedFolders(["src", "tests"]);

        Assert.NotNull(eventArgs);
        Assert.Equal(WorkspaceStateChangeType.ExpandedFoldersChanged, eventArgs.ChangeType);
    }

    [Fact]
    public async Task UpdateExpandedFolders_UpdatesWorkspaceState()
    {
        var path = CreateTempDirectory();
        await _service.OpenWorkspaceAsync(path);

        _service.UpdateExpandedFolders(["src", "tests"]);

        Assert.Equal(2, _service.CurrentWorkspace!.ExpandedFolders.Count);
    }

    [Fact]
    public void UpdateOpenFiles_DoesNothingWhenNoWorkspace()
    {
        WorkspaceStateChangedEventArgs? eventArgs = null;
        _service.StateChanged += (s, e) => eventArgs = e;

        _service.UpdateOpenFiles(["file.cs"], "file.cs");

        Assert.Null(eventArgs);
    }

    #endregion

    #region RestoreLastWorkspaceAsync Tests

    [Fact]
    public async Task RestoreLastWorkspaceAsync_RespectsSettingsDisabled()
    {
        _mockSettingsService.CurrentSettings.Returns(new AppSettings { RestoreLastWorkspace = false });

        var result = await _service.RestoreLastWorkspaceAsync();

        Assert.Null(result);
        await _mockRepository.DidNotReceive().GetRecentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreLastWorkspaceAsync_ReturnsNullWhenNoRecent()
    {
        _mockRepository.GetRecentAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Workspace>>([]));

        var result = await _service.RestoreLastWorkspaceAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task RestoreLastWorkspaceAsync_ReturnsNullWhenPathDeleted()
    {
        var workspaces = new List<Workspace> { new Workspace { RootPath = "/deleted-path" } };
        _mockRepository.GetRecentAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Workspace>>(workspaces));

        var result = await _service.RestoreLastWorkspaceAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task RestoreLastWorkspaceAsync_OpensLastWorkspace()
    {
        var path = CreateTempDirectory();
        var workspaces = new List<Workspace> { new Workspace { RootPath = path } };
        _mockRepository.GetRecentAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Workspace>>(workspaces));

        var result = await _service.RestoreLastWorkspaceAsync();

        Assert.NotNull(result);
        Assert.Equal(path, result.RootPath);
    }

    #endregion

    #region Repository Delegation Tests

    [Fact]
    public async Task RemoveFromRecentAsync_DelegatestoRepository()
    {
        var id = Guid.NewGuid();

        await _service.RemoveFromRecentAsync(id);

        await _mockRepository.Received(1).RemoveAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearRecentWorkspacesAsync_DelegatesToRepository()
    {
        await _service.ClearRecentWorkspacesAsync();

        await _mockRepository.Received(1).ClearAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetPinnedAsync_DelegatesToRepository()
    {
        var id = Guid.NewGuid();

        await _service.SetPinnedAsync(id, true);

        await _mockRepository.Received(1).SetPinnedAsync(id, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenameWorkspaceAsync_DelegatesToRepository()
    {
        var id = Guid.NewGuid();

        await _service.RenameWorkspaceAsync(id, "New Name");

        await _mockRepository.Received(1).RenameAsync(id, "New Name", Arg.Any<CancellationToken>());
    }

    #endregion
}
