// -----------------------------------------------------------------------
// <copyright file="TruncationMode.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Defines how to truncate captured output when it exceeds configured limits.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4d.</para>
/// <para>
/// Used by <see cref="OutputCaptureSettings"/> to control which portion of
/// output is preserved when truncation is necessary.
/// </para>
/// </remarks>
public enum TruncationMode
{
    /// <summary>
    /// Keep the beginning of output, truncate the end.
    /// </summary>
    /// <remarks>
    /// Best for: Initial setup output, error context appearing early.
    /// </remarks>
    KeepStart,

    /// <summary>
    /// Keep the end of output, truncate the beginning.
    /// </summary>
    /// <remarks>
    /// Best for: Recent errors, current state, command completion results.
    /// This is the default and most common use case.
    /// </remarks>
    KeepEnd,

    /// <summary>
    /// Keep both start and end, truncate the middle.
    /// </summary>
    /// <remarks>
    /// Best for: When context from both beginning and end is needed.
    /// Preserves initial setup and final results.
    /// </remarks>
    KeepBoth
}

/// <summary>
/// Extension methods for <see cref="TruncationMode"/>.
/// </summary>
/// <remarks>Added in v0.5.4d.</remarks>
public static class TruncationModeExtensions
{
    /// <summary>
    /// Gets the truncation indicator text for this mode.
    /// </summary>
    /// <param name="mode">The truncation mode.</param>
    /// <returns>The indicator text to insert where content was truncated.</returns>
    public static string GetIndicator(this TruncationMode mode) => mode switch
    {
        TruncationMode.KeepStart => "\n...(truncated)",
        TruncationMode.KeepEnd => "...(truncated)\n",
        TruncationMode.KeepBoth => "\n...(truncated)...\n",
        _ => "\n...(truncated)"
    };

    /// <summary>
    /// Gets a user-friendly description of this mode.
    /// </summary>
    /// <param name="mode">The truncation mode.</param>
    /// <returns>A description suitable for UI display.</returns>
    public static string ToDescription(this TruncationMode mode) => mode switch
    {
        TruncationMode.KeepStart => "Keep beginning, truncate end",
        TruncationMode.KeepEnd => "Keep end, truncate beginning",
        TruncationMode.KeepBoth => "Keep beginning and end, truncate middle",
        _ => mode.ToString()
    };
}
