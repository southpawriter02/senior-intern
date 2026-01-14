using Xunit;
using Moq;
using AIntern.Services;
using AIntern.Core.Interfaces;
using AIntern.Core.Events;
using AIntern.Core.Models;
using AIntern.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace AIntern.Services.Tests.Workspace;

/// <summary>
/// Unit tests for <see cref="WorkspaceService"/>.
/// </summary>
public class WorkspaceServiceTests : IDisposable
{
    private readonly Mock<IWorkspaceRepository> _mockRepository;
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly Mock<ISettingsService> _mockSettings;
    private readonly Mock<ILogger<WorkspaceService>> _mockLogger;
    private readonly WorkspaceService _service;
    private readonly string _testDirectory;

    public WorkspaceServiceTests()
    {
        _mockRepository = new Mock<IWorkspaceRepository>();
        _mockFileSystem = new Mock<IFileSystemService>();
        _mockSettings = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<WorkspaceService>>();

        // Default setup for file system service
        _mockFileSystem
            .Setup(fs => fs.LoadGitIgnorePatternsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { ".git/", "node_modules/" });

        _mockFileSystem
            .Setup(fs => fs.WatchDirectory(It.IsAny<string>(), It.IsAny<Action<FileSystemChangeEvent>>(), It.IsAny<bool>()))
            .Returns(Mock.Of<IDisposable>());

        // Default settings
        _mockSettings.Setup(s => s.CurrentSettings).Returns(new AppSettings { RestoreLastWorkspace = true });

        _service = new WorkspaceService(
            _mockRepository.Object,
            _mockFileSystem.Object,
            _mockSettings.Object,
            _mockLogger.Object);

        _testDirectory = Path.Combine(Path.GetTempPath(), $"WorkspaceServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, true); }
            catch { /* Cleanup best effort */ }
        }
    }

    #region OpenWorkspaceAsync

    [Fact]
    public async Task OpenWorkspaceAsync_NewWorkspace_CreatesAndSaves()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIntern.Core.Models.Workspace?)null);

        // Act
        var workspace = await _service.OpenWorkspaceAsync(_testDirectory);

        // Assert
        Assert.NotNull(workspace);
        Assert.Equal(_testDirectory, workspace.RootPath);
        _mockRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<AIntern.Core.Models.Workspace>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_ExistingWorkspace_RestoresState()
    {
        // Arrange
        var existing = new AIntern.Core.Models.Workspace
        {
            RootPath = _testDirectory,
            OpenFiles = new List<string> { "file.cs" }
        };
        _mockRepository.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var workspace = await _service.OpenWorkspaceAsync(_testDirectory);

        // Assert
        Assert.Single(workspace.OpenFiles);
        Assert.Equal("file.cs", workspace.OpenFiles[0]);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_RaisesWorkspaceChangedEvent()
    {
        // Arrange
        WorkspaceChangedEventArgs? eventArgs = null;
        _service.WorkspaceChanged += (s, e) => eventArgs = e;
        _mockRepository.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIntern.Core.Models.Workspace?)null);

        // Act
        await _service.OpenWorkspaceAsync(_testDirectory);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(WorkspaceChangeType.Opened, eventArgs.ChangeType);
        Assert.NotNull(eventArgs.CurrentWorkspace);
        Assert.Null(eventArgs.PreviousWorkspace);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_LoadsGitIgnorePatterns()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIntern.Core.Models.Workspace?)null);

        // Act
        var workspace = await _service.OpenWorkspaceAsync(_testDirectory);

        // Assert
        _mockFileSystem.Verify(fs => fs.LoadGitIgnorePatternsAsync(_testDirectory, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(".git/", workspace.GitIgnorePatterns);
    }

    [Fact]
    public async Task OpenWorkspaceAsync_ClosesCurrentWorkspace()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIntern.Core.Models.Workspace?)null);

        // Open first workspace
        await _service.OpenWorkspaceAsync(_testDirectory);

        // Create second temp directory
        var secondDir = Path.Combine(Path.GetTempPath(), $"WorkspaceTest2_{Guid.NewGuid():N}");
        Directory.CreateDirectory(secondDir);

        try
        {
            // Act - open second
            var eventsList = new List<WorkspaceChangedEventArgs>();
            _service.WorkspaceChanged += (s, e) => eventsList.Add(e);
            await _service.OpenWorkspaceAsync(secondDir);

            // Assert - first was closed, second was opened
            Assert.Contains(eventsList, e => e.ChangeType == WorkspaceChangeType.Closed);
            Assert.Contains(eventsList, e => e.ChangeType == WorkspaceChangeType.Opened);
        }
        finally
        {
            Directory.Delete(secondDir, true);
        }
    }

    [Fact]
    public async Task OpenWorkspaceAsync_InvalidPath_ThrowsDirectoryNotFound()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDirectory, "nonexistent");

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _service.OpenWorkspaceAsync(invalidPath));
    }

    #endregion

    #region CloseWorkspaceAsync

    [Fact]
    public async Task CloseWorkspaceAsync_SavesStateAndRaisesEvent()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIntern.Core.Models.Workspace?)null);

        await _service.OpenWorkspaceAsync(_testDirectory);

        WorkspaceChangedEventArgs? eventArgs = null;
        _service.WorkspaceChanged += (s, e) => eventArgs = e;

        // Act
        await _service.CloseWorkspaceAsync();

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(WorkspaceChangeType.Closed, eventArgs.ChangeType);
        Assert.Null(eventArgs.CurrentWorkspace);
        Assert.NotNull(eventArgs.PreviousWorkspace);
        Assert.Null(_service.CurrentWorkspace);
    }

    [Fact]
    public async Task CloseWorkspaceAsync_NoWorkspace_DoesNothing()
    {
        // Act
        await _service.CloseWorkspaceAsync();

        // Assert
        _mockRepository.Verify(r => r.AddOrUpdateAsync(It.IsAny<AIntern.Core.Models.Workspace>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region State Management

    [Fact]
    public async Task UpdateOpenFiles_SetsStateAndRaisesEvent()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIntern.Core.Models.Workspace?)null);

        await _service.OpenWorkspaceAsync(_testDirectory);

        WorkspaceStateChangedEventArgs? eventArgs = null;
        _service.StateChanged += (s, e) => eventArgs = e;

        // Act
        _service.UpdateOpenFiles(["file1.cs", "file2.cs"], "file1.cs");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(WorkspaceStateChangeType.OpenFilesChanged, eventArgs.ChangeType);
        Assert.Equal(2, _service.CurrentWorkspace!.OpenFiles.Count);
        Assert.Equal("file1.cs", _service.CurrentWorkspace.ActiveFilePath);
    }

    [Fact]
    public async Task UpdateExpandedFolders_SetsStateAndRaisesEvent()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIntern.Core.Models.Workspace?)null);

        await _service.OpenWorkspaceAsync(_testDirectory);

        WorkspaceStateChangedEventArgs? eventArgs = null;
        _service.StateChanged += (s, e) => eventArgs = e;

        // Act
        _service.UpdateExpandedFolders(["src", "tests"]);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(WorkspaceStateChangeType.ExpandedFoldersChanged, eventArgs.ChangeType);
        Assert.Equal(2, _service.CurrentWorkspace!.ExpandedFolders.Count);
    }

    #endregion

    #region Recent Workspaces

    [Fact]
    public async Task GetRecentWorkspacesAsync_FiltersNonExistent()
    {
        // Arrange
        var validWorkspace = new AIntern.Core.Models.Workspace { RootPath = _testDirectory };
        var invalidWorkspace = new AIntern.Core.Models.Workspace { RootPath = "/nonexistent/path" };

        _mockRepository.Setup(r => r.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AIntern.Core.Models.Workspace> { validWorkspace, invalidWorkspace });

        // Act
        var result = await _service.GetRecentWorkspacesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(_testDirectory, result[0].RootPath);
    }

    [Fact]
    public async Task SetPinnedAsync_CallsRepository()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();

        // Act
        await _service.SetPinnedAsync(workspaceId, true);

        // Assert
        _mockRepository.Verify(r => r.SetPinnedAsync(workspaceId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RenameWorkspaceAsync_CallsRepository()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();

        // Act
        await _service.RenameWorkspaceAsync(workspaceId, "New Name");

        // Assert
        _mockRepository.Verify(r => r.RenameAsync(workspaceId, "New Name", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RestoreLastWorkspaceAsync

    [Fact]
    public async Task RestoreLastWorkspaceAsync_DisabledSetting_ReturnsNull()
    {
        // Arrange
        _mockSettings.Setup(s => s.CurrentSettings).Returns(new AppSettings { RestoreLastWorkspace = false });

        // Act
        var result = await _service.RestoreLastWorkspaceAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RestoreLastWorkspaceAsync_NoRecent_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetRecentAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AIntern.Core.Models.Workspace>());

        // Act
        var result = await _service.RestoreLastWorkspaceAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RestoreLastWorkspaceAsync_ValidRecent_OpensWorkspace()
    {
        // Arrange
        var recentWorkspace = new AIntern.Core.Models.Workspace { RootPath = _testDirectory };
        _mockRepository.Setup(r => r.GetRecentAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AIntern.Core.Models.Workspace> { recentWorkspace });
        _mockRepository.Setup(r => r.GetByPathAsync(_testDirectory, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentWorkspace);

        // Act
        var result = await _service.RestoreLastWorkspaceAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testDirectory, result.RootPath);
    }

    #endregion

    #region Properties

    [Fact]
    public async Task HasOpenWorkspace_ReturnsTrueWhenOpen()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIntern.Core.Models.Workspace?)null);

        // Act
        Assert.False(_service.HasOpenWorkspace);
        await _service.OpenWorkspaceAsync(_testDirectory);

        // Assert
        Assert.True(_service.HasOpenWorkspace);
    }

    #endregion
}
