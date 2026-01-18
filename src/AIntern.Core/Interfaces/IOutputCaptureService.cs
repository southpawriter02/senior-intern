// -----------------------------------------------------------------------
// <copyright file="IOutputCaptureService.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Interfaces;

using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ OUTPUT CAPTURE SERVICE INTERFACE (v0.5.4d)                              │
// │ Captures terminal output for AI context with processing and history.    │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for capturing terminal output for AI context.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4d.</para>
/// <para>
/// Provides multiple capture modes:
/// </para>
/// <list type="bullet">
/// <item>
///     <term>Stream capture</term>
///     <description>
///     Use <see cref="StartCapture"/>/<see cref="StopCaptureAsync"/>
///     to capture output during command execution.
///     </description>
/// </item>
/// <item>
///     <term>Buffer capture</term>
///     <description>
///     Use <see cref="CaptureBufferAsync"/> for on-demand capture
///     of the current terminal buffer.
///     </description>
/// </item>
/// <item>
///     <term>Selection capture</term>
///     <description>
///     Use <see cref="CaptureSelectionAsync"/> to capture
///     user-selected text.
///     </description>
/// </item>
/// </list>
/// <para>
/// Output is processed according to <see cref="OutputCaptureSettings"/>:
/// </para>
/// <list type="bullet">
/// <item><description>ANSI escape sequence stripping</description></item>
/// <item><description>Line ending normalization</description></item>
/// <item><description>Size-based truncation</description></item>
/// </list>
/// <para>
/// Captured output is stored in a per-session history for later retrieval.
/// </para>
/// </remarks>
public interface IOutputCaptureService
{
    // ═══════════════════════════════════════════════════════════════════════
    // STREAM CAPTURE (during command execution)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Start capturing output from a terminal session.
    /// </summary>
    /// <param name="sessionId">The session to capture from.</param>
    /// <param name="commandContext">Optional command text for context.</param>
    /// <remarks>
    /// <para>
    /// Call this before executing a command to capture its output.
    /// Output is accumulated until <see cref="StopCaptureAsync"/> is called.
    /// </para>
    /// <para>
    /// Only one capture can be active per session. Starting a new capture
    /// while one is active discards the previous capture.
    /// </para>
    /// </remarks>
    void StartCapture(Guid sessionId, string? commandContext = null);

    /// <summary>
    /// Stop capturing and return the accumulated output.
    /// </summary>
    /// <param name="sessionId">The session to stop capturing.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Captured output with processing applied, or null if no capture was active.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The output is processed according to current settings:
    /// ANSI stripping, line ending normalization, and truncation.
    /// </para>
    /// <para>
    /// The capture is added to the session's history for later retrieval.
    /// </para>
    /// </remarks>
    Task<TerminalOutputCapture?> StopCaptureAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Check if a capture is currently active for a session.
    /// </summary>
    /// <param name="sessionId">The session to check.</param>
    /// <returns>True if capture is active, false otherwise.</returns>
    bool IsCaptureActive(Guid sessionId);

    // ═══════════════════════════════════════════════════════════════════════
    // BUFFER CAPTURE (on-demand)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Capture the current terminal buffer content.
    /// </summary>
    /// <param name="sessionId">The session to capture from.</param>
    /// <param name="mode">Capture mode (full buffer, last N lines, etc.).</param>
    /// <param name="lineCount">Number of lines for LastNLines mode.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Captured buffer content with processing applied.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the session or buffer is not found.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Captures the current state of the terminal buffer.
    /// This is a snapshot - output received after capture is not included.
    /// </para>
    /// <para>
    /// Use <see cref="OutputCaptureMode.LastNLines"/> to capture
    /// only recent output, reducing context size for AI prompts.
    /// </para>
    /// </remarks>
    Task<TerminalOutputCapture> CaptureBufferAsync(
        Guid sessionId,
        OutputCaptureMode mode = OutputCaptureMode.FullBuffer,
        int? lineCount = null,
        CancellationToken ct = default);

    /// <summary>
    /// Capture user-selected text from the terminal.
    /// </summary>
    /// <param name="sessionId">The session to capture from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Captured selection with processing applied, or null if nothing is selected.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Captures the text currently selected by the user in the terminal.
    /// Useful for allowing users to manually select relevant output.
    /// </para>
    /// </remarks>
    Task<TerminalOutputCapture?> CaptureSelectionAsync(
        Guid sessionId,
        CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════════════
    // HISTORY
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get recent captures for a session.
    /// </summary>
    /// <param name="sessionId">The session to get captures for.</param>
    /// <param name="count">Maximum number of captures to return.</param>
    /// <returns>
    /// List of recent captures, ordered from most recent to oldest.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Returns up to <paramref name="count"/> captures from the session's history.
    /// History size is controlled by <see cref="OutputCaptureSettings.CaptureHistorySize"/>.
    /// </para>
    /// </remarks>
    IReadOnlyList<TerminalOutputCapture> GetRecentCaptures(Guid sessionId, int count = 10);

    /// <summary>
    /// Get a specific capture by its ID.
    /// </summary>
    /// <param name="captureId">The capture ID.</param>
    /// <returns>The capture if found, null otherwise.</returns>
    TerminalOutputCapture? GetCapture(Guid captureId);

    /// <summary>
    /// Clear capture history for a session.
    /// </summary>
    /// <param name="sessionId">The session to clear history for.</param>
    void ClearHistory(Guid sessionId);

    // ═══════════════════════════════════════════════════════════════════════
    // CONFIGURATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Configure capture settings.
    /// </summary>
    /// <param name="settings">New settings to apply.</param>
    /// <remarks>
    /// <para>
    /// Settings are applied to all subsequent captures.
    /// Existing captures are not affected.
    /// </para>
    /// <para>
    /// Use <see cref="OutputCaptureSettings.ForAIContext"/> for
    /// optimal AI consumption, or <see cref="OutputCaptureSettings.Raw"/>
    /// to preserve original output.
    /// </para>
    /// </remarks>
    void Configure(OutputCaptureSettings settings);

    /// <summary>
    /// Gets the current capture settings.
    /// </summary>
    OutputCaptureSettings Settings { get; }
}
