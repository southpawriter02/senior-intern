namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalAttributes (v0.5.1b)                                             │
// │ Represents text styling attributes for terminal cells (SGR support).     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents text styling attributes for a terminal cell.
/// </summary>
/// <remarks>
/// <para>
/// These attributes correspond to SGR (Select Graphic Rendition) escape codes
/// used by VT100-compatible terminals:
/// <list type="bullet">
///   <item><description>SGR 1: Bold</description></item>
///   <item><description>SGR 2: Dim</description></item>
///   <item><description>SGR 3: Italic</description></item>
///   <item><description>SGR 4: Underline</description></item>
///   <item><description>SGR 5: Blink</description></item>
///   <item><description>SGR 7: Inverse</description></item>
///   <item><description>SGR 8: Hidden</description></item>
///   <item><description>SGR 9: Strikethrough</description></item>
/// </list>
/// </para>
/// <para>Added in v0.5.1b.</para>
/// </remarks>
public readonly struct TerminalAttributes : IEquatable<TerminalAttributes>
{
    #region Color Properties

    /// <summary>
    /// Gets the foreground (text) color.
    /// </summary>
    /// <remarks>Corresponds to SGR 30-37 (standard), SGR 38 (extended), or SGR 90-97 (bright).</remarks>
    public TerminalColor Foreground { get; init; }

    /// <summary>
    /// Gets the background color.
    /// </summary>
    /// <remarks>Corresponds to SGR 40-47 (standard), SGR 48 (extended), or SGR 100-107 (bright).</remarks>
    public TerminalColor Background { get; init; }

    #endregion

    #region Style Properties

    /// <summary>
    /// Gets a value indicating whether bold/intense text is enabled.
    /// </summary>
    /// <remarks>SGR 1: Often renders as brighter text or heavier font weight.</remarks>
    public bool Bold { get; init; }

    /// <summary>
    /// Gets a value indicating whether dim/faint text is enabled.
    /// </summary>
    /// <remarks>SGR 2: Renders text at reduced intensity.</remarks>
    public bool Dim { get; init; }

    /// <summary>
    /// Gets a value indicating whether italic text is enabled.
    /// </summary>
    /// <remarks>SGR 3: Requires font support for italic glyphs.</remarks>
    public bool Italic { get; init; }

    /// <summary>
    /// Gets a value indicating whether underlined text is enabled.
    /// </summary>
    /// <remarks>SGR 4: Draws a line below the text.</remarks>
    public bool Underline { get; init; }

    /// <summary>
    /// Gets a value indicating whether blinking text is enabled.
    /// </summary>
    /// <remarks>SGR 5: May be ignored by modern terminal emulators.</remarks>
    public bool Blink { get; init; }

    /// <summary>
    /// Gets a value indicating whether inverse video is enabled.
    /// </summary>
    /// <remarks>SGR 7: Swaps foreground and background colors.</remarks>
    public bool Inverse { get; init; }

    /// <summary>
    /// Gets a value indicating whether hidden/invisible text is enabled.
    /// </summary>
    /// <remarks>SGR 8: Text is not visible but still occupies space.</remarks>
    public bool Hidden { get; init; }

    /// <summary>
    /// Gets a value indicating whether strikethrough text is enabled.
    /// </summary>
    /// <remarks>SGR 9: Draws a line through the middle of the text.</remarks>
    public bool Strikethrough { get; init; }

    #endregion

    #region Static Factory Properties

    /// <summary>
    /// Gets the default terminal attributes (default colors, no styling).
    /// </summary>
    /// <remarks>
    /// Corresponds to SGR 0 (reset all attributes to default).
    /// </remarks>
    public static TerminalAttributes Default => new()
    {
        Foreground = TerminalColor.Default,
        Background = TerminalColor.Default
    };

    #endregion

    #region Methods

    /// <summary>
    /// Creates a copy of these attributes with the specified modifications.
    /// </summary>
    /// <param name="foreground">New foreground color, or <c>null</c> to keep current.</param>
    /// <param name="background">New background color, or <c>null</c> to keep current.</param>
    /// <param name="bold">New bold state, or <c>null</c> to keep current.</param>
    /// <param name="dim">New dim state, or <c>null</c> to keep current.</param>
    /// <param name="italic">New italic state, or <c>null</c> to keep current.</param>
    /// <param name="underline">New underline state, or <c>null</c> to keep current.</param>
    /// <param name="blink">New blink state, or <c>null</c> to keep current.</param>
    /// <param name="inverse">New inverse state, or <c>null</c> to keep current.</param>
    /// <param name="hidden">New hidden state, or <c>null</c> to keep current.</param>
    /// <param name="strikethrough">New strikethrough state, or <c>null</c> to keep current.</param>
    /// <returns>A new <see cref="TerminalAttributes"/> instance with the specified modifications.</returns>
    public TerminalAttributes With(
        TerminalColor? foreground = null,
        TerminalColor? background = null,
        bool? bold = null,
        bool? dim = null,
        bool? italic = null,
        bool? underline = null,
        bool? blink = null,
        bool? inverse = null,
        bool? hidden = null,
        bool? strikethrough = null)
    {
        return new TerminalAttributes
        {
            Foreground = foreground ?? Foreground,
            Background = background ?? Background,
            Bold = bold ?? Bold,
            Dim = dim ?? Dim,
            Italic = italic ?? Italic,
            Underline = underline ?? Underline,
            Blink = blink ?? Blink,
            Inverse = inverse ?? Inverse,
            Hidden = hidden ?? Hidden,
            Strikethrough = strikethrough ?? Strikethrough
        };
    }

    #endregion

    #region IEquatable<TerminalAttributes>

    /// <inheritdoc/>
    public bool Equals(TerminalAttributes other) =>
        Foreground == other.Foreground &&
        Background == other.Background &&
        Bold == other.Bold &&
        Dim == other.Dim &&
        Italic == other.Italic &&
        Underline == other.Underline &&
        Blink == other.Blink &&
        Inverse == other.Inverse &&
        Hidden == other.Hidden &&
        Strikethrough == other.Strikethrough;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is TerminalAttributes other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Using HashCode struct for combining multiple values efficiently
        var hash = new HashCode();
        hash.Add(Foreground);
        hash.Add(Background);
        hash.Add(Bold);
        hash.Add(Dim);
        hash.Add(Italic);
        hash.Add(Underline);
        hash.Add(Blink);
        hash.Add(Inverse);
        hash.Add(Hidden);
        hash.Add(Strikethrough);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two <see cref="TerminalAttributes"/> instances are equal.
    /// </summary>
    public static bool operator ==(TerminalAttributes left, TerminalAttributes right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="TerminalAttributes"/> instances are not equal.
    /// </summary>
    public static bool operator !=(TerminalAttributes left, TerminalAttributes right) =>
        !left.Equals(right);

    #endregion
}
