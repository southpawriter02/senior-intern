namespace AIntern.Core.Interfaces;

using AIntern.Core.Models;

/// <summary>
/// Service for indexing and searching workspace files.
/// </summary>
/// <remarks>Added in v0.3.5c.</remarks>
public interface IFileIndexService
{
    /// <summary>
    /// Indexes all files in the current workspace.
    /// </summary>
    /// <param name="workspacePath">Root path of the workspace.</param>
    /// <param name="ct">Cancellation token.</param>
    Task IndexWorkspaceAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Searches files using fuzzy matching.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="maxResults">Maximum results to return.</param>
    /// <returns>List of matching files ordered by score.</returns>
    IReadOnlyList<FileSearchResult> Search(string query, int maxResults = 20);

    /// <summary>
    /// Gets recently opened files.
    /// </summary>
    /// <param name="count">Maximum count.</param>
    /// <returns>List of recent file paths.</returns>
    IReadOnlyList<string> GetRecentFiles(int count = 10);

    /// <summary>
    /// Adds a file to the recent files list.
    /// </summary>
    /// <param name="filePath">Full path of the file.</param>
    void AddToRecent(string filePath);

    /// <summary>
    /// Clears the file index.
    /// </summary>
    void ClearIndex();

    /// <summary>
    /// Whether the index is ready.
    /// </summary>
    bool IsIndexed { get; }

    /// <summary>
    /// Number of indexed files.
    /// </summary>
    int IndexedFileCount { get; }
}
