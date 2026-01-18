namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalThemeColor (v0.5.2a)                                             │
// │ Named semantic colors for referencing theme colors programmatically.     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Named semantic colors in a terminal theme.
/// </summary>
/// <remarks>
/// <para>
/// Used for referencing specific theme colors by name rather than property.
/// This enables dynamic color resolution in rendering code without needing
/// to switch on property names or use reflection.
/// </para>
/// <para>Added in v0.5.2a.</para>
/// </remarks>
public enum TerminalThemeColor
{
    /// <summary>
    /// The terminal background color.
    /// </summary>
    Background,

    /// <summary>
    /// The default text foreground color.
    /// </summary>
    Foreground,

    /// <summary>
    /// The cursor color.
    /// </summary>
    Cursor,

    /// <summary>
    /// The text selection highlight color.
    /// </summary>
    Selection,

    /// <summary>
    /// The bold text foreground color.
    /// </summary>
    /// <remarks>
    /// When the theme does not specify a bold foreground color,
    /// the regular foreground color should be used with bold font weight.
    /// </remarks>
    BoldForeground
}
