// ============================================================================
// File: FontServiceTests.cs
// Path: tests/AIntern.Services.Tests/FontServiceTests.cs
// Description: Unit tests for FontService.
// Created: 2026-01-19
// AI Intern v0.5.5e - Terminal Settings Models
// ============================================================================

namespace AIntern.Services.Tests;

using AIntern.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="FontService"/>.
/// </summary>
public class FontServiceTests
{
    #region Test Fixtures

    private readonly Mock<ILogger<FontService>> _mockLogger;

    public FontServiceTests()
    {
        _mockLogger = new Mock<ILogger<FontService>>();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FontService(null!));
    }

    [Fact]
    public void Constructor_SucceedsWithValidLogger()
    {
        // Act
        var service = new FontService(_mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region IsFontAvailable Tests

    [Fact]
    public void IsFontAvailable_ReturnsFalseForNullFont()
    {
        // Arrange
        var service = new FontService(_mockLogger.Object);

        // Act
        var result = service.IsFontAvailable(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFontAvailable_ReturnsFalseForEmptyFont()
    {
        // Arrange
        var service = new FontService(_mockLogger.Object);

        // Act
        var result = service.IsFontAvailable("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFontAvailable_ReturnsTrueForMonospaceGenericFont()
    {
        // Arrange
        var service = new FontService(_mockLogger.Object);

        // Act
        var result = service.IsFontAvailable("monospace");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFontAvailable_UsesInjectedChecker()
    {
        // Arrange
        Func<string, bool> checker = f => f == "TestFont";
        var service = new FontService(_mockLogger.Object, checker);

        // Act & Assert
        Assert.True(service.IsFontAvailable("TestFont"));
        Assert.False(service.IsFontAvailable("OtherFont"));
    }

    [Fact]
    public void IsFontAvailable_ReturnsFalseWhenCheckerThrows()
    {
        // Arrange
        Func<string, bool> checker = f => throw new Exception("Test error");
        var service = new FontService(_mockLogger.Object, checker);

        // Act
        var result = service.IsFontAvailable("AnyFont");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetBestAvailableFont Tests

    [Fact]
    public void GetBestAvailableFont_ReturnsMonospaceForNullList()
    {
        // Arrange
        var service = new FontService(_mockLogger.Object);

        // Act
        var result = service.GetBestAvailableFont(null!);

        // Assert
        Assert.Equal("monospace", result);
    }

    [Fact]
    public void GetBestAvailableFont_ReturnsMonospaceForEmptyList()
    {
        // Arrange
        var service = new FontService(_mockLogger.Object);

        // Act
        var result = service.GetBestAvailableFont("");

        // Assert
        Assert.Equal("monospace", result);
    }

    [Fact]
    public void GetBestAvailableFont_ReturnsFirstAvailableFont()
    {
        // Arrange
        Func<string, bool> checker = f => f == "SecondFont" || f == "ThirdFont";
        var service = new FontService(_mockLogger.Object, checker);

        // Act
        var result = service.GetBestAvailableFont("FirstFont, SecondFont, ThirdFont");

        // Assert
        Assert.Equal("SecondFont", result);
    }

    [Fact]
    public void GetBestAvailableFont_TrimsWhitespace()
    {
        // Arrange
        Func<string, bool> checker = f => f == "MyFont";
        var service = new FontService(_mockLogger.Object, checker);

        // Act
        var result = service.GetBestAvailableFont("  MyFont  ");

        // Assert
        Assert.Equal("MyFont", result);
    }

    [Fact]
    public void GetBestAvailableFont_RemovesQuotes()
    {
        // Arrange
        Func<string, bool> checker = f => f == "My Font";
        var service = new FontService(_mockLogger.Object, checker);

        // Act
        var result = service.GetBestAvailableFont("\"My Font\"");

        // Assert
        Assert.Equal("My Font", result);
    }

    [Fact]
    public void GetBestAvailableFont_FallsBackToMonospace()
    {
        // Arrange
        Func<string, bool> checker = f => false;
        var service = new FontService(_mockLogger.Object, checker);

        // Act
        var result = service.GetBestAvailableFont("Font1, Font2, Font3");

        // Assert
        Assert.Equal("monospace", result);
    }

    #endregion

    #region GetMonospaceFonts Tests

    [Fact]
    public void GetMonospaceFonts_ReturnsSortedList()
    {
        // Arrange
        Func<string, bool> checker = f => f.Contains("Mono");
        var service = new FontService(_mockLogger.Object, checker);

        // Act
        var fonts = service.GetMonospaceFonts();

        // Assert
        Assert.NotNull(fonts);
        // Verify sorted order
        var sorted = fonts.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(sorted, fonts);
    }

    [Fact]
    public void GetMonospaceFonts_IsCached()
    {
        // Arrange
        var callCount = 0;
        Func<string, bool> checker = f =>
        {
            callCount++;
            return f == "TestFont";
        };
        var service = new FontService(_mockLogger.Object, checker);

        // Act
        var fonts1 = service.GetMonospaceFonts();
        var fonts2 = service.GetMonospaceFonts();

        // Assert
        Assert.Same(fonts1, fonts2);
    }

    #endregion

    #region GetRecommendedFonts Tests

    [Fact]
    public void GetRecommendedFonts_ReturnsOnlyAvailableFonts()
    {
        // Arrange
        Func<string, bool> checker = f => f == "Fira Code" || f == "Menlo";
        var service = new FontService(_mockLogger.Object, checker);

        // Act
        var fonts = service.GetRecommendedFonts();

        // Assert
        Assert.All(fonts, f =>
        {
            Assert.True(f == "Fira Code" || f == "Menlo");
        });
    }

    [Fact]
    public void GetRecommendedFonts_IsCached()
    {
        // Arrange
        var service = new FontService(_mockLogger.Object, _ => true);

        // Act
        var fonts1 = service.GetRecommendedFonts();
        var fonts2 = service.GetRecommendedFonts();

        // Assert
        Assert.Same(fonts1, fonts2);
    }

    #endregion
}
