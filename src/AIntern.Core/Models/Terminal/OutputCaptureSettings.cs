// -----------------------------------------------------------------------
// <copyright file="OutputCaptureSettings.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Configuration settings for terminal output capture.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4d.</para>
/// <para>
/// Controls how output is processed before being returned:
/// </para>
/// <list type="bullet">
/// <item><description>Size limits (characters and lines)</description></item>
/// <item><description>Truncation strategy when limits exceeded</description></item>
/// <item><description>ANSI escape sequence handling</description></item>
/// <item><description>Line ending normalization</description></item>
/// <item><description>History retention</description></item>
/// </list>
/// <para>
/// Use factory methods for common configurations:
/// <see cref="ForAIContext"/>, <see cref="ForFullCapture"/>, <see cref="Raw"/>.
/// </para>
/// </remarks>
public sealed class OutputCaptureSettings
{
    // ═══════════════════════════════════════════════════════════════════════
    // SIZE LIMITS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maximum characters to capture before truncation.
    /// </summary>
    /// <remarks>
    /// Default: 8000 characters (~2000 tokens for most LLMs).
    /// </remarks>
    public int MaxCaptureLength { get; set; } = 8000;

    /// <summary>
    /// Maximum lines to capture before truncation.
    /// </summary>
    /// <remarks>
    /// Default: 500 lines. Applied before character limit.
    /// </remarks>
    public int MaxCaptureLines { get; set; } = 500;

    // ═══════════════════════════════════════════════════════════════════════
    // TRUNCATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// How to truncate when limits are exceeded.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="TruncationMode.KeepEnd"/> - preserves recent output.
    /// </remarks>
    public TruncationMode TruncationMode { get; set; } = TruncationMode.KeepEnd;

    // ═══════════════════════════════════════════════════════════════════════
    // OUTPUT PROCESSING
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Whether to strip ANSI escape sequences from output.
    /// </summary>
    /// <remarks>
    /// Default: true. Removes color codes, cursor movements, etc.
    /// for cleaner text suitable for AI consumption.
    /// </remarks>
    public bool StripAnsiSequences { get; set; } = true;

    /// <summary>
    /// Whether to normalize line endings to \n (LF).
    /// </summary>
    /// <remarks>
    /// Default: true. Converts \r\n (CRLF) and \r (CR) to \n.
    /// </remarks>
    public bool NormalizeLineEndings { get; set; } = true;

    // ═══════════════════════════════════════════════════════════════════════
    // HISTORY
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Number of recent captures to retain per session.
    /// </summary>
    /// <remarks>
    /// Default: 20. Older captures are automatically pruned.
    /// </remarks>
    public int CaptureHistorySize { get; set; } = 20;

    // ═══════════════════════════════════════════════════════════════════════
    // FACTORY METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Settings optimized for AI context (smaller, cleaner output).
    /// </summary>
    /// <returns>Settings with reduced limits for efficient AI consumption.</returns>
    /// <remarks>
    /// <para>Configuration:</para>
    /// <list type="bullet">
    /// <item><description>MaxCaptureLength: 4000 chars (~1000 tokens)</description></item>
    /// <item><description>MaxCaptureLines: 200 lines</description></item>
    /// <item><description>TruncationMode: KeepEnd (recent output)</description></item>
    /// <item><description>All cleanup enabled</description></item>
    /// </list>
    /// </remarks>
    public static OutputCaptureSettings ForAIContext() => new()
    {
        MaxCaptureLength = 4000,
        MaxCaptureLines = 200,
        TruncationMode = TruncationMode.KeepEnd,
        StripAnsiSequences = true,
        NormalizeLineEndings = true,
        CaptureHistorySize = 10
    };

    /// <summary>
    /// Settings for full capture with larger limits.
    /// </summary>
    /// <returns>Settings with generous limits for detailed output.</returns>
    /// <remarks>
    /// <para>Configuration:</para>
    /// <list type="bullet">
    /// <item><description>MaxCaptureLength: 50000 chars</description></item>
    /// <item><description>MaxCaptureLines: 2000 lines</description></item>
    /// <item><description>TruncationMode: KeepBoth (preserve context)</description></item>
    /// <item><description>All cleanup enabled</description></item>
    /// </list>
    /// </remarks>
    public static OutputCaptureSettings ForFullCapture() => new()
    {
        MaxCaptureLength = 50000,
        MaxCaptureLines = 2000,
        TruncationMode = TruncationMode.KeepBoth,
        StripAnsiSequences = true,
        NormalizeLineEndings = true,
        CaptureHistorySize = 50
    };

    /// <summary>
    /// Settings that preserve raw output without processing.
    /// </summary>
    /// <returns>Settings with no limits and no cleanup.</returns>
    /// <remarks>
    /// <para>Configuration:</para>
    /// <list type="bullet">
    /// <item><description>No size limits</description></item>
    /// <item><description>ANSI sequences preserved</description></item>
    /// <item><description>Line endings unchanged</description></item>
    /// </list>
    /// <para>
    /// <b>Warning:</b> Raw output may be very large and contain
    /// control sequences that are hard to read.
    /// </para>
    /// </remarks>
    public static OutputCaptureSettings Raw() => new()
    {
        MaxCaptureLength = int.MaxValue,
        MaxCaptureLines = int.MaxValue,
        TruncationMode = TruncationMode.KeepEnd,
        StripAnsiSequences = false,
        NormalizeLineEndings = false,
        CaptureHistorySize = 5
    };
}
