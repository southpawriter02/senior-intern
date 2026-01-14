using System.Globalization;
using Xunit;
using AIntern.Desktop.Converters;

namespace AIntern.Desktop.Tests.Converters;

/// <summary>
/// Unit tests for the <see cref="PinTextConverter"/> class.
/// Verifies conversion logic for pin/unpin context menu text.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the PinTextConverter correctly handles:
/// </para>
/// <list type="bullet">
///   <item><description>Boolean true → "Unpin" conversion</description></item>
///   <item><description>Boolean false → "Pin" conversion</description></item>
///   <item><description>Null value → "Pin" fallback</description></item>
///   <item><description>Non-boolean types → "Pin" fallback</description></item>
///   <item><description>ConvertBack throws NotSupportedException</description></item>
/// </list>
/// </remarks>
public class PinTextConverterTests
{
    #region Singleton Instance Tests

    /// <summary>
    /// Verifies that the Instance property returns a non-null singleton.
    /// </summary>
    [Fact]
    public void Instance_ReturnsNonNull()
    {
        // Act
        var instance = PinTextConverter.Instance;

        // Assert
        Assert.NotNull(instance);
    }

    /// <summary>
    /// Verifies that the Instance property returns the same instance on repeated calls.
    /// </summary>
    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        // Act
        var instance1 = PinTextConverter.Instance;
        var instance2 = PinTextConverter.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    #endregion

    #region Convert Tests

    /// <summary>
    /// Verifies that Convert returns "Unpin" when value is true.
    /// </summary>
    [Fact]
    public void Convert_WithTrue_ReturnsUnpin()
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act
        var result = converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Unpin", result);
    }

    /// <summary>
    /// Verifies that Convert returns "Pin" when value is false.
    /// </summary>
    [Fact]
    public void Convert_WithFalse_ReturnsPin()
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act
        var result = converter.Convert(false, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Pin", result);
    }

    /// <summary>
    /// Verifies that Convert returns "Pin" fallback when value is null.
    /// </summary>
    [Fact]
    public void Convert_WithNull_ReturnsPin()
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act
        var result = converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Pin", result);
    }

    /// <summary>
    /// Verifies that Convert returns "Pin" fallback for non-boolean string values.
    /// </summary>
    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("Pin")]
    [InlineData("Unpin")]
    [InlineData("")]
    public void Convert_WithStringValue_ReturnsPin(string value)
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act
        var result = converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Pin", result);
    }

    /// <summary>
    /// Verifies that Convert returns "Pin" fallback for non-boolean numeric values.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    public void Convert_WithIntValue_ReturnsPin(int value)
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act
        var result = converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Pin", result);
    }

    /// <summary>
    /// Verifies that Convert works correctly regardless of the culture parameter.
    /// </summary>
    [Fact]
    public void Convert_WithDifferentCultures_ReturnsCorrectValue()
    {
        // Arrange
        var converter = PinTextConverter.Instance;
        var cultures = new[]
        {
            CultureInfo.InvariantCulture,
            CultureInfo.GetCultureInfo("en-US"),
            CultureInfo.GetCultureInfo("de-DE"),
            CultureInfo.GetCultureInfo("ja-JP")
        };

        foreach (var culture in cultures)
        {
            // Act
            var trueResult = converter.Convert(true, typeof(string), null, culture);
            var falseResult = converter.Convert(false, typeof(string), null, culture);

            // Assert
            Assert.Equal("Unpin", trueResult);
            Assert.Equal("Pin", falseResult);
        }
    }

    /// <summary>
    /// Verifies that Convert ignores the parameter value.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("ignored")]
    [InlineData(123)]
    public void Convert_WithParameter_IgnoresParameter(object? parameter)
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act
        var trueResult = converter.Convert(true, typeof(string), parameter, CultureInfo.InvariantCulture);
        var falseResult = converter.Convert(false, typeof(string), parameter, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Unpin", trueResult);
        Assert.Equal("Pin", falseResult);
    }

    /// <summary>
    /// Verifies that Convert returns string type result.
    /// </summary>
    [Fact]
    public void Convert_ReturnsStringType()
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act
        var result = converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.IsType<string>(result);
    }

    #endregion

    #region ConvertBack Tests

    /// <summary>
    /// Verifies that ConvertBack throws NotSupportedException.
    /// </summary>
    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack("Pin", typeof(bool), null, CultureInfo.InvariantCulture));

        Assert.Contains("PinTextConverter does not support ConvertBack", exception.Message);
    }

    /// <summary>
    /// Verifies that ConvertBack throws for any input value.
    /// </summary>
    [Theory]
    [InlineData("Pin")]
    [InlineData("Unpin")]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void ConvertBack_WithAnyValue_ThrowsNotSupportedException(object? value)
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(value, typeof(bool), null, CultureInfo.InvariantCulture));
    }

    #endregion

    #region SetLogger Tests

    /// <summary>
    /// Verifies that SetLogger can be called with null without throwing.
    /// </summary>
    [Fact]
    public void SetLogger_WithNull_DoesNotThrow()
    {
        // Act & Assert - should not throw
        PinTextConverter.SetLogger(null);
    }

    #endregion
}
