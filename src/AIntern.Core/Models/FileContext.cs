namespace AIntern.Core.Models;

using AIntern.Core.Utilities;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Represents file content attached to a chat message as context.
/// Supports both full file content and partial selections.
/// </summary>
/// <remarks>
/// <para>
/// FileContext captures a snapshot of file content at a specific point in time,
/// including metadata like line count, token estimate, and content hash.
/// </para>
/// <para>
/// The content hash can be used to detect if the file has changed since it was attached,
/// allowing the UI to warn users about outdated context.
/// </para>
/// </remarks>
public sealed class FileContext
{
    /// <summary>
    /// Gets the unique identifier for this context attachment.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the absolute path to the source file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the file name extracted from <see cref="FilePath"/>.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets the file content (full file or selection).
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the detected programming language.
    /// Null if the language could not be detected.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Gets the total line count of the content.
    /// </summary>
    public int LineCount { get; init; }

    /// <summary>
    /// Gets the estimated token count for LLM context budgeting.
    /// </summary>
    public int EstimatedTokens { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the context was attached.
    /// </summary>
    public DateTime AttachedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the starting line number if partial content (1-indexed).
    /// Null if the entire file is attached.
    /// </summary>
    public int? StartLine { get; init; }

    /// <summary>
    /// Gets the ending line number if partial content (1-indexed, inclusive).
    /// Null if the entire file is attached.
    /// </summary>
    public int? EndLine { get; init; }

    /// <summary>
    /// Gets the SHA256 hash prefix of the content for change detection.
    /// First 16 characters of the hex-encoded hash.
    /// </summary>
    public string ContentHash { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether this is a partial file (selection) rather than the full file.
    /// </summary>
    public bool IsPartialContent => StartLine.HasValue || EndLine.HasValue;

    /// <summary>
    /// Gets a display label for UI showing file name and line range if applicable.
    /// </summary>
    public string DisplayLabel => IsPartialContent
        ? $"{FileName} (lines {StartLine}-{EndLine})"
        : FileName;

    /// <summary>
    /// Gets the content size in bytes (UTF-8 encoded).
    /// </summary>
    public int ContentSizeBytes => Encoding.UTF8.GetByteCount(Content);

    /// <summary>
    /// Creates a FileContext from a full file.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="content">Full file content.</param>
    /// <returns>A new FileContext for the entire file.</returns>
    public static FileContext FromFile(string filePath, string content)
    {
        var lineCount = CountLines(content);
        var language = LanguageDetector.DetectByFileName(Path.GetFileName(filePath));

        return new FileContext
        {
            FilePath = filePath,
            Content = content,
            Language = language,
            LineCount = lineCount,
            EstimatedTokens = TokenEstimator.Estimate(content, language),
            ContentHash = ComputeHash(content)
        };
    }

    /// <summary>
    /// Creates a FileContext from a code selection.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="content">Selected content.</param>
    /// <param name="startLine">Starting line number (1-indexed).</param>
    /// <param name="endLine">Ending line number (1-indexed, inclusive).</param>
    /// <returns>A new FileContext for the selection.</returns>
    public static FileContext FromSelection(
        string filePath,
        string content,
        int startLine,
        int endLine)
    {
        var lineCount = endLine - startLine + 1;
        var language = LanguageDetector.DetectByFileName(Path.GetFileName(filePath));

        return new FileContext
        {
            FilePath = filePath,
            Content = content,
            Language = language,
            LineCount = lineCount,
            EstimatedTokens = TokenEstimator.Estimate(content, language),
            StartLine = startLine,
            EndLine = endLine,
            ContentHash = ComputeHash(content)
        };
    }

    /// <summary>
    /// Formats the content for inclusion in an LLM prompt.
    /// Includes a header comment with file info.
    /// </summary>
    /// <returns>Content formatted with file info header.</returns>
    public string FormatForLlmContext()
    {
        var sb = new StringBuilder();

        // Build header line
        if (IsPartialContent)
        {
            sb.Append($"// File: {FileName} (lines {StartLine}-{EndLine})");
        }
        else
        {
            sb.Append($"// File: {FileName}");
        }

        if (Language is not null)
        {
            sb.Append($" [{Language}]");
        }

        sb.AppendLine();
        sb.Append(Content);

        return sb.ToString();
    }

    /// <summary>
    /// Counts lines in the content (handles various line ending formats).
    /// </summary>
    private static int CountLines(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        // Count newlines and add 1 (last line may not end with newline)
        return content.Count(c => c == '\n') + 1;
    }

    /// <summary>
    /// Computes a SHA256 hash prefix for change detection.
    /// </summary>
    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        // First 16 characters is sufficient for change detection
        return Convert.ToHexString(hashBytes)[..16];
    }
}
