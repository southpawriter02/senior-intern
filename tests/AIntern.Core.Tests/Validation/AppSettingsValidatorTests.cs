namespace AIntern.Core.Tests.Validation;

using AIntern.Core.Configuration;
using AIntern.Core.Models;
using AIntern.Core.Validation;
using Xunit;

/// <summary>
/// Unit tests for <see cref="AppSettingsValidator"/>.
/// </summary>
public class AppSettingsValidatorTests
{
    [Fact]
    public void Validate_ValidSettings_ReturnsNoIssues()
    {
        var settings = new AppSettings();
        var result = AppSettingsValidator.Validate(settings);

        Assert.True(result.HasNoIssues);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DiffContextLinesBelowMin_ReturnsWarning()
    {
        var settings = new AppSettings { DiffContextLines = -1 };
        var result = AppSettingsValidator.Validate(settings);

        Assert.False(result.HasNoIssues);
        Assert.Single(result.Warnings.Where(w => w.PropertyName == nameof(AppSettings.DiffContextLines)));
    }

    [Fact]
    public void Validate_DiffContextLinesAboveMax_ReturnsWarning()
    {
        var settings = new AppSettings { DiffContextLines = 99 };
        var result = AppSettingsValidator.Validate(settings);

        Assert.False(result.HasNoIssues);
        Assert.Contains(result.Warnings, w => w.PropertyName == nameof(AppSettings.DiffContextLines));
    }

    [Fact]
    public void Validate_UndoWindowBelowMin_ReturnsWarning()
    {
        var settings = new AppSettings { UndoWindowMinutes = 1 };
        var result = AppSettingsValidator.Validate(settings);

        Assert.Contains(result.Warnings, w => w.PropertyName == nameof(AppSettings.UndoWindowMinutes));
    }

    [Fact]
    public void Validate_InvalidDiffViewMode_ReturnsWarning()
    {
        var settings = new AppSettings { DefaultDiffViewMode = (DiffViewMode)99 };
        var result = AppSettingsValidator.Validate(settings);

        Assert.Contains(result.Warnings, w => w.PropertyName == nameof(AppSettings.DefaultDiffViewMode));
    }

    [Fact]
    public void ApplyDefaults_ClampsDiffContextLines()
    {
        var settings = new AppSettings { DiffContextLines = 99 };
        AppSettingsValidator.ApplyDefaults(settings);

        Assert.Equal(SettingsDefaults.MaxDiffContextLines, settings.DiffContextLines);
    }

    [Fact]
    public void ApplyDefaults_ClampsUndoWindowMinutes()
    {
        var settings = new AppSettings { UndoWindowMinutes = 1 };
        AppSettingsValidator.ApplyDefaults(settings);

        Assert.Equal(SettingsDefaults.MinUndoWindowMinutes, settings.UndoWindowMinutes);
    }

    [Fact]
    public void ApplyDefaults_ResetsInvalidDiffViewMode()
    {
        var settings = new AppSettings { DefaultDiffViewMode = (DiffViewMode)99 };
        AppSettingsValidator.ApplyDefaults(settings);

        Assert.Equal(DiffViewMode.SideBySide, settings.DefaultDiffViewMode);
    }

    [Fact]
    public void GetEffectiveBackupDirectory_NullBackupDir_ReturnsDefault()
    {
        var settings = new AppSettings { BackupDirectory = null };
        var result = AppSettingsValidator.GetEffectiveBackupDirectory(settings, "/workspace");

        Assert.Contains(".aintern/backups", result);
    }

    [Fact]
    public void GetEffectiveBackupDirectory_RelativeBackupDir_CombinesWithWorkspace()
    {
        var settings = new AppSettings { BackupDirectory = "custom/backups" };
        var result = AppSettingsValidator.GetEffectiveBackupDirectory(settings, "/workspace");

        Assert.StartsWith("/workspace", result);
        Assert.Contains("custom/backups", result);
    }
}
