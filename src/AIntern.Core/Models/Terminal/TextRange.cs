// -----------------------------------------------------------------------
// <copyright file="TextRange.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Core.Models.Terminal;

/// <summary>
/// Represents a character range within text content.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4a.</para>
/// <para>
/// This struct is used to track the source location of extracted commands
/// within message content, enabling precise highlighting and navigation.
/// </para>
/// </remarks>
/// <param name="Start">Starting character index (0-based, inclusive).</param>
/// <param name="End">Ending character index (0-based, exclusive).</param>
public readonly record struct TextRange(int Start, int End)
{
    // ═══════════════════════════════════════════════════════════════════════
    // COMPUTED PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the number of characters in this range.
    /// </summary>
    /// <remarks>
    /// Calculated as <c>End - Start</c>. May be negative for invalid ranges.
    /// </remarks>
    public int Length => End - Start;

    /// <summary>
    /// Gets a value indicating whether this range is valid.
    /// </summary>
    /// <remarks>
    /// A range is valid if both indices are non-negative and End >= Start.
    /// </remarks>
    public bool IsValid => Start >= 0 && End >= Start;

    /// <summary>
    /// Gets a value indicating whether this range is empty (zero length).
    /// </summary>
    public bool IsEmpty => Length == 0;

    // ═══════════════════════════════════════════════════════════════════════
    // FACTORY PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets an empty range at position 0.
    /// </summary>
    public static TextRange Empty => new(0, 0);

    // ═══════════════════════════════════════════════════════════════════════
    // METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Extracts the text content covered by this range.
    /// </summary>
    /// <param name="source">Source text to extract from.</param>
    /// <returns>
    /// Substring within this range, or empty string if out of bounds or invalid.
    /// </returns>
    /// <remarks>
    /// This method clamps the End value to the source length to avoid exceptions.
    /// </remarks>
    public string Extract(string source)
    {
        // Log: Extracting text from range [{Start}..{End}], source length: {source?.Length ?? 0}
        if (string.IsNullOrEmpty(source))
        {
            return string.Empty;
        }

        if (!IsValid || Start >= source.Length)
        {
            return string.Empty;
        }

        // Clamp end to source length to handle overflow gracefully
        var actualEnd = Math.Min(End, source.Length);
        return source[Start..actualEnd];
    }

    /// <summary>
    /// Checks if this range contains a specific character index.
    /// </summary>
    /// <param name="index">The character index to check.</param>
    /// <returns>True if the index falls within [Start, End); otherwise false.</returns>
    public bool Contains(int index) => index >= Start && index < End;

    /// <summary>
    /// Checks if this range overlaps with another range.
    /// </summary>
    /// <param name="other">The other range to check against.</param>
    /// <returns>True if the ranges overlap; otherwise false.</returns>
    /// <remarks>
    /// Two ranges overlap if one starts before the other ends, and vice versa.
    /// Adjacent ranges (one ends where another starts) do not overlap.
    /// </remarks>
    public bool Overlaps(TextRange other) =>
        Start < other.End && End > other.Start;

    /// <summary>
    /// Returns a new range offset by the specified amount.
    /// </summary>
    /// <param name="offset">
    /// The offset to apply (positive shifts right, negative shifts left).
    /// </param>
    /// <returns>A new TextRange with both Start and End adjusted by offset.</returns>
    public TextRange Offset(int offset) => new(Start + offset, End + offset);

    /// <summary>
    /// Creates a range that encompasses both this range and another.
    /// </summary>
    /// <param name="other">The other range to include.</param>
    /// <returns>
    /// A new TextRange from the minimum Start to the maximum End of both ranges.
    /// </returns>
    public TextRange Union(TextRange other) =>
        new(Math.Min(Start, other.Start), Math.Max(End, other.End));

    /// <summary>
    /// Creates a range representing the intersection of this range and another.
    /// </summary>
    /// <param name="other">The other range to intersect with.</param>
    /// <returns>
    /// The overlapping portion, or an empty range if no overlap exists.
    /// </returns>
    public TextRange Intersect(TextRange other)
    {
        var newStart = Math.Max(Start, other.Start);
        var newEnd = Math.Min(End, other.End);
        return newEnd > newStart ? new(newStart, newEnd) : Empty;
    }

    /// <summary>
    /// Returns a string representation of this range.
    /// </summary>
    /// <returns>A string in the format "[Start..End]".</returns>
    public override string ToString() => $"[{Start}..{End}]";
}
