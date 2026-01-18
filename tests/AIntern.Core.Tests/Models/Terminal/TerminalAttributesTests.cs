using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalAttributes"/>.
/// </summary>
public sealed class TerminalAttributesTests
{
    [Fact]
    public void Default_HasDefaultColors_NoStyles()
    {
        // Act
        var attrs = TerminalAttributes.Default;

        // Assert
        Assert.True(attrs.Foreground.IsDefault);
        Assert.True(attrs.Background.IsDefault);
        Assert.False(attrs.Bold);
        Assert.False(attrs.Dim);
        Assert.False(attrs.Italic);
        Assert.False(attrs.Underline);
        Assert.False(attrs.Blink);
        Assert.False(attrs.Inverse);
        Assert.False(attrs.Hidden);
        Assert.False(attrs.Strikethrough);
    }

    [Fact]
    public void With_Foreground_ReturnsNewAttributesWithForeground()
    {
        // Arrange
        var original = TerminalAttributes.Default;

        // Act
        var modified = original.With(foreground: TerminalColor.Red);

        // Assert
        Assert.Equal(TerminalColor.Red, modified.Foreground);
        Assert.Equal(original.Background, modified.Background);
        Assert.True(original.Foreground.IsDefault); // Original unchanged
    }

    [Fact]
    public void With_Bold_ReturnsNewAttributesWithBold()
    {
        // Arrange
        var original = TerminalAttributes.Default;

        // Act
        var modified = original.With(bold: true);

        // Assert
        Assert.True(modified.Bold);
        Assert.False(original.Bold); // Original unchanged
    }

    [Fact]
    public void With_MultipleChanges_AppliesAll()
    {
        // Arrange
        var original = TerminalAttributes.Default;

        // Act
        var modified = original.With(
            foreground: TerminalColor.Green,
            background: TerminalColor.Black,
            bold: true,
            underline: true
        );

        // Assert
        Assert.Equal(TerminalColor.Green, modified.Foreground);
        Assert.Equal(TerminalColor.Black, modified.Background);
        Assert.True(modified.Bold);
        Assert.True(modified.Underline);
        Assert.False(modified.Italic); // Unchanged
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        // Arrange
        var attrs1 = TerminalAttributes.Default.With(bold: true);
        var attrs2 = TerminalAttributes.Default.With(bold: true);
        var attrs3 = TerminalAttributes.Default.With(italic: true);

        // Act & Assert
        Assert.True(attrs1.Equals(attrs2));
        Assert.True(attrs1 == attrs2);
        Assert.True(attrs1 != attrs3);
    }

    [Fact]
    public void GetHashCode_IsSameForEqualAttributes()
    {
        // Arrange
        var attrs1 = new TerminalAttributes
        {
            Foreground = TerminalColor.Red,
            Bold = true
        };
        var attrs2 = new TerminalAttributes
        {
            Foreground = TerminalColor.Red,
            Bold = true
        };

        // Act & Assert
        Assert.Equal(attrs1.GetHashCode(), attrs2.GetHashCode());
    }

    [Fact]
    public void AllStyleProperties_CanBeSet()
    {
        // Arrange & Act
        var attrs = new TerminalAttributes
        {
            Foreground = TerminalColor.Red,
            Background = TerminalColor.Blue,
            Bold = true,
            Dim = true,
            Italic = true,
            Underline = true,
            Blink = true,
            Inverse = true,
            Hidden = true,
            Strikethrough = true
        };

        // Assert
        Assert.True(attrs.Bold);
        Assert.True(attrs.Dim);
        Assert.True(attrs.Italic);
        Assert.True(attrs.Underline);
        Assert.True(attrs.Blink);
        Assert.True(attrs.Inverse);
        Assert.True(attrs.Hidden);
        Assert.True(attrs.Strikethrough);
    }
}
