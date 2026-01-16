namespace AIntern.Core.Tests.Models;

using Xunit;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF STATS TESTS (v0.4.2a)                                               │
// │ Unit tests for the DiffStats record.                                     │
// └─────────────────────────────────────────────────────────────────────────┘

public class DiffStatsTests
{
    [Fact]
    public void NetChange_CalculatesCorrectly()
    {
        var stats = new DiffStats { AddedLines = 10, RemovedLines = 3 };
        Assert.Equal(7, stats.NetChange);
    }

    [Fact]
    public void NetChange_Negative_WhenMoreRemoved()
    {
        var stats = new DiffStats { AddedLines = 2, RemovedLines = 8 };
        Assert.Equal(-6, stats.NetChange);
    }

    [Fact]
    public void ChangePercentage_CalculatesCorrectly()
    {
        var stats = DiffStats.FromCounts(added: 2, removed: 2, modified: 1, unchanged: 5);
        // 5 changed out of 10 total = 50%
        Assert.Equal(50.0, stats.ChangePercentage, precision: 1);
    }

    [Fact]
    public void ChangePercentage_ZeroTotal_ReturnsZero()
    {
        var stats = DiffStats.Empty;
        Assert.Equal(0.0, stats.ChangePercentage);
    }

    [Fact]
    public void Summary_AdditionsOnly()
    {
        var stats = new DiffStats { AddedLines = 5 };
        Assert.Equal("+5", stats.Summary);
    }

    [Fact]
    public void Summary_RemovalsOnly()
    {
        var stats = new DiffStats { RemovedLines = 3 };
        Assert.Equal("-3", stats.Summary);
    }

    [Fact]
    public void Summary_Mixed()
    {
        var stats = new DiffStats { AddedLines = 5, RemovedLines = 2 };
        Assert.Equal("+5 -2", stats.Summary);
    }

    [Fact]
    public void Summary_WithModifications()
    {
        var stats = new DiffStats { AddedLines = 5, RemovedLines = 2, ModifiedLines = 1 };
        Assert.Equal("+5 -2 ~1", stats.Summary);
    }

    [Fact]
    public void Summary_NoChanges()
    {
        var stats = DiffStats.Empty;
        Assert.Equal("+0 -0", stats.Summary);
    }

    [Fact]
    public void VerboseSummary_Singular()
    {
        var stats = new DiffStats { AddedLines = 1, RemovedLines = 1, ModifiedLines = 1 };
        Assert.Equal("1 addition, 1 deletion, 1 modification", stats.VerboseSummary);
    }

    [Fact]
    public void VerboseSummary_Plural()
    {
        var stats = new DiffStats { AddedLines = 5, RemovedLines = 2 };
        Assert.Equal("5 additions, 2 deletions", stats.VerboseSummary);
    }

    [Fact]
    public void VerboseSummary_NoChanges()
    {
        var stats = DiffStats.Empty;
        Assert.Equal("No changes", stats.VerboseSummary);
    }

    [Fact]
    public void HasChanges_WithChanges_ReturnsTrue()
    {
        var stats = new DiffStats { AddedLines = 1 };
        Assert.True(stats.HasChanges);
    }

    [Fact]
    public void HasChanges_Empty_ReturnsFalse()
    {
        Assert.False(DiffStats.Empty.HasChanges);
    }

    [Fact]
    public void FromCounts_CalculatesTotalLines()
    {
        var stats = DiffStats.FromCounts(added: 5, removed: 3, modified: 2, unchanged: 10);
        Assert.Equal(20, stats.TotalLines);
    }

    [Fact]
    public void Empty_IsDefault()
    {
        var empty = DiffStats.Empty;
        Assert.Equal(0, empty.TotalLines);
        Assert.Equal(0, empty.AddedLines);
        Assert.Equal(0, empty.RemovedLines);
        Assert.Equal(0, empty.ModifiedLines);
        Assert.Equal(0, empty.UnchangedLines);
    }

    [Fact]
    public void RecordEquality_Works()
    {
        var stats1 = DiffStats.FromCounts(1, 2, 3, 4);
        var stats2 = DiffStats.FromCounts(1, 2, 3, 4);
        var stats3 = DiffStats.FromCounts(1, 2, 3, 5);

        Assert.Equal(stats1, stats2);
        Assert.NotEqual(stats1, stats3);
    }

    [Fact]
    public void ChangedLines_CalculatesSum()
    {
        var stats = new DiffStats { AddedLines = 3, RemovedLines = 2, ModifiedLines = 1 };
        Assert.Equal(6, stats.ChangedLines);
    }
}
