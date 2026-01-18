namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ CursorStyle (v0.5.2a)                                                    │
// │ Defines the visual style for the terminal cursor.                        │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Terminal cursor display style.
/// Controls how the cursor is rendered in the terminal view.
/// </summary>
/// <remarks>
/// <para>
/// The cursor style affects the visual appearance of the text insertion point
/// in the terminal. Different styles may be more appropriate for different
/// use cases:
/// <list type="bullet">
///   <item><description><see cref="Block"/>: Most common for general terminal use</description></item>
///   <item><description><see cref="Underline"/>: Often used in insert mode</description></item>
///   <item><description><see cref="Bar"/>: Common in modern text editors</description></item>
/// </list>
/// </para>
/// <para>Added in v0.5.2a.</para>
/// </remarks>
public enum CursorStyle
{
    /// <summary>
    /// Solid block cursor that fills the entire character cell.
    /// </summary>
    /// <remarks>
    /// The most common cursor style for terminal emulators.
    /// When the cursor is over a character, the character is typically
    /// displayed in reverse video (inverted colors).
    /// </remarks>
    Block,

    /// <summary>
    /// Underline cursor displayed at the bottom of the character cell.
    /// </summary>
    /// <remarks>
    /// Typically 1-2 pixels high, positioned at the baseline of the text.
    /// Often used in insert mode for some editors (like vim) to distinguish
    /// between different editing modes.
    /// </remarks>
    Underline,

    /// <summary>
    /// Vertical bar cursor displayed at the left edge of the character cell.
    /// </summary>
    /// <remarks>
    /// Also known as "I-beam" or "line" cursor. Typically 1-2 pixels wide.
    /// Common in modern text editors and IDEs as it indicates the exact
    /// insertion point between characters.
    /// </remarks>
    Bar
}
