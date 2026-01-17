using AIntern.Desktop.Converters;
using Avalonia.Media;
using Xunit;

namespace AIntern.Desktop.Tests.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CONFIDENCE TO COLOR CONVERTER TESTS (v0.4.5e)                            │
// └─────────────────────────────────────────────────────────────────────────┘

public class ConfidenceToColorConverterTests
{
    private readonly ConfidenceToColorConverter _converter = new();

    [Theory]
    [InlineData(1.0)]
    [InlineData(0.9)]
    [InlineData(0.8)]
    public void Convert_HighConfidence_ReturnsHighBrush(double confidence)
    {
        var result = _converter.Convert(confidence, typeof(IBrush), null!, null!);
        Assert.Same(_converter.HighConfidenceBrush, result);
    }

    [Theory]
    [InlineData(0.79)]
    [InlineData(0.6)]
    [InlineData(0.5)]
    public void Convert_MediumConfidence_ReturnsMediumBrush(double confidence)
    {
        var result = _converter.Convert(confidence, typeof(IBrush), null!, null!);
        Assert.Same(_converter.MediumConfidenceBrush, result);
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(0.3)]
    [InlineData(0.0)]
    public void Convert_LowConfidence_ReturnsLowBrush(double confidence)
    {
        var result = _converter.Convert(confidence, typeof(IBrush), null!, null!);
        Assert.Same(_converter.LowConfidenceBrush, result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsLowBrush()
    {
        var result = _converter.Convert(null, typeof(IBrush), null!, null!);
        Assert.Same(_converter.LowConfidenceBrush, result);
    }

    [Fact]
    public void Convert_IntValue_ConvertsToPercentage()
    {
        // 80 as int should be treated as 80/100 = 0.8 = High
        var result = _converter.Convert(80, typeof(IBrush), null!, null!);
        Assert.Same(_converter.HighConfidenceBrush, result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(Brushes.Green, typeof(double), null!, null!));
    }
}
