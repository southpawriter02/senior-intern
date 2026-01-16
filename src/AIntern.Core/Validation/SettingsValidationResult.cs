namespace AIntern.Core.Validation;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SETTINGS VALIDATION RESULT (v0.4.5a)                                    │
// │ Result of validating AppSettings.                                       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Result of validating AppSettings.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5a.</para>
/// </remarks>
public sealed class SettingsValidationResult
{
    /// <summary>
    /// Creates a new validation result with the given issues.
    /// </summary>
    public SettingsValidationResult(IEnumerable<SettingsValidationIssue> issues)
    {
        Issues = issues?.ToList().AsReadOnly()
            ?? throw new ArgumentNullException(nameof(issues));
    }

    /// <summary>
    /// All validation issues found.
    /// </summary>
    public IReadOnlyList<SettingsValidationIssue> Issues { get; }

    /// <summary>
    /// True if all settings are valid (no errors, warnings acceptable).
    /// </summary>
    public bool IsValid => !Issues.Any(i => i.Severity == SettingsValidationSeverity.Error);

    /// <summary>
    /// True if there are no issues at all.
    /// </summary>
    public bool HasNoIssues => Issues.Count == 0;

    /// <summary>
    /// Get all error-severity issues.
    /// </summary>
    public IEnumerable<SettingsValidationIssue> Errors =>
        Issues.Where(i => i.Severity == SettingsValidationSeverity.Error);

    /// <summary>
    /// Get all warning-severity issues.
    /// </summary>
    public IEnumerable<SettingsValidationIssue> Warnings =>
        Issues.Where(i => i.Severity == SettingsValidationSeverity.Warning);

    /// <summary>
    /// Creates a valid result with no issues.
    /// </summary>
    public static SettingsValidationResult Valid() =>
        new(Array.Empty<SettingsValidationIssue>());
}

/// <summary>
/// A single validation issue for a settings property.
/// </summary>
/// <param name="PropertyName">Name of the property with the issue.</param>
/// <param name="Message">Human-readable description of the issue.</param>
/// <param name="Severity">Severity level of the issue.</param>
/// <param name="SuggestedValue">Suggested value to fix the issue (if applicable).</param>
/// <remarks>
/// <para>Added in v0.4.5a.</para>
/// </remarks>
public sealed record SettingsValidationIssue(
    string PropertyName,
    string Message,
    SettingsValidationSeverity Severity,
    object? SuggestedValue);

/// <summary>
/// Severity level for settings validation issues.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5a.</para>
/// </remarks>
public enum SettingsValidationSeverity
{
    /// <summary>
    /// Informational note, not a problem.
    /// </summary>
    Info,

    /// <summary>
    /// Out-of-range or unusual value, will be clamped/fixed.
    /// </summary>
    Warning,

    /// <summary>
    /// Invalid value that cannot be automatically fixed.
    /// </summary>
    Error
}
