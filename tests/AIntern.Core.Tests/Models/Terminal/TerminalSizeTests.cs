using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalSize"/>.
/// </summary>
public sealed class TerminalSizeTests
{
    [Fact]
    public void Constructor_SetsColumnsAndRows()
    {
        // Arrange & Act
        var size = new TerminalSize(80, 24);

        // Assert
        Assert.Equal(80, size.Columns);
        Assert.Equal(24, size.Rows);
    }

    [Fact]
    public void Default_Returns80x24()
    {
        // Act
        var size = TerminalSize.Default;

        // Assert
        Assert.Equal(80, size.Columns);
        Assert.Equal(24, size.Rows);
    }

    [Fact]
    public void Wide_Returns120x30()
    {
        // Act
        var size = TerminalSize.Wide;

        // Assert
        Assert.Equal(120, size.Columns);
        Assert.Equal(30, size.Rows);
    }

    [Fact]
    public void Compact_Returns80x12()
    {
        // Act
        var size = TerminalSize.Compact;

        // Assert
        Assert.Equal(80, size.Columns);
        Assert.Equal(12, size.Rows);
    }

    [Theory]
    [InlineData(80, 24, true)]
    [InlineData(1, 1, true)]
    [InlineData(0, 24, false)]
    [InlineData(80, 0, false)]
    [InlineData(-1, 24, false)]
    [InlineData(80, -1, false)]
    public void IsValid_ReturnsExpectedValue(int columns, int rows, bool expected)
    {
        // Arrange
        var size = new TerminalSize(columns, rows);

        // Act & Assert
        Assert.Equal(expected, size.IsValid);
    }

    [Fact]
    public void TotalCells_ReturnsProduct()
    {
        // Arrange
        var size = new TerminalSize(80, 24);

        // Act & Assert
        Assert.Equal(1920, size.TotalCells);
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var size = new TerminalSize(80, 24);

        // Act & Assert
        Assert.Equal("80x24", size.ToString());
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        // Arrange
        var size1 = new TerminalSize(80, 24);
        var size2 = new TerminalSize(80, 24);
        var size3 = new TerminalSize(120, 30);

        // Act & Assert
        Assert.Equal(size1, size2);
        Assert.NotEqual(size1, size3);
    }
}
