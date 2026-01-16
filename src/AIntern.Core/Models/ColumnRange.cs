namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ COLUMN RANGE (v0.4.5c)                                                  │
// │ Represents a range of columns within a line (0-indexed, end exclusive).│
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a range of columns within a line (0-indexed, end exclusive).
/// Reserved for future character-level operations.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5c.</para>
/// </remarks>
public readonly struct ColumnRange : IEquatable<ColumnRange>
{
    /// <summary>
    /// Start column (0-indexed, inclusive).
    /// </summary>
    public int StartColumn { get; }

    /// <summary>
    /// End column (0-indexed, exclusive).
    /// </summary>
    public int EndColumn { get; }

    /// <summary>
    /// Creates a new column range.
    /// </summary>
    public ColumnRange(int startColumn, int endColumn)
    {
        StartColumn = startColumn;
        EndColumn = endColumn;
    }

    /// <summary>
    /// Number of characters in this range.
    /// </summary>
    public int Length => IsValid ? EndColumn - StartColumn : 0;

    /// <summary>
    /// Whether this is a valid range.
    /// </summary>
    public bool IsValid => StartColumn >= 0 && EndColumn >= StartColumn;

    /// <summary>
    /// An empty column range.
    /// </summary>
    public static ColumnRange Empty => new(0, 0);

    /// <summary>
    /// Creates a range representing the entire line.
    /// </summary>
    public static ColumnRange EntireLine(int lineLength) => new(0, lineLength);

    /// <summary>
    /// Creates a range from start to end of line.
    /// </summary>
    public static ColumnRange FromColumn(int startColumn) => new(startColumn, int.MaxValue);

    public bool Equals(ColumnRange other) =>
        StartColumn == other.StartColumn && EndColumn == other.EndColumn;

    public override bool Equals(object? obj) =>
        obj is ColumnRange other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(StartColumn, EndColumn);

    public static bool operator ==(ColumnRange left, ColumnRange right) => left.Equals(right);
    public static bool operator !=(ColumnRange left, ColumnRange right) => !left.Equals(right);

    public override string ToString() =>
        !IsValid ? "Invalid" : $"Columns {StartColumn}-{EndColumn}";
}
