using Xunit;
using AIntern.Desktop.Utilities;
using AIntern.Desktop.Converters;
using System.Globalization;

namespace AIntern.Desktop.Tests.Utilities;

/// <summary>
/// Unit tests for FileIconProvider and FileIconConverter (v0.3.2c).
/// </summary>
public class FileIconProviderTests
{
    #region GetIconKeyForExtension Tests

    [Theory]
    [InlineData(".cs", "file-csharp")]
    [InlineData(".js", "file-javascript")]
    [InlineData(".ts", "file-typescript")]
    [InlineData(".py", "file-python")]
    [InlineData(".json", "file-json")]
    [InlineData(".md", "file-markdown")]
    public void GetIconKeyForExtension_ReturnsCorrectKey(string extension, string expected)
    {
        var result = FileIconProvider.GetIconKeyForExtension(extension);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".CS", "file-csharp")]
    [InlineData(".Cs", "file-csharp")]
    [InlineData(".JSON", "file-json")]
    public void GetIconKeyForExtension_IsCaseInsensitive(string extension, string expected)
    {
        var result = FileIconProvider.GetIconKeyForExtension(extension);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetIconKeyForExtension_ReturnsFileCodeForUnknown()
    {
        var result = FileIconProvider.GetIconKeyForExtension(".xyz");
        Assert.Equal("file-code", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetIconKeyForExtension_ReturnsFileForEmpty(string? extension)
    {
        var result = FileIconProvider.GetIconKeyForExtension(extension);
        Assert.Equal("file", result);
    }

    [Fact]
    public void GetIconKeyForExtension_HandlesExtensionWithoutDot()
    {
        var result = FileIconProvider.GetIconKeyForExtension("cs");
        Assert.Equal("file-csharp", result);
    }

    #endregion

    #region GetIconKeyForFileName Tests

    [Theory]
    [InlineData("Dockerfile", "file-config")]
    [InlineData("Makefile", "file-shell")]
    [InlineData("package.json", "file-json")]
    [InlineData("tsconfig.json", "file-json")]
    public void GetIconKeyForFileName_HandlesSpecialFiles(string fileName, string expected)
    {
        var result = FileIconProvider.GetIconKeyForFileName(fileName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetIconKeyForFileName_FallsBackToExtension()
    {
        var result = FileIconProvider.GetIconKeyForFileName("test.cs");
        Assert.Equal("file-csharp", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetIconKeyForFileName_ReturnsFileForEmpty(string? fileName)
    {
        var result = FileIconProvider.GetIconKeyForFileName(fileName!);
        Assert.Equal("file", result);
    }

    #endregion

    #region GetIconPath Tests

    [Fact]
    public void GetIconPath_ReturnsValidSvgPath()
    {
        var result = FileIconProvider.GetIconPath("file");
        Assert.NotEmpty(result);
        Assert.Contains("M", result); // SVG paths start with M
    }

    [Fact]
    public void GetIconPath_ReturnsFilePathForUnknown()
    {
        var result = FileIconProvider.GetIconPath("nonexistent-icon");
        var expected = FileIconProvider.GetIconPath("file");
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetIconPath_ReturnsFilePathForEmpty(string? iconKey)
    {
        var result = FileIconProvider.GetIconPath(iconKey!);
        var expected = FileIconProvider.GetIconPath("file");
        Assert.Equal(expected, result);
    }

    #endregion

    #region GetAllIconKeys Tests

    [Fact]
    public void GetAllIconKeys_ReturnsAtLeast25Keys()
    {
        var keys = FileIconProvider.GetAllIconKeys();
        Assert.True(keys.Count >= 25, $"Expected at least 25 icon keys, got {keys.Count}");
    }

    [Fact]
    public void GetAllIconKeys_ContainsEssentialKeys()
    {
        var keys = FileIconProvider.GetAllIconKeys();
        Assert.Contains("folder", keys);
        Assert.Contains("folder-open", keys);
        Assert.Contains("file", keys);
        Assert.Contains("file-csharp", keys);
    }

    #endregion

    #region FileIconConverter Tests

    [Fact]
    public void FileIconConverter_Instance_IsNotNull()
    {
        Assert.NotNull(FileIconConverter.Instance);
    }

    [Fact]
    public void FileIconConverter_ConvertBackThrows()
    {
        var converter = new FileIconConverter();
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture));
    }

    #endregion
}
