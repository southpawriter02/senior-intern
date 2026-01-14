using Xunit;
using AIntern.Desktop.Converters;
using AIntern.Desktop.Utilities;

namespace AIntern.Desktop.Tests.Utilities;

/// <summary>
/// Unit tests for <see cref="FileIconProvider"/>.
/// </summary>
public class FileIconTests
{
    #region GetIconKeyForExtension Tests

    [Theory]
    [InlineData(".cs", "file-csharp")]
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
    [InlineData(".Json", "file-json")]
    [InlineData(".MD", "file-markdown")]
    public void GetIconKeyForExtension_IsCaseInsensitive(string extension, string expected)
    {
        var result = FileIconProvider.GetIconKeyForExtension(extension);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetIconKeyForExtension_UnknownExtension_ReturnsFileCode()
    {
        var result = FileIconProvider.GetIconKeyForExtension(".xyz");
        Assert.Equal("file-code", result);
    }

    [Fact]
    public void GetIconKeyForExtension_NullOrEmpty_ReturnsFile()
    {
        Assert.Equal("file", FileIconProvider.GetIconKeyForExtension(null!));
        Assert.Equal("file", FileIconProvider.GetIconKeyForExtension(""));
    }

    #endregion

    #region GetIconKeyForSpecialFile Tests

    [Theory]
    [InlineData("Dockerfile", "file-config")]
    [InlineData("Makefile", "file-shell")]
    [InlineData("package.json", "file-json")]
    [InlineData("README.md", "file-markdown")]
    [InlineData(".gitignore", "file-git")]
    public void GetIconKeyForSpecialFile_ReturnsCorrectKey(string fileName, string expected)
    {
        var result = FileIconProvider.GetIconKeyForSpecialFile(fileName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetIconKeyForSpecialFile_UnknownFile_ReturnsNull()
    {
        var result = FileIconProvider.GetIconKeyForSpecialFile("random.txt");
        Assert.Null(result);
    }

    #endregion

    #region GetIconPath Tests

    [Fact]
    public void GetIconPath_KnownKey_ReturnsPathData()
    {
        var result = FileIconProvider.GetIconPath("file-csharp");
        Assert.NotEmpty(result);
        Assert.StartsWith("M", result); // SVG paths start with M
    }

    [Fact]
    public void GetIconPath_UnknownKey_ReturnsFallback()
    {
        var result = FileIconProvider.GetIconPath("unknown-key");
        var fileResult = FileIconProvider.GetIconPath("file");
        Assert.Equal(fileResult, result);
    }

    [Fact]
    public void GetIconPath_FolderKeys_ReturnDifferentPaths()
    {
        var folder = FileIconProvider.GetIconPath("folder");
        var folderOpen = FileIconProvider.GetIconPath("folder-open");
        Assert.NotEqual(folder, folderOpen);
    }

    #endregion

    #region GetAllIconKeys Tests

    [Fact]
    public void GetAllIconKeys_ReturnsMultipleKeys()
    {
        var keys = FileIconProvider.GetAllIconKeys();
        Assert.True(keys.Count >= 20); // Should have 25+ icons
    }

    [Fact]
    public void GetAllIconKeys_ContainsExpectedKeys()
    {
        var keys = FileIconProvider.GetAllIconKeys();
        Assert.Contains("file", keys);
        Assert.Contains("folder", keys);
        Assert.Contains("file-csharp", keys);
        Assert.Contains("file-javascript", keys);
    }

    #endregion

    #region FileIconConverter Tests

    [Fact]
    public void FileIconConverter_Instance_IsSingleton()
    {
        var instance1 = FileIconConverter.Instance;
        var instance2 = FileIconConverter.Instance;
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void FileIconConverter_ConvertBack_ThrowsNotSupported()
    {
        var converter = FileIconConverter.Instance;
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(null, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture));
    }

    #endregion
}

