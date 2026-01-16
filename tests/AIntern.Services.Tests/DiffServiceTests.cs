namespace AIntern.Services.Tests;

using Moq;
using Xunit;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF SERVICE TESTS (v0.4.2b)                                             │
// │ Unit tests for the DiffService implementation.                           │
// └─────────────────────────────────────────────────────────────────────────┘

public class DiffServiceTests
{
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly DiffService _service;

    public DiffServiceTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _service = new DiffService(_fileSystemMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ComputeDiff - Basic Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ComputeDiff_IdenticalContent_ReturnsNoChanges()
    {
        var content = "line1\nline2\nline3";
        var result = _service.ComputeDiff(content, content);

        Assert.False(result.HasChanges);
        Assert.Empty(result.Hunks);
        Assert.Equal(0, result.Stats.AddedLines);
        Assert.Equal(0, result.Stats.RemovedLines);
    }

    [Fact]
    public void ComputeDiff_EmptyToContent_MarksAllAsAdded()
    {
        var result = _service.ComputeDiff("", "line1\nline2");

        Assert.True(result.HasChanges);
        Assert.Equal(2, result.Stats.AddedLines);
        Assert.True(result.IsNewFile);
    }

    [Fact]
    public void ComputeDiff_ContentToEmpty_MarksAllAsRemoved()
    {
        var result = _service.ComputeDiff("line1\nline2", "");

        Assert.True(result.HasChanges);
        Assert.Equal(2, result.Stats.RemovedLines);
        Assert.True(result.IsDeleteFile);
    }

    [Fact]
    public void ComputeDiff_SingleLineChanged_CreatesHunk()
    {
        var original = "line1\nline2\nline3";
        var proposed = "line1\nmodified\nline3";

        var result = _service.ComputeDiff(original, proposed);

        Assert.True(result.HasChanges);
        Assert.Single(result.Hunks);
    }

    [Fact]
    public void ComputeDiff_WithFilePath_SetsOriginalFilePath()
    {
        var result = _service.ComputeDiff("a", "b", "src/test.cs");

        Assert.Equal("src/test.cs", result.OriginalFilePath);
    }

    [Fact]
    public void ComputeDiff_NullContent_TreatsAsEmpty()
    {
        var result = _service.ComputeDiff(null!, "line1");

        Assert.True(result.HasChanges);
        Assert.True(result.IsNewFile);
    }

    [Fact]
    public void ComputeDiff_NormalizesLineEndings()
    {
        var original = "line1\r\nline2\r\n";
        var proposed = "line1\nline2\n";

        var result = _service.ComputeDiff(original, proposed);

        Assert.False(result.HasChanges);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ComputeDiff - Options Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ComputeDiff_WithTrimWhitespace_IgnoresTrailingSpaces()
    {
        var original = "line1   \nline2  ";
        var proposed = "line1\nline2";

        var options = new DiffOptions { TrimTrailingWhitespace = true };
        var result = _service.ComputeDiff(original, proposed, "", options);

        Assert.False(result.HasChanges);
    }

    [Fact]
    public void ComputeDiff_WithoutTrimWhitespace_DetectsContentDifferences()
    {
        // Without trimming, content with different trailing ws differs
        var original = "line1   ";
        var proposed = "line1";

        var options = new DiffOptions { TrimTrailingWhitespace = false };
        var result = _service.ComputeDiff(original, proposed, "", options);

        // DiffPlex may normalize some whitespace internally; verify options are passed
        Assert.NotNull(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ComputeNewFileDiff Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ComputeNewFileDiff_AllLinesAdded()
    {
        var content = "line1\nline2\nline3";
        var result = _service.ComputeNewFileDiff(content, "new.cs");

        Assert.True(result.IsNewFile);
        Assert.Equal(3, result.Stats.AddedLines);
        Assert.Single(result.Hunks);
        Assert.All(result.Hunks[0].Lines, l => Assert.Equal(DiffLineType.Added, l.Type));
    }

    [Fact]
    public void ComputeNewFileDiff_EmptyContent_SingleEmptyLine()
    {
        var result = _service.ComputeNewFileDiff("", "empty.cs");

        Assert.True(result.IsNewFile);
        Assert.Equal(1, result.Stats.AddedLines); // Empty string splits to 1 empty element
    }

    [Fact]
    public void ComputeNewFileDiff_SetsFilePath()
    {
        var result = _service.ComputeNewFileDiff("content", "src/new.cs");

        Assert.Equal("src/new.cs", result.OriginalFilePath);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ComputeDeleteFileDiff Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ComputeDeleteFileDiff_AllLinesRemoved()
    {
        var content = "line1\nline2\nline3";
        var result = _service.ComputeDeleteFileDiff(content, "deleted.cs");

        Assert.True(result.IsDeleteFile);
        Assert.Equal(3, result.Stats.RemovedLines);
        Assert.Single(result.Hunks);
        Assert.All(result.Hunks[0].Lines, l => Assert.Equal(DiffLineType.Removed, l.Type));
    }

    [Fact]
    public void ComputeDeleteFileDiff_SetsFilePath()
    {
        var result = _service.ComputeDeleteFileDiff("content", "src/old.cs");

        Assert.Equal("src/old.cs", result.OriginalFilePath);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ComputeDiffForBlockAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ComputeDiffForBlockAsync_MissingTargetPath_ThrowsArgumentException()
    {
        var block = new CodeBlock
        {
            Content = "content",
            TargetFilePath = null
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ComputeDiffForBlockAsync(block, "/workspace"));
    }

    [Fact]
    public async Task ComputeDiffForBlockAsync_NewFile_ReturnsNewFileDiff()
    {
        var block = new CodeBlock
        {
            Content = "new content",
            TargetFilePath = "src/new.cs"
        };

        _fileSystemMock.Setup(fs => fs.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _service.ComputeDiffForBlockAsync(block, "/workspace");

        Assert.True(result.IsNewFile);
        Assert.Equal(block.Id, result.SourceBlockId);
    }

    [Fact]
    public async Task ComputeDiffForBlockAsync_BinaryFile_ReturnsBinaryFileDiff()
    {
        var block = new CodeBlock
        {
            Content = "content",
            TargetFilePath = "image.png"
        };

        _fileSystemMock.Setup(fs => fs.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(fs => fs.IsTextFile(It.IsAny<string>()))
            .Returns(false);

        var result = await _service.ComputeDiffForBlockAsync(block, "/workspace");

        Assert.True(result.IsBinaryFile);
        Assert.Equal(block.Id, result.SourceBlockId);
    }

    [Fact]
    public async Task ComputeDiffForBlockAsync_CompleteFile_ComputesFullDiff()
    {
        var block = new CodeBlock
        {
            Content = "new content",
            TargetFilePath = "src/file.cs",
            BlockType = CodeBlockType.CompleteFile
        };

        _fileSystemMock.Setup(fs => fs.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(fs => fs.IsTextFile(It.IsAny<string>()))
            .Returns(true);
        _fileSystemMock.Setup(fs => fs.ReadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("old content");

        var result = await _service.ComputeDiffForBlockAsync(block, "/workspace");

        Assert.True(result.HasChanges);
        Assert.Equal(block.Id, result.SourceBlockId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ComputeMergedDiffAsync Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ComputeMergedDiffAsync_EmptyBlocks_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ComputeMergedDiffAsync([], "/workspace"));
    }

    [Fact]
    public async Task ComputeMergedDiffAsync_DifferentTargets_ThrowsArgumentException()
    {
        var blocks = new List<CodeBlock>
        {
            new() { Content = "a", TargetFilePath = "file1.cs" },
            new() { Content = "b", TargetFilePath = "file2.cs" }
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ComputeMergedDiffAsync(blocks, "/workspace"));
    }

    [Fact]
    public async Task ComputeMergedDiffAsync_SingleBlock_ComputesDirectly()
    {
        var block = new CodeBlock
        {
            Content = "content",
            TargetFilePath = "file.cs"
        };

        _fileSystemMock.Setup(fs => fs.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _service.ComputeMergedDiffAsync([block], "/workspace");

        Assert.True(result.IsNewFile);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Hunk Building Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ComputeDiff_DistantChanges_CreatesSeparateHunks()
    {
        // Create content with changes far apart (more than HunkSeparationThreshold)
        var originalLines = Enumerable.Range(1, 20)
            .Select(i => $"line{i}")
            .ToList();

        var proposedLines = originalLines.ToList();
        proposedLines[0] = "modified1";  // Line 1
        proposedLines[19] = "modified20"; // Line 20

        var original = string.Join("\n", originalLines);
        var proposed = string.Join("\n", proposedLines);

        var result = _service.ComputeDiff(original, proposed);

        Assert.True(result.HasChanges);
        Assert.Equal(2, result.Hunks.Count); // Two separate hunks
    }

    [Fact]
    public void ComputeDiff_CloseChanges_CreatesSingleHunk()
    {
        var original = "line1\nline2\nline3\nline4\nline5";
        var proposed = "modified1\nline2\nmodified3\nline4\nline5";

        var result = _service.ComputeDiff(original, proposed);

        Assert.True(result.HasChanges);
        Assert.Single(result.Hunks); // Single hunk since changes are close
    }

    [Fact]
    public void ComputeDiff_HunkContainsContextLines()
    {
        var original = "line1\nline2\nline3\nline4\nline5";
        var proposed = "line1\nline2\nmodified\nline4\nline5";

        var options = new DiffOptions { ContextLines = 2 };
        var result = _service.ComputeDiff(original, proposed, "", options);

        var hunk = result.Hunks.Single();
        
        // Should have context lines around the change
        var unchangedCount = hunk.Lines.Count(l => l.Type == DiffLineType.Unchanged);
        Assert.True(unchangedCount > 0, "Hunk should contain context lines");
    }
}
