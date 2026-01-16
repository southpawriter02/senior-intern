namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF VIEW MODE (v0.4.5a)                                                │
// │ Display mode for the diff viewer component.                             │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Display mode for the diff viewer component.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5a.</para>
/// </remarks>
public enum DiffViewMode
{
    /// <summary>
    /// Side-by-side comparison with synchronized scrolling.
    /// Original content on left, new content on right.
    /// Best for: Viewing additions and modifications in context.
    /// </summary>
    SideBySide = 0,

    /// <summary>
    /// Inline changes with additions and deletions interleaved.
    /// Deletions shown with strikethrough, additions highlighted.
    /// Best for: Compact view of small changes.
    /// </summary>
    Inline = 1,

    /// <summary>
    /// Unified diff format (similar to git diff output).
    /// Lines prefixed with +/- indicators, context lines shown.
    /// Best for: Developers familiar with command-line diff tools.
    /// </summary>
    Unified = 2
}
