using System.Globalization;
using Xunit;
using AIntern.Desktop.Converters;

namespace AIntern.Desktop.Tests.Converters;

/// <summary>
/// Unit tests for TokenCountConverter (v0.3.4d).
/// </summary>
public class TokenCountConverterTests
{
    private readonly TokenCountConverter _converter = TokenCountConverter.Instance;

    #region Instance Tests

    [Fact]
    public void Instance_ReturnsSameInstance()
    {
        // Arrange & Act
        var instance1 = TokenCountConverter.Instance;
        var instance2 = TokenCountConverter.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    #endregion

    #region Convert Tests

    [Fact]
    public void Convert_Integer_ReturnsFormattedString()
    {
        // Arrange
        var value = 1650;

        // Act
        var result = _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("1,650", result);
    }

    [Fact]
    public void Convert_LargeInteger_ReturnsFormattedString()
    {
        // Arrange
        var value = 10000;

        // Act
        var result = _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("10,000", result);
    }

    [Fact]
    public void Convert_Long_ReturnsFormattedString()
    {
        // Arrange
        var value = 1000000L;

        // Act
        var result = _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("1,000,000", result);
    }

    [Fact]
    public void Convert_Zero_ReturnsZero()
    {
        // Arrange
        var value = 0;

        // Act
        var result = _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("0", result);
    }

    [Fact]
    public void Convert_Null_ReturnsZeroString()
    {
        // Arrange & Act
        var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("0", result);
    }

    [Fact]
    public void Convert_String_ReturnsOriginalString()
    {
        // Arrange
        var value = "not a number";

        // Act
        var result = _converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("not a number", result);
    }

    #endregion

    #region ConvertBack Tests

    [Fact]
    public void ConvertBack_FormattedString_ReturnsInt()
    {
        // Arrange
        var value = "1,650";

        // Act
        var result = _converter.ConvertBack(value, typeof(int), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(1650, result);
    }

    [Fact]
    public void ConvertBack_UnformattedString_ReturnsInt()
    {
        // Arrange
        var value = "1650";

        // Act
        var result = _converter.ConvertBack(value, typeof(int), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(1650, result);
    }

    [Fact]
    public void ConvertBack_InvalidString_ReturnsZero()
    {
        // Arrange
        var value = "not a number";

        // Act
        var result = _converter.ConvertBack(value, typeof(int), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ConvertBack_Null_ReturnsZero()
    {
        // Arrange & Act
        var result = _converter.ConvertBack(null, typeof(int), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ConvertBack_EmptyString_ReturnsZero()
    {
        // Arrange
        var value = "";

        // Act
        var result = _converter.ConvertBack(value, typeof(int), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion
}
