using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalSelection"/>.
/// </summary>
public sealed class TerminalSelectionTests
{
    [Fact]
    public void Normalized_WhenAlreadyNormal_ReturnsSame()
    {
        // Arrange
        var selection = new TerminalSelection
        {
            StartLine = 0,
            StartColumn = 0,
            EndLine = 5,
            EndColumn = 10
        };

        // Act
        var normalized = selection.Normalized;

        // Assert
        Assert.Equal(0, normalized.StartLine);
        Assert.Equal(0, normalized.StartColumn);
        Assert.Equal(5, normalized.EndLine);
        Assert.Equal(10, normalized.EndColumn);
    }

    [Fact]
    public void Normalized_WhenReversed_SwapsStartAndEnd()
    {
        // Arrange
        var selection = new TerminalSelection
        {
            StartLine = 5,
            StartColumn = 10,
            EndLine = 0,
            EndColumn = 0
        };

        // Act
        var normalized = selection.Normalized;

        // Assert
        Assert.Equal(0, normalized.StartLine);
        Assert.Equal(0, normalized.StartColumn);
        Assert.Equal(5, normalized.EndLine);
        Assert.Equal(10, normalized.EndColumn);
    }

    [Fact]
    public void IsEmpty_WhenStartEqualsEnd_ReturnsTrue()
    {
        // Arrange
        var selection = new TerminalSelection
        {
            StartLine = 5,
            StartColumn = 10,
            EndLine = 5,
            EndColumn = 10
        };

        // Act & Assert
        Assert.True(selection.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WhenDifferent_ReturnsFalse()
    {
        // Arrange
        var selection = new TerminalSelection
        {
            StartLine = 0,
            StartColumn = 0,
            EndLine = 0,
            EndColumn = 1
        };

        // Act & Assert
        Assert.False(selection.IsEmpty);
    }

    [Fact]
    public void LineCount_ReturnsCorrectCount()
    {
        // Arrange
        var selection = new TerminalSelection
        {
            StartLine = 2,
            StartColumn = 0,
            EndLine = 5,
            EndColumn = 10
        };

        // Act & Assert
        Assert.Equal(4, selection.LineCount);
    }

    [Fact]
    public void Contains_LineSelection_SingleLine()
    {
        // Arrange
        var selection = new TerminalSelection
        {
            StartLine = 5,
            StartColumn = 10,
            EndLine = 5,
            EndColumn = 20,
            IsBlock = false
        };

        // Act & Assert
        Assert.True(selection.Contains(5, 10));
        Assert.True(selection.Contains(5, 15));
        Assert.True(selection.Contains(5, 20));
        Assert.False(selection.Contains(5, 9));
        Assert.False(selection.Contains(5, 21));
        Assert.False(selection.Contains(4, 15));
    }

    [Fact]
    public void Contains_LineSelection_MultiLine()
    {
        // Arrange
        var selection = new TerminalSelection
        {
            StartLine = 2,
            StartColumn = 10,
            EndLine = 4,
            EndColumn = 5,
            IsBlock = false
        };

        // Act & Assert
        // First line: after start column
        Assert.True(selection.Contains(2, 10));
        Assert.True(selection.Contains(2, 50));
        Assert.False(selection.Contains(2, 9));

        // Middle line: fully selected
        Assert.True(selection.Contains(3, 0));
        Assert.True(selection.Contains(3, 50));

        // Last line: before end column
        Assert.True(selection.Contains(4, 0));
        Assert.True(selection.Contains(4, 5));
        Assert.False(selection.Contains(4, 6));
    }

    [Fact]
    public void Contains_BlockSelection_ChecksRectangle()
    {
        // Arrange
        var selection = new TerminalSelection
        {
            StartLine = 2,
            StartColumn = 10,
            EndLine = 4,
            EndColumn = 20,
            IsBlock = true
        };

        // Act & Assert
        Assert.True(selection.Contains(2, 10));
        Assert.True(selection.Contains(3, 15));
        Assert.True(selection.Contains(4, 20));
        Assert.False(selection.Contains(3, 9));
        Assert.False(selection.Contains(3, 21));
        Assert.False(selection.Contains(1, 15));
        Assert.False(selection.Contains(5, 15));
    }

    [Fact]
    public void Union_CombinesSelections()
    {
        // Arrange
        var sel1 = new TerminalSelection
        {
            StartLine = 2,
            StartColumn = 5,
            EndLine = 3,
            EndColumn = 10
        };
        var sel2 = new TerminalSelection
        {
            StartLine = 1,
            StartColumn = 0,
            EndLine = 2,
            EndColumn = 8
        };

        // Act
        var union = sel1.Union(sel2);

        // Assert
        Assert.Equal(1, union.StartLine);
        Assert.Equal(0, union.StartColumn);
        Assert.Equal(3, union.EndLine);
        Assert.Equal(10, union.EndColumn);
        Assert.False(union.IsBlock);
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        // Arrange
        var sel1 = new TerminalSelection
        {
            StartLine = 0,
            StartColumn = 0,
            EndLine = 10,
            EndColumn = 5,
            IsBlock = false
        };
        var sel2 = new TerminalSelection
        {
            StartLine = 0,
            StartColumn = 0,
            EndLine = 10,
            EndColumn = 5,
            IsBlock = false
        };
        var sel3 = new TerminalSelection
        {
            StartLine = 0,
            StartColumn = 0,
            EndLine = 10,
            EndColumn = 5,
            IsBlock = true
        };

        // Act & Assert
        Assert.Equal(sel1, sel2);
        Assert.NotEqual(sel1, sel3);
    }
}
