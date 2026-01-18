namespace AIntern.Desktop.Tests.Controls.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalControlTests (v0.5.2c)                                               │
// │ Unit tests for TerminalControl input handling, selection, and key sequences.│
// └─────────────────────────────────────────────────────────────────────────────┘

using Avalonia.Input;
using AIntern.Desktop.Controls.Terminal;
using Xunit;

/// <summary>
/// Unit tests for <see cref="TerminalControl"/> input handling.
/// </summary>
/// <remarks>
/// <para>
/// Tests cover:
/// <list type="bullet">
///   <item><description>Key sequence mapping - VT100/xterm escape sequences</description></item>
///   <item><description>Word character detection - Boundary detection for word selection</description></item>
///   <item><description>Constants verification - Click timing and scroll settings</description></item>
/// </list>
/// </para>
/// </remarks>
public class TerminalControlTests
{
    #region Key Sequence Tests - Special Keys

    /// <summary>
    /// Verifies that Enter key produces carriage return.
    /// </summary>
    [Fact]
    public void GetKeySequence_Enter_ReturnsCarriageReturn()
    {
        // Act
        var result = TerminalControl.GetKeySequence(Key.Enter, KeyModifiers.None);

        // Assert
        Assert.Equal("\r", result);
    }

    /// <summary>
    /// Verifies that Escape key produces ESC character.
    /// </summary>
    [Fact]
    public void GetKeySequence_Escape_ReturnsEscapeCharacter()
    {
        // Act
        var result = TerminalControl.GetKeySequence(Key.Escape, KeyModifiers.None);

        // Assert
        Assert.Equal("\x1B", result);
    }

    /// <summary>
    /// Verifies that Tab key produces horizontal tab.
    /// </summary>
    [Fact]
    public void GetKeySequence_Tab_ReturnsHorizontalTab()
    {
        // Act
        var result = TerminalControl.GetKeySequence(Key.Tab, KeyModifiers.None);

        // Assert
        Assert.Equal("\t", result);
    }

    /// <summary>
    /// Verifies that Shift+Tab produces back-tab escape sequence.
    /// </summary>
    [Fact]
    public void GetKeySequence_ShiftTab_ReturnsBackTab()
    {
        // Act
        var result = TerminalControl.GetKeySequence(Key.Tab, KeyModifiers.Shift);

        // Assert
        Assert.Equal("\x1B[Z", result);
    }

    /// <summary>
    /// Verifies that Backspace produces DEL character.
    /// </summary>
    [Fact]
    public void GetKeySequence_Backspace_ReturnsDelete()
    {
        // Act
        var result = TerminalControl.GetKeySequence(Key.Back, KeyModifiers.None);

        // Assert
        Assert.Equal("\x7F", result);
    }

    /// <summary>
    /// Verifies that Delete key produces CSI 3 ~ sequence.
    /// </summary>
    [Fact]
    public void GetKeySequence_Delete_ReturnsCsiSequence()
    {
        // Act
        var result = TerminalControl.GetKeySequence(Key.Delete, KeyModifiers.None);

        // Assert
        Assert.Equal("\x1B[3~", result);
    }

    /// <summary>
    /// Verifies that Insert key produces CSI 2 ~ sequence.
    /// </summary>
    [Fact]
    public void GetKeySequence_Insert_ReturnsCsiSequence()
    {
        // Act
        var result = TerminalControl.GetKeySequence(Key.Insert, KeyModifiers.None);

        // Assert
        Assert.Equal("\x1B[2~", result);
    }

    #endregion

    #region Key Sequence Tests - Arrow Keys

    /// <summary>
    /// Verifies that arrow keys produce correct CSI sequences.
    /// </summary>
    [Theory]
    [InlineData(Key.Up, "\x1B[A")]
    [InlineData(Key.Down, "\x1B[B")]
    [InlineData(Key.Right, "\x1B[C")]
    [InlineData(Key.Left, "\x1B[D")]
    public void GetKeySequence_ArrowKeys_ReturnCsiSequences(Key key, string expected)
    {
        // Act
        var result = TerminalControl.GetKeySequence(key, KeyModifiers.None);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Key Sequence Tests - Navigation Keys

    /// <summary>
    /// Verifies that Home and End keys produce correct sequences.
    /// </summary>
    [Theory]
    [InlineData(Key.Home, KeyModifiers.None, "\x1B[H")]
    [InlineData(Key.Home, KeyModifiers.Control, "\x1B[1;5H")]
    [InlineData(Key.End, KeyModifiers.None, "\x1B[F")]
    [InlineData(Key.End, KeyModifiers.Control, "\x1B[1;5F")]
    public void GetKeySequence_HomeEnd_ReturnCorrectSequences(
        Key key, KeyModifiers modifiers, string expected)
    {
        // Act
        var result = TerminalControl.GetKeySequence(key, modifiers);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that PageUp and PageDown produce correct CSI sequences.
    /// </summary>
    [Theory]
    [InlineData(Key.PageUp, "\x1B[5~")]
    [InlineData(Key.PageDown, "\x1B[6~")]
    public void GetKeySequence_PageUpDown_ReturnCsiSequences(Key key, string expected)
    {
        // Act
        var result = TerminalControl.GetKeySequence(key, KeyModifiers.None);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Key Sequence Tests - Function Keys

    /// <summary>
    /// Verifies that F1-F4 produce SS3 sequences.
    /// </summary>
    [Theory]
    [InlineData(Key.F1, "\x1BOP")]
    [InlineData(Key.F2, "\x1BOQ")]
    [InlineData(Key.F3, "\x1BOR")]
    [InlineData(Key.F4, "\x1BOS")]
    public void GetKeySequence_F1ToF4_ReturnSs3Sequences(Key key, string expected)
    {
        // Act
        var result = TerminalControl.GetKeySequence(key, KeyModifiers.None);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that F5-F12 produce CSI sequences.
    /// </summary>
    [Theory]
    [InlineData(Key.F5, "\x1B[15~")]
    [InlineData(Key.F6, "\x1B[17~")]
    [InlineData(Key.F7, "\x1B[18~")]
    [InlineData(Key.F8, "\x1B[19~")]
    [InlineData(Key.F9, "\x1B[20~")]
    [InlineData(Key.F10, "\x1B[21~")]
    [InlineData(Key.F11, "\x1B[23~")]
    [InlineData(Key.F12, "\x1B[24~")]
    public void GetKeySequence_F5ToF12_ReturnCsiSequences(Key key, string expected)
    {
        // Act
        var result = TerminalControl.GetKeySequence(key, KeyModifiers.None);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Key Sequence Tests - Control Key Combinations

    /// <summary>
    /// Verifies that Ctrl+C produces SIGINT character.
    /// </summary>
    [Fact]
    public void GetKeySequence_CtrlC_ReturnsSigInt()
    {
        // Act
        var result = TerminalControl.GetKeySequence(Key.C, KeyModifiers.Control);

        // Assert
        Assert.Equal("\x03", result);
    }

    /// <summary>
    /// Verifies that Ctrl+Z produces SIGTSTP character.
    /// </summary>
    [Fact]
    public void GetKeySequence_CtrlZ_ReturnsSigTstp()
    {
        // Act
        var result = TerminalControl.GetKeySequence(Key.Z, KeyModifiers.Control);

        // Assert
        Assert.Equal("\x1A", result);
    }

    /// <summary>
    /// Verifies that Ctrl+D produces EOF character.
    /// </summary>
    [Fact]
    public void GetKeySequence_CtrlD_ReturnsEof()
    {
        // Act
        var result = TerminalControl.GetKeySequence(Key.D, KeyModifiers.Control);

        // Assert
        Assert.Equal("\x04", result);
    }

    /// <summary>
    /// Verifies that Ctrl+L produces form feed (clear screen) character.
    /// </summary>
    [Fact]
    public void GetKeySequence_CtrlL_ReturnsFormFeed()
    {
        // Act
        var result = TerminalControl.GetKeySequence(Key.L, KeyModifiers.Control);

        // Assert
        Assert.Equal("\x0C", result);
    }

    /// <summary>
    /// Verifies common shell editing control key combinations.
    /// </summary>
    [Theory]
    [InlineData(Key.A, "\x01")]   // Beginning of line
    [InlineData(Key.E, "\x05")]   // End of line
    [InlineData(Key.K, "\x0B")]   // Kill to end of line
    [InlineData(Key.U, "\x15")]   // Kill to beginning
    [InlineData(Key.W, "\x17")]   // Kill word backward
    [InlineData(Key.R, "\x12")]   // Reverse search
    [InlineData(Key.P, "\x10")]   // Previous history
    [InlineData(Key.N, "\x0E")]   // Next history
    public void GetKeySequence_CtrlShellEditing_ReturnControlCharacters(
        Key key, string expected)
    {
        // Act
        var result = TerminalControl.GetKeySequence(key, KeyModifiers.Control);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that Ctrl+Shift combinations are not intercepted (reserved for clipboard).
    /// </summary>
    [Theory]
    [InlineData(Key.C)]
    [InlineData(Key.V)]
    public void GetKeySequence_CtrlShift_ReturnsNull(Key key)
    {
        // Act
        var result = TerminalControl.GetKeySequence(
            key, KeyModifiers.Control | KeyModifiers.Shift);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that unhandled keys return null.
    /// </summary>
    [Theory]
    [InlineData(Key.A, KeyModifiers.None)]
    [InlineData(Key.Z, KeyModifiers.None)]
    [InlineData(Key.Space, KeyModifiers.None)]
    [InlineData(Key.D1, KeyModifiers.None)]
    public void GetKeySequence_UnhandledKeys_ReturnsNull(Key key, KeyModifiers modifiers)
    {
        // Act
        var result = TerminalControl.GetKeySequence(key, modifiers);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Word Character Tests

    /// <summary>
    /// Verifies that letters are considered word characters.
    /// </summary>
    [Theory]
    [InlineData('a')]
    [InlineData('z')]
    [InlineData('A')]
    [InlineData('Z')]
    [InlineData('m')]
    public void IsWordChar_Letters_ReturnsTrue(char c)
    {
        // Act
        var result = TerminalControl.IsWordChar(c);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that digits are considered word characters.
    /// </summary>
    [Theory]
    [InlineData('0')]
    [InlineData('9')]
    [InlineData('5')]
    public void IsWordChar_Digits_ReturnsTrue(char c)
    {
        // Act
        var result = TerminalControl.IsWordChar(c);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that underscore and hyphen are considered word characters.
    /// </summary>
    [Theory]
    [InlineData('_')]
    [InlineData('-')]
    public void IsWordChar_UnderscoreHyphen_ReturnsTrue(char c)
    {
        // Act
        var result = TerminalControl.IsWordChar(c);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that whitespace and punctuation are not word characters.
    /// </summary>
    [Theory]
    [InlineData(' ')]
    [InlineData('\t')]
    [InlineData('.')]
    [InlineData(',')]
    [InlineData(';')]
    [InlineData(':')]
    [InlineData('!')]
    [InlineData('@')]
    [InlineData('#')]
    [InlineData('$')]
    [InlineData('(')]
    [InlineData(')')]
    [InlineData('[')]
    [InlineData(']')]
    public void IsWordChar_WhitespaceAndPunctuation_ReturnsFalse(char c)
    {
        // Act
        var result = TerminalControl.IsWordChar(c);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Constants Verification

    /// <summary>
    /// Verifies that click time window is set to expected value.
    /// </summary>
    [Fact]
    public void ClickTimeWindowMs_HasExpectedValue()
    {
        // Assert
        Assert.Equal(500, TerminalControl.ClickTimeWindowMs);
    }

    /// <summary>
    /// Verifies that click position tolerance is set to expected value.
    /// </summary>
    [Fact]
    public void ClickPositionTolerancePx_HasExpectedValue()
    {
        // Assert
        Assert.Equal(5.0, TerminalControl.ClickPositionTolerancePx);
    }

    /// <summary>
    /// Verifies that scroll lines per tick is set to expected value.
    /// </summary>
    [Fact]
    public void ScrollLinesPerTick_HasExpectedValue()
    {
        // Assert
        Assert.Equal(3, TerminalControl.ScrollLinesPerTick);
    }

    #endregion
}
