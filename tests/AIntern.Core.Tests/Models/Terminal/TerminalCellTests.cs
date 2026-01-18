using System.Text;
using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalCell"/>.
/// </summary>
public sealed class TerminalCellTests
{
    [Fact]
    public void Empty_HasSpaceAndDefaultAttributes()
    {
        // Act
        var cell = TerminalCell.Empty;

        // Assert
        Assert.Equal(' ', cell.Character.Value);
        Assert.Equal(TerminalAttributes.Default, cell.Attributes);
        Assert.Equal(1, cell.Width);
        Assert.False(cell.IsContinuation);
    }

    [Fact]
    public void FromChar_SetsCharacterWithDefaultAttributes()
    {
        // Act
        var cell = TerminalCell.FromChar('A');

        // Assert
        Assert.Equal('A', cell.Character.Value);
        Assert.Equal(TerminalAttributes.Default, cell.Attributes);
        Assert.Equal(1, cell.Width);
    }

    [Fact]
    public void FromChar_WithAttributes_SetsCharacterAndAttributes()
    {
        // Arrange
        var attrs = TerminalAttributes.Default.With(bold: true);

        // Act
        var cell = TerminalCell.FromChar('B', attrs);

        // Assert
        Assert.Equal('B', cell.Character.Value);
        Assert.True(cell.Attributes.Bold);
    }

    [Fact]
    public void FromRune_SetsRuneAndWidth()
    {
        // Arrange
        var rune = new Rune('X');
        var attrs = TerminalAttributes.Default;

        // Act
        var cell = TerminalCell.FromRune(rune, attrs, width: 2);

        // Assert
        Assert.Equal(rune, cell.Character);
        Assert.Equal(2, cell.Width);
    }

    [Theory]
    [InlineData(' ', false, true)]
    [InlineData('\0', false, true)]
    [InlineData('A', false, false)]
    [InlineData('A', true, true)]
    public void IsBlank_ReturnsExpectedValue(char c, bool isContinuation, bool expected)
    {
        // Arrange
        var cell = new TerminalCell
        {
            Character = new Rune(c),
            IsContinuation = isContinuation
        };

        // Act & Assert
        Assert.Equal(expected, cell.IsBlank);
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        // Arrange
        var cell1 = TerminalCell.FromChar('A');
        var cell2 = TerminalCell.FromChar('A');
        var cell3 = TerminalCell.FromChar('B');

        // Act & Assert
        Assert.True(cell1.Equals(cell2));
        Assert.True(cell1 == cell2);
        Assert.True(cell1 != cell3);
    }

    [Fact]
    public void GetHashCode_IsSameForEqualCells()
    {
        // Arrange
        var cell1 = TerminalCell.FromChar('X');
        var cell2 = TerminalCell.FromChar('X');

        // Act & Assert
        Assert.Equal(cell1.GetHashCode(), cell2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsCharacterString()
    {
        // Arrange
        var cell = TerminalCell.FromChar('Q');

        // Act & Assert
        Assert.Equal("Q", cell.ToString());
    }

    [Fact]
    public void ToString_Continuation_ReturnsMarker()
    {
        // Arrange
        var cell = new TerminalCell { IsContinuation = true };

        // Act & Assert
        Assert.Equal("[continuation]", cell.ToString());
    }

    [Fact]
    public void IsMutable_CanUpdateProperties()
    {
        // Arrange
        var cell = TerminalCell.Empty;

        // Act
        cell.Character = new Rune('Z');
        cell.Attributes = TerminalAttributes.Default.With(italic: true);
        cell.Width = 2;
        cell.IsContinuation = true;

        // Assert
        Assert.Equal('Z', cell.Character.Value);
        Assert.True(cell.Attributes.Italic);
        Assert.Equal(2, cell.Width);
        Assert.True(cell.IsContinuation);
    }
}
