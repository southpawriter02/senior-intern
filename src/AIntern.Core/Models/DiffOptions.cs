namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF OPTIONS (v0.4.2b)                                                   │
// │ Configuration options for diff computation.                              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Configuration options for diff computation.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2b.</para>
/// <para>
/// Provides configurable behavior for the diff service including context lines,
/// inline diff computation, whitespace handling, and hunk separation thresholds.
/// </para>
/// </remarks>
public sealed class DiffOptions
{
    /// <summary>
    /// Number of unchanged context lines to include around changes.
    /// These lines help provide context for understanding the changes.
    /// </summary>
    /// <remarks>
    /// Standard Git default is 3. Increase for more context, decrease for compact diffs.
    /// </remarks>
    public int ContextLines { get; init; } = 3;

    /// <summary>
    /// Whether to compute inline character-level diffs for modified lines.
    /// When enabled, modified lines will have InlineChanges populated.
    /// </summary>
    /// <remarks>
    /// Inline diffs are computed in v0.4.2c. This option is passed through
    /// to enable/disable that functionality.
    /// </remarks>
    public bool ComputeInlineDiffs { get; init; } = true;

    /// <summary>
    /// Whether to ignore whitespace-only differences.
    /// When true, lines differing only in whitespace are treated as unchanged.
    /// </summary>
    public bool IgnoreWhitespace { get; init; } = false;

    /// <summary>
    /// Whether to ignore case differences.
    /// When true, lines differing only in case are treated as unchanged.
    /// </summary>
    public bool IgnoreCase { get; init; } = false;

    /// <summary>
    /// Whether to trim trailing whitespace before comparison.
    /// Helps normalize files with inconsistent trailing whitespace.
    /// </summary>
    public bool TrimTrailingWhitespace { get; init; } = true;

    /// <summary>
    /// Minimum number of unchanged lines required to separate hunks.
    /// When there are more than this many unchanged lines between changes,
    /// a new hunk is started instead of including all the unchanged lines.
    /// </summary>
    /// <remarks>
    /// Value should be at least (2 * ContextLines) for proper separation.
    /// Default of 6 means changes must be 6+ lines apart to be in separate hunks.
    /// </remarks>
    public int HunkSeparationThreshold { get; init; } = 6;

    /// <summary>
    /// Maximum line length for inline diff computation.
    /// Lines longer than this skip inline diff for performance.
    /// </summary>
    public int MaxInlineDiffLineLength { get; init; } = 500;

    /// <summary>
    /// Minimum similarity ratio (0.0-1.0) for computing inline diffs.
    /// Lines that differ by more than (1 - ratio) are not inline-diffed.
    /// </summary>
    /// <remarks>
    /// At 0.3, lines must be at least 30% similar to get inline diffs.
    /// Lower values produce more inline diffs, higher values fewer.
    /// </remarks>
    public double InlineDiffSimilarityThreshold { get; init; } = 0.3;

    // ═══════════════════════════════════════════════════════════════════════
    // Static Preset Options
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Default options instance with standard values.
    /// </summary>
    public static DiffOptions Default => new();

    /// <summary>
    /// Options optimized for compact display (fewer context lines).
    /// </summary>
    public static DiffOptions Compact => new()
    {
        ContextLines = 1,
        HunkSeparationThreshold = 4
    };

    /// <summary>
    /// Options optimized for maximum context.
    /// </summary>
    public static DiffOptions Full => new()
    {
        ContextLines = 10,
        HunkSeparationThreshold = 20
    };

    /// <summary>
    /// Options that ignore whitespace differences.
    /// </summary>
    public static DiffOptions IgnoreWhitespaceOptions => new()
    {
        IgnoreWhitespace = true,
        TrimTrailingWhitespace = true
    };
}
