namespace AIntern.Desktop.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF LINE VIEWMODEL (v0.4.2d)                                            │
// │ ViewModel for a single line in the diff viewer.                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel for a single line in the diff viewer.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2d.</para>
/// <para>
/// Represents one line in either the original or proposed panel.
/// Supports inline segments for character-level change highlighting,
/// and placeholder lines for maintaining side-by-side alignment.
/// </para>
/// </remarks>
public partial class DiffLineViewModel : ViewModelBase
{
    // ═══════════════════════════════════════════════════════════════════════
    // Observable Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Line number (null for placeholder lines).
    /// </summary>
    [ObservableProperty]
    private int? _lineNumber;

    /// <summary>
    /// Text content of the line.
    /// </summary>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// Type of change for this line.
    /// </summary>
    [ObservableProperty]
    private DiffLineType _type;

    /// <summary>
    /// Which side of the diff this line belongs to.
    /// </summary>
    [ObservableProperty]
    private DiffSide _side;

    /// <summary>
    /// Inline segments for character-level highlighting.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<InlineSegment>? _inlineSegments;

    /// <summary>
    /// Whether this is a placeholder line for alignment.
    /// </summary>
    [ObservableProperty]
    private bool _isPlaceholder;

    /// <summary>
    /// Whether this line is currently highlighted.
    /// </summary>
    [ObservableProperty]
    private bool _isHighlighted;

    /// <summary>
    /// Whether this line is selected.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether this line has inline segments to display.
    /// </summary>
    public bool HasInlineSegments => InlineSegments?.Count > 0;

    /// <summary>
    /// Line number formatted for display.
    /// </summary>
    public string LineNumberDisplay => LineNumber?.ToString() ?? string.Empty;

    /// <summary>
    /// Whether this line represents an addition.
    /// </summary>
    public bool IsAdded => Type == DiffLineType.Added;

    /// <summary>
    /// Whether this line represents a removal.
    /// </summary>
    public bool IsRemoved => Type == DiffLineType.Removed;

    /// <summary>
    /// Whether this line represents a modification.
    /// </summary>
    public bool IsModified => Type == DiffLineType.Modified;

    /// <summary>
    /// Whether this line is unchanged.
    /// </summary>
    public bool IsUnchanged => Type == DiffLineType.Unchanged;

    /// <summary>
    /// Whether this line represents any kind of change.
    /// </summary>
    public bool IsChanged => IsAdded || IsRemoved || IsModified;

    /// <summary>
    /// Prefix character for unified diff format.
    /// </summary>
    public char Prefix => Type switch
    {
        DiffLineType.Added => '+',
        DiffLineType.Removed => '-',
        DiffLineType.Modified => '~',
        _ => ' '
    };

    /// <summary>
    /// CSS-style class name for styling.
    /// </summary>
    public string ChangeClass => Type switch
    {
        DiffLineType.Added => "diff-added",
        DiffLineType.Removed => "diff-removed",
        DiffLineType.Modified => "diff-modified",
        _ => IsPlaceholder ? "diff-placeholder" : "diff-unchanged"
    };

    /// <summary>
    /// Background style resource key.
    /// </summary>
    public string BackgroundStyle => Type switch
    {
        DiffLineType.Added => "DiffAddedBackground",
        DiffLineType.Removed => "DiffRemovedBackground",
        DiffLineType.Modified => "DiffModifiedBackground",
        _ => IsPlaceholder ? "DiffPlaceholderBackground" : "Transparent"
    };

    /// <summary>
    /// Content length for alignment calculations.
    /// </summary>
    public int ContentLength => Content?.Length ?? 0;

    // ═══════════════════════════════════════════════════════════════════════
    // Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get the inline segment at a specific column position.
    /// </summary>
    public InlineSegment? GetSegmentAtColumn(int column)
    {
        if (InlineSegments == null)
            return null;

        int currentPos = 0;
        foreach (var segment in InlineSegments)
        {
            if (column >= currentPos && column < currentPos + segment.Length)
            {
                return segment;
            }
            currentPos += segment.Length;
        }

        return null;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (IsPlaceholder)
            return "[Placeholder]";

        var truncatedContent = Content?.Length > 50
            ? Content[..47] + "..."
            : Content ?? string.Empty;

        return $"[{LineNumber}] {Prefix} {truncatedContent}";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Static Factory Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a placeholder line for alignment.
    /// </summary>
    public static DiffLineViewModel Placeholder() => new()
    {
        LineNumber = null,
        Content = string.Empty,
        Type = DiffLineType.Unchanged,
        Side = DiffSide.Original,
        IsPlaceholder = true
    };

    /// <summary>
    /// Creates a line ViewModel from a DiffLine model.
    /// </summary>
    public static DiffLineViewModel FromDiffLine(
        DiffLine line,
        DiffSide side,
        IReadOnlyList<InlineSegment>? segments = null) => new()
    {
        LineNumber = line.GetLineNumber(side),
        Content = line.Content,
        Type = line.Type,
        Side = side,
        InlineSegments = segments,
        IsPlaceholder = false
    };
}
