using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalBuffer"/>.
/// </summary>
public sealed class TerminalBufferTests
{
    [Fact]
    public void Constructor_InitializesDimensions()
    {
        // Act
        var buffer = new TerminalBuffer(80, 24);

        // Assert
        Assert.Equal(80, buffer.Columns);
        Assert.Equal(24, buffer.Rows);
        Assert.Equal(24, buffer.TotalLines);
    }

    [Fact]
    public void Constructor_InitializesCursor()
    {
        // Act
        var buffer = new TerminalBuffer(80, 24);

        // Assert
        Assert.Equal(0, buffer.CursorX);
        Assert.Equal(0, buffer.CursorY);
        Assert.True(buffer.CursorVisible);
    }

    [Fact]
    public void Constructor_ThrowsForInvalidDimensions()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalBuffer(0, 24));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalBuffer(80, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalBuffer(80, 24, -1));
    }

    [Fact]
    public void WriteChar_WritesAtCursor()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act
        buffer.WriteChar('A');

        // Assert
        var line = buffer.GetLine(0);
        Assert.NotNull(line);
        Assert.Equal('A', line![0].Character.Value);
        Assert.Equal(1, buffer.CursorX);
    }

    [Fact]
    public void WriteString_WritesMultipleChars()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act
        buffer.WriteString("Hello");

        // Assert
        var line = buffer.GetLine(0);
        Assert.NotNull(line);
        Assert.Equal("Hello", line!.GetText());
        Assert.Equal(5, buffer.CursorX);
    }

    [Fact]
    public void LineFeed_MovesCursorDown()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act
        buffer.LineFeed();

        // Assert
        Assert.Equal(1, buffer.CursorY);
    }

    [Fact]
    public void CarriageReturn_MovesCursorToColumn0()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        buffer.WriteString("Hello");

        // Act
        buffer.CarriageReturn();

        // Assert
        Assert.Equal(0, buffer.CursorX);
    }

    [Fact]
    public void Backspace_MovesCursorLeft()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        buffer.WriteString("AB");

        // Act
        buffer.Backspace();

        // Assert
        Assert.Equal(1, buffer.CursorX);
    }

    [Fact]
    public void Backspace_DoesNotGoNegative()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act
        buffer.Backspace();

        // Assert
        Assert.Equal(0, buffer.CursorX);
    }

    [Fact]
    public void Tab_MovesToNextTabStop()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        buffer.WriteString("AB");

        // Act
        buffer.Tab();

        // Assert
        Assert.Equal(8, buffer.CursorX);
    }

    [Fact]
    public void SetCursorPosition_SetsPosition_1Indexed()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act
        buffer.SetCursorPosition(10, 20);

        // Assert (converts to 0-indexed)
        Assert.Equal(9, buffer.CursorY);
        Assert.Equal(19, buffer.CursorX);
    }

    [Fact]
    public void SetCursorPosition_ClampsToBounds()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act
        buffer.SetCursorPosition(100, 200);

        // Assert
        Assert.Equal(23, buffer.CursorY);
        Assert.Equal(79, buffer.CursorX);
    }

    [Fact]
    public void SaveAndRestoreCursor_Roundtrips()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        buffer.CursorX = 40;
        buffer.CursorY = 12;
        buffer.CurrentAttributes = TerminalAttributes.Default.With(bold: true);

        // Act
        buffer.SaveCursor();
        buffer.CursorX = 0;
        buffer.CursorY = 0;
        buffer.CurrentAttributes = TerminalAttributes.Default;
        buffer.RestoreCursor();

        // Assert
        Assert.Equal(40, buffer.CursorX);
        Assert.Equal(12, buffer.CursorY);
        Assert.True(buffer.CurrentAttributes.Bold);
    }

    [Fact]
    public void Clear_ClearsAllLines()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        buffer.WriteString("Hello");

        // Act
        buffer.Clear();

        // Assert
        var line = buffer.GetLine(0);
        Assert.Equal(string.Empty, line!.GetText());
    }

    [Fact]
    public void ClearLine_ClearsCurrentLine()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        buffer.WriteString("Hello");
        buffer.CarriageReturn();

        // Act
        buffer.ClearLine();

        // Assert
        var line = buffer.GetLine(0);
        Assert.Equal(string.Empty, line!.GetText());
    }

    [Fact]
    public void ClearLineToEnd_ClearsFromCursor()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        buffer.WriteString("Hello World");
        buffer.CursorX = 5;

        // Act
        buffer.ClearLineToEnd();

        // Assert
        var line = buffer.GetLine(0);
        Assert.Equal("Hello", line!.GetText());
    }

    [Fact]
    public void Resize_UpdatesDimensions()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act
        buffer.Resize(120, 30);

        // Assert
        Assert.Equal(120, buffer.Columns);
        Assert.Equal(30, buffer.Rows);
    }

    [Fact]
    public void Resize_ClampsCursor()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        buffer.CursorX = 79;
        buffer.CursorY = 23;

        // Act
        buffer.Resize(40, 10);

        // Assert
        Assert.Equal(39, buffer.CursorX);
        Assert.Equal(9, buffer.CursorY);
    }

    [Fact]
    public void GetVisibleLines_ReturnsCorrectCount()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act
        var lines = buffer.GetVisibleLines();

        // Assert
        Assert.Equal(24, lines.Count);
    }

    [Fact]
    public void GetLine_OutOfRange_ReturnsNull()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act & Assert
        Assert.Null(buffer.GetLine(-1));
        Assert.Null(buffer.GetLine(24));
    }

    [Fact]
    public void Reset_RestoresDefaultState()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        buffer.WriteString("Hello");
        buffer.CursorX = 40;
        buffer.CursorY = 12;
        buffer.CursorVisible = false;
        buffer.AutoWrapMode = false;

        // Act
        buffer.Reset();

        // Assert
        Assert.Equal(0, buffer.CursorX);
        Assert.Equal(0, buffer.CursorY);
        Assert.True(buffer.CursorVisible);
        Assert.True(buffer.AutoWrapMode);
        Assert.Equal(TerminalAttributes.Default, buffer.CurrentAttributes);
        var line = buffer.GetLine(0);
        Assert.Equal(string.Empty, line!.GetText());
    }

    [Fact]
    public void AutoWrap_WrapsToCursor()
    {
        // Arrange
        var buffer = new TerminalBuffer(10, 5);
        buffer.AutoWrapMode = true;

        // Act - write more than column width
        buffer.WriteString("1234567890AB");

        // Assert
        Assert.Equal(1, buffer.CursorY);
        Assert.Equal(2, buffer.CursorX);
        Assert.Equal("AB", buffer.GetLine(1)!.GetText());
    }

    [Fact]
    public void ContentChanged_IsRaisedOnWrite()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        var raised = false;
        buffer.ContentChanged += (s, e) => raised = true;

        // Act
        buffer.WriteChar('A');

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public void ScrollbackLines_InitiallyZero()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);

        // Act & Assert
        Assert.Equal(0, buffer.ScrollbackLines);
    }

    [Fact]
    public void GetAllText_ReturnsBufferContent()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24);
        buffer.WriteString("Line1");
        buffer.LineFeed();
        buffer.WriteString("Line2");

        // Act
        var text = buffer.GetAllText();

        // Assert
        Assert.Contains("Line1", text);
        Assert.Contains("Line2", text);
    }
}
