// ============================================================================
// File: ShellTypeToIconConverterTests.cs
// Path: tests/AIntern.Desktop.Tests/Converters/ShellTypeToIconConverterTests.cs
// Description: Unit tests for ShellTypeToIconConverter.
// Created: 2026-01-19
// AI Intern v0.5.5f - Terminal Settings Panel
// ============================================================================

namespace AIntern.Desktop.Tests.Converters;

using System.Globalization;
using AIntern.Core.Interfaces;
using AIntern.Desktop.Converters;
using Avalonia.Media;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ShellTypeToIconConverter"/>.
/// </summary>
public class ShellTypeToIconConverterTests
{
    private readonly ShellTypeToIconConverter _converter = new();

    #region Convert Tests

    [Theory]
    [InlineData(ShellType.Bash)]
    [InlineData(ShellType.Zsh)]
    [InlineData(ShellType.PowerShell)]
    [InlineData(ShellType.Cmd)]
    [InlineData(ShellType.Fish)]
    [InlineData(ShellType.Unknown)]
    public void Convert_ReturnsGeometry_ForAllShellTypes(ShellType shellType)
    {
        // Act
        var result = _converter.Convert(shellType, typeof(Geometry), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<Geometry>(result);
    }

    [Fact]
    public void Convert_ReturnsDefaultIcon_ForNullValue()
    {
        // Act
        var result = _converter.Convert(null, typeof(Geometry), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<Geometry>(result);
    }

    [Fact]
    public void Convert_ReturnsDefaultIcon_ForInvalidType()
    {
        // Act
        var result = _converter.Convert("invalid", typeof(Geometry), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<Geometry>(result);
    }

    #endregion

    #region ConvertBack Tests

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(null, typeof(ShellType), null, CultureInfo.InvariantCulture));
    }

    #endregion
}
