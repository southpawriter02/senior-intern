using AIntern.Core.Configuration;
using AIntern.Core.Models;

namespace AIntern.Core.Validation;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ APP SETTINGS VALIDATOR (v0.4.5a)                                        │
// │ Validates AppSettings values and applies constraints.                   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Validates AppSettings values and applies constraints.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5a.</para>
/// </remarks>
public static class AppSettingsValidator
{
    /// <summary>
    /// Validates the given settings and returns any validation issues.
    /// </summary>
    /// <param name="settings">The settings to validate.</param>
    /// <returns>A validation result containing any issues found.</returns>
    public static SettingsValidationResult Validate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var issues = new List<SettingsValidationIssue>();

        // Validate DiffContextLines
        if (settings.DiffContextLines < SettingsDefaults.MinDiffContextLines ||
            settings.DiffContextLines > SettingsDefaults.MaxDiffContextLines)
        {
            issues.Add(new SettingsValidationIssue(
                nameof(AppSettings.DiffContextLines),
                $"DiffContextLines must be between {SettingsDefaults.MinDiffContextLines} and {SettingsDefaults.MaxDiffContextLines}.",
                SettingsValidationSeverity.Warning,
                SettingsDefaults.DiffContextLines));
        }

        // Validate UndoWindowMinutes
        if (settings.UndoWindowMinutes < SettingsDefaults.MinUndoWindowMinutes ||
            settings.UndoWindowMinutes > SettingsDefaults.MaxUndoWindowMinutes)
        {
            issues.Add(new SettingsValidationIssue(
                nameof(AppSettings.UndoWindowMinutes),
                $"UndoWindowMinutes must be between {SettingsDefaults.MinUndoWindowMinutes} and {SettingsDefaults.MaxUndoWindowMinutes}.",
                SettingsValidationSeverity.Warning,
                SettingsDefaults.UndoWindowMinutes));
        }

        // Validate MaxBackupAgeDays
        if (settings.MaxBackupAgeDays < SettingsDefaults.MinBackupAgeDays ||
            settings.MaxBackupAgeDays > SettingsDefaults.MaxBackupAgeDays)
        {
            issues.Add(new SettingsValidationIssue(
                nameof(AppSettings.MaxBackupAgeDays),
                $"MaxBackupAgeDays must be between {SettingsDefaults.MinBackupAgeDays} and {SettingsDefaults.MaxBackupAgeDays}.",
                SettingsValidationSeverity.Warning,
                SettingsDefaults.MaxBackupAgeDays));
        }

        // Validate MaxChangeHistoryItems
        if (settings.MaxChangeHistoryItems < SettingsDefaults.MinChangeHistoryItems ||
            settings.MaxChangeHistoryItems > SettingsDefaults.MaxChangeHistoryItems)
        {
            issues.Add(new SettingsValidationIssue(
                nameof(AppSettings.MaxChangeHistoryItems),
                $"MaxChangeHistoryItems must be between {SettingsDefaults.MinChangeHistoryItems} and {SettingsDefaults.MaxChangeHistoryItems}.",
                SettingsValidationSeverity.Warning,
                SettingsDefaults.MaxChangeHistoryItems));
        }

        // Validate BackupDirectory path (if specified)
        if (!string.IsNullOrWhiteSpace(settings.BackupDirectory))
        {
            try
            {
                var invalidChars = Path.GetInvalidPathChars();
                if (settings.BackupDirectory.IndexOfAny(invalidChars) >= 0)
                {
                    issues.Add(new SettingsValidationIssue(
                        nameof(AppSettings.BackupDirectory),
                        "BackupDirectory contains invalid path characters.",
                        SettingsValidationSeverity.Error,
                        null));
                }
            }
            catch (Exception)
            {
                issues.Add(new SettingsValidationIssue(
                    nameof(AppSettings.BackupDirectory),
                    "BackupDirectory is not a valid path.",
                    SettingsValidationSeverity.Error,
                    null));
            }
        }

        // Validate DiffViewMode is a valid enum value
        if (!Enum.IsDefined(typeof(DiffViewMode), settings.DefaultDiffViewMode))
        {
            issues.Add(new SettingsValidationIssue(
                nameof(AppSettings.DefaultDiffViewMode),
                "DefaultDiffViewMode is not a valid value.",
                SettingsValidationSeverity.Warning,
                DiffViewMode.SideBySide));
        }

        return new SettingsValidationResult(issues);
    }

    /// <summary>
    /// Applies default values to any out-of-range settings.
    /// </summary>
    /// <param name="settings">The settings to normalize.</param>
    /// <returns>The normalized settings (same instance, modified in place).</returns>
    public static AppSettings ApplyDefaults(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Clamp DiffContextLines
        settings.DiffContextLines = Math.Clamp(
            settings.DiffContextLines,
            SettingsDefaults.MinDiffContextLines,
            SettingsDefaults.MaxDiffContextLines);

        // Clamp UndoWindowMinutes
        settings.UndoWindowMinutes = Math.Clamp(
            settings.UndoWindowMinutes,
            SettingsDefaults.MinUndoWindowMinutes,
            SettingsDefaults.MaxUndoWindowMinutes);

        // Clamp MaxBackupAgeDays
        settings.MaxBackupAgeDays = Math.Clamp(
            settings.MaxBackupAgeDays,
            SettingsDefaults.MinBackupAgeDays,
            SettingsDefaults.MaxBackupAgeDays);

        // Clamp MaxChangeHistoryItems
        settings.MaxChangeHistoryItems = Math.Clamp(
            settings.MaxChangeHistoryItems,
            SettingsDefaults.MinChangeHistoryItems,
            SettingsDefaults.MaxChangeHistoryItems);

        // Reset invalid DiffViewMode to default
        if (!Enum.IsDefined(typeof(DiffViewMode), settings.DefaultDiffViewMode))
        {
            settings.DefaultDiffViewMode = DiffViewMode.SideBySide;
        }

        return settings;
    }

    /// <summary>
    /// Gets the effective backup directory path.
    /// </summary>
    /// <param name="settings">The settings to read from.</param>
    /// <param name="workspaceRoot">The workspace root path.</param>
    /// <returns>The absolute path to the backup directory.</returns>
    public static string GetEffectiveBackupDirectory(AppSettings settings, string workspaceRoot)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);

        if (!string.IsNullOrWhiteSpace(settings.BackupDirectory))
        {
            return Path.IsPathRooted(settings.BackupDirectory)
                ? settings.BackupDirectory
                : Path.Combine(workspaceRoot, settings.BackupDirectory);
        }

        return Path.Combine(workspaceRoot, SettingsDefaults.DefaultBackupSubdirectory);
    }
}
