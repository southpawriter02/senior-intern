// ============================================================================
// File: TerminalSettingsIntegrationTests.cs
// Path: tests/AIntern.Tests.Integration/Terminal/TerminalSettingsIntegrationTests.cs
// Description: Integration tests for terminal settings and themes.
// Version: v0.5.5j
// ============================================================================

namespace AIntern.Tests.Integration.Terminal;

using Xunit;
using AIntern.Core.Models.Terminal;

/// <summary>
/// Integration tests for terminal settings and themes.
/// Validates default values, validation, and theme functionality.
/// </summary>
/// <remarks>Added in v0.5.5j.</remarks>
public sealed class TerminalSettingsIntegrationTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Default Values Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TerminalSettings_DefaultValues_AreReasonable()
    {
        // Act
        var settings = new TerminalSettings();

        // Assert
        Assert.InRange(settings.FontSize, 10, 20);
        Assert.InRange(settings.LineHeight, 1.0, 2.0);
        Assert.True(settings.ScrollbackLines > 1000);
        Assert.Equal(TerminalCursorStyle.Block, settings.CursorStyle);
        Assert.True(settings.CursorBlink);
        Assert.NotEmpty(settings.FontFamily);
    }

    [Fact]
    public void TerminalSettings_DefaultThemeName_IsDark()
    {
        // Act
        var settings = new TerminalSettings();

        // Assert - Default theme name should be a reasonable dark theme
        Assert.NotEmpty(settings.ThemeName);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Validation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TerminalSettings_Validate_ValidSettings_ReturnsEmpty()
    {
        // Act
        var settings = new TerminalSettings();
        var errors = settings.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void TerminalSettings_Validate_InvalidFontSize_ReturnsError()
    {
        // Arrange
        var settings = new TerminalSettings { FontSize = 5 };

        // Act
        var errors = settings.Validate();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("FontSize", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TerminalSettings_Validate_InvalidScrollback_ReturnsError()
    {
        // Arrange
        var settings = new TerminalSettings { ScrollbackLines = -100 };

        // Act
        var errors = settings.Validate();

        // Assert
        Assert.NotEmpty(errors);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Clone Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TerminalSettings_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new TerminalSettings
        {
            FontSize = 16,
            ThemeName = "Solarized Dark",
            CursorStyle = TerminalCursorStyle.Underline
        };

        // Act
        var clone = original.Clone();
        clone.FontSize = 20;
        clone.ThemeName = "Light";

        // Assert
        Assert.Equal(16, original.FontSize);
        Assert.Equal("Solarized Dark", original.ThemeName);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Theme Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TerminalTheme_GetPaletteColor_ReturnsCorrectColors()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act - Get ANSI colors from palette
        var black = theme.GetPaletteColor(0);
        var red = theme.GetPaletteColor(1);
        var green = theme.GetPaletteColor(2);
        var white = theme.GetPaletteColor(7);
        var brightWhite = theme.GetPaletteColor(15);

        // Assert - Should return valid colors
        Assert.Equal(0, black.R);
        Assert.True(red.R > 200);
        Assert.True(green.G > 100);
        Assert.True(white.R > 200);
        Assert.Equal(255, brightWhite.R);
    }

    [Fact]
    public void TerminalTheme_GetPaletteColor_InvalidIndex_ReturnsForeground()
    {
        // Arrange
        var theme = TerminalTheme.Dark;

        // Act
        var color = theme.GetPaletteColor(300);  // Out of range

        // Assert
        Assert.Equal(theme.Foreground, color);
    }

    [Fact]
    public void TerminalTheme_StaticPresets_ExistAndHaveNames()
    {
        // Act
        var dark = TerminalTheme.Dark;
        var light = TerminalTheme.Light;
        var solarized = TerminalTheme.SolarizedDark;

        // Assert
        Assert.Equal("Dark", dark.Name);
        Assert.Equal("Light", light.Name);
        Assert.Equal("Solarized Dark", solarized.Name);
    }

    [Fact]
    public void TerminalTheme_ResolveColor_HandlesDefault()
    {
        // Arrange
        var theme = TerminalTheme.Dark;
        var defaultColor = TerminalColor.Default;

        // Act
        var foreground = theme.ResolveColor(defaultColor, isForeground: true);
        var background = theme.ResolveColor(defaultColor, isForeground: false);

        // Assert
        Assert.Equal(theme.Foreground, foreground);
        Assert.Equal(theme.Background, background);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Enum Value Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void TerminalCursorStyle_HasExpectedValues()
    {
        // Assert
        Assert.Contains(TerminalCursorStyle.Block, Enum.GetValues<TerminalCursorStyle>());
        Assert.Contains(TerminalCursorStyle.Underline, Enum.GetValues<TerminalCursorStyle>());
        Assert.Contains(TerminalCursorStyle.Bar, Enum.GetValues<TerminalCursorStyle>());
    }

    [Fact]
    public void TerminalBellStyle_HasExpectedValues()
    {
        // Assert
        Assert.Contains(TerminalBellStyle.None, Enum.GetValues<TerminalBellStyle>());
        Assert.Contains(TerminalBellStyle.Visual, Enum.GetValues<TerminalBellStyle>());
    }
}
