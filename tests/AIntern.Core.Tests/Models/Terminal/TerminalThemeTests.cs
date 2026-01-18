using AIntern.Core.Models.Terminal;
using Xunit;

namespace AIntern.Core.Tests.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalThemeTests (v0.5.2a)                                             │
// │ Unit tests for TerminalTheme color palette and theme presets.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for the <see cref="TerminalTheme"/> class.
/// </summary>
/// <remarks>
/// Tests cover:
/// <list type="bullet">
///   <item><description>256-color palette calculation (ANSI, color cube, grayscale)</description></item>
///   <item><description>Color resolution for default, palette, and RGB colors</description></item>
///   <item><description>Theme presets (Dark, Light, Solarized Dark)</description></item>
///   <item><description>Semantic color lookup</description></item>
/// </list>
/// </remarks>
public class TerminalThemeTests
{
    #region ANSI Palette Tests (Colors 0-15)

    [Fact]
    public void GetPaletteColor_Index0_ReturnsBlack()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetPaletteColor(0);

        // Assert - Default ANSI black is (0, 0, 0)
        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void GetPaletteColor_Index1_ReturnsRed()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetPaletteColor(1);

        // Assert - Default ANSI red
        Assert.Equal(205, color.R);
        Assert.Equal(49, color.G);
        Assert.Equal(49, color.B);
    }

    [Fact]
    public void GetPaletteColor_Index15_ReturnsBrightWhite()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetPaletteColor(15);

        // Assert - Bright white is (255, 255, 255)
        Assert.Equal(255, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(255, color.B);
    }

    [Theory]
    [InlineData(0)]  // Black
    [InlineData(7)]  // White
    [InlineData(8)]  // Bright Black
    [InlineData(15)] // Bright White
    public void GetPaletteColor_AnsiRange_ReturnsFromPalette(int index)
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetPaletteColor(index);

        // Assert - Should match the palette entry
        Assert.Equal(theme.AnsiPalette[index], color);
    }

    #endregion

    #region Color Cube Tests (Colors 16-231)

    [Fact]
    public void GetPaletteColor_Index16_ReturnsBlackFromCube()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act - Index 16 is the first color cube entry (0, 0, 0)
        var color = theme.GetPaletteColor(16);

        // Assert
        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void GetPaletteColor_Index196_ReturnsPureRedFromCube()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act - Index 196 = 16 + (5*36) + (0*6) + 0 = pure red (255, 0, 0)
        var color = theme.GetPaletteColor(196);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void GetPaletteColor_Index21_ReturnsPureBlueFromCube()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act - Index 21 = 16 + (0*36) + (0*6) + 5 = pure blue (0, 0, 255)
        var color = theme.GetPaletteColor(21);

        // Assert
        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(255, color.B);
    }

    [Fact]
    public void GetPaletteColor_Index46_ReturnsPureGreenFromCube()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act - Index 46 = 16 + (0*36) + (5*6) + 0 = pure green (0, 255, 0)
        var color = theme.GetPaletteColor(46);

        // Assert
        Assert.Equal(0, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void GetPaletteColor_Index231_ReturnsWhiteFromCube()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act - Index 231 = last color cube entry (255, 255, 255)
        var color = theme.GetPaletteColor(231);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(255, color.B);
    }

    [Theory]
    [InlineData(16, 0, 0, 0)]       // (0,0,0) - black
    [InlineData(17, 0, 0, 51)]      // (0,0,1) - dark blue
    [InlineData(22, 0, 51, 0)]      // (0,1,0) - dark green
    [InlineData(52, 51, 0, 0)]      // (1,0,0) - dark red
    [InlineData(124, 153, 0, 0)]    // (3,0,0) - medium red (index-16=108, 108/36=3, 108%36=0)
    public void GetPaletteColor_ColorCube_CalculatesCorrectly(int index, byte r, byte g, byte b)
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetPaletteColor(index);

        // Assert
        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
    }

    #endregion

    #region Grayscale Ramp Tests (Colors 232-255)

    [Fact]
    public void GetPaletteColor_Index232_ReturnsDarkestGray()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act - First grayscale: (232-232)*10 + 8 = 8
        var color = theme.GetPaletteColor(232);

        // Assert
        Assert.Equal(8, color.R);
        Assert.Equal(8, color.G);
        Assert.Equal(8, color.B);
    }

    [Fact]
    public void GetPaletteColor_Index255_ReturnsLightestGray()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act - Last grayscale: (255-232)*10 + 8 = 238
        var color = theme.GetPaletteColor(255);

        // Assert
        Assert.Equal(238, color.R);
        Assert.Equal(238, color.G);
        Assert.Equal(238, color.B);
    }

    [Theory]
    [InlineData(232, 8)]    // Darkest
    [InlineData(240, 88)]   // Mid-dark
    [InlineData(248, 168)]  // Mid-light
    [InlineData(255, 238)]  // Lightest
    public void GetPaletteColor_Grayscale_CalculatesCorrectly(int index, byte gray)
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetPaletteColor(index);

        // Assert
        Assert.Equal(gray, color.R);
        Assert.Equal(gray, color.G);
        Assert.Equal(gray, color.B);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void GetPaletteColor_NegativeIndex_ReturnsForeground()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetPaletteColor(-1);

        // Assert - Should fallback to foreground
        Assert.Equal(theme.Foreground, color);
    }

    [Fact]
    public void GetPaletteColor_IndexAbove255_ReturnsForeground()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetPaletteColor(256);

        // Assert - Should fallback to foreground
        Assert.Equal(theme.Foreground, color);
    }

    #endregion

    #region ResolveColor Tests

    [Fact]
    public void ResolveColor_DefaultForeground_ReturnsThemeForeground()
    {
        // Arrange
        var theme = TerminalTheme.Dark;
        var color = TerminalColor.Default;

        // Act
        var resolved = theme.ResolveColor(color, isForeground: true);

        // Assert
        Assert.Equal(theme.Foreground, resolved);
    }

    [Fact]
    public void ResolveColor_DefaultBackground_ReturnsThemeBackground()
    {
        // Arrange
        var theme = TerminalTheme.Dark;
        var color = TerminalColor.Default;

        // Act
        var resolved = theme.ResolveColor(color, isForeground: false);

        // Assert
        Assert.Equal(theme.Background, resolved);
    }

    [Fact]
    public void ResolveColor_PaletteColor_ReturnsResolvedPaletteColor()
    {
        // Arrange
        var theme = TerminalTheme.Dark;
        var color = TerminalColor.FromPalette(1);  // Red

        // Act
        var resolved = theme.ResolveColor(color, isForeground: true);

        // Assert - Should resolve to palette color 1 (red)
        Assert.Equal(theme.GetPaletteColor(1), resolved);
    }

    [Fact]
    public void ResolveColor_RgbColor_ReturnsUnchanged()
    {
        // Arrange
        var theme = TerminalTheme.Dark;
        var color = TerminalColor.FromRgb(100, 150, 200);

        // Act
        var resolved = theme.ResolveColor(color, isForeground: true);

        // Assert - RGB colors should pass through unchanged
        Assert.Equal(color, resolved);
    }

    #endregion

    #region GetSemanticColor Tests

    [Fact]
    public void GetSemanticColor_Background_ReturnsBackground()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetSemanticColor(TerminalThemeColor.Background);

        // Assert
        Assert.Equal(theme.Background, color);
    }

    [Fact]
    public void GetSemanticColor_Foreground_ReturnsForeground()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetSemanticColor(TerminalThemeColor.Foreground);

        // Assert
        Assert.Equal(theme.Foreground, color);
    }

    [Fact]
    public void GetSemanticColor_Cursor_ReturnsCursor()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetSemanticColor(TerminalThemeColor.Cursor);

        // Assert
        Assert.Equal(theme.Cursor, color);
    }

    [Fact]
    public void GetSemanticColor_Selection_ReturnsSelection()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetSemanticColor(TerminalThemeColor.Selection);

        // Assert
        Assert.Equal(theme.Selection, color);
    }

    [Fact]
    public void GetSemanticColor_BoldForeground_WhenNull_ReturnsForeground()
    {
        // Arrange
        var theme = new TerminalTheme { BoldForeground = null };

        // Act
        var color = theme.GetSemanticColor(TerminalThemeColor.BoldForeground);

        // Assert - Should fallback to foreground
        Assert.Equal(theme.Foreground, color);
    }

    [Fact]
    public void GetSemanticColor_BoldForeground_WhenSet_ReturnsBoldForeground()
    {
        // Arrange
        var boldColor = TerminalColor.FromRgb(255, 255, 255);
        var theme = new TerminalTheme { BoldForeground = boldColor };

        // Act
        var color = theme.GetSemanticColor(TerminalThemeColor.BoldForeground);

        // Assert
        Assert.Equal(boldColor, color);
    }

    #endregion

    #region Theme Preset Tests

    [Fact]
    public void Dark_HasExpectedName()
    {
        // Act
        var theme = TerminalTheme.Dark;

        // Assert
        Assert.Equal("Dark", theme.Name);
    }

    [Fact]
    public void Dark_HasDarkBackground()
    {
        // Act
        var theme = TerminalTheme.Dark;

        // Assert - #1E1E1E = (30, 30, 30)
        Assert.Equal(30, theme.Background.R);
        Assert.Equal(30, theme.Background.G);
        Assert.Equal(30, theme.Background.B);
    }

    [Fact]
    public void Light_HasExpectedName()
    {
        // Act
        var theme = TerminalTheme.Light;

        // Assert
        Assert.Equal("Light", theme.Name);
    }

    [Fact]
    public void Light_HasWhiteBackground()
    {
        // Act
        var theme = TerminalTheme.Light;

        // Assert
        Assert.Equal(255, theme.Background.R);
        Assert.Equal(255, theme.Background.G);
        Assert.Equal(255, theme.Background.B);
    }

    [Fact]
    public void Light_HasBlackForeground()
    {
        // Act
        var theme = TerminalTheme.Light;

        // Assert
        Assert.Equal(0, theme.Foreground.R);
        Assert.Equal(0, theme.Foreground.G);
        Assert.Equal(0, theme.Foreground.B);
    }

    [Fact]
    public void SolarizedDark_HasExpectedName()
    {
        // Act
        var theme = TerminalTheme.SolarizedDark;

        // Assert
        Assert.Equal("Solarized Dark", theme.Name);
    }

    [Fact]
    public void SolarizedDark_HasSolarizedBackground()
    {
        // Act
        var theme = TerminalTheme.SolarizedDark;

        // Assert - #002B36 = (0, 43, 54)
        Assert.Equal(0, theme.Background.R);
        Assert.Equal(43, theme.Background.G);
        Assert.Equal(54, theme.Background.B);
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void Default_CursorStyle_IsBlock()
    {
        // Arrange & Act
        var theme = new TerminalTheme();

        // Assert
        Assert.Equal(CursorStyle.Block, theme.CursorStyle);
    }

    [Fact]
    public void Default_CursorBlink_IsTrue()
    {
        // Arrange & Act
        var theme = new TerminalTheme();

        // Assert
        Assert.True(theme.CursorBlink);
    }

    [Fact]
    public void Default_CursorBlinkIntervalMs_Is530()
    {
        // Arrange & Act
        var theme = new TerminalTheme();

        // Assert
        Assert.Equal(530, theme.CursorBlinkIntervalMs);
    }

    [Fact]
    public void Default_SelectionAlpha_Is80()
    {
        // Arrange & Act
        var theme = new TerminalTheme();

        // Assert
        Assert.Equal(80, theme.SelectionAlpha);
    }

    [Fact]
    public void Default_AnsiPalette_Has16Colors()
    {
        // Arrange & Act
        var theme = new TerminalTheme();

        // Assert
        Assert.Equal(16, theme.AnsiPalette.Length);
    }

    #endregion
}
