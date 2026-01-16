namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET OPTIONS VALIDATION (v0.4.5d)                                    │
// │ Validation result for snippet apply options.                            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Severity of a validation issue.
/// </summary>
public enum SnippetOptionsValidationSeverity
{
    /// <summary>
    /// Informational message, not a problem.
    /// </summary>
    Info,

    /// <summary>
    /// Warning that may indicate a problem.
    /// </summary>
    Warning,

    /// <summary>
    /// Error that prevents the operation.
    /// </summary>
    Error
}

/// <summary>
/// A single validation issue.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5d.</para>
/// </remarks>
public sealed record SnippetOptionsValidationIssue(
    SnippetOptionsValidationSeverity Severity,
    string Message);

/// <summary>
/// Result of validating snippet apply options.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5d.</para>
/// </remarks>
public sealed record SnippetOptionsValidationResult
{
    /// <summary>
    /// All validation issues found.
    /// </summary>
    public IReadOnlyList<SnippetOptionsValidationIssue> Issues { get; }

    public SnippetOptionsValidationResult(IReadOnlyList<SnippetOptionsValidationIssue> issues)
    {
        Issues = issues;
    }

    /// <summary>
    /// Whether validation passed (no errors).
    /// </summary>
    public bool IsValid => !Issues.Any(i => i.Severity == SnippetOptionsValidationSeverity.Error);

    /// <summary>
    /// Whether there are any warnings.
    /// </summary>
    public bool HasWarnings => Issues.Any(i => i.Severity == SnippetOptionsValidationSeverity.Warning);

    /// <summary>
    /// Error messages only.
    /// </summary>
    public IEnumerable<string> Errors =>
        Issues.Where(i => i.Severity == SnippetOptionsValidationSeverity.Error)
              .Select(i => i.Message);

    /// <summary>
    /// Warning messages only.
    /// </summary>
    public IEnumerable<string> Warnings =>
        Issues.Where(i => i.Severity == SnippetOptionsValidationSeverity.Warning)
              .Select(i => i.Message);

    /// <summary>
    /// A successful validation with no issues.
    /// </summary>
    public static SnippetOptionsValidationResult Valid => new([]);

    /// <summary>
    /// Creates a validation result with a single error.
    /// </summary>
    public static SnippetOptionsValidationResult WithError(string message) =>
        new([new SnippetOptionsValidationIssue(SnippetOptionsValidationSeverity.Error, message)]);
}
