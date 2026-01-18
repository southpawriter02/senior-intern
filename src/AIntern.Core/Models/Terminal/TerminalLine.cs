using System.Text;

namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalLine (v0.5.1b)                                                   │
// │ Represents a single line in the terminal buffer.                         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a single line of cells in the terminal buffer.
/// </summary>
/// <remarks>
/// <para>
/// Each line manages an array of <see cref="TerminalCell"/> instances and tracks
/// modification state for efficient incremental rendering.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
///   <item><description>Fixed-size cell array for consistent column width</description></item>
///   <item><description>Dirty tracking for render optimization</description></item>
///   <item><description>Wrap flag for soft line breaks</description></item>
///   <item><description>Span accessors for zero-copy iteration</description></item>
/// </list>
/// </para>
/// <para>Added in v0.5.1b.</para>
/// </remarks>
public sealed class TerminalLine
{
    #region Private Fields

    /// <summary>
    /// The array of cells comprising this line.
    /// </summary>
    private TerminalCell[] _cells;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the number of columns (cells) in this line.
    /// </summary>
    public int Length => _cells.Length;

    /// <summary>
    /// Gets or sets a value indicating whether this line is wrapped from the previous line.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, this line is a continuation of the previous line due to word wrap
    /// rather than an explicit newline character. This affects text selection and copy behavior.
    /// </remarks>
    public bool IsWrapped { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this line has been modified since last render.
    /// </summary>
    /// <remarks>
    /// Used by the renderer to determine which lines need to be redrawn.
    /// Call <see cref="MarkClean"/> after rendering and <see cref="MarkDirty"/> after modification.
    /// </remarks>
    public bool IsDirty { get; private set; } = true;

    /// <summary>
    /// Gets the UTC timestamp when this line was last modified.
    /// </summary>
    /// <remarks>
    /// Can be used for time-based effects like dimming old content.
    /// </remarks>
    public DateTime LastModified { get; private set; } = DateTime.UtcNow;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalLine"/> class.
    /// </summary>
    /// <param name="columns">The number of columns (width) for this line.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="columns"/> is less than or equal to zero.
    /// </exception>
    public TerminalLine(int columns)
    {
        if (columns <= 0)
            throw new ArgumentOutOfRangeException(nameof(columns), "Columns must be greater than zero.");

        _cells = new TerminalCell[columns];
        Clear();
    }

    #endregion

    #region Indexer

    /// <summary>
    /// Gets the cell at the specified column by reference.
    /// </summary>
    /// <param name="column">The 0-indexed column position.</param>
    /// <returns>A reference to the <see cref="TerminalCell"/> at the specified column.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is outside the valid range.
    /// </exception>
    /// <remarks>
    /// Returns by reference for efficient in-place modification.
    /// </remarks>
    public ref TerminalCell this[int column]
    {
        get
        {
            if (column < 0 || column >= _cells.Length)
                throw new ArgumentOutOfRangeException(nameof(column), 
                    $"Column must be between 0 and {_cells.Length - 1}.");
            return ref _cells[column];
        }
    }

    #endregion

    #region Span Accessors

    /// <summary>
    /// Gets all cells as a mutable span for efficient iteration and modification.
    /// </summary>
    public Span<TerminalCell> Cells => _cells.AsSpan();

    /// <summary>
    /// Gets all cells as a read-only span for safe iteration.
    /// </summary>
    public ReadOnlySpan<TerminalCell> ReadOnlyCells => _cells.AsSpan();

    #endregion

    #region Clear Methods

    /// <summary>
    /// Clears all cells to empty (space with default attributes).
    /// </summary>
    public void Clear()
    {
        Array.Fill(_cells, TerminalCell.Empty);
        MarkDirty();
    }

    /// <summary>
    /// Clears cells in the specified range (inclusive) to empty.
    /// </summary>
    /// <param name="start">The starting column index (0-indexed).</param>
    /// <param name="end">The ending column index (0-indexed, inclusive).</param>
    /// <remarks>
    /// Indices are clamped to valid range to prevent exceptions.
    /// </remarks>
    public void Clear(int start, int end)
    {
        // Clamp indices to valid range
        start = Math.Max(0, start);
        end = Math.Min(_cells.Length - 1, end);

        for (int i = start; i <= end; i++)
        {
            _cells[i] = TerminalCell.Empty;
        }

        MarkDirty();
    }

    #endregion

    #region Cell Operations

    /// <summary>
    /// Sets the cell at the specified column.
    /// </summary>
    /// <param name="column">The 0-indexed column position.</param>
    /// <param name="cell">The cell value to set.</param>
    /// <remarks>
    /// If the column is out of range, this method does nothing (no exception).
    /// </remarks>
    public void SetCell(int column, TerminalCell cell)
    {
        if (column >= 0 && column < _cells.Length)
        {
            _cells[column] = cell;
            MarkDirty();
        }
    }

    /// <summary>
    /// Copies cells from another line.
    /// </summary>
    /// <param name="source">The source line to copy from.</param>
    /// <remarks>
    /// Copies as many cells as will fit, truncating if source is longer.
    /// Also copies the <see cref="IsWrapped"/> flag.
    /// </remarks>
    public void CopyFrom(TerminalLine source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var copyLength = Math.Min(_cells.Length, source._cells.Length);
        Array.Copy(source._cells, _cells, copyLength);
        IsWrapped = source.IsWrapped;
        MarkDirty();
    }

    #endregion

    #region Resize

    /// <summary>
    /// Resizes this line to a new column count.
    /// </summary>
    /// <param name="newColumns">The new number of columns.</param>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>If expanding: new cells are filled with empty cells.</description></item>
    ///   <item><description>If shrinking: excess cells are truncated.</description></item>
    /// </list>
    /// </remarks>
    public void Resize(int newColumns)
    {
        if (newColumns <= 0 || newColumns == _cells.Length)
            return;

        var newCells = new TerminalCell[newColumns];
        var copyLength = Math.Min(_cells.Length, newColumns);
        Array.Copy(_cells, newCells, copyLength);

        // Fill new cells with empty if we expanded
        for (int i = copyLength; i < newColumns; i++)
        {
            newCells[i] = TerminalCell.Empty;
        }

        _cells = newCells;
        MarkDirty();
    }

    #endregion

    #region Text Extraction

    /// <summary>
    /// Gets the text content of this line as a string.
    /// </summary>
    /// <returns>The text content with trailing whitespace trimmed.</returns>
    /// <remarks>
    /// Continuation cells (from wide characters) are skipped to avoid duplication.
    /// </remarks>
    public string GetText()
    {
        var sb = new StringBuilder(_cells.Length);
        foreach (var cell in _cells)
        {
            // Skip continuation cells to avoid duplicate characters
            if (!cell.IsContinuation)
            {
                sb.Append(cell.Character.ToString());
            }
        }
        return sb.ToString().TrimEnd();
    }

    #endregion

    #region Dirty Tracking

    /// <summary>
    /// Marks this line as modified (needing re-render).
    /// </summary>
    public void MarkDirty()
    {
        IsDirty = true;
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this line as clean (rendered).
    /// </summary>
    public void MarkClean()
    {
        IsDirty = false;
    }

    #endregion
}
