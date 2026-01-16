namespace AIntern.Core.Models;

/// <summary>
/// Result of a file search query.
/// </summary>
/// <remarks>Added in v0.3.5c.</remarks>
public sealed class FileSearchResult
{
    /// <summary>
    /// Full path to the file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// File name without path.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Path relative to workspace root.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Detected language (e.g., "csharp", "javascript").
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Match score (higher is better).
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Indices of matched characters in the file name.
    /// </summary>
    public IReadOnlyList<int> MatchedIndices { get; init; } = Array.Empty<int>();
}
