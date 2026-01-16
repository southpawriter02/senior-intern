namespace AIntern.Core.Tests.Models;

using Xunit;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF LINE TESTS (v0.4.2a)                                                │
// │ Unit tests for the DiffLine model.                                       │
// └─────────────────────────────────────────────────────────────────────────┘

public class DiffLineTests
{
    [Theory]
    [InlineData(DiffLineType.Added, '+')]
    [InlineData(DiffLineType.Removed, '-')]
    [InlineData(DiffLineType.Modified, '~')]
    [InlineData(DiffLineType.Unchanged, ' ')]
    public void Prefix_ReturnsCorrectSymbol(DiffLineType type, char expected)
    {
        var line = new DiffLine { Type = type };
        Assert.Equal(expected, line.Prefix);
    }

    [Fact]
    public void GetLineNumber_Original_ReturnsOriginalLineNumber()
    {
        var line = new DiffLine
        {
            OriginalLineNumber = 42,
            ProposedLineNumber = 45
        };

        Assert.Equal(42, line.GetLineNumber(DiffSide.Original));
    }

    [Fact]
    public void GetLineNumber_Proposed_ReturnsProposedLineNumber()
    {
        var line = new DiffLine
        {
            OriginalLineNumber = 42,
            ProposedLineNumber = 45
        };

        Assert.Equal(45, line.GetLineNumber(DiffSide.Proposed));
    }

    [Fact]
    public void GetLineNumber_AddedLine_Original_ReturnsNull()
    {
        var line = DiffLine.Added(10, "new content");

        Assert.Null(line.GetLineNumber(DiffSide.Original));
        Assert.Equal(10, line.GetLineNumber(DiffSide.Proposed));
    }

    [Fact]
    public void GetLineNumber_RemovedLine_Proposed_ReturnsNull()
    {
        var line = DiffLine.Removed(10, "old content");

        Assert.Equal(10, line.GetLineNumber(DiffSide.Original));
        Assert.Null(line.GetLineNumber(DiffSide.Proposed));
    }

    [Fact]
    public void HasInlineChanges_WithChanges_ReturnsTrue()
    {
        var line = new DiffLine
        {
            InlineChanges = [InlineChange.Added(0, "new")]
        };

        Assert.True(line.HasInlineChanges);
    }

    [Fact]
    public void HasInlineChanges_WithEmptyList_ReturnsFalse()
    {
        var line = new DiffLine
        {
            InlineChanges = []
        };

        Assert.False(line.HasInlineChanges);
    }

    [Fact]
    public void HasInlineChanges_WithNull_ReturnsFalse()
    {
        var line = new DiffLine
        {
            InlineChanges = null
        };

        Assert.False(line.HasInlineChanges);
    }

    [Fact]
    public void ExistsOnOriginalSide_AddedLine_ReturnsFalse()
    {
        var line = DiffLine.Added(1, "content");
        Assert.False(line.ExistsOnOriginalSide);
    }

    [Fact]
    public void ExistsOnProposedSide_RemovedLine_ReturnsFalse()
    {
        var line = DiffLine.Removed(1, "content");
        Assert.False(line.ExistsOnProposedSide);
    }

    [Fact]
    public void ExistsOnOriginalSide_UnchangedLine_ReturnsTrue()
    {
        var line = DiffLine.Unchanged(1, 1, "content");
        Assert.True(line.ExistsOnOriginalSide);
    }

    [Fact]
    public void ExistsOnProposedSide_UnchangedLine_ReturnsTrue()
    {
        var line = DiffLine.Unchanged(1, 1, "content");
        Assert.True(line.ExistsOnProposedSide);
    }

    [Fact]
    public void StaticFactories_CreateCorrectTypes()
    {
        var unchanged = DiffLine.Unchanged(1, 1, "ctx");
        var added = DiffLine.Added(2, "new");
        var removed = DiffLine.Removed(3, "old");
        var modified = DiffLine.Modified(4, 5, "mod", []);

        Assert.Equal(DiffLineType.Unchanged, unchanged.Type);
        Assert.Equal(DiffLineType.Added, added.Type);
        Assert.Equal(DiffLineType.Removed, removed.Type);
        Assert.Equal(DiffLineType.Modified, modified.Type);
    }
}
