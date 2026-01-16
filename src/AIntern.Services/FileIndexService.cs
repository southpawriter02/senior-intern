namespace AIntern.Services;

using System.Collections.Concurrent;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Core.Utilities;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for indexing workspace files and performing fuzzy search.
/// </summary>
/// <remarks>Added in v0.3.5c.</remarks>
public sealed class FileIndexService : IFileIndexService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<FileIndexService> _logger;
    private readonly ConcurrentDictionary<string, IndexedFile> _index = new();
    private readonly LinkedList<string> _recentFiles = new();
    private readonly object _recentLock = new();
    private const int MaxRecentFiles = 20;

    private string? _workspacePath;
    private bool _isIndexed;

    /// <inheritdoc />
    public bool IsIndexed => _isIndexed;

    /// <inheritdoc />
    public int IndexedFileCount => _index.Count;

    /// <summary>
    /// Creates a new file index service.
    /// </summary>
    /// <param name="fileSystemService">File system service for ignore patterns.</param>
    /// <param name="logger">Logger instance.</param>
    public FileIndexService(IFileSystemService fileSystemService, ILogger<FileIndexService> logger)
    {
        _fileSystemService = fileSystemService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task IndexWorkspaceAsync(string workspacePath, CancellationToken ct = default)
    {
        _logger.LogDebug("[ENTER] IndexWorkspaceAsync: {Path}", workspacePath);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        _workspacePath = workspacePath;
        _index.Clear();
        _isIndexed = false;

        var ignorePatterns = await _fileSystemService.LoadGitIgnorePatternsAsync(workspacePath);

        await Task.Run(() =>
        {
            IndexDirectory(workspacePath, workspacePath, ignorePatterns, ct);
        }, ct);

        _isIndexed = true;
        sw.Stop();
        _logger.LogInformation("[INDEX] Indexed {Count} files in {Ms}ms", _index.Count, sw.ElapsedMilliseconds);
    }

    private void IndexDirectory(
        string directory,
        string rootPath,
        IReadOnlyList<string> ignorePatterns,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                if (ct.IsCancellationRequested) return;

                var relativePath = Path.GetRelativePath(rootPath, file);

                if (_fileSystemService.ShouldIgnore(relativePath, rootPath, ignorePatterns))
                    continue;

                var fileName = Path.GetFileName(file);
                var language = LanguageDetector.DetectByFileName(fileName);

                _index[file] = new IndexedFile
                {
                    FullPath = file,
                    FileName = fileName,
                    FileNameLower = fileName.ToLowerInvariant(),
                    RelativePath = relativePath,
                    RelativePathLower = relativePath.ToLowerInvariant(),
                    Language = language
                };
            }

            foreach (var subDir in Directory.EnumerateDirectories(directory))
            {
                if (ct.IsCancellationRequested) return;

                var dirName = Path.GetFileName(subDir);
                var relativePath = Path.GetRelativePath(rootPath, subDir);

                if (_fileSystemService.ShouldIgnore(relativePath + "/", rootPath, ignorePatterns))
                    continue;

                IndexDirectory(subDir, rootPath, ignorePatterns, ct);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<FileSearchResult> Search(string query, int maxResults = 20)
    {
        _logger.LogDebug("[SEARCH] Query: '{Query}', MaxResults: {Max}", query, maxResults);

        if (string.IsNullOrWhiteSpace(query))
        {
            // Return recent files when no query
            return GetRecentFiles(maxResults)
                .Where(f => _index.ContainsKey(f))
                .Select(f => new FileSearchResult
                {
                    FilePath = f,
                    FileName = _index[f].FileName,
                    RelativePath = _index[f].RelativePath,
                    Language = _index[f].Language,
                    Score = 1.0
                })
                .ToList();
        }

        var queryLower = query.ToLowerInvariant();

        return _index.Values
            .Select(file => ScoreFile(file, queryLower))
            .Where(r => r.Score > 0)
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.FileName.Length)
            .Take(maxResults)
            .ToList();
    }

    private FileSearchResult ScoreFile(IndexedFile file, string query)
    {
        // Score based on file name match (weighted higher)
        var nameScore = FuzzyMatch(file.FileNameLower, query, out var nameIndices);

        // Score based on path match
        var pathScore = FuzzyMatch(file.RelativePathLower, query, out var pathIndices);

        // Combine scores with name weighted higher
        var score = nameScore * 2 + pathScore;

        // Boost exact prefix matches
        if (file.FileNameLower.StartsWith(query))
            score += 5;

        // Boost if query appears as word boundary
        if (file.FileNameLower.Contains("_" + query) || file.FileNameLower.Contains("-" + query))
            score += 2;

        return new FileSearchResult
        {
            FilePath = file.FullPath,
            FileName = file.FileName,
            RelativePath = file.RelativePath,
            Language = file.Language,
            Score = score,
            MatchedIndices = nameScore > pathScore ? nameIndices : pathIndices
        };
    }

    private static double FuzzyMatch(string text, string query, out List<int> matchedIndices)
    {
        matchedIndices = new List<int>();

        if (string.IsNullOrEmpty(query)) return 0;

        var textIndex = 0;
        var queryIndex = 0;
        var score = 0.0;
        var consecutiveBonus = 0;

        while (textIndex < text.Length && queryIndex < query.Length)
        {
            if (text[textIndex] == query[queryIndex])
            {
                matchedIndices.Add(textIndex);
                score += 1 + (consecutiveBonus * 0.5);
                consecutiveBonus++;
                queryIndex++;
            }
            else
            {
                consecutiveBonus = 0;
            }
            textIndex++;
        }

        // All query characters must match
        if (queryIndex < query.Length)
        {
            matchedIndices.Clear();
            return 0;
        }

        // Normalize by query length
        return score / query.Length;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRecentFiles(int count = 10)
    {
        lock (_recentLock)
        {
            return _recentFiles.Take(count).ToList();
        }
    }

    /// <inheritdoc />
    public void AddToRecent(string filePath)
    {
        _logger.LogDebug("[RECENT] Adding: {Path}", filePath);

        lock (_recentLock)
        {
            // Remove if already exists
            var node = _recentFiles.Find(filePath);
            if (node != null)
                _recentFiles.Remove(node);

            // Add to front
            _recentFiles.AddFirst(filePath);

            // Trim if too long
            while (_recentFiles.Count > MaxRecentFiles)
                _recentFiles.RemoveLast();
        }
    }

    /// <inheritdoc />
    public void ClearIndex()
    {
        _logger.LogDebug("[CLEAR] Clearing index");
        _index.Clear();
        _isIndexed = false;
    }

    private sealed class IndexedFile
    {
        public string FullPath { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public string FileNameLower { get; init; } = string.Empty;
        public string RelativePath { get; init; } = string.Empty;
        public string RelativePathLower { get; init; } = string.Empty;
        public string? Language { get; init; }
    }
}
