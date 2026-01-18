// -----------------------------------------------------------------------
// <copyright file="OutputCaptureMode.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Mode of terminal output capture.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4a.</para>
/// <para>
/// Different capture modes offer trade-offs between context completeness
/// and token budget. The mode determines how much terminal content is
/// included when sharing output with the AI assistant.
/// </para>
/// </remarks>
public enum OutputCaptureMode
{
    /// <summary>
    /// Capture entire visible scrollback buffer.
    /// </summary>
    /// <remarks>
    /// Use for comprehensive context when asking "Show me all terminal output".
    /// Token impact: High (may require truncation).
    /// </remarks>
    FullBuffer,

    /// <summary>
    /// Capture output from the most recent command only.
    /// </summary>
    /// <remarks>
    /// Uses shell integration (OSC 133) or prompt detection to identify
    /// command boundaries. Use for "Why did this command fail?".
    /// Token impact: Low to Medium.
    /// </remarks>
    LastCommand,

    /// <summary>
    /// Capture user-selected text only.
    /// </summary>
    /// <remarks>
    /// Use for "Explain this error message" when user highlights specific text.
    /// Token impact: Varies based on selection size.
    /// </remarks>
    Selection,

    /// <summary>
    /// Capture last N lines of output.
    /// </summary>
    /// <remarks>
    /// Configurable line count for "What happened recently?" queries.
    /// Token impact: Configurable.
    /// </remarks>
    LastNLines,

    /// <summary>
    /// Manual capture with explicit start/end markers.
    /// </summary>
    /// <remarks>
    /// User explicitly marks capture boundaries for precise control.
    /// Token impact: Varies based on marked region.
    /// </remarks>
    Manual
}

// ═══════════════════════════════════════════════════════════════════════════
// EXTENSION METHODS
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Extension methods for <see cref="OutputCaptureMode"/>.
/// </summary>
/// <remarks>Added in v0.5.4a.</remarks>
public static class OutputCaptureModeExtensions
{
    /// <summary>
    /// Gets a description suitable for UI display.
    /// </summary>
    /// <param name="mode">The capture mode.</param>
    /// <returns>Human-readable description of the mode.</returns>
    public static string ToDescription(this OutputCaptureMode mode) => mode switch
    {
        OutputCaptureMode.FullBuffer => "Entire terminal buffer",
        OutputCaptureMode.LastCommand => "Last command output",
        OutputCaptureMode.Selection => "Selected text",
        OutputCaptureMode.LastNLines => "Last N lines",
        OutputCaptureMode.Manual => "Manual selection",
        _ => mode.ToString()
    };

    /// <summary>
    /// Gets the default maximum characters for this mode.
    /// </summary>
    /// <param name="mode">The capture mode.</param>
    /// <returns>Default character limit for truncation.</returns>
    /// <remarks>
    /// These defaults balance context completeness with token budget.
    /// Users may override these values in settings.
    /// </remarks>
    public static int GetDefaultMaxCharacters(this OutputCaptureMode mode) => mode switch
    {
        OutputCaptureMode.FullBuffer => 50000,
        OutputCaptureMode.LastCommand => 20000,
        OutputCaptureMode.Selection => 10000,
        OutputCaptureMode.LastNLines => 10000,
        OutputCaptureMode.Manual => 50000,
        _ => 20000
    };

    /// <summary>
    /// Gets the estimated token count for the default character limit.
    /// </summary>
    /// <param name="mode">The capture mode.</param>
    /// <returns>Approximate token count assuming ~4 characters per token.</returns>
    public static int GetEstimatedMaxTokens(this OutputCaptureMode mode) =>
        mode.GetDefaultMaxCharacters() / 4;

    /// <summary>
    /// Gets a value indicating whether this mode requires user interaction.
    /// </summary>
    /// <param name="mode">The capture mode.</param>
    /// <returns>True if the mode requires user selection or marking.</returns>
    public static bool RequiresUserInteraction(this OutputCaptureMode mode) =>
        mode is OutputCaptureMode.Selection or OutputCaptureMode.Manual;
}
