using System.Security.Cryptography;
using System.Text;
using AIntern.Core.Utilities;

namespace AIntern.Core.Models;

/// <summary>
/// Represents file content attached to a chat message as context.
/// </summary>
public sealed class FileContext
{
    /// <summary>Unique identifier for the context attachment.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Absolute path to the file.</summary>
    public required string FilePath { get; init; }

    /// <summary>File name extracted from the path.</summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>The actual file content (or selection content).</summary>
    public required string Content { get; init; }

    /// <summary>Detected programming language.</summary>
    public string? Language { get; init; }

    /// <summary>Total line count of the content.</summary>
    public int LineCount { get; init; }

    /// <summary>Estimated token count for LLM context budget.</summary>
    public int EstimatedTokens { get; init; }

    /// <summary>When the context was attached.</summary>
    public DateTime AttachedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Starting line number if partial content (1-indexed). Null if entire file.</summary>
    public int? StartLine { get; init; }

    /// <summary>Ending line number if partial content (1-indexed, inclusive). Null if entire file.</summary>
    public int? EndLine { get; init; }

    /// <summary>Hash of the content for detecting changes.</summary>
    public string ContentHash { get; init; } = string.Empty;

    /// <summary>Whether this is a partial file (selection) or full file.</summary>
    public bool IsPartialContent => StartLine.HasValue || EndLine.HasValue;

    /// <summary>Display label for UI showing file name and line range if applicable.</summary>
    public string DisplayLabel => IsPartialContent
        ? $"{FileName} (lines {StartLine}-{EndLine})"
        : FileName;

    /// <summary>Content size in bytes.</summary>
    public int ContentSizeBytes => Encoding.UTF8.GetByteCount(Content);

    /// <summary>Creates a FileContext from a full file.</summary>
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

    /// <summary>Creates a FileContext from a code selection.</summary>
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

    /// <summary>Formats the content for LLM context with file info header.</summary>
    public string FormatForLlmContext()
    {
        var header = IsPartialContent
            ? $"// File: {FileName} (lines {StartLine}-{EndLine})"
            : $"// File: {FileName}";

        if (Language is not null)
            header += $" [{Language}]";

        return $"{header}\n{Content}";
    }

    private static int CountLines(string content)
        => string.IsNullOrEmpty(content) ? 0 : content.Count(c => c == '\n') + 1;

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes)[..16]; // First 16 chars
    }
}
