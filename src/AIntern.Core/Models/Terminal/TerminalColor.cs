namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalColor (v0.5.1b)                                                  │
// │ Represents terminal colors with support for default, palette, and RGB.   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents a terminal color with support for default, 256-color palette, and 24-bit true color modes.
/// </summary>
/// <remarks>
/// <para>
/// Terminal colors can be represented in three ways:
/// <list type="bullet">
///   <item><description><b>Default</b>: Uses the terminal's configured foreground/background color.</description></item>
///   <item><description><b>Palette</b>: Uses one of 256 indexed colors (0-15 ANSI, 16-231 color cube, 232-255 grayscale).</description></item>
///   <item><description><b>True Color</b>: Uses explicit RGB values for 24-bit color.</description></item>
/// </list>
/// </para>
/// <para>Added in v0.5.1b.</para>
/// </remarks>
public readonly struct TerminalColor : IEquatable<TerminalColor>
{
    #region Properties

    /// <summary>
    /// Gets the red component of the color (0-255).
    /// </summary>
    /// <remarks>
    /// Only meaningful when <see cref="IsDefault"/> is <c>false</c> and <see cref="PaletteIndex"/> is <c>null</c>.
    /// </remarks>
    public byte R { get; init; }

    /// <summary>
    /// Gets the green component of the color (0-255).
    /// </summary>
    /// <remarks>
    /// Only meaningful when <see cref="IsDefault"/> is <c>false</c> and <see cref="PaletteIndex"/> is <c>null</c>.
    /// </remarks>
    public byte G { get; init; }

    /// <summary>
    /// Gets the blue component of the color (0-255).
    /// </summary>
    /// <remarks>
    /// Only meaningful when <see cref="IsDefault"/> is <c>false</c> and <see cref="PaletteIndex"/> is <c>null</c>.
    /// </remarks>
    public byte B { get; init; }

    /// <summary>
    /// Gets a value indicating whether this represents the terminal's default color.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the actual color is determined by the terminal's theme settings.
    /// </remarks>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Gets the 256-color palette index (0-255), or <c>null</c> for default/RGB colors.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>0-7: Standard ANSI colors (black, red, green, yellow, blue, magenta, cyan, white)</description></item>
    ///   <item><description>8-15: Bright ANSI colors</description></item>
    ///   <item><description>16-231: 6×6×6 color cube</description></item>
    ///   <item><description>232-255: 24 grayscale shades</description></item>
    /// </list>
    /// </remarks>
    public byte? PaletteIndex { get; init; }

    #endregion

    #region Static Factory Properties - Default

    /// <summary>
    /// Gets the terminal's default foreground or background color.
    /// </summary>
    public static TerminalColor Default => new() { IsDefault = true };

    #endregion

    #region Static Factory Properties - Standard ANSI Colors (0-7)

    /// <summary>Standard ANSI black (palette index 0).</summary>
    public static TerminalColor Black => FromPalette(0);

    /// <summary>Standard ANSI red (palette index 1).</summary>
    public static TerminalColor Red => FromPalette(1);

    /// <summary>Standard ANSI green (palette index 2).</summary>
    public static TerminalColor Green => FromPalette(2);

    /// <summary>Standard ANSI yellow (palette index 3).</summary>
    public static TerminalColor Yellow => FromPalette(3);

    /// <summary>Standard ANSI blue (palette index 4).</summary>
    public static TerminalColor Blue => FromPalette(4);

    /// <summary>Standard ANSI magenta (palette index 5).</summary>
    public static TerminalColor Magenta => FromPalette(5);

    /// <summary>Standard ANSI cyan (palette index 6).</summary>
    public static TerminalColor Cyan => FromPalette(6);

    /// <summary>Standard ANSI white (palette index 7).</summary>
    public static TerminalColor White => FromPalette(7);

    #endregion

    #region Static Factory Properties - Bright ANSI Colors (8-15)

    /// <summary>Bright ANSI black/gray (palette index 8).</summary>
    public static TerminalColor BrightBlack => FromPalette(8);

    /// <summary>Bright ANSI red (palette index 9).</summary>
    public static TerminalColor BrightRed => FromPalette(9);

    /// <summary>Bright ANSI green (palette index 10).</summary>
    public static TerminalColor BrightGreen => FromPalette(10);

    /// <summary>Bright ANSI yellow (palette index 11).</summary>
    public static TerminalColor BrightYellow => FromPalette(11);

    /// <summary>Bright ANSI blue (palette index 12).</summary>
    public static TerminalColor BrightBlue => FromPalette(12);

    /// <summary>Bright ANSI magenta (palette index 13).</summary>
    public static TerminalColor BrightMagenta => FromPalette(13);

    /// <summary>Bright ANSI cyan (palette index 14).</summary>
    public static TerminalColor BrightCyan => FromPalette(14);

    /// <summary>Bright ANSI white (palette index 15).</summary>
    public static TerminalColor BrightWhite => FromPalette(15);

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a true color (24-bit RGB) terminal color.
    /// </summary>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <returns>A new <see cref="TerminalColor"/> with the specified RGB values.</returns>
    public static TerminalColor FromRgb(byte r, byte g, byte b) =>
        new() { R = r, G = g, B = b };

    /// <summary>
    /// Creates a color from the 256-color palette.
    /// </summary>
    /// <param name="index">Palette index (0-255).</param>
    /// <returns>A new <see cref="TerminalColor"/> referencing the specified palette index.</returns>
    public static TerminalColor FromPalette(byte index) =>
        new() { PaletteIndex = index };

    #endregion

    #region IEquatable<TerminalColor>

    /// <inheritdoc/>
    public bool Equals(TerminalColor other) =>
        IsDefault == other.IsDefault &&
        PaletteIndex == other.PaletteIndex &&
        R == other.R && G == other.G && B == other.B;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is TerminalColor other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(IsDefault, PaletteIndex, R, G, B);

    /// <summary>
    /// Determines whether two <see cref="TerminalColor"/> instances are equal.
    /// </summary>
    public static bool operator ==(TerminalColor left, TerminalColor right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="TerminalColor"/> instances are not equal.
    /// </summary>
    public static bool operator !=(TerminalColor left, TerminalColor right) =>
        !left.Equals(right);

    #endregion

    #region Methods

    /// <inheritdoc/>
    public override string ToString()
    {
        if (IsDefault)
            return "Default";
        if (PaletteIndex.HasValue)
            return $"Palette({PaletteIndex.Value})";
        return $"RGB({R},{G},{B})";
    }

    #endregion
}
