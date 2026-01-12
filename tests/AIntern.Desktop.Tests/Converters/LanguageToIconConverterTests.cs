using System.Globalization;
using AIntern.Desktop.Converters;
using Xunit;

namespace AIntern.Desktop.Tests.Converters;

public class LanguageToIconConverterTests
{
    private readonly LanguageToIconConverter _converter = new();

    [Fact]
    public void Convert_NullValue_ReturnsNull()
    {
        // When Application.Current is null (unit test), GetGeometry returns null
        var result = _converter.Convert(null, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_EmptyString_ReturnsNull()
    {
        var result = _converter.Convert("", typeof(object), null, CultureInfo.InvariantCulture);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("csharp")]
    [InlineData("javascript")]
    [InlineData("typescript")]
    [InlineData("python")]
    [InlineData("html")]
    [InlineData("css")]
    [InlineData("json")]
    [InlineData("markdown")]
    [InlineData("xml")]
    [InlineData("yaml")]
    [InlineData("rust")]
    [InlineData("go")]
    [InlineData("java")]
    [InlineData("dockerfile")]
    public void Convert_KnownLanguage_ReturnsNull_WhenNoApplication(string language)
    {
        // When Application.Current is null (unit test), GetGeometry returns null
        // This tests that the converter doesn't throw for known languages
        var result = _converter.Convert(language, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.Null(result);
    }

    [Fact]
    public void Convert_UnknownLanguage_ReturnsNull_WhenNoApplication()
    {
        var result = _converter.Convert("unknown", typeof(object), null, CultureInfo.InvariantCulture);

        Assert.Null(result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void StaticInstance_ReturnsSameInstance()
    {
        var instance1 = LanguageToIconConverter.Instance;
        var instance2 = LanguageToIconConverter.Instance;

        Assert.Same(instance1, instance2);
    }
}
