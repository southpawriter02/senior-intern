namespace AIntern.Services.Tests;

using Moq;
using Xunit;
using AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ ROLLBACK MANAGER TESTS (v0.4.4c)                                         │
// │ Unit tests for the RollbackManager implementation.                       │
// └─────────────────────────────────────────────────────────────────────────┘

public class RollbackManagerTests
{
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<IBackupService> _backupServiceMock;
    private readonly RollbackManager _manager;

    public RollbackManagerTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _backupServiceMock = new Mock<IBackupService>();
        _manager = new RollbackManager(_fileSystemMock.Object, _backupServiceMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Registration Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void RegisterCreatedFile_AddsAction()
    {
        _manager.RegisterCreatedFile("/path/to/file.cs");
        Assert.Equal(1, _manager.ActionCount);
    }

    [Fact]
    public void RegisterModifiedFile_AddsAction()
    {
        _manager.RegisterModifiedFile("/path/to/file.cs", "/backups/backup.bak");
        Assert.Equal(1, _manager.ActionCount);
    }

    [Fact]
    public void RegisterCreatedDirectory_AddsAction()
    {
        _manager.RegisterCreatedDirectory("/path/to/dir");
        Assert.Equal(1, _manager.ActionCount);
    }

    [Fact]
    public void RegisterDeletedFile_AddsAction()
    {
        _manager.RegisterDeletedFile("/path/to/file.cs", "/backups/backup.bak");
        Assert.Equal(1, _manager.ActionCount);
    }

    [Fact]
    public void RegisterRenamedFile_AddsAction()
    {
        _manager.RegisterRenamedFile("/original/path.cs", "/new/path.cs");
        Assert.Equal(1, _manager.ActionCount);
    }

    [Fact]
    public void MultipleRegistrations_AccumulateActions()
    {
        _manager.RegisterCreatedFile("/file1.cs");
        _manager.RegisterCreatedFile("/file2.cs");
        _manager.RegisterModifiedFile("/file3.cs", "/backup.bak");

        Assert.Equal(3, _manager.ActionCount);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Commit Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Commit_ClearsActions()
    {
        _manager.RegisterCreatedFile("/file.cs");
        Assert.Equal(1, _manager.ActionCount);

        _manager.Commit();

        Assert.Equal(0, _manager.ActionCount);
        Assert.True(_manager.IsCommitted);
    }

    [Fact]
    public void AfterCommit_RegistrationsIgnored()
    {
        _manager.Commit();

        _manager.RegisterCreatedFile("/file.cs");

        Assert.Equal(0, _manager.ActionCount);
    }

    [Fact]
    public async Task AfterCommit_RollbackReturnsFalse()
    {
        _manager.Commit();

        var result = await _manager.RollbackAsync();

        Assert.False(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Clear Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Clear_RemovesAllActions()
    {
        _manager.RegisterCreatedFile("/file1.cs");
        _manager.RegisterCreatedFile("/file2.cs");

        _manager.Clear();

        Assert.Equal(0, _manager.ActionCount);
        Assert.False(_manager.IsCommitted);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Rollback Execution Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RollbackAsync_DeletesCreatedFiles()
    {
        _manager.RegisterCreatedFile("/workspace/new-file.cs");

        _fileSystemMock.Setup(f => f.FileExistsAsync("/workspace/new-file.cs"))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(f => f.DeleteFileAsync("/workspace/new-file.cs", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _manager.RollbackAsync();

        Assert.True(result);
        _fileSystemMock.Verify(f => f.DeleteFileAsync("/workspace/new-file.cs", It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_RestoresModifiedFiles()
    {
        _manager.RegisterModifiedFile("/workspace/modified.cs", "/backups/backup.bak");

        _backupServiceMock.Setup(b => b.RestoreBackupAsync("/backups/backup.bak", "/workspace/modified.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _manager.RollbackAsync();

        Assert.True(result);
        _backupServiceMock.Verify(b => b.RestoreBackupAsync(
            "/backups/backup.bak",
            "/workspace/modified.cs",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_RestoresDeletedFiles()
    {
        _manager.RegisterDeletedFile("/workspace/deleted.cs", "/backups/deleted.bak");

        _backupServiceMock.Setup(b => b.RestoreBackupAsync(
            "/backups/deleted.bak", 
            "/workspace/deleted.cs", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _manager.RollbackAsync();

        Assert.True(result);
        _backupServiceMock.Verify(b => b.RestoreBackupAsync(
            "/backups/deleted.bak",
            "/workspace/deleted.cs",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_UndoesRenames_SkipsWhenOriginalExists()
    {
        // When original path already exists, rename rollback should be skipped
        _manager.RegisterRenamedFile("/original.cs", "/renamed.cs");

        _fileSystemMock.Setup(f => f.FileExistsAsync("/renamed.cs"))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(f => f.FileExistsAsync("/original.cs"))
            .ReturnsAsync(true); // Original exists, so rollback skips

        var result = await _manager.RollbackAsync();

        Assert.True(result);
        // File.Move is skipped when original already exists
    }

    [Fact]
    public async Task RollbackAsync_ExecutesInReverseOrder()
    {
        var callOrder = new List<string>();

        _manager.RegisterCreatedFile("/file1.cs");
        _manager.RegisterCreatedFile("/file2.cs");
        _manager.RegisterCreatedFile("/file3.cs");

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(f => f.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((path, _) =>
            {
                callOrder.Add(path);
                return Task.CompletedTask;
            });

        await _manager.RollbackAsync();

        // Should be in reverse order: file3, file2, file1
        Assert.Equal(3, callOrder.Count);
        Assert.Equal("/file3.cs", callOrder[0]);
        Assert.Equal("/file2.cs", callOrder[1]);
        Assert.Equal("/file1.cs", callOrder[2]);
    }

    [Fact]
    public async Task RollbackAsync_SkipsNonExistentFiles()
    {
        _manager.RegisterCreatedFile("/nonexistent.cs");

        _fileSystemMock.Setup(f => f.FileExistsAsync("/nonexistent.cs"))
            .ReturnsAsync(false);

        var result = await _manager.RollbackAsync();

        Assert.True(result);
        _fileSystemMock.Verify(f => f.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task RollbackAsync_EmptyActions_ReturnsTrue()
    {
        var result = await _manager.RollbackAsync();
        Assert.True(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dispose Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_ClearsActions()
    {
        _manager.RegisterCreatedFile("/file.cs");

        _manager.Dispose();

        Assert.Equal(0, _manager.ActionCount);
    }
}
