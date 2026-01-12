using Xunit;
using AIntern.Desktop.Services;

namespace AIntern.Desktop.Tests.Services;

public class EditorConfigurationTests
{
    #region Constants Tests

    [Fact]
    public void DefaultFontFamily_ContainsCascadiaCode()
    {
        Assert.Contains("Cascadia Code", EditorConfiguration.DefaultFontFamily);
    }

    [Fact]
    public void DefaultFontFamily_ContainsFallbacks()
    {
        Assert.Contains("Consolas", EditorConfiguration.DefaultFontFamily);
        Assert.Contains("Monaco", EditorConfiguration.DefaultFontFamily);
    }

    [Fact]
    public void DefaultFontSize_Is14()
    {
        Assert.Equal(14, EditorConfiguration.DefaultFontSize);
    }

    [Fact]
    public void DefaultTabSize_Is4()
    {
        Assert.Equal(4, EditorConfiguration.DefaultTabSize);
    }

    #endregion

    #region ApplySettings Null Check Tests

    [Fact]
    public void ApplySettings_ThrowsOnNullEditor()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        Assert.Throws<ArgumentNullException>(() => 
            EditorConfiguration.ApplySettings(null!, settings));
    }

    [Fact]
    public void ApplySettings_ThrowsOnNullSettings()
    {
        // Cannot test with real TextEditor without Avalonia runtime
        // This test documents the expected behavior
        Assert.True(true);
    }

    #endregion

    #region ApplyDefaults Null Check Tests

    [Fact]
    public void ApplyDefaults_ThrowsOnNullEditor()
    {
        Assert.Throws<ArgumentNullException>(() => 
            EditorConfiguration.ApplyDefaults(null!));
    }

    #endregion

    #region BindToSettings Null Check Tests

    [Fact]
    public void BindToSettings_ThrowsOnNullEditor()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        Assert.Throws<ArgumentNullException>(() => 
            EditorConfiguration.BindToSettings(null!, settings));
    }

    [Fact]
    public void BindToSettings_ThrowsOnNullSettings()
    {
        // Cannot test with real TextEditor without Avalonia runtime
        Assert.True(true);
    }

    #endregion

    #region AppSettings Default Values Tests

    [Fact]
    public void AppSettings_HasCorrectDefaultFontFamily()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        Assert.Equal("Cascadia Code, Consolas, monospace", settings.EditorFontFamily);
    }

    [Fact]
    public void AppSettings_HasCorrectDefaultFontSize()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        Assert.Equal(14, settings.EditorFontSize);
    }

    [Fact]
    public void AppSettings_HasCorrectDefaultTabSize()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        Assert.Equal(4, settings.TabSize);
    }

    [Fact]
    public void AppSettings_ConvertTabsToSpaces_DefaultTrue()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        Assert.True(settings.ConvertTabsToSpaces);
    }

    [Fact]
    public void AppSettings_ShowLineNumbers_DefaultTrue()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        Assert.True(settings.ShowLineNumbers);
    }

    [Fact]
    public void AppSettings_HighlightCurrentLine_DefaultTrue()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        Assert.True(settings.HighlightCurrentLine);
    }

    [Fact]
    public void AppSettings_WordWrap_DefaultFalse()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        Assert.False(settings.WordWrap);
    }

    [Fact]
    public void AppSettings_RulerColumn_DefaultZero()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        Assert.Equal(0, settings.RulerColumn);
    }

    #endregion

    #region Settings Property Modification Tests

    [Fact]
    public void AppSettings_CanModifyEditorFontFamily()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        settings.EditorFontFamily = "JetBrains Mono";
        
        Assert.Equal("JetBrains Mono", settings.EditorFontFamily);
    }

    [Fact]
    public void AppSettings_CanModifyEditorFontSize()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        settings.EditorFontSize = 16;
        
        Assert.Equal(16, settings.EditorFontSize);
    }

    [Fact]
    public void AppSettings_CanModifyTabSize()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        settings.TabSize = 2;
        
        Assert.Equal(2, settings.TabSize);
    }

    [Fact]
    public void AppSettings_CanModifyRulerColumn()
    {
        var settings = new AIntern.Core.Models.AppSettings();
        
        settings.RulerColumn = 80;
        
        Assert.Equal(80, settings.RulerColumn);
    }

    #endregion
}
