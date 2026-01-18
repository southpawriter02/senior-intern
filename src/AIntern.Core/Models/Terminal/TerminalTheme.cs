namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalTheme (v0.5.2a)                                                  │
// │ Defines the color scheme for terminal rendering with 256-color support. │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Defines the color scheme for terminal rendering.
/// </summary>
/// <remarks>
/// <para>
/// A terminal theme provides all colors used for rendering terminal content:
/// <list type="bullet">
///   <item><description>Background and foreground defaults</description></item>
///   <item><description>Cursor color and style</description></item>
///   <item><description>Selection highlight with alpha blending</description></item>
///   <item><description>16-color ANSI palette (customizable per theme)</description></item>
///   <item><description>Extended 256-color palette (calculated)</description></item>
/// </list>
/// </para>
/// <para>
/// The 256-color palette follows the standard xterm specification:
/// <list type="bullet">
///   <item><description>0-15: ANSI colors (configurable via <see cref="AnsiPalette"/>)</description></item>
///   <item><description>16-231: 6×6×6 color cube (calculated)</description></item>
///   <item><description>232-255: Grayscale ramp (calculated)</description></item>
/// </list>
/// </para>
/// <para>Added in v0.5.2a.</para>
/// </remarks>
public sealed class TerminalTheme
{
    #region Properties

    /// <summary>
    /// Display name for the theme.
    /// </summary>
    /// <remarks>
    /// Used in theme selection UI and for serialization identification.
    /// </remarks>
    public string Name { get; init; } = "Default";

    /// <summary>
    /// Default background color for the terminal.
    /// </summary>
    /// <remarks>
    /// This color is used when <see cref="TerminalColor.IsDefault"/> is true
    /// for a background color property.
    /// </remarks>
    public TerminalColor Background { get; init; } = TerminalColor.FromRgb(30, 30, 30);

    /// <summary>
    /// Default foreground (text) color for the terminal.
    /// </summary>
    /// <remarks>
    /// This color is used when <see cref="TerminalColor.IsDefault"/> is true
    /// for a foreground color property.
    /// </remarks>
    public TerminalColor Foreground { get; init; } = TerminalColor.FromRgb(204, 204, 204);

    /// <summary>
    /// Cursor color.
    /// </summary>
    /// <remarks>
    /// The color used to render the cursor. For block cursors, this is also
    /// used to determine the color of any text beneath the cursor (typically
    /// the background color is used for text when under a block cursor).
    /// </remarks>
    public TerminalColor Cursor { get; init; } = TerminalColor.FromRgb(204, 204, 204);

    /// <summary>
    /// Selection highlight color (alpha will be applied separately).
    /// </summary>
    /// <remarks>
    /// The base color for text selection. The actual rendered color combines
    /// this with <see cref="SelectionAlpha"/> for transparency.
    /// </remarks>
    public TerminalColor Selection { get; init; } = TerminalColor.FromRgb(68, 119, 170);

    /// <summary>
    /// Selection highlight alpha value (0-255).
    /// </summary>
    /// <remarks>
    /// Applied as transparency when rendering the selection overlay.
    /// Lower values create a more transparent selection highlight.
    /// Default of 80 provides good visibility while still showing the
    /// underlying text colors.
    /// </remarks>
    public byte SelectionAlpha { get; init; } = 80;

    /// <summary>
    /// Bold text color. When null, uses foreground color with bold font style.
    /// </summary>
    /// <remarks>
    /// Some themes prefer to use a brighter foreground color for bold text
    /// (simulating the "bright" colors behavior of older terminals).
    /// When null, bold text uses the regular foreground color but with
    /// a bold font weight applied.
    /// </remarks>
    public TerminalColor? BoldForeground { get; init; }

    /// <summary>
    /// 16-color ANSI palette (colors 0-15).
    /// </summary>
    /// <remarks>
    /// Used for SGR color codes:
    /// <list type="bullet">
    ///   <item><description>30-37: Standard foreground colors (0-7)</description></item>
    ///   <item><description>40-47: Standard background colors (0-7)</description></item>
    ///   <item><description>90-97: Bright foreground colors (8-15)</description></item>
    ///   <item><description>100-107: Bright background colors (8-15)</description></item>
    /// </list>
    /// </remarks>
    public TerminalColor[] AnsiPalette { get; init; } = CreateDefaultAnsiPalette();

    /// <summary>
    /// Cursor visual style (block, underline, or bar).
    /// </summary>
    /// <remarks>
    /// Controls the shape of the cursor. Can be overridden by applications
    /// using DECSCUSR (set cursor style) escape sequences.
    /// </remarks>
    public CursorStyle CursorStyle { get; init; } = CursorStyle.Block;

    /// <summary>
    /// Whether the cursor should blink.
    /// </summary>
    /// <remarks>
    /// When true, the cursor alternates between visible and invisible
    /// at the interval specified by <see cref="CursorBlinkIntervalMs"/>.
    /// </remarks>
    public bool CursorBlink { get; init; } = true;

    /// <summary>
    /// Cursor blink interval in milliseconds.
    /// </summary>
    /// <remarks>
    /// The time between cursor visibility toggles. The default value of 530ms
    /// matches typical terminal emulator blink rates (approximately 1 blink
    /// per second with equal on/off times).
    /// </remarks>
    public int CursorBlinkIntervalMs { get; init; } = 530;

    #endregion

    #region Methods

    /// <summary>
    /// Gets a color from the extended 256-color palette.
    /// </summary>
    /// <param name="index">Color index (0-255).</param>
    /// <returns>The resolved terminal color.</returns>
    /// <remarks>
    /// <para>
    /// The 256-color palette is divided into three ranges:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <b>0-15</b>: ANSI palette colors (from <see cref="AnsiPalette"/>)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>16-231</b>: 6×6×6 color cube (216 colors, calculated)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>232-255</b>: Grayscale ramp (24 shades, calculated)
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The color cube formula: For index i in 16-231:
    /// <code>
    /// adjusted = i - 16
    /// r = (adjusted / 36) * 51
    /// g = ((adjusted / 6) % 6) * 51
    /// b = (adjusted % 6) * 51
    /// </code>
    /// </para>
    /// <para>
    /// The grayscale formula: For index i in 232-255:
    /// <code>
    /// gray = (i - 232) * 10 + 8
    /// </code>
    /// </para>
    /// </remarks>
    public TerminalColor GetPaletteColor(int index)
    {
        // Clamp out-of-range indices to foreground as fallback
        if (index < 0 || index > 255)
            return Foreground;

        // ─────────────────────────────────────────────────────────────────
        // Colors 0-15: ANSI palette (configurable per theme)
        // These are the classic terminal colors: 8 standard + 8 bright
        // ─────────────────────────────────────────────────────────────────
        if (index < 16)
            return AnsiPalette[index];

        // ─────────────────────────────────────────────────────────────────
        // Colors 16-231: 6×6×6 color cube (216 colors)
        // Each RGB component has 6 levels: 0, 51, 102, 153, 204, 255
        // Index = 16 + (36 × r) + (6 × g) + b, where r,g,b ∈ {0,1,2,3,4,5}
        // ─────────────────────────────────────────────────────────────────
        if (index < 232)
        {
            // Adjust index to 0-based for color cube
            var adjusted = index - 16;

            // Extract RGB components from the linear index
            var r = (byte)((adjusted / 36) * 51);        // 0, 51, 102, 153, 204, 255
            var g = (byte)(((adjusted / 6) % 6) * 51);   // 0, 51, 102, 153, 204, 255
            var b = (byte)((adjusted % 6) * 51);         // 0, 51, 102, 153, 204, 255

            return TerminalColor.FromRgb(r, g, b);
        }

        // ─────────────────────────────────────────────────────────────────
        // Colors 232-255: Grayscale ramp (24 shades)
        // Evenly spaced from near-black (#080808) to near-white (#EEEEEE)
        // Does not include pure black or white (those are in ANSI palette)
        // ─────────────────────────────────────────────────────────────────
        var gray = (byte)((index - 232) * 10 + 8);
        return TerminalColor.FromRgb(gray, gray, gray);
    }

    /// <summary>
    /// Resolves a <see cref="TerminalColor"/> to its actual RGB values.
    /// </summary>
    /// <param name="color">The color to resolve.</param>
    /// <param name="isForeground">Whether this is a foreground (true) or background (false) color.</param>
    /// <returns>The resolved color with concrete RGB values.</returns>
    /// <remarks>
    /// <para>
    /// This method handles all three color representation modes:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <b>Default</b>: Returns <see cref="Foreground"/> or <see cref="Background"/>
    ///       based on <paramref name="isForeground"/>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Palette Index</b>: Looks up the color via <see cref="GetPaletteColor"/>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>RGB</b>: Returns the color unchanged (already has concrete values)
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    public TerminalColor ResolveColor(TerminalColor color, bool isForeground)
    {
        // Handle default colors - use theme's foreground or background
        if (color.IsDefault)
            return isForeground ? Foreground : Background;

        // Handle palette colors - look up from 256-color palette
        if (color.PaletteIndex.HasValue)
            return GetPaletteColor(color.PaletteIndex.Value);

        // RGB colors are already resolved - return as-is
        return color;
    }

    /// <summary>
    /// Gets a semantic color from the theme by name.
    /// </summary>
    /// <param name="colorName">The semantic color name.</param>
    /// <returns>The corresponding color from the theme.</returns>
    /// <remarks>
    /// This method enables dynamic color lookup using the <see cref="TerminalThemeColor"/>
    /// enum, which is useful for rendering code that needs to access theme colors
    /// programmatically.
    /// </remarks>
    public TerminalColor GetSemanticColor(TerminalThemeColor colorName) => colorName switch
    {
        TerminalThemeColor.Background => Background,
        TerminalThemeColor.Foreground => Foreground,
        TerminalThemeColor.Cursor => Cursor,
        TerminalThemeColor.Selection => Selection,
        TerminalThemeColor.BoldForeground => BoldForeground ?? Foreground,
        _ => Foreground  // Fallback for unknown color names
    };

    #endregion

    #region Static Methods

    /// <summary>
    /// Creates the default 16-color ANSI palette.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default colors match typical modern terminal emulators (similar to VS Code).
    /// The palette is organized as:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>0-7: Standard colors (black, red, green, yellow, blue, magenta, cyan, white)</description></item>
    ///   <item><description>8-15: Bright variants of the standard colors</description></item>
    /// </list>
    /// </remarks>
    private static TerminalColor[] CreateDefaultAnsiPalette() =>
    [
        // ─────────────────────────────────────────────────────────────────
        // Standard colors (0-7)
        // These are the classic 8 ANSI colors, tuned for dark backgrounds
        // ─────────────────────────────────────────────────────────────────
        TerminalColor.FromRgb(0, 0, 0),         // 0: Black
        TerminalColor.FromRgb(205, 49, 49),     // 1: Red
        TerminalColor.FromRgb(13, 188, 121),    // 2: Green
        TerminalColor.FromRgb(229, 229, 16),    // 3: Yellow
        TerminalColor.FromRgb(36, 114, 200),    // 4: Blue
        TerminalColor.FromRgb(188, 63, 188),    // 5: Magenta
        TerminalColor.FromRgb(17, 168, 205),    // 6: Cyan
        TerminalColor.FromRgb(229, 229, 229),   // 7: White

        // ─────────────────────────────────────────────────────────────────
        // Bright colors (8-15)
        // Lighter/more saturated versions for bold or highlighted text
        // ─────────────────────────────────────────────────────────────────
        TerminalColor.FromRgb(102, 102, 102),   // 8: Bright Black (Gray)
        TerminalColor.FromRgb(241, 76, 76),     // 9: Bright Red
        TerminalColor.FromRgb(35, 209, 139),    // 10: Bright Green
        TerminalColor.FromRgb(245, 245, 67),    // 11: Bright Yellow
        TerminalColor.FromRgb(59, 142, 234),    // 12: Bright Blue
        TerminalColor.FromRgb(214, 112, 214),   // 13: Bright Magenta
        TerminalColor.FromRgb(41, 184, 219),    // 14: Bright Cyan
        TerminalColor.FromRgb(255, 255, 255),   // 15: Bright White
    ];

    #endregion

    #region Static Theme Presets

    /// <summary>
    /// Default dark theme matching VS Code's terminal colors.
    /// </summary>
    /// <remarks>
    /// A modern dark theme with neutral grays and vibrant ANSI colors.
    /// Background: #1E1E1E (very dark gray)
    /// Foreground: #CCCCCC (light gray)
    /// </remarks>
    public static TerminalTheme Dark => new()
    {
        Name = "Dark",
        Background = TerminalColor.FromRgb(30, 30, 30),      // #1E1E1E
        Foreground = TerminalColor.FromRgb(204, 204, 204),   // #CCCCCC
        Cursor = TerminalColor.FromRgb(204, 204, 204),       // #CCCCCC
        Selection = TerminalColor.FromRgb(68, 119, 170),     // #4477AA
        SelectionAlpha = 80
    };

    /// <summary>
    /// Light theme with white background.
    /// </summary>
    /// <remarks>
    /// A clean light theme suitable for bright environments.
    /// Background: #FFFFFF (white)
    /// Foreground: #000000 (black)
    /// Uses adjusted ANSI colors for better readability on light backgrounds.
    /// </remarks>
    public static TerminalTheme Light => new()
    {
        Name = "Light",
        Background = TerminalColor.FromRgb(255, 255, 255),   // #FFFFFF
        Foreground = TerminalColor.FromRgb(0, 0, 0),         // #000000
        Cursor = TerminalColor.FromRgb(0, 0, 0),             // #000000
        Selection = TerminalColor.FromRgb(173, 214, 255),    // #ADD6FF
        SelectionAlpha = 100,
        AnsiPalette =
        [
            // Light-adjusted ANSI palette for better contrast
            TerminalColor.FromRgb(0, 0, 0),           // 0: Black
            TerminalColor.FromRgb(205, 49, 49),       // 1: Red
            TerminalColor.FromRgb(0, 135, 0),         // 2: Green (darker for light bg)
            TerminalColor.FromRgb(135, 135, 0),       // 3: Yellow (darker for light bg)
            TerminalColor.FromRgb(0, 0, 205),         // 4: Blue
            TerminalColor.FromRgb(135, 0, 135),       // 5: Magenta
            TerminalColor.FromRgb(0, 135, 135),       // 6: Cyan
            TerminalColor.FromRgb(229, 229, 229),     // 7: White
            TerminalColor.FromRgb(102, 102, 102),     // 8: Bright Black
            TerminalColor.FromRgb(241, 76, 76),       // 9: Bright Red
            TerminalColor.FromRgb(0, 175, 0),         // 10: Bright Green
            TerminalColor.FromRgb(175, 175, 0),       // 11: Bright Yellow
            TerminalColor.FromRgb(0, 0, 255),         // 12: Bright Blue
            TerminalColor.FromRgb(175, 0, 175),       // 13: Bright Magenta
            TerminalColor.FromRgb(0, 175, 175),       // 14: Bright Cyan
            TerminalColor.FromRgb(255, 255, 255),     // 15: Bright White
        ]
    };

    /// <summary>
    /// Solarized Dark theme with the classic Solarized color palette.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Solarized theme by Ethan Schoonover features precisely tuned colors
    /// designed for optimal readability and reduced eye strain.
    /// </para>
    /// <para>
    /// Background: #002B36 (Base03 - very dark blue-green)
    /// Foreground: #839496 (Base0 - light gray with blue tint)
    /// </para>
    /// </remarks>
    public static TerminalTheme SolarizedDark => new()
    {
        Name = "Solarized Dark",
        Background = TerminalColor.FromRgb(0, 43, 54),       // #002B36 Base03
        Foreground = TerminalColor.FromRgb(131, 148, 150),   // #839496 Base0
        Cursor = TerminalColor.FromRgb(131, 148, 150),       // #839496 Base0
        Selection = TerminalColor.FromRgb(7, 54, 66),        // #073642 Base02
        SelectionAlpha = 120,
        AnsiPalette =
        [
            // Solarized-specific ANSI palette
            TerminalColor.FromRgb(7, 54, 66),         // 0: Base02 (dark background highlight)
            TerminalColor.FromRgb(220, 50, 47),       // 1: Red
            TerminalColor.FromRgb(133, 153, 0),       // 2: Green
            TerminalColor.FromRgb(181, 137, 0),       // 3: Yellow
            TerminalColor.FromRgb(38, 139, 210),      // 4: Blue
            TerminalColor.FromRgb(211, 54, 130),      // 5: Magenta
            TerminalColor.FromRgb(42, 161, 152),      // 6: Cyan
            TerminalColor.FromRgb(238, 232, 213),     // 7: Base2 (light text)
            TerminalColor.FromRgb(0, 43, 54),         // 8: Base03 (background)
            TerminalColor.FromRgb(203, 75, 22),       // 9: Orange
            TerminalColor.FromRgb(88, 110, 117),      // 10: Base01 (comments)
            TerminalColor.FromRgb(101, 123, 131),     // 11: Base00 (optional emphasis)
            TerminalColor.FromRgb(131, 148, 150),     // 12: Base0 (body text)
            TerminalColor.FromRgb(108, 113, 196),     // 13: Violet
            TerminalColor.FromRgb(147, 161, 161),     // 14: Base1 (optional emphasis)
            TerminalColor.FromRgb(253, 246, 227),     // 15: Base3 (background highlights)
        ]
    };

    #endregion
}
