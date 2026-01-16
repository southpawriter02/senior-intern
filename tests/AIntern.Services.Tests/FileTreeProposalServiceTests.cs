namespace AIntern.Services.Tests;

using Moq;
using Xunit;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE TREE PROPOSAL SERVICE TESTS (v0.4.4c)                               │
// │ Unit tests for the FileTreeProposalService implementation.               │
// └─────────────────────────────────────────────────────────────────────────┘

public class FileTreeProposalServiceTests
{
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<IFileChangeService> _changeServiceMock;
    private readonly Mock<IDiffService> _diffServiceMock;
    private readonly Mock<IBackupService> _backupServiceMock;
    private readonly FileTreeProposalService _service;

    public FileTreeProposalServiceTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _changeServiceMock = new Mock<IFileChangeService>();
        _diffServiceMock = new Mock<IDiffService>();
        _backupServiceMock = new Mock<IBackupService>();

        // Use options that disable directory creation to avoid hitting real filesystem
        var options = ProposalServiceOptions.Default with
        {
            CreateParentDirectories = false,
            ValidateBeforeApply = false
        };

        _service = new FileTreeProposalService(
            _fileSystemMock.Object,
            _changeServiceMock.Object,
            _diffServiceMock.Object,
            _backupServiceMock.Object,
            options);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsWithinWorkspace Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsWithinWorkspace_ValidPath_ReturnsTrue()
    {
        var result = _service.IsWithinWorkspace("src/Models/User.cs", "/workspace");
        Assert.True(result);
    }

    [Fact]
    public void IsWithinWorkspace_RelativePath_ReturnsTrue()
    {
        var result = _service.IsWithinWorkspace("nested/deep/file.txt", "/workspace");
        Assert.True(result);
    }

    [Fact]
    public void IsWithinWorkspace_ParentTraversal_ReturnsFalse()
    {
        var result = _service.IsWithinWorkspace("../outside.txt", "/workspace/project");
        Assert.False(result);
    }

    [Fact]
    public void IsWithinWorkspace_EmptyPath_ReturnsFalse()
    {
        var result = _service.IsWithinWorkspace("", "/workspace");
        Assert.False(result);
    }

    [Fact]
    public void IsWithinWorkspace_NullPath_ReturnsFalse()
    {
        var result = _service.IsWithinWorkspace(null!, "/workspace");
        Assert.False(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ValidateOperationAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ValidateOperationAsync_EmptyPath_ReturnsError()
    {
        var operation = new FileOperation { Path = "", Type = FileOperationType.Create };

        var issues = await _service.ValidateOperationAsync(operation, "/workspace");

        Assert.Single(issues);
        Assert.Equal(ValidationIssueType.InvalidPath, issues[0].Type);
    }

    [Fact]
    public async Task ValidateOperationAsync_ValidNewFile_ReturnsEmpty()
    {
        var operation = new FileOperation
        {
            Path = "NewFile.cs", // No subdirectory to avoid parent check
            Type = FileOperationType.Create,
            Content = "// Content"
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var issues = await _service.ValidateOperationAsync(operation, "/tmp");

        // Should have no errors (may have warnings)
        Assert.DoesNotContain(issues, i => i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public async Task ValidateOperationAsync_CreateExistingFile_ReturnsWarning()
    {
        var operation = new FileOperation
        {
            Path = "Existing.cs", // No subdirectory
            Type = FileOperationType.Create,
            Content = "// New content"
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var issues = await _service.ValidateOperationAsync(operation, "/tmp");

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Warning && i.Type == ValidationIssueType.FileExists);
    }

    [Fact]
    public async Task ValidateOperationAsync_ModifyNonExistentFile_ReturnsWarning()
    {
        var operation = new FileOperation
        {
            Path = "src/Missing.cs",
            Type = FileOperationType.Modify,
            Content = "// Modified"
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var issues = await _service.ValidateOperationAsync(operation, "/workspace");

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public async Task ValidateOperationAsync_DeleteNonExistentFile_ReturnsWarning()
    {
        var operation = new FileOperation
        {
            Path = "src/Missing.cs",
            Type = FileOperationType.Delete
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var issues = await _service.ValidateOperationAsync(operation, "/workspace");

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public async Task ValidateOperationAsync_EmptyContent_ReturnsWarning()
    {
        var operation = new FileOperation
        {
            Path = "src/Empty.cs",
            Type = FileOperationType.Create,
            Content = ""
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var issues = await _service.ValidateOperationAsync(operation, "/workspace");

        Assert.Contains(issues, i => i.Type == ValidationIssueType.EmptyContent);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ValidateProposalAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ValidateProposalAsync_ValidProposal_ReturnsValid()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new List<FileOperation>
            {
                new() { Path = "File1.cs", Type = FileOperationType.Create, Content = "// F1" },
                new() { Path = "File2.cs", Type = FileOperationType.Create, Content = "// F2" }
            }
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _service.ValidateProposalAsync(proposal, "/tmp");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateProposalAsync_DuplicatePaths_ReturnsInvalid()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new List<FileOperation>
            {
                new() { Path = "src/Same.cs", Type = FileOperationType.Create, Content = "// 1" },
                new() { Path = "src/same.cs", Type = FileOperationType.Create, Content = "// 2" }
            }
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _service.ValidateProposalAsync(proposal, "/workspace");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Type == ValidationIssueType.DuplicatePath);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ApplyOperationAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ApplyOperationAsync_CreateNewFile_Succeeds()
    {
        var operation = new FileOperation
        {
            Path = "src/NewFile.cs",
            Type = FileOperationType.Create,
            Content = "// New file content"
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _fileSystemMock.Setup(f => f.WriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ApplyOperationAsync(operation, "/workspace");

        Assert.True(result.Success);
        _fileSystemMock.Verify(f => f.WriteFileAsync(
            It.Is<string>(p => p.Contains("NewFile.cs")),
            It.Is<string>(c => c == "// New file content"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyOperationAsync_DeleteExistingFile_Succeeds()
    {
        var operation = new FileOperation
        {
            Path = "src/OldFile.cs",
            Type = FileOperationType.Delete
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(f => f.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _backupServiceMock.Setup(b => b.CreateBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/backups/backup.bak");

        var result = await _service.ApplyOperationAsync(
            operation,
            "/workspace",
            new ApplyOptions { CreateBackup = true });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ApplyOperationAsync_DeleteNonExistentFile_Succeeds()
    {
        var operation = new FileOperation
        {
            Path = "src/Missing.cs",
            Type = FileOperationType.Delete
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _service.ApplyOperationAsync(operation, "/workspace");

        Assert.True(result.Success);
        _fileSystemMock.Verify(f => f.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ApplyProposalAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ApplyProposalAsync_AllSucceed_ReturnsFullSuccess()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new List<FileOperation>
            {
                new() { Path = "src/File1.cs", Type = FileOperationType.Create, Content = "// 1", IsSelected = true },
                new() { Path = "src/File2.cs", Type = FileOperationType.Create, Content = "// 2", IsSelected = true }
            }
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _fileSystemMock.Setup(f => f.WriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ApplyProposalAsync(proposal, "/workspace");

        Assert.True(result.AllSucceeded);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task ApplyProposalAsync_OnlySelectedOperations_Applied()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new List<FileOperation>
            {
                new() { Path = "src/File1.cs", Type = FileOperationType.Create, Content = "// 1", IsSelected = true },
                new() { Path = "src/File2.cs", Type = FileOperationType.Create, Content = "// 2", IsSelected = false }
            }
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _fileSystemMock.Setup(f => f.WriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.ApplyProposalAsync(proposal, "/workspace");

        Assert.Equal(1, result.SuccessCount);
        _fileSystemMock.Verify(f => f.WriteFileAsync(
            It.Is<string>(p => p.Contains("File1.cs")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _fileSystemMock.Verify(f => f.WriteFileAsync(
            It.Is<string>(p => p.Contains("File2.cs")),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EstimateApplyTime Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void EstimateApplyTime_SmallFiles_ReturnsReasonableEstimate()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new List<FileOperation>
            {
                new() { Path = "f1.cs", Type = FileOperationType.Create, Content = "short" },
                new() { Path = "f2.cs", Type = FileOperationType.Create, Content = "short" }
            }
        };

        var estimate = _service.EstimateApplyTime(proposal);

        Assert.True(estimate.TotalMilliseconds > 0);
        Assert.True(estimate.TotalSeconds < 10); // Should be fast for small files
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CheckExistingFilesAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CheckExistingFilesAsync_MixedExistence_ReturnsCorrectMap()
    {
        var proposal = new FileTreeProposal
        {
            Operations = new List<FileOperation>
            {
                new() { Path = "existing.cs", Type = FileOperationType.Modify },
                new() { Path = "new.cs", Type = FileOperationType.Create }
            }
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.Is<string>(p => p.Contains("existing"))))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(f => f.FileExistsAsync(It.Is<string>(p => p.Contains("new"))))
            .ReturnsAsync(false);

        var result = await _service.CheckExistingFilesAsync(proposal, "/workspace");

        Assert.Equal(2, result.Count);
        Assert.True(result["existing.cs"]);
        Assert.False(result["new.cs"]);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Event Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ValidationCompleted_Event_Raised()
    {
        var raised = false;
        var proposal = new FileTreeProposal
        {
            Operations = new List<FileOperation>
            {
                new() { Path = "file.cs", Type = FileOperationType.Create, Content = "x" }
            }
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        _service.ValidationCompleted += (_, _) => raised = true;

        await _service.ValidateProposalAsync(proposal, "/workspace");

        Assert.True(raised);
    }

    [Fact]
    public async Task OperationCompleted_Event_RaisedForEachOperation()
    {
        var count = 0;
        var proposal = new FileTreeProposal
        {
            Operations = new List<FileOperation>
            {
                new() { Path = "f1.cs", Type = FileOperationType.Create, Content = "1", IsSelected = true },
                new() { Path = "f2.cs", Type = FileOperationType.Create, Content = "2", IsSelected = true }
            }
        };

        _fileSystemMock.Setup(f => f.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _fileSystemMock.Setup(f => f.WriteFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service.OperationCompleted += (_, _) => count++;

        await _service.ApplyProposalAsync(proposal, "/workspace");

        Assert.Equal(2, count);
    }
}
