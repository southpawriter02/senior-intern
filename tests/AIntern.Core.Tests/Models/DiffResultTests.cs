namespace AIntern.Core.Tests.Models;

using Xunit;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF RESULT TESTS (v0.4.2a)                                              │
// │ Unit tests for the DiffResult model.                                     │
// └─────────────────────────────────────────────────────────────────────────┘

public class DiffResultTests
{
    [Fact]
    public void HasChanges_WithAddedLines_ReturnsTrue()
    {
        var result = new DiffResult
        {
            Stats = DiffStats.FromCounts(added: 5, removed: 0, modified: 0, unchanged: 10),
            Hunks = [new DiffHunk()]
        };

        Assert.True(result.HasChanges);
    }

    [Fact]
    public void HasChanges_WithRemovedLines_ReturnsTrue()
    {
        var result = new DiffResult
        {
            Stats = DiffStats.FromCounts(added: 0, removed: 3, modified: 0, unchanged: 10),
            Hunks = [new DiffHunk()]
        };

        Assert.True(result.HasChanges);
    }

    [Fact]
    public void HasChanges_WithModifiedLines_ReturnsTrue()
    {
        var result = new DiffResult
        {
            Stats = DiffStats.FromCounts(added: 0, removed: 0, modified: 2, unchanged: 10),
            Hunks = [new DiffHunk()]
        };

        Assert.True(result.HasChanges);
    }

    [Fact]
    public void HasChanges_WithNoChanges_ReturnsFalse()
    {
        var result = new DiffResult
        {
            Stats = DiffStats.Empty,
            Hunks = []
        };

        Assert.False(result.HasChanges);
    }

    [Fact]
    public void HasChanges_WithEmptyHunks_ReturnsFalse()
    {
        var result = new DiffResult
        {
            Stats = DiffStats.FromCounts(added: 5, removed: 0, modified: 0, unchanged: 0),
            Hunks = [] // No hunks even though stats say there are adds
        };

        Assert.False(result.HasChanges);
    }

    [Fact]
    public void IsApplicable_BinaryFile_ReturnsFalse()
    {
        var result = DiffResult.BinaryFile("image.png");

        Assert.False(result.IsApplicable);
    }

    [Fact]
    public void IsApplicable_NewFile_ReturnsTrue()
    {
        var result = DiffResult.NewFile(
            "src/NewFile.cs",
            "public class NewFile { }",
            [new DiffHunk()],
            DiffStats.FromCounts(added: 1, removed: 0, modified: 0, unchanged: 0));

        Assert.True(result.IsApplicable);
    }

    [Fact]
    public void FileName_WithPath_ReturnsFileName()
    {
        var result = new DiffResult { OriginalFilePath = "src/Services/MyService.cs" };

        Assert.Equal("MyService.cs", result.FileName);
    }

    [Fact]
    public void FileName_WithEmptyPath_ReturnsUntitled()
    {
        var result = new DiffResult { OriginalFilePath = "" };

        Assert.Equal("(untitled)", result.FileName);
    }

    [Fact]
    public void NoChanges_CreatesIdenticalResult()
    {
        var content = "line1\nline2";
        var result = DiffResult.NoChanges("file.txt", content);

        Assert.Equal(content, result.OriginalContent);
        Assert.Equal(content, result.ProposedContent);
        Assert.Empty(result.Hunks);
        Assert.False(result.HasChanges);
    }

    [Fact]
    public void NewFile_SetsIsNewFileFlag()
    {
        var result = DiffResult.NewFile(
            "new.cs",
            "content",
            [],
            DiffStats.Empty);

        Assert.True(result.IsNewFile);
        Assert.False(result.IsDeleteFile);
        Assert.False(result.IsBinaryFile);
    }

    [Fact]
    public void DeleteFile_SetsIsDeleteFileFlag()
    {
        var result = DiffResult.DeleteFile(
            "deleted.cs",
            "old content",
            [],
            DiffStats.Empty);

        Assert.False(result.IsNewFile);
        Assert.True(result.IsDeleteFile);
        Assert.False(result.IsBinaryFile);
    }

    [Fact]
    public void BinaryFile_SetsIsBinaryFileFlag()
    {
        var result = DiffResult.BinaryFile("image.png");

        Assert.False(result.IsNewFile);
        Assert.False(result.IsDeleteFile);
        Assert.True(result.IsBinaryFile);
    }

    [Fact]
    public void HunkCount_ReturnsCorrectCount()
    {
        var result = new DiffResult
        {
            Hunks = [new DiffHunk(), new DiffHunk(), new DiffHunk()]
        };

        Assert.Equal(3, result.HunkCount);
    }
}
