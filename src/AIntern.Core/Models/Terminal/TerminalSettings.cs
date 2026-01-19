// ============================================================================
// File: TerminalSettings.cs
// Path: src/AIntern.Core/Models/Terminal/TerminalSettings.cs
// Description: Terminal-specific settings for appearance, behavior, and shell.
// Created: 2026-01-19
// AI Intern v0.5.5e - Terminal Settings Models
// ============================================================================

namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalSettings (v0.5.5e)                                                   │
// │ Comprehensive terminal settings for appearance, behavior, and shell config. │
// │                                                                              │
// │ Settings Categories:                                                         │
// │   - Appearance: Font, cursor, theme, ligatures                              │
// │   - Behavior: Scrollback, bell, copy/paste, scrolling                       │
// │   - Shell: Default profile, custom profiles                                 │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Terminal-specific settings for appearance, behavior, and shell configuration.
/// </summary>
/// <remarks>
/// <para>
/// This class consolidates all terminal-related settings into a single model.
/// It supports:
/// </para>
/// <list type="bullet">
///   <item><description>Appearance customization (fonts, colors, cursor)</description></item>
///   <item><description>Behavior configuration (scrollback, bell, selection)</description></item>
///   <item><description>Shell profile management</description></item>
///   <item><description>Validation with range constraints</description></item>
///   <item><description>Deep cloning for settings snapshots</description></item>
/// </list>
/// <para>Added in v0.5.5e.</para>
/// </remarks>
public sealed class TerminalSettings
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Appearance Settings
    // ═══════════════════════════════════════════════════════════════════════════

    #region Appearance

    /// <summary>
    /// Font family for terminal text.
    /// </summary>
    /// <remarks>
    /// Supports comma-separated fallback list (e.g., "SF Mono, Menlo, monospace").
    /// Platform-specific default is selected automatically.
    /// </remarks>
    public string FontFamily { get; set; } = GetDefaultMonospaceFont();

    /// <summary>
    /// Font size in points.
    /// </summary>
    /// <remarks>
    /// Valid range: 8-72. Default: 14.
    /// </remarks>
    public double FontSize { get; set; } = 14;

    /// <summary>
    /// Line height multiplier.
    /// </summary>
    /// <remarks>
    /// 1.0 = single spacing. Valid range: 1.0-3.0. Default: 1.2.
    /// </remarks>
    public double LineHeight { get; set; } = 1.2;

    /// <summary>
    /// Letter spacing in pixels.
    /// </summary>
    /// <remarks>
    /// Positive values add space, negative values tighten.
    /// Valid range: -2.0 to 5.0. Default: 0.
    /// </remarks>
    public double LetterSpacing { get; set; } = 0;

    /// <summary>
    /// Terminal color theme name.
    /// </summary>
    /// <remarks>
    /// References <see cref="TerminalTheme.Name"/> from built-in or custom themes.
    /// Default: "Default Dark".
    /// </remarks>
    public string ThemeName { get; set; } = "Default Dark";

    /// <summary>
    /// Cursor display style.
    /// </summary>
    /// <remarks>
    /// Block (full character), Underline (_), or Bar (|).
    /// Default: Block.
    /// </remarks>
    public TerminalCursorStyle CursorStyle { get; set; } = TerminalCursorStyle.Block;

    /// <summary>
    /// Whether the cursor should blink.
    /// </summary>
    public bool CursorBlink { get; set; } = true;

    /// <summary>
    /// Cursor blink rate in milliseconds.
    /// </summary>
    /// <remarks>
    /// Time for one blink cycle (on + off).
    /// Valid range: 250-1500. Default: 530.
    /// </remarks>
    public int CursorBlinkRate { get; set; } = 530;

    /// <summary>
    /// Whether bold text uses bright ANSI colors.
    /// </summary>
    /// <remarks>
    /// When true, bold text (SGR 1) also sets bright color (SGR 90-97).
    /// Traditional terminal behavior. Default: true.
    /// </remarks>
    public bool BoldIsBright { get; set; } = true;

    /// <summary>
    /// Minimum contrast ratio for text accessibility.
    /// </summary>
    /// <remarks>
    /// WCAG AA recommends 4.5:1 for normal text, 3:1 for large text.
    /// Valid range: 1.0-21.0. Default: 4.5.
    /// </remarks>
    public double MinimumContrastRatio { get; set; } = 4.5;

    /// <summary>
    /// Whether to enable font ligatures.
    /// </summary>
    /// <remarks>
    /// Programming ligatures convert sequences like -&gt; to → and != to ≠.
    /// Requires a font that supports ligatures (e.g., Fira Code).
    /// Default: true.
    /// </remarks>
    public bool EnableLigatures { get; set; } = true;

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Behavior Settings
    // ═══════════════════════════════════════════════════════════════════════════

    #region Behavior

    /// <summary>
    /// Number of lines to keep in scrollback buffer.
    /// </summary>
    /// <remarks>
    /// 0 = unlimited (not recommended for memory).
    /// Valid range: 0-1000000. Default: 10000.
    /// </remarks>
    public int ScrollbackLines { get; set; } = 10000;

    /// <summary>
    /// Whether terminal bell is enabled.
    /// </summary>
    /// <remarks>
    /// When false, BEL character (0x07) is ignored.
    /// Default: false.
    /// </remarks>
    public bool BellEnabled { get; set; } = false;

    /// <summary>
    /// Bell notification style.
    /// </summary>
    /// <remarks>
    /// None, Audio (system beep), Visual (screen flash), or Both.
    /// Only applies when <see cref="BellEnabled"/> is true.
    /// Default: Visual.
    /// </remarks>
    public TerminalBellStyle BellStyle { get; set; } = TerminalBellStyle.Visual;

    /// <summary>
    /// Whether to copy selected text automatically.
    /// </summary>
    /// <remarks>
    /// Linux-style behavior: text is copied when selected, paste with middle-click.
    /// Default: false.
    /// </remarks>
    public bool CopyOnSelect { get; set; } = false;

    /// <summary>
    /// Whether to scroll to bottom when typing.
    /// </summary>
    /// <remarks>
    /// When true, any keyboard input scrolls viewport to bottom.
    /// Default: true.
    /// </remarks>
    public bool ScrollOnInput { get; set; } = true;

    /// <summary>
    /// Whether to scroll to bottom when new output appears.
    /// </summary>
    /// <remarks>
    /// When true, new command output scrolls viewport to bottom.
    /// Default: false (allows reviewing scrollback during output).
    /// </remarks>
    public bool ScrollOnOutput { get; set; } = false;

    /// <summary>
    /// Characters that separate words for double-click selection.
    /// </summary>
    /// <remarks>
    /// Default includes common shell delimiters.
    /// </remarks>
    public string WordSeparators { get; set; } = " ()[]{}',\"`";

    /// <summary>
    /// Whether to sync terminal working directory with workspace.
    /// </summary>
    /// <remarks>
    /// When true, terminal cd follows workspace root changes.
    /// Default: true.
    /// </remarks>
    public bool SyncWithWorkspace { get; set; } = true;

    /// <summary>
    /// Whether to confirm before closing terminal with running process.
    /// </summary>
    /// <remarks>
    /// Prevents accidental termination of long-running commands.
    /// Default: true.
    /// </remarks>
    public bool ConfirmOnClose { get; set; } = true;

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Shell Configuration
    // ═══════════════════════════════════════════════════════════════════════════

    #region Shell

    /// <summary>
    /// ID of the default shell profile.
    /// </summary>
    /// <remarks>
    /// Null = auto-detect system default shell.
    /// References <see cref="ShellProfile.Id"/>.
    /// </remarks>
    public string? DefaultProfileId { get; set; }

    /// <summary>
    /// User-defined shell profiles.
    /// </summary>
    /// <remarks>
    /// Custom profiles in addition to detected system shells.
    /// </remarks>
    public List<ShellProfile> CustomProfiles { get; set; } = new();

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════════

    #region Helpers

    /// <summary>
    /// Gets the platform-appropriate default monospace font.
    /// </summary>
    /// <returns>Comma-separated font fallback list.</returns>
    private static string GetDefaultMonospaceFont()
    {
        // ─────────────────────────────────────────────────────────────────────
        // Windows: Cascadia Code (Win10+), Consolas (fallback)
        // ─────────────────────────────────────────────────────────────────────
        if (OperatingSystem.IsWindows())
            return "Cascadia Code, Cascadia Mono, Consolas, Courier New";

        // ─────────────────────────────────────────────────────────────────────
        // macOS: SF Mono (Sierra+), Menlo (older macOS), Monaco (fallback)
        // ─────────────────────────────────────────────────────────────────────
        if (OperatingSystem.IsMacOS())
            return "SF Mono, Menlo, Monaco, Courier New";

        // ─────────────────────────────────────────────────────────────────────
        // Linux: Ubuntu Mono (Ubuntu), DejaVu (common), Liberation (fallback)
        // ─────────────────────────────────────────────────────────────────────
        return "Ubuntu Mono, DejaVu Sans Mono, Liberation Mono, monospace";
    }

    /// <summary>
    /// Creates a deep copy of these settings.
    /// </summary>
    /// <returns>Independent copy of settings.</returns>
    public TerminalSettings Clone()
    {
        return new TerminalSettings
        {
            // Appearance
            FontFamily = FontFamily,
            FontSize = FontSize,
            LineHeight = LineHeight,
            LetterSpacing = LetterSpacing,
            ThemeName = ThemeName,
            CursorStyle = CursorStyle,
            CursorBlink = CursorBlink,
            CursorBlinkRate = CursorBlinkRate,
            BoldIsBright = BoldIsBright,
            MinimumContrastRatio = MinimumContrastRatio,
            EnableLigatures = EnableLigatures,

            // Behavior
            ScrollbackLines = ScrollbackLines,
            BellEnabled = BellEnabled,
            BellStyle = BellStyle,
            CopyOnSelect = CopyOnSelect,
            ScrollOnInput = ScrollOnInput,
            ScrollOnOutput = ScrollOnOutput,
            WordSeparators = WordSeparators,
            SyncWithWorkspace = SyncWithWorkspace,
            ConfirmOnClose = ConfirmOnClose,

            // Shell
            DefaultProfileId = DefaultProfileId,
            CustomProfiles = CustomProfiles.Select(p => p.Clone()).ToList()
        };
    }

    /// <summary>
    /// Validates settings and returns any validation errors.
    /// </summary>
    /// <returns>List of validation error messages (empty if valid).</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        // ─────────────────────────────────────────────────────────────────────
        // Appearance Validation
        // ─────────────────────────────────────────────────────────────────────
        if (FontSize < 8 || FontSize > 72)
            errors.Add($"FontSize must be between 8 and 72, got {FontSize}");

        if (LineHeight < 1.0 || LineHeight > 3.0)
            errors.Add($"LineHeight must be between 1.0 and 3.0, got {LineHeight}");

        if (LetterSpacing < -2.0 || LetterSpacing > 5.0)
            errors.Add($"LetterSpacing must be between -2.0 and 5.0, got {LetterSpacing}");

        if (CursorBlinkRate < 250 || CursorBlinkRate > 1500)
            errors.Add($"CursorBlinkRate must be between 250 and 1500, got {CursorBlinkRate}");

        if (MinimumContrastRatio < 1.0 || MinimumContrastRatio > 21.0)
            errors.Add($"MinimumContrastRatio must be between 1.0 and 21.0, got {MinimumContrastRatio}");

        // ─────────────────────────────────────────────────────────────────────
        // Behavior Validation
        // ─────────────────────────────────────────────────────────────────────
        if (ScrollbackLines < 0 || ScrollbackLines > 1_000_000)
            errors.Add($"ScrollbackLines must be between 0 and 1000000, got {ScrollbackLines}");

        return errors;
    }

    /// <summary>
    /// Returns a string representation of the settings.
    /// </summary>
    public override string ToString() =>
        $"TerminalSettings {{ Font={FontFamily} {FontSize}pt, Theme={ThemeName}, Cursor={CursorStyle} }}";

    #endregion
}
