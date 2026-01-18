using System.Text;

namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalCell (v0.5.1b)                                                   │
// │ Represents a single cell in the terminal buffer with Unicode support.    │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a single cell in the terminal buffer.
/// </summary>
/// <remarks>
/// <para>
/// Each cell contains:
/// <list type="bullet">
///   <item><description>A Unicode character stored as <see cref="Rune"/> for full Unicode support</description></item>
///   <item><description>Display attributes (colors and styles)</description></item>
///   <item><description>Width information for wide characters (CJK, emoji)</description></item>
///   <item><description>Continuation flag for the second cell of wide characters</description></item>
/// </list>
/// </para>
/// <para>
/// This struct is intentionally mutable (not readonly) to allow efficient in-place
/// updates when writing to the buffer.
/// </para>
/// <para>Added in v0.5.1b.</para>
/// </remarks>
public struct TerminalCell : IEquatable<TerminalCell>
{
    #region Properties

    /// <summary>
    /// Gets or sets the character displayed in this cell.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Rune"/> to properly represent Unicode scalar values,
    /// including characters outside the Basic Multilingual Plane (emoji, CJK extensions).
    /// </remarks>
    public Rune Character { get; set; }

    /// <summary>
    /// Gets or sets the display attributes for this cell (colors and styles).
    /// </summary>
    public TerminalAttributes Attributes { get; set; }

    /// <summary>
    /// Gets or sets the width of this character in terminal cells.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>1: Standard width (ASCII, most characters)</description></item>
    ///   <item><description>2: Double width (full-width CJK characters, some emoji)</description></item>
    /// </list>
    /// </remarks>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this cell is a continuation of a wide character.
    /// </summary>
    /// <remarks>
    /// When a wide character (Width=2) is written, the second cell is marked as a continuation.
    /// Continuation cells should not be independently rendered.
    /// </remarks>
    public bool IsContinuation { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets a value indicating whether this cell displays as blank.
    /// </summary>
    /// <value>
    /// <c>true</c> if the cell contains a space, null character, or is a continuation;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// Useful for rendering optimizations where blank cells can be skipped.
    /// </remarks>
    public readonly bool IsBlank =>
        Character.Value == ' ' || Character.Value == 0 || IsContinuation;

    #endregion

    #region Static Factory Properties

    /// <summary>
    /// Gets an empty cell containing a space with default attributes.
    /// </summary>
    public static TerminalCell Empty => new()
    {
        Character = new Rune(' '),
        Attributes = TerminalAttributes.Default,
        Width = 1,
        IsContinuation = false
    };

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a cell from a character with default attributes.
    /// </summary>
    /// <param name="c">The character to display.</param>
    /// <returns>A new <see cref="TerminalCell"/> with the specified character.</returns>
    public static TerminalCell FromChar(char c) => new()
    {
        Character = new Rune(c),
        Attributes = TerminalAttributes.Default,
        Width = 1,
        IsContinuation = false
    };

    /// <summary>
    /// Creates a cell from a character with specific attributes.
    /// </summary>
    /// <param name="c">The character to display.</param>
    /// <param name="attributes">The display attributes to apply.</param>
    /// <returns>A new <see cref="TerminalCell"/> with the specified character and attributes.</returns>
    public static TerminalCell FromChar(char c, TerminalAttributes attributes) => new()
    {
        Character = new Rune(c),
        Attributes = attributes,
        Width = 1,
        IsContinuation = false
    };

    /// <summary>
    /// Creates a cell from a Unicode rune with specific attributes.
    /// </summary>
    /// <param name="rune">The Unicode rune to display.</param>
    /// <param name="attributes">The display attributes to apply.</param>
    /// <param name="width">The character width (1 for normal, 2 for wide).</param>
    /// <returns>A new <see cref="TerminalCell"/> with the specified rune and attributes.</returns>
    public static TerminalCell FromRune(Rune rune, TerminalAttributes attributes, int width = 1) => new()
    {
        Character = rune,
        Attributes = attributes,
        Width = width,
        IsContinuation = false
    };

    #endregion

    #region IEquatable<TerminalCell>

    /// <inheritdoc/>
    public readonly bool Equals(TerminalCell other) =>
        Character == other.Character &&
        Attributes == other.Attributes &&
        Width == other.Width &&
        IsContinuation == other.IsContinuation;

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) =>
        obj is TerminalCell other && Equals(other);

    /// <inheritdoc/>
    public override readonly int GetHashCode() =>
        HashCode.Combine(Character, Attributes, Width, IsContinuation);

    /// <summary>
    /// Determines whether two <see cref="TerminalCell"/> instances are equal.
    /// </summary>
    public static bool operator ==(TerminalCell left, TerminalCell right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="TerminalCell"/> instances are not equal.
    /// </summary>
    public static bool operator !=(TerminalCell left, TerminalCell right) =>
        !left.Equals(right);

    #endregion

    #region Methods

    /// <inheritdoc/>
    public override readonly string ToString() =>
        IsContinuation ? "[continuation]" : Character.ToString();

    #endregion
}
