using AIntern.Desktop.Converters;
using Xunit;

namespace AIntern.Desktop.Tests.Converters;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE SIZE CONVERTER TESTS (v0.4.5e)                                      │
// └─────────────────────────────────────────────────────────────────────────┘

public class FileSizeConverterTests
{
    private readonly FileSizeConverter _converter = new();

    [Fact]
    public void Convert_ZeroBytes_ReturnsZeroB()
    {
        var result = _converter.Convert(0L, typeof(string), null!, null!);
        Assert.Equal("0 B", result);
    }

    [Fact]
    public void Convert_NegativeBytes_ReturnsZeroB()
    {
        var result = _converter.Convert(-100L, typeof(string), null!, null!);
        Assert.Equal("0 B", result);
    }

    [Fact]
    public void Convert_SmallBytes_ShowsWholeNumber()
    {
        var result = _converter.Convert(512L, typeof(string), null!, null!);
        Assert.Equal("512 B", result);
    }

    [Fact]
    public void Convert_Kilobytes_ShowsOneDecimal()
    {
        var result = _converter.Convert(1536L, typeof(string), null!, null!); // 1.5 KB
        Assert.Equal("1.5 KB", result);
    }

    [Fact]
    public void Convert_Megabytes_ShowsOneDecimal()
    {
        var result = _converter.Convert(2_621_440L, typeof(string), null!, null!); // 2.5 MB
        Assert.Equal("2.5 MB", result);
    }

    [Fact]
    public void Convert_Gigabytes_ShowsOneDecimal()
    {
        var result = _converter.Convert(1_073_741_824L, typeof(string), null!, null!); // 1.0 GB
        Assert.Equal("1.0 GB", result);
    }

    [Fact]
    public void Convert_IntValue_WorksCorrectly()
    {
        var result = _converter.Convert(1024, typeof(string), null!, null!);
        Assert.Equal("1.0 KB", result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsZeroB()
    {
        var result = _converter.Convert(null, typeof(string), null!, null!);
        Assert.Equal("0 B", result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupported()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack("1.0 KB", typeof(long), null!, null!));
    }
}
