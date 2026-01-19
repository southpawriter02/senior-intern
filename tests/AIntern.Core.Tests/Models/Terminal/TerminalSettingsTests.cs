// ============================================================================
// File: TerminalSettingsTests.cs
// Path: tests/AIntern.Core.Tests/Models/Terminal/TerminalSettingsTests.cs
// Description: Unit tests for TerminalSettings model.
// Created: 2026-01-19
// AI Intern v0.5.5e - Terminal Settings Models
// ============================================================================

namespace AIntern.Core.Tests.Models.Terminal;

using AIntern.Core.Models.Terminal;
using Xunit;

/// <summary>
/// Unit tests for <see cref="TerminalSettings"/>.
/// </summary>
public class TerminalSettingsTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultValues_AreValid()
    {
        // Arrange
        var settings = new TerminalSettings();

        // Act
        var errors = settings.Validate();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void DefaultFontSize_Is14()
    {
        // Arrange & Act
        var settings = new TerminalSettings();

        // Assert
        Assert.Equal(14, settings.FontSize);
    }

    [Fact]
    public void DefaultLineHeight_Is1Point2()
    {
        // Arrange & Act
        var settings = new TerminalSettings();

        // Assert
        Assert.Equal(1.2, settings.LineHeight);
    }

    [Fact]
    public void DefaultScrollbackLines_Is10000()
    {
        // Arrange & Act
        var settings = new TerminalSettings();

        // Assert
        Assert.Equal(10000, settings.ScrollbackLines);
    }

    [Fact]
    public void DefaultCursorStyle_IsBlock()
    {
        // Arrange & Act
        var settings = new TerminalSettings();

        // Assert
        Assert.Equal(TerminalCursorStyle.Block, settings.CursorStyle);
    }

    [Fact]
    public void DefaultFontFamily_IsPlatformSpecific()
    {
        // Arrange & Act
        var settings = new TerminalSettings();

        // Assert
        Assert.NotNull(settings.FontFamily);
        Assert.NotEmpty(settings.FontFamily);
    }

    #endregion

    #region Validation Tests

    [Theory]
    [InlineData(7)]
    [InlineData(73)]
    [InlineData(-1)]
    public void Validate_DetectsInvalidFontSize(double fontSize)
    {
        // Arrange
        var settings = new TerminalSettings { FontSize = fontSize };

        // Act
        var errors = settings.Validate();

        // Assert
        Assert.Single(errors, e => e.Contains("FontSize"));
    }

    [Theory]
    [InlineData(0.9)]
    [InlineData(3.1)]
    public void Validate_DetectsInvalidLineHeight(double lineHeight)
    {
        // Arrange
        var settings = new TerminalSettings { LineHeight = lineHeight };

        // Act
        var errors = settings.Validate();

        // Assert
        Assert.Single(errors, e => e.Contains("LineHeight"));
    }

    [Theory]
    [InlineData(-3.0)]
    [InlineData(6.0)]
    public void Validate_DetectsInvalidLetterSpacing(double letterSpacing)
    {
        // Arrange
        var settings = new TerminalSettings { LetterSpacing = letterSpacing };

        // Act
        var errors = settings.Validate();

        // Assert
        Assert.Single(errors, e => e.Contains("LetterSpacing"));
    }

    [Theory]
    [InlineData(200)]
    [InlineData(1600)]
    public void Validate_DetectsInvalidCursorBlinkRate(int blinkRate)
    {
        // Arrange
        var settings = new TerminalSettings { CursorBlinkRate = blinkRate };

        // Act
        var errors = settings.Validate();

        // Assert
        Assert.Single(errors, e => e.Contains("CursorBlinkRate"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void Validate_DetectsInvalidScrollbackLines(int scrollback)
    {
        // Arrange
        var settings = new TerminalSettings { ScrollbackLines = scrollback };

        // Act
        var errors = settings.Validate();

        // Assert
        Assert.Single(errors, e => e.Contains("ScrollbackLines"));
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(22.0)]
    public void Validate_DetectsInvalidMinimumContrastRatio(double contrast)
    {
        // Arrange
        var settings = new TerminalSettings { MinimumContrastRatio = contrast };

        // Act
        var errors = settings.Validate();

        // Assert
        Assert.Single(errors, e => e.Contains("MinimumContrastRatio"));
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new TerminalSettings
        {
            FontSize = 16,
            ThemeName = "Dracula",
            CursorBlink = false
        };

        // Act
        var clone = original.Clone();

        // Assert
        Assert.NotSame(original, clone);
        Assert.Equal(original.FontSize, clone.FontSize);
        Assert.Equal(original.ThemeName, clone.ThemeName);
        Assert.Equal(original.CursorBlink, clone.CursorBlink);
    }

    [Fact]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        // Arrange
        var original = new TerminalSettings { FontSize = 14 };
        var clone = original.Clone();

        // Act
        clone.FontSize = 20;

        // Assert
        Assert.Equal(14, original.FontSize);
        Assert.Equal(20, clone.FontSize);
    }

    [Fact]
    public void Clone_CreatesDeepCopyOfCustomProfiles()
    {
        // Arrange
        var original = new TerminalSettings();
        original.CustomProfiles.Add(new ShellProfile { Name = "Test" });

        // Act
        var clone = original.Clone();

        // Assert
        Assert.NotSame(original.CustomProfiles, clone.CustomProfiles);
        Assert.Single(clone.CustomProfiles);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_IncludesKeyInformation()
    {
        // Arrange
        var settings = new TerminalSettings
        {
            FontSize = 16,
            ThemeName = "Monokai",
            CursorStyle = TerminalCursorStyle.Bar
        };

        // Act
        var result = settings.ToString();

        // Assert
        Assert.Contains("16", result);
        Assert.Contains("Monokai", result);
        Assert.Contains("Bar", result);
    }

    #endregion
}
