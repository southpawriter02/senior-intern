using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Services;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.Services;

/// <summary>
/// Unit tests for v0.4.3b FileChangeService.
/// </summary>
public class FileChangeServiceTests
{
    private readonly Mock<IFileSystemService> _fileSystemMock = new();
    private readonly Mock<IDiffService> _diffServiceMock = new();
    private readonly Mock<IBackupService> _backupServiceMock = new();

    private FileChangeService CreateService()
    {
        return new FileChangeService(
            _fileSystemMock.Object,
            _diffServiceMock.Object,
            _backupServiceMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_NullFileSystem_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FileChangeService(null!, _diffServiceMock.Object, _backupServiceMock.Object));
    }

    [Fact]
    public void Constructor_NullDiffService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FileChangeService(_fileSystemMock.Object, null!, _backupServiceMock.Object));
    }

    [Fact]
    public void Constructor_NullBackupService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FileChangeService(_fileSystemMock.Object, _diffServiceMock.Object, null!));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ApplyCodeBlockAsync Validation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ApplyCodeBlockAsync_NullBlock_ReturnsValidationFailed()
    {
        var service = CreateService();

        var result = await service.ApplyCodeBlockAsync(null!, "/workspace");

        Assert.False(result.Success);
        Assert.Equal(ApplyResultType.ValidationFailed, result.ResultType);
    }

    [Fact]
    public async Task ApplyCodeBlockAsync_BlockWithoutTargetPath_ReturnsValidationFailed()
    {
        var service = CreateService();
        var block = new CodeBlock { TargetFilePath = null };

        var result = await service.ApplyCodeBlockAsync(block, "/workspace");

        Assert.False(result.Success);
        Assert.Equal(ApplyResultType.ValidationFailed, result.ResultType);
    }

    [Fact]
    public async Task ApplyCodeBlockAsync_EmptyWorkspace_ReturnsValidationFailed()
    {
        var service = CreateService();
        var block = new CodeBlock { TargetFilePath = "test.cs" };

        var result = await service.ApplyCodeBlockAsync(block, "");

        Assert.False(result.Success);
        Assert.Equal(ApplyResultType.ValidationFailed, result.ResultType);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ApplyDiffAsync Validation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ApplyDiffAsync_NullDiff_ReturnsValidationFailed()
    {
        var service = CreateService();

        var result = await service.ApplyDiffAsync(null!, "/workspace");

        Assert.False(result.Success);
        Assert.Equal(ApplyResultType.ValidationFailed, result.ResultType);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ApplyCodeBlocksAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ApplyCodeBlocksAsync_EmptyList_ReturnsEmptyResults()
    {
        var service = CreateService();

        var results = await service.ApplyCodeBlocksAsync(
            Array.Empty<CodeBlock>(), "/workspace");

        Assert.Empty(results);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CanUndo Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CanUndo_NoHistory_ReturnsFalse()
    {
        var service = CreateService();

        var canUndo = service.CanUndo("/path/to/file.cs");

        Assert.False(canUndo);
    }

    [Fact]
    public void CanUndo_NullPath_ReturnsFalse()
    {
        var service = CreateService();

        var canUndo = service.CanUndo(null!);

        Assert.False(canUndo);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetChangeHistory Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetChangeHistory_NoHistory_ReturnsEmptyList()
    {
        var service = CreateService();

        var history = service.GetChangeHistory("/path/to/file.cs");

        Assert.Empty(history);
    }

    [Fact]
    public void GetChangeHistory_NullPath_ReturnsEmptyList()
    {
        var service = CreateService();

        var history = service.GetChangeHistory(null!);

        Assert.Empty(history);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetPendingUndos Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetPendingUndos_NoHistory_ReturnsEmptyList()
    {
        var service = CreateService();

        var pending = service.GetPendingUndos();

        Assert.Empty(pending);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UndoLastChangeAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UndoLastChangeAsync_EmptyPath_ReturnsFalse()
    {
        var service = CreateService();

        var result = await service.UndoLastChangeAsync("");

        Assert.False(result);
    }

    [Fact]
    public async Task UndoLastChangeAsync_NoHistory_ReturnsFalse()
    {
        var service = CreateService();

        var result = await service.UndoLastChangeAsync("/path/to/file.cs");

        Assert.False(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CheckForConflictsAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CheckForConflictsAsync_NullBlock_ReturnsNoConflict()
    {
        var service = CreateService();

        var result = await service.CheckForConflictsAsync(null!, "/workspace");

        Assert.False(result.HasConflict);
    }

    [Fact]
    public async Task CheckForConflictsAsync_FileNotExists_ReturnsNoConflict()
    {
        var service = CreateService();
        var block = new CodeBlock { TargetFilePath = "test.cs" };

        _fileSystemMock
            .Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await service.CheckForConflictsAsync(block, "/workspace");

        Assert.False(result.HasConflict);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dispose Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var service = CreateService();

        service.Dispose();
        service.Dispose(); // Should not throw
    }
}
