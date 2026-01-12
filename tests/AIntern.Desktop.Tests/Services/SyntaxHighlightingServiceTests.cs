using Xunit;
using TextMateSharp.Grammars;
using AIntern.Desktop.Services;

namespace AIntern.Desktop.Tests.Services;

public class SyntaxHighlightingServiceTests : IDisposable
{
    private readonly SyntaxHighlightingService _service;

    public SyntaxHighlightingServiceTests()
    {
        _service = new SyntaxHighlightingService();
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_UsesDarkThemeByDefault()
    {
        var service = new SyntaxHighlightingService();
        
        Assert.Equal(ThemeName.DarkPlus, service.CurrentTheme);
        service.Dispose();
    }

    [Fact]
    public void Constructor_UsesLightThemeWhenSpecified()
    {
        var service = new SyntaxHighlightingService(useDarkTheme: false);
        
        Assert.Equal(ThemeName.LightPlus, service.CurrentTheme);
        service.Dispose();
    }

    #endregion

    #region GetScopeForLanguage Tests

    [Theory]
    [InlineData("csharp", "source.cs")]
    [InlineData("javascript", "source.js")]
    [InlineData("python", "source.python")]
    [InlineData("typescript", "source.ts")]
    [InlineData("html", "text.html.basic")]
    [InlineData("json", "source.json")]
    public void GetScopeForLanguage_ReturnsCorrectScope(string language, string expectedScope)
    {
        var scope = _service.GetScopeForLanguage(language);
        
        Assert.Equal(expectedScope, scope);
    }

    [Fact]
    public void GetScopeForLanguage_ReturnsNullForUnknown()
    {
        var scope = _service.GetScopeForLanguage("nonexistent");
        
        Assert.Null(scope);
    }

    [Fact]
    public void GetScopeForLanguage_IsCaseInsensitive()
    {
        var scope1 = _service.GetScopeForLanguage("CSHARP");
        var scope2 = _service.GetScopeForLanguage("CSharp");
        var scope3 = _service.GetScopeForLanguage("csharp");
        
        Assert.Equal("source.cs", scope1);
        Assert.Equal("source.cs", scope2);
        Assert.Equal("source.cs", scope3);
    }

    #endregion

    #region IsLanguageSupported Tests

    [Fact]
    public void IsLanguageSupported_ReturnsTrueForKnown()
    {
        Assert.True(_service.IsLanguageSupported("csharp"));
        Assert.True(_service.IsLanguageSupported("javascript"));
        Assert.True(_service.IsLanguageSupported("python"));
    }

    [Fact]
    public void IsLanguageSupported_ReturnsFalseForUnknown()
    {
        Assert.False(_service.IsLanguageSupported("nonexistent"));
        Assert.False(_service.IsLanguageSupported("unknown"));
    }

    #endregion

    #region SupportedLanguages Tests

    [Fact]
    public void SupportedLanguages_ContainsAllLanguages()
    {
        var languages = _service.SupportedLanguages;
        
        Assert.Contains("csharp", languages);
        Assert.Contains("javascript", languages);
        Assert.Contains("typescript", languages);
        Assert.Contains("python", languages);
        Assert.Contains("java", languages);
        Assert.Contains("go", languages);
        Assert.Contains("rust", languages);
        Assert.Contains("html", languages);
        Assert.Contains("css", languages);
        Assert.Contains("json", languages);
        Assert.Contains("yaml", languages);
        Assert.Contains("markdown", languages);
        Assert.Contains("sql", languages);
        Assert.Contains("dockerfile", languages);
    }

    [Fact]
    public void SupportedLanguages_HasCorrectCount()
    {
        var languages = _service.SupportedLanguages;
        
        // 48 languages supported
        Assert.Equal(48, languages.Count);
    }

    #endregion

    #region AvailableThemes Tests

    [Fact]
    public void AvailableThemes_ContainsAllThemes()
    {
        var themes = SyntaxHighlightingService.AvailableThemes;
        
        Assert.Contains(ThemeName.DarkPlus, themes);
        Assert.Contains(ThemeName.LightPlus, themes);
        Assert.Contains(ThemeName.Monokai, themes);
        Assert.Contains(ThemeName.SolarizedDark, themes);
        Assert.Contains(ThemeName.SolarizedLight, themes);
        Assert.Contains(ThemeName.HighContrastDark, themes);
        Assert.Contains(ThemeName.HighContrastLight, themes);
    }

    [Fact]
    public void AvailableThemes_HasSevenThemes()
    {
        Assert.Equal(7, SyntaxHighlightingService.AvailableThemes.Count);
    }

    #endregion

    #region ChangeTheme Tests

    [Fact]
    public void ChangeTheme_UpdatesCurrentTheme()
    {
        _service.ChangeTheme(ThemeName.Monokai);
        
        Assert.Equal(ThemeName.Monokai, _service.CurrentTheme);
    }

    [Fact]
    public void ChangeTheme_SkipsSameTheme()
    {
        var initialTheme = _service.CurrentTheme;
        
        _service.ChangeTheme(initialTheme); // No-op
        
        Assert.Equal(initialTheme, _service.CurrentTheme);
    }

    #endregion

    #region RegistryOptions Tests

    [Fact]
    public void RegistryOptions_IsNotNull()
    {
        Assert.NotNull(_service.RegistryOptions);
    }

    #endregion

    #region RegisteredEditorCount Tests

    [Fact]
    public void RegisteredEditorCount_StartsAtZero()
    {
        Assert.Equal(0, _service.RegisteredEditorCount);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var service = new SyntaxHighlightingService();
        
        service.Dispose();
        service.Dispose(); // Should not throw
        
        Assert.True(true);
    }

    #endregion

    #region Language Category Tests

    [Fact]
    public void Languages_HasAllDotNetLanguages()
    {
        Assert.True(_service.IsLanguageSupported("csharp"));
        Assert.True(_service.IsLanguageSupported("fsharp"));
        Assert.True(_service.IsLanguageSupported("vb"));
    }

    [Fact]
    public void Languages_HasAllWebLanguages()
    {
        Assert.True(_service.IsLanguageSupported("javascript"));
        Assert.True(_service.IsLanguageSupported("typescript"));
        Assert.True(_service.IsLanguageSupported("html"));
        Assert.True(_service.IsLanguageSupported("css"));
        Assert.True(_service.IsLanguageSupported("scss"));
        Assert.True(_service.IsLanguageSupported("vue"));
    }

    [Fact]
    public void Languages_HasAllSystemsLanguages()
    {
        Assert.True(_service.IsLanguageSupported("c"));
        Assert.True(_service.IsLanguageSupported("cpp"));
        Assert.True(_service.IsLanguageSupported("rust"));
        Assert.True(_service.IsLanguageSupported("go"));
        Assert.True(_service.IsLanguageSupported("swift"));
    }

    [Fact]
    public void Languages_HasAllJvmLanguages()
    {
        Assert.True(_service.IsLanguageSupported("java"));
        Assert.True(_service.IsLanguageSupported("kotlin"));
        Assert.True(_service.IsLanguageSupported("scala"));
        Assert.True(_service.IsLanguageSupported("groovy"));
    }

    [Fact]
    public void Languages_HasAllDataFormats()
    {
        Assert.True(_service.IsLanguageSupported("json"));
        Assert.True(_service.IsLanguageSupported("xml"));
        Assert.True(_service.IsLanguageSupported("yaml"));
        Assert.True(_service.IsLanguageSupported("toml"));
    }

    #endregion
}
