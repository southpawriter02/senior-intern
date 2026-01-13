namespace AIntern.Data;

/// <summary>
/// String extension methods for seed data formatting.
/// </summary>
/// <remarks>
/// <para>
/// This class provides utility methods for formatting multi-line string literals
/// used in seed data definitions. The <see cref="TrimIndent"/> method is particularly
/// useful for raw string literals that include leading whitespace for code alignment.
/// </para>
/// </remarks>
internal static class StringExtensions
{
    /// <summary>
    /// Trims common leading whitespace from all lines in a multi-line string.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The text with common indentation removed.</returns>
    /// <remarks>
    /// <para>
    /// This method finds the minimum indentation across all non-empty lines
    /// and removes that many leading whitespace characters from each line.
    /// This is useful for cleaning up raw string literals that are indented
    /// for readability in the source code.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var text = """
    ///     Line 1 with indent
    ///     Line 2 with indent
    ///     """;
    /// var trimmed = text.TrimIndent();
    /// // Result:
    /// // "Line 1 with indent\nLine 2 with indent"
    /// </code>
    /// </example>
    public static string TrimIndent(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var lines = text.Split('\n');

        // Find minimum indentation (ignoring empty lines).
        // We only consider lines with actual content to determine the common indent.
        var minIndent = lines
            .Where(line => line.Trim().Length > 0)
            .Select(line => line.TakeWhile(char.IsWhiteSpace).Count())
            .DefaultIfEmpty(0)
            .Min();

        // Remove the common indentation from each line.
        // Lines shorter than minIndent are fully trimmed to handle edge cases.
        var result = lines
            .Select(line => line.Length >= minIndent ? line[minIndent..] : line.TrimStart())
            .ToArray();

        return string.Join('\n', result).Trim();
    }
}
