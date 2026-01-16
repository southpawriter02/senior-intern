namespace AIntern.Core.Tests.Models;

using Xunit;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ INLINE CHANGE TESTS (v0.4.2a)                                            │
// │ Unit tests for the InlineChange model.                                   │
// └─────────────────────────────────────────────────────────────────────────┘

public class InlineChangeTests
{
    [Fact]
    public void EndColumn_CalculatesCorrectly()
    {
        var change = new InlineChange
        {
            StartColumn = 10,
            Length = 5,
            Text = "hello"
        };

        Assert.Equal(15, change.EndColumn);
    }

    [Fact]
    public void IsChange_Added_ReturnsTrue()
    {
        var change = InlineChange.Added(0, "new");
        Assert.True(change.IsChange);
    }

    [Fact]
    public void IsChange_Removed_ReturnsTrue()
    {
        var change = InlineChange.Removed(0, "old");
        Assert.True(change.IsChange);
    }

    [Fact]
    public void IsChange_Unchanged_ReturnsFalse()
    {
        var change = InlineChange.Unchanged(0, "same");
        Assert.False(change.IsChange);
    }

    [Fact]
    public void StaticFactories_SetLengthFromText()
    {
        var added = InlineChange.Added(5, "hello");
        var removed = InlineChange.Removed(10, "world");
        var unchanged = InlineChange.Unchanged(0, "test");

        Assert.Equal(5, added.Length);
        Assert.Equal(5, removed.Length);
        Assert.Equal(4, unchanged.Length);
    }

    [Fact]
    public void StaticFactories_SetCorrectTypes()
    {
        var added = InlineChange.Added(0, "x");
        var removed = InlineChange.Removed(0, "y");
        var unchanged = InlineChange.Unchanged(0, "z");

        Assert.Equal(InlineChangeType.Added, added.Type);
        Assert.Equal(InlineChangeType.Removed, removed.Type);
        Assert.Equal(InlineChangeType.Unchanged, unchanged.Type);
    }
}
