// ============================================================================
// ParameterSliderTests.cs
// AIntern.Desktop.Tests - Parameter Slider Control Tests (v0.2.3d)
// ============================================================================
// Unit tests for the ParameterSlider templated control, covering value
// coercion and FormattedValue formatting. Keyboard navigation tests are
// excluded as they require the full Avalonia platform with input simulation.
// ============================================================================

using AIntern.Desktop.Controls;
using Xunit;

namespace AIntern.Desktop.Tests.Controls;

/// <summary>
/// Unit tests for <see cref="ParameterSlider"/> control.
/// </summary>
/// <remarks>
/// <para>
/// Tests cover the following v0.2.3d functionality:
/// </para>
/// <list type="bullet">
///   <item><description>Value coercion: Values are clamped to [Minimum, Maximum]</description></item>
///   <item><description>FormattedValue: Proper formatting with format string, unit suffix, IsInteger mode</description></item>
/// </list>
/// <para>
/// <b>Excluded Tests:</b> Keyboard navigation tests (Arrow, Shift+Arrow, Home/End) are
/// excluded because they require Avalonia's input system and full templated control
/// rendering. These would require the Avalonia.Headless test framework.
/// </para>
/// </remarks>
public class ParameterSliderTests
{
    #region Value Coercion Tests

    [Fact]
    public void Value_WhenBelowMinimum_ClampsToMinimum()
    {
        // Arrange
        var slider = new ParameterSlider
        {
            Minimum = 0,
            Maximum = 100
        };

        // Act
        slider.Value = -50;

        // Assert
        Assert.Equal(0, slider.Value);
    }

    [Fact]
    public void Value_WhenAboveMaximum_ClampsToMaximum()
    {
        // Arrange
        var slider = new ParameterSlider
        {
            Minimum = 0,
            Maximum = 100
        };

        // Act
        slider.Value = 150;

        // Assert
        Assert.Equal(100, slider.Value);
    }

    [Fact]
    public void Value_WhenWithinRange_IsNotClamped()
    {
        // Arrange
        var slider = new ParameterSlider
        {
            Minimum = 0,
            Maximum = 100
        };

        // Act
        slider.Value = 50;

        // Assert
        Assert.Equal(50, slider.Value);
    }

    [Fact]
    public void Value_WhenMinimumChanges_RecoercesToNewMinimum()
    {
        // Arrange
        var slider = new ParameterSlider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 25
        };

        // Act - raise minimum above current value
        slider.Minimum = 50;

        // Assert - value should be clamped to new minimum
        Assert.Equal(50, slider.Value);
    }

    [Fact]
    public void Value_WhenMaximumChanges_RecoercesToNewMaximum()
    {
        // Arrange
        var slider = new ParameterSlider
        {
            Minimum = 0,
            Maximum = 100,
            Value = 75
        };

        // Act - lower maximum below current value
        slider.Maximum = 50;

        // Assert - value should be clamped to new maximum
        Assert.Equal(50, slider.Value);
    }

    [Fact]
    public void Value_AtMinimumBoundary_IsAccepted()
    {
        // Arrange - Must set Maximum first since default is 1.0 and Minimum > 1.0 would fail
        var slider = new ParameterSlider();
        slider.Maximum = 100;
        slider.Minimum = 10;

        // Act
        slider.Value = 10;

        // Assert
        Assert.Equal(10, slider.Value);
    }

    [Fact]
    public void Value_AtMaximumBoundary_IsAccepted()
    {
        // Arrange
        var slider = new ParameterSlider
        {
            Minimum = 0,
            Maximum = 100
        };

        // Act
        slider.Value = 100;

        // Assert
        Assert.Equal(100, slider.Value);
    }

    #endregion

    #region FormattedValue Tests

    [Fact]
    public void FormattedValue_WithDefaultFormat_ReturnsOneDecimalPlace()
    {
        // Arrange
        var slider = new ParameterSlider
        {
            Value = 0.7
        };

        // Act
        var formatted = slider.FormattedValue;

        // Assert
        Assert.Equal("0.7", formatted);
    }

    [Fact]
    public void FormattedValue_WithF2Format_ReturnsTwoDecimalPlaces()
    {
        // Arrange
        var slider = new ParameterSlider
        {
            Value = 0.95,
            ValueFormat = "F2"
        };

        // Act
        var formatted = slider.FormattedValue;

        // Assert
        Assert.Equal("0.95", formatted);
    }

    [Fact]
    public void FormattedValue_WithUnit_AppendsUnitSuffix()
    {
        // Arrange - Must set Maximum before Value since default Maximum is 1.0
        var slider = new ParameterSlider();
        slider.Maximum = 10000;
        slider.Value = 2048;
        slider.ValueFormat = "F0";
        slider.Unit = "tokens";

        // Act
        var formatted = slider.FormattedValue;

        // Assert
        Assert.Equal("2048 tokens", formatted);
    }

    [Fact]
    public void FormattedValue_WithNullUnit_OmitsUnitSuffix()
    {
        // Arrange
        var slider = new ParameterSlider
        {
            Value = 0.7,
            Unit = null
        };

        // Act
        var formatted = slider.FormattedValue;

        // Assert
        Assert.Equal("0.7", formatted);
    }

    [Fact]
    public void FormattedValue_WhenIsIntegerTrue_IgnoresValueFormatAndUsesF0()
    {
        // Arrange - Must set Maximum before Value since default Maximum is 1.0
        var slider = new ParameterSlider();
        slider.Maximum = 10000;
        slider.Value = 1024.567;
        slider.ValueFormat = "F2"; // Should be ignored
        slider.IsInteger = true;

        // Act
        var formatted = slider.FormattedValue;

        // Assert - F0 should round to nearest integer
        Assert.Equal("1025", formatted);
    }

    [Fact]
    public void FormattedValue_WhenIsIntegerFalse_UsesValueFormat()
    {
        // Arrange - Must set Maximum before Value since default Maximum is 1.0
        var slider = new ParameterSlider();
        slider.Maximum = 10;
        slider.Value = 1.5;
        slider.ValueFormat = "F2";
        slider.IsInteger = false;

        // Act
        var formatted = slider.FormattedValue;

        // Assert
        Assert.Equal("1.50", formatted);
    }

    [Fact]
    public void FormattedValue_WithIsIntegerAndUnit_CombinesBoth()
    {
        // Arrange - Must set Maximum before Value since default Maximum is 1.0
        var slider = new ParameterSlider();
        slider.Maximum = 10000;
        slider.Value = 4096;
        slider.IsInteger = true;
        slider.Unit = "tokens";

        // Act
        var formatted = slider.FormattedValue;

        // Assert
        Assert.Equal("4096 tokens", formatted);
    }

    [Theory]
    [InlineData(0.0, "0.0")]
    [InlineData(1.0, "1.0")]
    [InlineData(0.5, "0.5")]
    [InlineData(2.0, "2.0")]
    public void FormattedValue_VariousValues_FormatsCorrectly(double value, string expected)
    {
        // Arrange - Must set Maximum before Value since default Maximum is 1.0
        var slider = new ParameterSlider();
        slider.Maximum = 10;
        slider.Value = value;
        slider.ValueFormat = "F1";

        // Act
        var formatted = slider.FormattedValue;

        // Assert
        Assert.Equal(expected, formatted);
    }

    #endregion

    #region Default Property Value Tests

    [Fact]
    public void DefaultLabel_IsParameter()
    {
        // Arrange & Act
        var slider = new ParameterSlider();

        // Assert
        Assert.Equal("Parameter", slider.Label);
    }

    [Fact]
    public void DefaultValue_IsZero()
    {
        // Arrange & Act
        var slider = new ParameterSlider();

        // Assert
        Assert.Equal(0.0, slider.Value);
    }

    [Fact]
    public void DefaultMinimum_IsZero()
    {
        // Arrange & Act
        var slider = new ParameterSlider();

        // Assert
        Assert.Equal(0.0, slider.Minimum);
    }

    [Fact]
    public void DefaultMaximum_IsOne()
    {
        // Arrange & Act
        var slider = new ParameterSlider();

        // Assert
        Assert.Equal(1.0, slider.Maximum);
    }

    [Fact]
    public void DefaultStep_IsPointOne()
    {
        // Arrange & Act
        var slider = new ParameterSlider();

        // Assert
        Assert.Equal(0.1, slider.Step);
    }

    [Fact]
    public void DefaultDescription_IsEmpty()
    {
        // Arrange & Act
        var slider = new ParameterSlider();

        // Assert
        Assert.Equal(string.Empty, slider.Description);
    }

    [Fact]
    public void DefaultValueFormat_IsF1()
    {
        // Arrange & Act
        var slider = new ParameterSlider();

        // Assert
        Assert.Equal("F1", slider.ValueFormat);
    }

    [Fact]
    public void DefaultUnit_IsNull()
    {
        // Arrange & Act
        var slider = new ParameterSlider();

        // Assert
        Assert.Null(slider.Unit);
    }

    [Fact]
    public void DefaultShowDescription_IsTrue()
    {
        // Arrange & Act
        var slider = new ParameterSlider();

        // Assert
        Assert.True(slider.ShowDescription);
    }

    [Fact]
    public void DefaultIsInteger_IsFalse()
    {
        // Arrange & Act
        var slider = new ParameterSlider();

        // Assert
        Assert.False(slider.IsInteger);
    }

    #endregion

    #region Property Setter Tests

    [Fact]
    public void Label_CanBeSet()
    {
        // Arrange
        var slider = new ParameterSlider();

        // Act
        slider.Label = "Temperature";

        // Assert
        Assert.Equal("Temperature", slider.Label);
    }

    [Fact]
    public void Description_CanBeSet()
    {
        // Arrange
        var slider = new ParameterSlider();

        // Act
        slider.Description = "Balanced creativity";

        // Assert
        Assert.Equal("Balanced creativity", slider.Description);
    }

    [Fact]
    public void ShowDescription_CanBeSetToFalse()
    {
        // Arrange
        var slider = new ParameterSlider();

        // Act
        slider.ShowDescription = false;

        // Assert
        Assert.False(slider.ShowDescription);
    }

    #endregion

    // NOTE: Keyboard navigation tests (OnSliderKeyDown) are excluded because they
    // require the Avalonia input system and templated control rendering, which
    // cannot be unit tested without the Avalonia.Headless test framework.
    // The keyboard navigation functionality includes:
    // - Arrow keys (Left/Down decrease, Right/Up increase by Step)
    // - Shift+Arrow (increase/decrease by Step × 10)
    // - Home (jump to Minimum)
    // - End (jump to Maximum)
    // These are covered by integration/UI tests.
}
