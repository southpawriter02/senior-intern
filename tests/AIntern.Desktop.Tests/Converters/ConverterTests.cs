using System.Globalization;
using AIntern.Desktop.Converters;
using Avalonia.Media;  // For FontWeight
using Xunit;

namespace AIntern.Desktop.Tests.Converters;

/// <summary>
/// Unit tests for value converters in AIntern.Desktop.
/// Tests v0.2.3e converters (BoolToFontWeight, ExpandIcon) and
/// v0.2.2c converters (BoolToSelected, PinText).
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>Correct conversion logic for boolean values</description></item>
///   <item><description>Handling of null and non-boolean inputs</description></item>
///   <item><description>ConvertBack throws NotSupportedException for one-way converters</description></item>
///   <item><description>Singleton instance availability</description></item>
/// </list>
/// <para>Added in v0.2.5a (test coverage for v0.2.3e and v0.2.2c).</para>
/// </remarks>
public class ConverterTests
{
    #region BoolToFontWeightConverter Tests (v0.2.3e)

    /// <summary>
    /// Verifies Instance returns non-null singleton.
    /// </summary>
    [Fact]
    public void BoolToFontWeightConverter_Instance_ReturnsNonNull()
    {
        // Act
        var instance = BoolToFontWeightConverter.Instance;

        // Assert
        Assert.NotNull(instance);
    }

    /// <summary>
    /// Verifies Instance returns same singleton each time.
    /// </summary>
    [Fact]
    public void BoolToFontWeightConverter_Instance_ReturnsSameSingleton()
    {
        // Act
        var instance1 = BoolToFontWeightConverter.Instance;
        var instance2 = BoolToFontWeightConverter.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    /// <summary>
    /// Verifies Convert returns Bold for true.
    /// </summary>
    [Fact]
    public void BoolToFontWeightConverter_Convert_TrueReturnsBold()
    {
        // Arrange
        var converter = BoolToFontWeightConverter.Instance;

        // Act
        var result = converter.Convert(true, typeof(FontWeight), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(FontWeight.Bold, result);
    }

    /// <summary>
    /// Verifies Convert returns Normal for false.
    /// </summary>
    [Fact]
    public void BoolToFontWeightConverter_Convert_FalseReturnsNormal()
    {
        // Arrange
        var converter = BoolToFontWeightConverter.Instance;

        // Act
        var result = converter.Convert(false, typeof(FontWeight), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(FontWeight.Normal, result);
    }

    /// <summary>
    /// Verifies Convert returns Normal for null.
    /// </summary>
    [Fact]
    public void BoolToFontWeightConverter_Convert_NullReturnsNormal()
    {
        // Arrange
        var converter = BoolToFontWeightConverter.Instance;

        // Act
        var result = converter.Convert(null, typeof(FontWeight), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(FontWeight.Normal, result);
    }

    /// <summary>
    /// Verifies Convert returns Normal for non-boolean values.
    /// </summary>
    [Theory]
    [InlineData("true")]
    [InlineData(1)]
    [InlineData("Bold")]
    public void BoolToFontWeightConverter_Convert_NonBooleanReturnsNormal(object value)
    {
        // Arrange
        var converter = BoolToFontWeightConverter.Instance;

        // Act
        var result = converter.Convert(value, typeof(FontWeight), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(FontWeight.Normal, result);
    }

    /// <summary>
    /// Verifies ConvertBack throws NotSupportedException.
    /// </summary>
    [Fact]
    public void BoolToFontWeightConverter_ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange
        var converter = BoolToFontWeightConverter.Instance;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(FontWeight.Bold, typeof(bool), null, CultureInfo.InvariantCulture));
    }

    #endregion

    // NOTE: ExpandIconConverter tests are excluded because the converter uses
    // StreamGeometry.Parse() which requires Avalonia's rendering platform.
    // This cannot be unit tested without the Avalonia headless platform.
    // The converter logic is simple (bool ? expandedIcon : collapsedIcon) and
    // is covered by integration/UI tests.

    #region BoolToSelectedConverter Tests (v0.2.2c)

    /// <summary>
    /// Verifies Instance returns non-null singleton.
    /// </summary>
    [Fact]
    public void BoolToSelectedConverter_Instance_ReturnsNonNull()
    {
        // Act
        var instance = BoolToSelectedConverter.Instance;

        // Assert
        Assert.NotNull(instance);
    }

    /// <summary>
    /// Verifies Instance returns same singleton each time.
    /// </summary>
    [Fact]
    public void BoolToSelectedConverter_Instance_ReturnsSameSingleton()
    {
        // Act
        var instance1 = BoolToSelectedConverter.Instance;
        var instance2 = BoolToSelectedConverter.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    /// <summary>
    /// Verifies Convert returns "Selected" for true.
    /// </summary>
    [Fact]
    public void BoolToSelectedConverter_Convert_TrueReturnsSelected()
    {
        // Arrange
        var converter = BoolToSelectedConverter.Instance;

        // Act
        var result = converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Selected", result);
    }

    /// <summary>
    /// Verifies Convert returns empty string for false.
    /// </summary>
    [Fact]
    public void BoolToSelectedConverter_Convert_FalseReturnsEmpty()
    {
        // Arrange
        var converter = BoolToSelectedConverter.Instance;

        // Act
        var result = converter.Convert(false, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Verifies Convert returns empty string for null.
    /// </summary>
    [Fact]
    public void BoolToSelectedConverter_Convert_NullReturnsEmpty()
    {
        // Arrange
        var converter = BoolToSelectedConverter.Instance;

        // Act
        var result = converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Verifies Convert returns empty string for non-boolean values.
    /// </summary>
    [Theory]
    [InlineData("true")]
    [InlineData(1)]
    [InlineData("Selected")]
    public void BoolToSelectedConverter_Convert_NonBooleanReturnsEmpty(object value)
    {
        // Arrange
        var converter = BoolToSelectedConverter.Instance;

        // Act
        var result = converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Verifies ConvertBack throws NotSupportedException.
    /// </summary>
    [Fact]
    public void BoolToSelectedConverter_ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange
        var converter = BoolToSelectedConverter.Instance;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack("Selected", typeof(bool), null, CultureInfo.InvariantCulture));
    }

    #endregion

    #region PinTextConverter Tests (v0.2.2c)

    /// <summary>
    /// Verifies Instance returns non-null singleton.
    /// </summary>
    [Fact]
    public void PinTextConverter_Instance_ReturnsNonNull()
    {
        // Act
        var instance = PinTextConverter.Instance;

        // Assert
        Assert.NotNull(instance);
    }

    /// <summary>
    /// Verifies Instance returns same singleton each time.
    /// </summary>
    [Fact]
    public void PinTextConverter_Instance_ReturnsSameSingleton()
    {
        // Act
        var instance1 = PinTextConverter.Instance;
        var instance2 = PinTextConverter.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    /// <summary>
    /// Verifies Convert returns "Unpin" for true (already pinned).
    /// </summary>
    [Fact]
    public void PinTextConverter_Convert_TrueReturnsUnpin()
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act
        var result = converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Unpin", result);
    }

    /// <summary>
    /// Verifies Convert returns "Pin" for false (not pinned).
    /// </summary>
    [Fact]
    public void PinTextConverter_Convert_FalseReturnsPin()
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act
        var result = converter.Convert(false, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Pin", result);
    }

    /// <summary>
    /// Verifies Convert returns "Pin" for null.
    /// </summary>
    [Fact]
    public void PinTextConverter_Convert_NullReturnsPin()
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act
        var result = converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Pin", result);
    }

    /// <summary>
    /// Verifies Convert returns "Pin" for non-boolean values.
    /// </summary>
    [Theory]
    [InlineData("true")]
    [InlineData(1)]
    [InlineData("Unpin")]
    public void PinTextConverter_Convert_NonBooleanReturnsPin(object value)
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act
        var result = converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Pin", result);
    }

    /// <summary>
    /// Verifies ConvertBack throws NotSupportedException.
    /// </summary>
    [Fact]
    public void PinTextConverter_ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange
        var converter = PinTextConverter.Instance;

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack("Pin", typeof(bool), null, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies SetLogger accepts null without throwing.
    /// </summary>
    [Fact]
    public void PinTextConverter_SetLogger_AcceptsNull()
    {
        // Act & Assert - should not throw
        PinTextConverter.SetLogger(null);
    }

    #endregion
}
