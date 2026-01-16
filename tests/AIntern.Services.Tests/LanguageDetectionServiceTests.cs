namespace AIntern.Services.Tests;

using AIntern.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="LanguageDetectionService"/> (v0.4.1c).
/// </summary>
public class LanguageDetectionServiceTests
{
    private readonly LanguageDetectionService _service;

    public LanguageDetectionServiceTests()
    {
        _service = new LanguageDetectionService();
    }

    #region DetectLanguage - Fence Tests

    [Theory]
    [InlineData("csharp", "csharp", "C#")]
    [InlineData("cs", "csharp", "C#")]
    [InlineData("c#", "csharp", "C#")]
    [InlineData("javascript", "javascript", "JavaScript")]
    [InlineData("js", "javascript", "JavaScript")]
    [InlineData("typescript", "typescript", "TypeScript")]
    [InlineData("ts", "typescript", "TypeScript")]
    [InlineData("python", "python", "Python")]
    [InlineData("py", "python", "Python")]
    public void DetectLanguage_FromFence_ReturnsNormalized(string fence, string expected, string display)
    {
        var (lang, disp) = _service.DetectLanguage(fence, "some code");
        Assert.Equal(expected, lang);
        Assert.Equal(display, disp);
    }

    [Fact]
    public void DetectLanguage_UnknownFence_ReturnsNull()
    {
        var (lang, disp) = _service.DetectLanguage("unknownlang", "some code");
        Assert.Null(lang);
        Assert.Null(disp);
    }

    #endregion

    #region DetectLanguage - Extension Tests

    [Theory]
    [InlineData("test.cs", "csharp")]
    [InlineData("test.py", "python")]
    [InlineData("test.js", "javascript")]
    [InlineData("test.ts", "typescript")]
    [InlineData("test.json", "json")]
    [InlineData("test.yaml", "yaml")]
    [InlineData("test.xml", "xml")]
    public void DetectLanguage_FromExtension_ReturnsCorrect(string path, string expected)
    {
        var (lang, _) = _service.DetectLanguage(null, "", path);
        Assert.Equal(expected, lang);
    }

    #endregion

    #region DetectLanguage - Content Heuristics Tests

    [Fact]
    public void DetectLanguage_CSharpContent_DetectsCorrectly()
    {
        var content = """
            using System;
            
            namespace Test
            {
                public class Foo { }
            }
            """;

        var (lang, _) = _service.DetectLanguage(null, content);
        Assert.Equal("csharp", lang);
    }

    [Fact]
    public void DetectLanguage_PythonContent_DetectsCorrectly()
    {
        var content = """
            def hello():
                print("Hello")
            """;

        var (lang, _) = _service.DetectLanguage(null, content);
        Assert.Equal("python", lang);
    }

    [Fact]
    public void DetectLanguage_JsonContent_DetectsCorrectly()
    {
        var content = """{ "name": "test" }""";

        var (lang, _) = _service.DetectLanguage(null, content);
        Assert.Equal("json", lang);
    }

    [Fact]
    public void DetectLanguage_XmlContent_DetectsCorrectly()
    {
        var content = """<?xml version="1.0"?><root></root>""";

        var (lang, _) = _service.DetectLanguage(null, content);
        Assert.Equal("xml", lang);
    }

    [Fact]
    public void DetectLanguage_SqlContent_DetectsCorrectly()
    {
        var content = "SELECT * FROM users WHERE id = 1";

        var (lang, _) = _service.DetectLanguage(null, content);
        Assert.Equal("sql", lang);
    }

    #endregion

    #region DetectLanguage - Priority Tests

    [Fact]
    public void DetectLanguage_FenceTakesPriority_OverExtension()
    {
        var (lang, _) = _service.DetectLanguage("python", "class Foo {}", "test.cs");
        Assert.Equal("python", lang);
    }

    [Fact]
    public void DetectLanguage_ExtensionTakesPriority_OverContent()
    {
        var (lang, _) = _service.DetectLanguage(null, "def hello():", "test.cs");
        Assert.Equal("csharp", lang);
    }

    #endregion

    #region NormalizeLanguageId Tests

    [Theory]
    [InlineData("cs", "csharp")]
    [InlineData("c#", "csharp")]
    [InlineData("js", "javascript")]
    [InlineData("sh", "bash")]
    [InlineData("yml", "yaml")]
    public void NormalizeLanguageId_ReturnsCanonical(string alias, string expected)
    {
        Assert.Equal(expected, _service.NormalizeLanguageId(alias));
    }

    [Fact]
    public void NormalizeLanguageId_Unknown_ReturnsNull()
    {
        Assert.Null(_service.NormalizeLanguageId("foobar"));
    }

    #endregion

    #region GetDisplayName Tests

    [Theory]
    [InlineData("csharp", "C#")]
    [InlineData("javascript", "JavaScript")]
    [InlineData("cpp", "C++")]
    public void GetDisplayName_ReturnsUserFriendly(string id, string expected)
    {
        Assert.Equal(expected, _service.GetDisplayName(id));
    }

    [Fact]
    public void GetDisplayName_Unknown_ReturnsNull()
    {
        Assert.Null(_service.GetDisplayName("unknown"));
    }

    #endregion

    #region GetFileExtension Tests

    [Theory]
    [InlineData("csharp", ".cs")]
    [InlineData("python", ".py")]
    [InlineData("javascript", ".js")]
    public void GetFileExtension_ReturnsCorrect(string id, string expected)
    {
        Assert.Equal(expected, _service.GetFileExtension(id));
    }

    [Fact]
    public void GetFileExtension_NoExtension_ReturnsNull()
    {
        Assert.Null(_service.GetFileExtension("dockerfile"));
    }

    #endregion

    #region GetLanguageForExtension Tests

    [Theory]
    [InlineData(".cs", "csharp")]
    [InlineData("cs", "csharp")]
    [InlineData(".py", "python")]
    [InlineData("py", "python")]
    public void GetLanguageForExtension_WorksWithAndWithoutDot(string ext, string expected)
    {
        Assert.Equal(expected, _service.GetLanguageForExtension(ext));
    }

    #endregion

    #region IsShellLanguage Tests

    [Theory]
    [InlineData("bash", true)]
    [InlineData("powershell", true)]
    [InlineData("cmd", true)]
    [InlineData("fish", true)]
    [InlineData("csharp", false)]
    [InlineData("python", false)]
    public void IsShellLanguage_ReturnsCorrect(string id, bool expected)
    {
        Assert.Equal(expected, _service.IsShellLanguage(id));
    }

    #endregion

    #region IsConfigLanguage Tests

    [Theory]
    [InlineData("json", true)]
    [InlineData("yaml", true)]
    [InlineData("xml", true)]
    [InlineData("toml", true)]
    [InlineData("ini", true)]
    [InlineData("terraform", true)]
    [InlineData("csharp", false)]
    [InlineData("python", false)]
    public void IsConfigLanguage_ReturnsCorrect(string id, bool expected)
    {
        Assert.Equal(expected, _service.IsConfigLanguage(id));
    }

    #endregion

    #region GetSupportedLanguages Tests

    [Fact]
    public void GetSupportedLanguages_ReturnsAll()
    {
        var languages = _service.GetSupportedLanguages();
        Assert.True(languages.Count >= 40);
        Assert.Contains("csharp", languages);
        Assert.Contains("python", languages);
        Assert.Contains("javascript", languages);
    }

    #endregion
}
