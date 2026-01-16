namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ INDENTATION STYLE (v0.4.5c)                                             │
// │ Detected or specified indentation style for code formatting.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Detected or specified indentation style for code formatting.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5c.</para>
/// </remarks>
public sealed record IndentationStyle
{
    /// <summary>
    /// Whether tabs should be used for indentation.
    /// </summary>
    public bool UseTabs { get; init; }

    /// <summary>
    /// Number of spaces per indent level (1-8).
    /// </summary>
    public int SpacesPerIndent { get; init; } = 4;

    /// <summary>
    /// Confidence level of detection (0.0 to 1.0).
    /// 1.0 = explicitly specified, lower values for detected.
    /// </summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>
    /// The actual indentation string for one level.
    /// </summary>
    public string IndentString => UseTabs ? "\t" : new string(' ', SpacesPerIndent);

    /// <summary>
    /// Creates indentation for the specified level.
    /// </summary>
    public string GetIndent(int level) =>
        level <= 0 ? string.Empty : string.Concat(Enumerable.Repeat(IndentString, level));

    // ═══════════════════════════════════════════════════════════════
    // Static Instances
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Default style: 4 spaces per indent.
    /// </summary>
    public static IndentationStyle Default => new()
    {
        UseTabs = false,
        SpacesPerIndent = 4,
        Confidence = 0.5
    };

    /// <summary>
    /// Two spaces per indent (common in JavaScript/TypeScript).
    /// </summary>
    public static IndentationStyle TwoSpaces => new()
    {
        UseTabs = false,
        SpacesPerIndent = 2,
        Confidence = 1.0
    };

    /// <summary>
    /// Tab-based indentation.
    /// </summary>
    public static IndentationStyle Tabs => new()
    {
        UseTabs = true,
        SpacesPerIndent = 4, // For display width
        Confidence = 1.0
    };

    /// <summary>
    /// Unknown/undetected style.
    /// </summary>
    public static IndentationStyle Unknown => new()
    {
        UseTabs = false,
        SpacesPerIndent = 4,
        Confidence = 0.0
    };

    public override string ToString() =>
        UseTabs ? "Tabs" : $"{SpacesPerIndent} spaces";
}
