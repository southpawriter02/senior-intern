namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalSize (v0.5.1b)                                                   │
// │ Represents terminal dimensions in columns and rows.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents terminal dimensions in columns and rows.
/// </summary>
/// <remarks>
/// <para>
/// This readonly record struct provides value semantics with automatic equality
/// for terminal size operations. Standard terminal sizes are provided as static
/// factory properties.
/// </para>
/// <para>Added in v0.5.1b.</para>
/// </remarks>
/// <param name="Columns">Number of character columns (width).</param>
/// <param name="Rows">Number of character rows (height).</param>
public readonly record struct TerminalSize(int Columns, int Rows)
{
    #region Static Factory Properties

    /// <summary>
    /// Default VT100 terminal size (80 columns × 24 rows).
    /// </summary>
    /// <remarks>
    /// This is the standard terminal size dating back to the DEC VT100 terminal
    /// and remains the most widely compatible default for terminal emulators.
    /// </remarks>
    public static TerminalSize Default => new(80, 24);

    /// <summary>
    /// Wide terminal size for modern displays (120 columns × 30 rows).
    /// </summary>
    /// <remarks>
    /// Suitable for widescreen displays where more horizontal content is beneficial,
    /// such as viewing log files or side-by-side diffs.
    /// </remarks>
    public static TerminalSize Wide => new(120, 30);

    /// <summary>
    /// Compact terminal size for split views (80 columns × 12 rows).
    /// </summary>
    /// <remarks>
    /// Half the standard height, useful for embedding terminals in split-pane
    /// layouts or displaying quick command output.
    /// </remarks>
    public static TerminalSize Compact => new(80, 12);

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets a value indicating whether the dimensions are valid.
    /// </summary>
    /// <value>
    /// <c>true</c> if both <see cref="Columns"/> and <see cref="Rows"/> are greater than zero;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// Invalid dimensions can cause issues with buffer allocation and rendering.
    /// Always validate dimensions before using them.
    /// </remarks>
    public bool IsValid => Columns > 0 && Rows > 0;

    /// <summary>
    /// Gets the total number of cells in the terminal grid.
    /// </summary>
    /// <value>The product of <see cref="Columns"/> and <see cref="Rows"/>.</value>
    /// <remarks>
    /// Useful for memory allocation calculations when creating buffers.
    /// For the default 80×24 terminal, this equals 1,920 cells.
    /// </remarks>
    public int TotalCells => Columns * Rows;

    #endregion

    #region Methods

    /// <summary>
    /// Returns a string representation of the terminal size.
    /// </summary>
    /// <returns>A string in the format "COLUMNSxROWS" (e.g., "80x24").</returns>
    public override string ToString() => $"{Columns}x{Rows}";

    #endregion
}
