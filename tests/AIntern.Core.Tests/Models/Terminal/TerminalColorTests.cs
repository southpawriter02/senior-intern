using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalColor"/>.
/// </summary>
public sealed class TerminalColorTests
{
    [Fact]
    public void Default_IsDefault()
    {
        // Act
        var color = TerminalColor.Default;

        // Assert
        Assert.True(color.IsDefault);
        Assert.Null(color.PaletteIndex);
    }

    [Fact]
    public void FromRgb_SetsRgbValues()
    {
        // Act
        var color = TerminalColor.FromRgb(255, 128, 64);

        // Assert
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(64, color.B);
        Assert.False(color.IsDefault);
        Assert.Null(color.PaletteIndex);
    }

    [Fact]
    public void FromPalette_SetsPaletteIndex()
    {
        // Act
        var color = TerminalColor.FromPalette(196);

        // Assert
        Assert.Equal((byte)196, color.PaletteIndex);
        Assert.False(color.IsDefault);
    }

    [Fact]
    public void StandardColors_ArePaletteColors()
    {
        // Assert
        Assert.Equal((byte)0, TerminalColor.Black.PaletteIndex);
        Assert.Equal((byte)1, TerminalColor.Red.PaletteIndex);
        Assert.Equal((byte)2, TerminalColor.Green.PaletteIndex);
        Assert.Equal((byte)3, TerminalColor.Yellow.PaletteIndex);
        Assert.Equal((byte)4, TerminalColor.Blue.PaletteIndex);
        Assert.Equal((byte)5, TerminalColor.Magenta.PaletteIndex);
        Assert.Equal((byte)6, TerminalColor.Cyan.PaletteIndex);
        Assert.Equal((byte)7, TerminalColor.White.PaletteIndex);
    }

    [Fact]
    public void BrightColors_ArePaletteColors()
    {
        // Assert
        Assert.Equal((byte)8, TerminalColor.BrightBlack.PaletteIndex);
        Assert.Equal((byte)9, TerminalColor.BrightRed.PaletteIndex);
        Assert.Equal((byte)10, TerminalColor.BrightGreen.PaletteIndex);
        Assert.Equal((byte)11, TerminalColor.BrightYellow.PaletteIndex);
        Assert.Equal((byte)12, TerminalColor.BrightBlue.PaletteIndex);
        Assert.Equal((byte)13, TerminalColor.BrightMagenta.PaletteIndex);
        Assert.Equal((byte)14, TerminalColor.BrightCyan.PaletteIndex);
        Assert.Equal((byte)15, TerminalColor.BrightWhite.PaletteIndex);
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        // Arrange
        var color1 = TerminalColor.FromRgb(255, 128, 64);
        var color2 = TerminalColor.FromRgb(255, 128, 64);
        var color3 = TerminalColor.FromRgb(0, 0, 0);

        // Act & Assert
        Assert.True(color1.Equals(color2));
        Assert.True(color1 == color2);
        Assert.True(color1 != color3);
    }

    [Fact]
    public void GetHashCode_IsSameForEqualColors()
    {
        // Arrange
        var color1 = TerminalColor.FromPalette(42);
        var color2 = TerminalColor.FromPalette(42);

        // Act & Assert
        Assert.Equal(color1.GetHashCode(), color2.GetHashCode());
    }

    [Fact]
    public void ToString_Default_ReturnsDefault()
    {
        // Arrange
        var color = TerminalColor.Default;

        // Act & Assert
        Assert.Equal("Default", color.ToString());
    }

    [Fact]
    public void ToString_Palette_ReturnsPaletteFormat()
    {
        // Arrange
        var color = TerminalColor.FromPalette(42);

        // Act & Assert
        Assert.Equal("Palette(42)", color.ToString());
    }

    [Fact]
    public void ToString_Rgb_ReturnsRgbFormat()
    {
        // Arrange
        var color = TerminalColor.FromRgb(128, 64, 32);

        // Act & Assert
        Assert.Equal("RGB(128,64,32)", color.ToString());
    }
}
