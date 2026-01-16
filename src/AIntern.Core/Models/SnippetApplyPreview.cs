namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET APPLY PREVIEW (v0.4.5d)                                         │
// │ Preview of changes before applying a snippet.                           │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Preview of changes before applying a snippet.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5d.</para>
/// </remarks>
public sealed record SnippetApplyPreview
{
    /// <summary>
    /// The resulting file content if applied.
    /// </summary>
    public string ResultContent { get; init; } = string.Empty;

    /// <summary>
    /// The computed diff between original and result.
    /// </summary>
    public DiffResult? Diff { get; init; }

    /// <summary>
    /// The range of lines affected by the operation.
    /// </summary>
    public LineRange AffectedRange { get; init; }

    /// <summary>
    /// Number of lines that will be added.
    /// </summary>
    public int LinesAdded { get; init; }

    /// <summary>
    /// Number of lines that will be removed.
    /// </summary>
    public int LinesRemoved { get; init; }

    /// <summary>
    /// Warning messages (non-fatal issues).
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// Whether the preview is valid and can be applied.
    /// </summary>
    public bool IsValid => Warnings.All(w => !w.StartsWith("Error", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Net line change (added - removed).
    /// </summary>
    public int NetLineChange => LinesAdded - LinesRemoved;

    /// <summary>
    /// Creates an empty preview (for errors or no-ops).
    /// </summary>
    public static SnippetApplyPreview Empty(string filePath, string warning) => new()
    {
        ResultContent = string.Empty,
        Diff = DiffResult.NoChanges(filePath, string.Empty),
        AffectedRange = LineRange.Empty,
        Warnings = [warning]
    };
}
