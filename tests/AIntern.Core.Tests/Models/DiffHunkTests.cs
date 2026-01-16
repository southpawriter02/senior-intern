namespace AIntern.Core.Tests.Models;

using Xunit;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF HUNK TESTS (v0.4.2a)                                                │
// │ Unit tests for the DiffHunk model.                                       │
// └─────────────────────────────────────────────────────────────────────────┘

public class DiffHunkTests
{
    [Fact]
    public void Header_FormatsCorrectly()
    {
        var hunk = new DiffHunk
        {
            OriginalStartLine = 10,
            OriginalLineCount = 5,
            ProposedStartLine = 10,
            ProposedLineCount = 7
        };

        Assert.Equal("@@ -10,5 +10,7 @@", hunk.Header);
    }

    [Fact]
    public void Header_SingleLine_FormatsCorrectly()
    {
        var hunk = new DiffHunk
        {
            OriginalStartLine = 1,
            OriginalLineCount = 1,
            ProposedStartLine = 1,
            ProposedLineCount = 1
        };

        Assert.Equal("@@ -1,1 +1,1 @@", hunk.Header);
    }

    [Fact]
    public void FullHeader_WithContext_IncludesContext()
    {
        var hunk = new DiffHunk
        {
            OriginalStartLine = 10,
            OriginalLineCount = 5,
            ProposedStartLine = 10,
            ProposedLineCount = 7,
            ContextHeader = "public void ProcessData()"
        };

        Assert.Equal("@@ -10,5 +10,7 @@ public void ProcessData()", hunk.FullHeader);
    }

    [Fact]
    public void FullHeader_WithoutContext_ReturnsHeaderOnly()
    {
        var hunk = new DiffHunk
        {
            OriginalStartLine = 10,
            OriginalLineCount = 5,
            ProposedStartLine = 10,
            ProposedLineCount = 7,
            ContextHeader = null
        };

        Assert.Equal("@@ -10,5 +10,7 @@", hunk.FullHeader);
    }

    [Fact]
    public void AddedLines_FiltersCorrectly()
    {
        var hunk = new DiffHunk
        {
            Lines =
            [
                DiffLine.Unchanged(1, 1, "context"),
                DiffLine.Added(2, "new line 1"),
                DiffLine.Added(3, "new line 2"),
                DiffLine.Removed(2, "old line"),
                DiffLine.Unchanged(3, 4, "more context")
            ]
        };

        var added = hunk.AddedLines.ToList();
        Assert.Equal(2, added.Count);
        Assert.All(added, l => Assert.Equal(DiffLineType.Added, l.Type));
    }

    [Fact]
    public void IsInsertOnly_WithOnlyAdditions_ReturnsTrue()
    {
        var hunk = new DiffHunk
        {
            Lines =
            [
                DiffLine.Unchanged(1, 1, "context"),
                DiffLine.Added(2, "new line"),
                DiffLine.Unchanged(2, 3, "more context")
            ]
        };

        Assert.True(hunk.IsInsertOnly);
    }

    [Fact]
    public void IsInsertOnly_WithRemovals_ReturnsFalse()
    {
        var hunk = new DiffHunk
        {
            Lines =
            [
                DiffLine.Unchanged(1, 1, "context"),
                DiffLine.Removed(2, "old line"),
                DiffLine.Unchanged(3, 2, "more context")
            ]
        };

        Assert.False(hunk.IsInsertOnly);
    }

    [Fact]
    public void IsDeleteOnly_WithOnlyRemovals_ReturnsTrue()
    {
        var hunk = new DiffHunk
        {
            Lines =
            [
                DiffLine.Unchanged(1, 1, "context"),
                DiffLine.Removed(2, "deleted line"),
                DiffLine.Unchanged(3, 2, "more context")
            ]
        };

        Assert.True(hunk.IsDeleteOnly);
    }

    [Fact]
    public void Counts_CalculateCorrectly()
    {
        var hunk = new DiffHunk
        {
            Lines =
            [
                DiffLine.Unchanged(1, 1, "context"),
                DiffLine.Added(2, "new 1"),
                DiffLine.Added(3, "new 2"),
                DiffLine.Removed(2, "old"),
                DiffLine.Modified(3, 4, "mod", null)
            ]
        };

        Assert.Equal(2, hunk.AddedCount);
        Assert.Equal(1, hunk.RemovedCount);
        Assert.Equal(1, hunk.ModifiedCount);
    }
}
