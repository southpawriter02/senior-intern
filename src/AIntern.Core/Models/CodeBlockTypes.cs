namespace AIntern.Core.Models;

/// <summary>
/// Classification of a code block's purpose (v0.4.1a).
/// </summary>
public enum CodeBlockType
{
    /// <summary>
    /// A complete file to be created or replaced entirely.
    /// </summary>
    CompleteFile,

    /// <summary>
    /// A partial snippet to be inserted or to replace a section of a file.
    /// </summary>
    Snippet,

    /// <summary>
    /// Example/illustration code (not meant to be applied).
    /// </summary>
    Example,

    /// <summary>
    /// Shell/terminal command (not a code file).
    /// </summary>
    Command,

    /// <summary>
    /// Output/log content (readonly, not applicable).
    /// </summary>
    Output,

    /// <summary>
    /// Configuration or data file (JSON, YAML, XML, TOML, etc.).
    /// </summary>
    Config
}

/// <summary>
/// Status of a code block in the apply workflow (v0.4.1a).
/// </summary>
public enum CodeBlockStatus
{
    /// <summary>
    /// Not yet processed by user.
    /// </summary>
    Pending,

    /// <summary>
    /// User is reviewing/viewing the diff (v0.4.1g).
    /// </summary>
    Reviewing,

    /// <summary>
    /// Apply operation in progress (v0.4.1g).
    /// </summary>
    Applying,

    /// <summary>
    /// Successfully applied to target file.
    /// </summary>
    Applied,

    /// <summary>
    /// User rejected this code block.
    /// </summary>
    Rejected,

    /// <summary>
    /// Skipped (e.g., not applicable or user chose to skip).
    /// </summary>
    Skipped,

    /// <summary>
    /// Conflict detected with current file state.
    /// </summary>
    Conflict,

    /// <summary>
    /// Error occurred during apply.
    /// </summary>
    Error
}

/// <summary>
/// Represents a range of text by character positions (v0.4.1a).
/// </summary>
public readonly record struct TextRange(int Start, int End)
{
    /// <summary>
    /// Length of this range in characters.
    /// </summary>
    public int Length => End - Start;

    /// <summary>
    /// Whether this is an empty range.
    /// </summary>
    public bool IsEmpty => Start == End;

    /// <summary>
    /// Whether this is a valid range (non-negative, end >= start).
    /// </summary>
    public bool IsValid => Start >= 0 && End >= Start;

    /// <summary>
    /// An empty range at position 0.
    /// </summary>
    public static TextRange Empty => new(0, 0);

    /// <summary>
    /// Creates a range from a start position and length.
    /// </summary>
    public static TextRange FromLength(int start, int length) => new(start, start + length);

    /// <summary>
    /// Whether this range contains the specified position.
    /// </summary>
    public bool Contains(int position) => position >= Start && position < End;

    /// <summary>
    /// Whether this range overlaps with another range.
    /// </summary>
    public bool Overlaps(TextRange other) =>
        Start < other.End && other.Start < End;
}

/// <summary>
/// Represents a range of lines in a file (v0.4.1a, extended v0.4.5c).
/// </summary>
public readonly record struct LineRange(int StartLine, int EndLine) : IComparable<LineRange>
{
    /// <summary>
    /// Number of lines in this range (inclusive).
    /// </summary>
    public int LineCount => EndLine - StartLine + 1;

    /// <summary>
    /// Whether this is a valid range (start <= end, both positive).
    /// </summary>
    public bool IsValid => StartLine > 0 && EndLine >= StartLine;

    /// <summary>
    /// Whether this is an empty/invalid range.
    /// </summary>
    public bool IsEmpty => StartLine == 0 && EndLine == 0;

    /// <summary>
    /// Whether this is a single-line range.
    /// </summary>
    public bool IsSingleLine => IsValid && StartLine == EndLine;

    // ═══════════════════════════════════════════════════════════════
    // Factory Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// An empty line range.
    /// </summary>
    public static LineRange Empty => new(0, 0);

    /// <summary>
    /// Creates a range for a single line.
    /// </summary>
    public static LineRange SingleLine(int line) => new(line, line);

    /// <summary>
    /// Creates a range from a start line to end of file (int.MaxValue).
    /// </summary>
    public static LineRange FromLine(int startLine) => new(startLine, int.MaxValue);

    /// <summary>
    /// Creates a range representing the entire file.
    /// </summary>
    public static LineRange EntireFile(int lineCount) => new(1, lineCount);

    /// <summary>
    /// Creates a range from 0-indexed array bounds.
    /// </summary>
    public static LineRange FromZeroIndexed(int startIndex, int endIndex) =>
        new(startIndex + 1, endIndex);

    // ═══════════════════════════════════════════════════════════════
    // Query Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether this range contains the specified line.
    /// </summary>
    public bool Contains(int line) => line >= StartLine && line <= EndLine;

    /// <summary>
    /// Whether this range contains another range entirely.
    /// </summary>
    public bool Contains(LineRange other) =>
        IsValid && other.IsValid &&
        other.StartLine >= StartLine && other.EndLine <= EndLine;

    /// <summary>
    /// Whether this range overlaps with another range.
    /// </summary>
    public bool Overlaps(LineRange other) =>
        StartLine <= other.EndLine && other.StartLine <= EndLine;

    /// <summary>
    /// Whether this range is adjacent to another.
    /// </summary>
    public bool IsAdjacentTo(LineRange other) =>
        IsValid && other.IsValid &&
        (EndLine + 1 == other.StartLine || other.EndLine + 1 == StartLine);

    // ═══════════════════════════════════════════════════════════════
    // Transformation Methods (v0.4.5c)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Merges this range with another, creating a range that spans both.
    /// </summary>
    public LineRange Merge(LineRange other)
    {
        if (!IsValid) return other;
        if (!other.IsValid) return this;
        return new LineRange(
            Math.Min(StartLine, other.StartLine),
            Math.Max(EndLine, other.EndLine));
    }

    /// <summary>
    /// Returns the intersection of this range with another.
    /// </summary>
    public LineRange Intersect(LineRange other)
    {
        if (!Overlaps(other)) return Empty;
        return new LineRange(
            Math.Max(StartLine, other.StartLine),
            Math.Min(EndLine, other.EndLine));
    }

    /// <summary>
    /// Expands the range by the specified number of lines on each side.
    /// </summary>
    public LineRange Expand(int linesBefore, int linesAfter) =>
        IsValid
            ? new LineRange(
                Math.Max(1, StartLine - linesBefore),
                EndLine + linesAfter)
            : this;

    /// <summary>
    /// Shifts the range by the specified number of lines.
    /// </summary>
    public LineRange Shift(int offset) =>
        IsValid
            ? new LineRange(
                Math.Max(1, StartLine + offset),
                Math.Max(1, EndLine + offset))
            : this;

    /// <summary>
    /// Clamps the range to fit within the specified bounds.
    /// </summary>
    public LineRange ClampTo(int maxLine) =>
        IsValid
            ? new LineRange(
                Math.Min(StartLine, maxLine),
                Math.Min(EndLine, maxLine))
            : this;

    // ═══════════════════════════════════════════════════════════════
    // Conversion Methods (v0.4.5c)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Converts to 0-indexed start position (for array access).
    /// </summary>
    public int ToZeroIndexedStart() => StartLine - 1;

    /// <summary>
    /// Converts to 0-indexed end position (exclusive, for array slicing).
    /// </summary>
    public int ToZeroIndexedEnd() => EndLine;

    /// <summary>
    /// Enumerates all line numbers in this range.
    /// </summary>
    public IEnumerable<int> EnumerateLines()
    {
        if (!IsValid) yield break;
        for (int i = StartLine; i <= EndLine; i++)
            yield return i;
    }

    /// <summary>
    /// Formats as a unified diff header range.
    /// </summary>
    public string ToDiffHeader() =>
        IsSingleLine ? $"{StartLine}" : $"{StartLine},{LineCount}";

    // ═══════════════════════════════════════════════════════════════
    // Comparison
    // ═══════════════════════════════════════════════════════════════

    public int CompareTo(LineRange other)
    {
        var startComparison = StartLine.CompareTo(other.StartLine);
        return startComparison != 0 ? startComparison : EndLine.CompareTo(other.EndLine);
    }

    /// <summary>
    /// Returns a string representation of this range.
    /// </summary>
    public override string ToString() =>
        StartLine == EndLine ? $"Line {StartLine}" : $"Lines {StartLine}-{EndLine}";
}
