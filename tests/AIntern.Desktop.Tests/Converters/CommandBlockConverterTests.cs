using Xunit;
using System;
using System.Globalization;
using AIntern.Desktop.Converters;

namespace AIntern.Desktop.Tests.Converters;

/// <summary>
/// Unit tests for <see cref="EqualityConverter"/>.
/// </summary>
/// <remarks>Added in v0.5.4f.</remarks>
public sealed class EqualityConverterTests
{
    private readonly EqualityConverter _converter = new();

    // ═══════════════════════════════════════════════════════════════════════
    // Basic Equality
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Convert_BothNull_ReturnsTrue()
    {
        var result = _converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.True((bool)result!);
    }

    [Fact]
    public void Convert_ValueNullParameterNotNull_ReturnsFalse()
    {
        var result = _converter.Convert(null, typeof(bool), "test", CultureInfo.InvariantCulture);
        Assert.False((bool)result!);
    }

    [Fact]
    public void Convert_ValueNotNullParameterNull_ReturnsFalse()
    {
        var result = _converter.Convert("test", typeof(bool), null, CultureInfo.InvariantCulture);
        Assert.False((bool)result!);
    }

    [Fact]
    public void Convert_EqualStrings_ReturnsTrue()
    {
        var result = _converter.Convert("success", typeof(bool), "success", CultureInfo.InvariantCulture);
        Assert.True((bool)result!);
    }

    [Fact]
    public void Convert_DifferentStrings_ReturnsFalse()
    {
        var result = _converter.Convert("success", typeof(bool), "error", CultureInfo.InvariantCulture);
        Assert.False((bool)result!);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Case Insensitivity
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Convert_CaseInsensitive_ReturnsTrue()
    {
        var result = _converter.Convert("SUCCESS", typeof(bool), "success", CultureInfo.InvariantCulture);
        Assert.True((bool)result!);
    }

    [Fact]
    public void Convert_MixedCase_ReturnsTrue()
    {
        var result = _converter.Convert("SuCcEsS", typeof(bool), "SUCCESS", CultureInfo.InvariantCulture);
        Assert.True((bool)result!);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Non-String Values
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Convert_IntegerValues_ComparesToStrings()
    {
        var result = _converter.Convert(123, typeof(bool), "123", CultureInfo.InvariantCulture);
        Assert.True((bool)result!);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ConvertBack
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(true, typeof(string), "test", CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Unit tests for <see cref="BoolToTextConverter"/>.
/// </summary>
/// <remarks>Added in v0.5.4f.</remarks>
public sealed class BoolToTextConverterTests
{
    private readonly BoolToTextConverter _converter = new();

    // ═══════════════════════════════════════════════════════════════════════
    // True/False Text
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Convert_TrueValue_ReturnsTrueText()
    {
        var result = _converter.Convert(true, typeof(string), "Running...|Run", CultureInfo.InvariantCulture);
        Assert.Equal("Running...", result);
    }

    [Fact]
    public void Convert_FalseValue_ReturnsFalseText()
    {
        var result = _converter.Convert(false, typeof(string), "Running...|Run", CultureInfo.InvariantCulture);
        Assert.Equal("Run", result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Edge Cases
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Convert_NonBoolValue_ReturnsNull()
    {
        var result = _converter.Convert("notabool", typeof(string), "True|False", CultureInfo.InvariantCulture);
        Assert.Null(result);
    }

    [Fact]
    public void Convert_InvalidParameter_ReturnsValueString()
    {
        var result = _converter.Convert(true, typeof(string), "NoSeparator", CultureInfo.InvariantCulture);
        Assert.Equal("True", result);
    }

    [Fact]
    public void Convert_EmptyParameter_ReturnsValueString()
    {
        var result = _converter.Convert(true, typeof(string), "", CultureInfo.InvariantCulture);
        Assert.Equal("True", result);
    }

    [Fact]
    public void Convert_NullParameter_ReturnsValueString()
    {
        var result = _converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal("True", result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ConvertBack
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack("Run", typeof(bool), "Running...|Run", CultureInfo.InvariantCulture));
    }
}
