namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalSelection (v0.5.1b)                                              │
// │ Represents a text selection in the terminal buffer.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a text selection in the terminal buffer.
/// </summary>
/// <remarks>
/// <para>
/// Supports two selection modes:
/// <list type="bullet">
///   <item><description><b>Line selection</b> (<see cref="IsBlock"/> = false): 
///     Selects text flowing from start to end across lines.</description></item>
///   <item><description><b>Block selection</b> (<see cref="IsBlock"/> = true): 
///     Selects a rectangular region, useful for columnar data.</description></item>
/// </list>
/// </para>
/// <para>
/// Line coordinates are 0-indexed from the top of the scrollback buffer.
/// Column coordinates are 0-indexed from the left edge.
/// </para>
/// <para>Added in v0.5.1b.</para>
/// </remarks>
public sealed record TerminalSelection
{
    #region Properties

    /// <summary>
    /// Gets the starting line of the selection (0-indexed from top of scrollback).
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    /// Gets the starting column of the selection (0-indexed).
    /// </summary>
    public int StartColumn { get; init; }

    /// <summary>
    /// Gets the ending line of the selection (0-indexed from top of scrollback).
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    /// Gets the ending column of the selection (0-indexed).
    /// </summary>
    public int EndColumn { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a block (rectangular) selection.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description><c>false</c>: Standard line selection flows naturally across lines.</description></item>
    ///   <item><description><c>true</c>: Block selection selects a rectangular region.</description></item>
    /// </list>
    /// Block selection is typically triggered by holding Alt/Option while selecting.
    /// </remarks>
    public bool IsBlock { get; init; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the normalized selection with start always before end.
    /// </summary>
    /// <remarks>
    /// Allows selections to be made in any direction while ensuring consistent processing.
    /// </remarks>
    public TerminalSelection Normalized
    {
        get
        {
            // Determine if start is before end
            var startBefore = StartLine < EndLine ||
                (StartLine == EndLine && StartColumn <= EndColumn);

            return startBefore ? this : new TerminalSelection
            {
                StartLine = EndLine,
                StartColumn = EndColumn,
                EndLine = StartLine,
                EndColumn = StartColumn,
                IsBlock = IsBlock
            };
        }
    }

    /// <summary>
    /// Gets a value indicating whether the selection is empty (zero length).
    /// </summary>
    public bool IsEmpty =>
        StartLine == EndLine && StartColumn == EndColumn;

    /// <summary>
    /// Gets the number of lines spanned by the selection.
    /// </summary>
    public int LineCount
    {
        get
        {
            var norm = Normalized;
            return norm.EndLine - norm.StartLine + 1;
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Determines whether a cell at the specified position is within this selection.
    /// </summary>
    /// <param name="line">The line index (0-indexed).</param>
    /// <param name="column">The column index (0-indexed).</param>
    /// <returns><c>true</c> if the cell is selected; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// For block selections, checks if the cell is within the rectangular bounds.
    /// For line selections, checks for continuous flow from start to end.
    /// </remarks>
    public bool Contains(int line, int column)
    {
        var norm = Normalized;

        if (IsBlock)
        {
            // Block selection: check rectangular bounds
            var minCol = Math.Min(norm.StartColumn, norm.EndColumn);
            var maxCol = Math.Max(norm.StartColumn, norm.EndColumn);

            return line >= norm.StartLine && line <= norm.EndLine &&
                   column >= minCol && column <= maxCol;
        }

        // Line selection: check continuous flow
        if (line < norm.StartLine || line > norm.EndLine)
            return false;

        // Single-line selection
        if (line == norm.StartLine && line == norm.EndLine)
            return column >= norm.StartColumn && column <= norm.EndColumn;

        // First line of multi-line selection
        if (line == norm.StartLine)
            return column >= norm.StartColumn;

        // Last line of multi-line selection
        if (line == norm.EndLine)
            return column <= norm.EndColumn;

        // Middle lines are fully selected
        return true;
    }

    /// <summary>
    /// Creates a new selection that is the union of this selection and another.
    /// </summary>
    /// <param name="other">The other selection to merge.</param>
    /// <returns>A new selection spanning both selections.</returns>
    /// <remarks>
    /// The result is a line selection even if either input is a block selection.
    /// </remarks>
    public TerminalSelection Union(TerminalSelection other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var thisNorm = Normalized;
        var otherNorm = other.Normalized;

        // Find the minimum start
        int startLine, startColumn;
        if (thisNorm.StartLine < otherNorm.StartLine ||
            (thisNorm.StartLine == otherNorm.StartLine && thisNorm.StartColumn <= otherNorm.StartColumn))
        {
            startLine = thisNorm.StartLine;
            startColumn = thisNorm.StartColumn;
        }
        else
        {
            startLine = otherNorm.StartLine;
            startColumn = otherNorm.StartColumn;
        }

        // Find the maximum end
        int endLine, endColumn;
        if (thisNorm.EndLine > otherNorm.EndLine ||
            (thisNorm.EndLine == otherNorm.EndLine && thisNorm.EndColumn >= otherNorm.EndColumn))
        {
            endLine = thisNorm.EndLine;
            endColumn = thisNorm.EndColumn;
        }
        else
        {
            endLine = otherNorm.EndLine;
            endColumn = otherNorm.EndColumn;
        }

        return new TerminalSelection
        {
            StartLine = startLine,
            StartColumn = startColumn,
            EndLine = endLine,
            EndColumn = endColumn,
            IsBlock = false // Union is always a line selection
        };
    }

    #endregion
}
