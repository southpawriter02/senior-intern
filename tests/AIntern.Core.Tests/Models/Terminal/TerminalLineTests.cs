using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalLine"/>.
/// </summary>
public sealed class TerminalLineTests
{
    [Fact]
    public void Constructor_InitializesWithEmptyCells()
    {
        // Act
        var line = new TerminalLine(80);

        // Assert
        Assert.Equal(80, line.Length);
        Assert.Equal(TerminalCell.Empty, line[0]);
        Assert.Equal(TerminalCell.Empty, line[79]);
    }

    [Fact]
    public void Constructor_ThrowsForInvalidColumns()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalLine(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalLine(-1));
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        // Arrange
        var line = new TerminalLine(80);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = line[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = line[80]);
    }

    [Fact]
    public void SetCell_UpdatesCell()
    {
        // Arrange
        var line = new TerminalLine(80);
        var cell = TerminalCell.FromChar('X');

        // Act
        line.SetCell(5, cell);

        // Assert
        Assert.Equal('X', line[5].Character.Value);
    }

    [Fact]
    public void SetCell_OutOfRange_DoesNothing()
    {
        // Arrange
        var line = new TerminalLine(80);
        var cell = TerminalCell.FromChar('X');

        // Act - should not throw
        line.SetCell(-1, cell);
        line.SetCell(100, cell);

        // Assert - no exception
    }

    [Fact]
    public void Clear_ResetsAllCells()
    {
        // Arrange
        var line = new TerminalLine(80);
        line.SetCell(0, TerminalCell.FromChar('A'));
        line.SetCell(79, TerminalCell.FromChar('B'));

        // Act
        line.Clear();

        // Assert
        Assert.Equal(TerminalCell.Empty, line[0]);
        Assert.Equal(TerminalCell.Empty, line[79]);
    }

    [Fact]
    public void Clear_Range_ClearsOnlySpecifiedCells()
    {
        // Arrange
        var line = new TerminalLine(80);
        for (int i = 0; i < 80; i++)
            line.SetCell(i, TerminalCell.FromChar('X'));

        // Act
        line.Clear(10, 20);

        // Assert
        Assert.Equal('X', line[9].Character.Value);
        Assert.Equal(TerminalCell.Empty, line[10]);
        Assert.Equal(TerminalCell.Empty, line[20]);
        Assert.Equal('X', line[21].Character.Value);
    }

    [Fact]
    public void Clear_Range_ClampsToValidRange()
    {
        // Arrange
        var line = new TerminalLine(10);
        for (int i = 0; i < 10; i++)
            line.SetCell(i, TerminalCell.FromChar('X'));

        // Act - should not throw with out-of-range values
        line.Clear(-5, 100);

        // Assert - all cleared due to clamping
        for (int i = 0; i < 10; i++)
            Assert.Equal(TerminalCell.Empty, line[i]);
    }

    [Fact]
    public void GetText_ReturnsTextContent()
    {
        // Arrange
        var line = new TerminalLine(80);
        line.SetCell(0, TerminalCell.FromChar('H'));
        line.SetCell(1, TerminalCell.FromChar('i'));

        // Act
        var text = line.GetText();

        // Assert
        Assert.Equal("Hi", text);
    }

    [Fact]
    public void GetText_SkipsContinuationCells()
    {
        // Arrange
        var line = new TerminalLine(10);
        line.SetCell(0, TerminalCell.FromChar('A'));
        line.SetCell(1, new TerminalCell { IsContinuation = true });
        line.SetCell(2, TerminalCell.FromChar('B'));

        // Act
        var text = line.GetText();

        // Assert
        Assert.Equal("AB", text);
    }

    [Fact]
    public void Resize_Shrink_Truncates()
    {
        // Arrange
        var line = new TerminalLine(80);
        line.SetCell(79, TerminalCell.FromChar('X'));

        // Act
        line.Resize(40);

        // Assert
        Assert.Equal(40, line.Length);
    }

    [Fact]
    public void Resize_Expand_AddsEmptyCells()
    {
        // Arrange
        var line = new TerminalLine(40);
        line.SetCell(0, TerminalCell.FromChar('A'));

        // Act
        line.Resize(80);

        // Assert
        Assert.Equal(80, line.Length);
        Assert.Equal('A', line[0].Character.Value);
        Assert.Equal(TerminalCell.Empty, line[79]);
    }

    [Fact]
    public void CopyFrom_CopiesContent()
    {
        // Arrange
        var source = new TerminalLine(80);
        source.SetCell(0, TerminalCell.FromChar('X'));
        source.IsWrapped = true;
        var dest = new TerminalLine(80);

        // Act
        dest.CopyFrom(source);

        // Assert
        Assert.Equal('X', dest[0].Character.Value);
        Assert.True(dest.IsWrapped);
    }

    [Fact]
    public void IsDirty_InitiallyTrue()
    {
        // Arrange
        var line = new TerminalLine(80);

        // Assert
        Assert.True(line.IsDirty);
    }

    [Fact]
    public void MarkClean_ClearsDirtyFlag()
    {
        // Arrange
        var line = new TerminalLine(80);

        // Act
        line.MarkClean();

        // Assert
        Assert.False(line.IsDirty);
    }

    [Fact]
    public void SetCell_MarksDirty()
    {
        // Arrange
        var line = new TerminalLine(80);
        line.MarkClean();

        // Act
        line.SetCell(0, TerminalCell.FromChar('A'));

        // Assert
        Assert.True(line.IsDirty);
    }

    [Fact]
    public void Cells_Span_AllowsDirectAccess()
    {
        // Arrange
        var line = new TerminalLine(80);

        // Act
        var span = line.Cells;
        span[5] = TerminalCell.FromChar('Z');

        // Assert
        Assert.Equal('Z', line[5].Character.Value);
    }
}
