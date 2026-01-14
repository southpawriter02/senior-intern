namespace AIntern.Services;

using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// File system service providing workspace-aware file operations.
/// </summary>
/// <remarks>
/// <para>
/// Implements debounced file watching, binary detection, and .gitignore support.
/// </para>
/// <para>Added in v0.3.1d.</para>
/// </remarks>
public sealed class FileSystemService : IFileSystemService
{
    private readonly ILogger<FileSystemService> _logger;

    #region Constants

    /// <summary>Binary file signatures for detection.</summary>
    private static readonly byte[][] BinarySignatures =
    [
        [0x89, 0x50, 0x4E, 0x47],       // PNG
        [0xFF, 0xD8, 0xFF],             // JPEG
        [0x47, 0x49, 0x46, 0x38],       // GIF
        [0x25, 0x50, 0x44, 0x46],       // PDF
        [0x50, 0x4B, 0x03, 0x04],       // ZIP/DOCX/XLSX
        [0x7F, 0x45, 0x4C, 0x46],       // ELF
        [0x4D, 0x5A],                   // Windows EXE/DLL
        [0x52, 0x61, 0x72, 0x21],       // RAR
        [0x1F, 0x8B],                   // GZIP
        [0x42, 0x5A, 0x68],             // BZIP2
    ];

    /// <summary>Extensions that are always considered text files.</summary>
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".markdown", ".rst", ".adoc",
        ".cs", ".csx", ".vb", ".fs", ".fsx",
        ".js", ".mjs", ".cjs", ".jsx", ".ts", ".mts", ".cts", ".tsx",
        ".py", ".pyi", ".pyw", ".rb", ".rake", ".go", ".rs", ".swift",
        ".java", ".kt", ".kts", ".scala", ".groovy", ".gradle",
        ".c", ".h", ".cpp", ".hpp", ".cc", ".cxx",
        ".json", ".jsonc", ".xml", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".conf",
        ".html", ".htm", ".css", ".scss", ".sass", ".less", ".vue", ".svelte",
        ".sql", ".graphql", ".gql", ".proto",
        ".sh", ".bash", ".zsh", ".fish", ".ps1", ".psm1", ".bat", ".cmd",
        ".dockerfile", ".gitignore", ".dockerignore", ".editorconfig",
        ".env", ".properties", ".config",
        ".csproj", ".fsproj", ".vbproj", ".sln", ".props", ".targets",
        ".axaml", ".xaml", ".tex", ".bib", ".log", ".csv", ".tsv"
    };

    /// <summary>Default patterns to always ignore.</summary>
    private static readonly string[] DefaultIgnorePatterns =
    [
        ".git/",
        ".vs/",
        ".vscode/",
        ".idea/",
        "node_modules/",
        "bin/",
        "obj/",
        "packages/",
        "*.user",
        "*.suo",
        ".DS_Store",
        "Thumbs.db",
        "*.swp",
        "*~"
    ];

    /// <summary>Debounce window in milliseconds.</summary>
    private const int DebounceMs = 200;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the FileSystemService.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FileSystemService(ILogger<FileSystemService> logger)
    {
        _logger = logger;
        _logger.LogDebug("[INIT] FileSystemService created");
    }

    #endregion

    #region Directory Operations

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FileSystemItem>> GetDirectoryContentsAsync(
        string path,
        bool includeHidden = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTRY] GetDirectoryContentsAsync - Path: {Path}, IncludeHidden: {IncludeHidden}", path, includeHidden);

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        var items = await Task.Run(() =>
        {
            var results = new List<FileSystemItem>();
            var dirInfo = new DirectoryInfo(path);

            // Get directories
            foreach (var dir in dirInfo.EnumerateDirectories())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!includeHidden && IsHidden(dir)) continue;
                results.Add(FileSystemItem.FromDirectoryInfo(dir, HasChildren(dir)));
            }

            // Get files
            foreach (var file in dirInfo.EnumerateFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!includeHidden && IsHidden(file)) continue;
                results.Add(FileSystemItem.FromFileInfo(file));
            }

            // Sort: directories first, then alphabetically
            return results
                .OrderByDescending(i => i.IsDirectory)
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }, cancellationToken);

        _logger.LogDebug("[EXIT] GetDirectoryContentsAsync - Found {Count} items in {Elapsed}ms", items.Count, sw.ElapsedMilliseconds);
        return items;
    }

    /// <inheritdoc/>
    public Task<FileSystemItem> GetItemInfoAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _logger.LogDebug("[ENTRY] GetItemInfoAsync - Path: {Path}", path);

        return Task.Run(() =>
        {
            if (Directory.Exists(path))
            {
                var dirInfo = new DirectoryInfo(path);
                return FileSystemItem.FromDirectoryInfo(dirInfo, HasChildren(dirInfo));
            }

            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                return FileSystemItem.FromFileInfo(fileInfo);
            }

            throw new FileNotFoundException($"Path not found: {path}");
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<FileSystemItem> CreateDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _logger.LogInformation("[ACTION] CreateDirectoryAsync - Path: {Path}", path);

        return Task.Run(() =>
        {
            var dirInfo = Directory.CreateDirectory(path);
            return FileSystemItem.FromDirectoryInfo(dirInfo, false);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task DeleteDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _logger.LogWarning("[ACTION] DeleteDirectoryAsync - Recursive delete: {Path}", path);

        return Task.Run(() =>
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            Directory.Delete(path, recursive: true);
        }, cancellationToken);
    }

    #endregion

    #region File Operations

    /// <inheritdoc/>
    public async Task<string> ReadFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _logger.LogDebug("[ENTRY] ReadFileAsync - Path: {Path}", path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        return await File.ReadAllTextAsync(path, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<byte[]> ReadFileBytesAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _logger.LogDebug("[ENTRY] ReadFileBytesAsync - Path: {Path}", path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        return await File.ReadAllBytesAsync(path, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task WriteFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _logger.LogInformation("[ACTION] WriteFileAsync - Path: {Path}, ContentLength: {Length}", path, content.Length);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<FileSystemItem> CreateFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _logger.LogInformation("[ACTION] CreateFileAsync - Path: {Path}", path);

        return Task.Run(() =>
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Create empty file
            using (File.Create(path)) { }

            return FileSystemItem.FromFileInfo(new FileInfo(path));
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task DeleteFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _logger.LogWarning("[ACTION] DeleteFileAsync - Path: {Path}", path);

        return Task.Run(() =>
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");

            File.Delete(path);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<FileSystemItem> RenameAsync(
        string path,
        string newName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        _logger.LogInformation("[ACTION] RenameAsync - Path: {Path}, NewName: {NewName}", path, newName);

        return Task.Run(() =>
        {
            var directory = Path.GetDirectoryName(path)!;
            var newPath = Path.Combine(directory, newName);

            if (Directory.Exists(path))
            {
                Directory.Move(path, newPath);
                return FileSystemItem.FromDirectoryInfo(new DirectoryInfo(newPath), HasChildren(new DirectoryInfo(newPath)));
            }

            if (File.Exists(path))
            {
                File.Move(path, newPath);
                return FileSystemItem.FromFileInfo(new FileInfo(newPath));
            }

            throw new FileNotFoundException($"Path not found: {path}");
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<FileSystemItem> CopyFileAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        _logger.LogInformation("[ACTION] CopyFileAsync - Source: {Source}, Dest: {Dest}, Overwrite: {Overwrite}",
            sourcePath, destinationPath, overwrite);

        return Task.Run(() =>
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException($"Source file not found: {sourcePath}");

            // Ensure destination directory exists
            var destDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);

            File.Copy(sourcePath, destinationPath, overwrite);
            return FileSystemItem.FromFileInfo(new FileInfo(destinationPath));
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<FileSystemItem> MoveAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        _logger.LogInformation("[ACTION] MoveAsync - Source: {Source}, Dest: {Dest}", sourcePath, destinationPath);

        return Task.Run(() =>
        {
            // Ensure destination directory exists
            var destDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);

            if (Directory.Exists(sourcePath))
            {
                Directory.Move(sourcePath, destinationPath);
                return FileSystemItem.FromDirectoryInfo(new DirectoryInfo(destinationPath), HasChildren(new DirectoryInfo(destinationPath)));
            }

            if (File.Exists(sourcePath))
            {
                File.Move(sourcePath, destinationPath);
                return FileSystemItem.FromFileInfo(new FileInfo(destinationPath));
            }

            throw new FileNotFoundException($"Source not found: {sourcePath}");
        }, cancellationToken);
    }

    #endregion

    #region Existence Checks

    /// <inheritdoc/>
    public Task<bool> FileExistsAsync(string path)
        => Task.FromResult(File.Exists(path));

    /// <inheritdoc/>
    public Task<bool> DirectoryExistsAsync(string path)
        => Task.FromResult(Directory.Exists(path));

    #endregion

    #region File Watching

    /// <inheritdoc/>
    public IDisposable WatchDirectory(
        string path,
        Action<FileSystemChangeEvent> onChange,
        bool includeSubdirectories = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(onChange);

        _logger.LogInformation("[ACTION] WatchDirectory - Path: {Path}, IncludeSubdirs: {IncludeSubdirs}",
            path, includeSubdirectories);

        var watcher = new FileSystemWatcher(path)
        {
            IncludeSubdirectories = includeSubdirectories,
            NotifyFilter = NotifyFilters.FileName
                         | NotifyFilters.DirectoryName
                         | NotifyFilters.LastWrite
                         | NotifyFilters.Size
        };

        // Debounce timer to batch rapid changes
        var debounceTimer = new System.Timers.Timer(DebounceMs) { AutoReset = false };
        var pendingEvents = new Dictionary<string, FileSystemChangeEvent>();
        var lockObj = new object();

        debounceTimer.Elapsed += (s, e) =>
        {
            List<FileSystemChangeEvent> events;
            lock (lockObj)
            {
                events = pendingEvents.Values.ToList();
                pendingEvents.Clear();
            }

            foreach (var evt in events)
            {
                try
                {
                    onChange(evt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in file watcher callback for {Path}", evt.Path);
                }
            }
        };

        void QueueEvent(FileSystemChangeEvent evt)
        {
            lock (lockObj)
            {
                pendingEvents[evt.Path] = evt;
            }
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        watcher.Created += (s, e) => QueueEvent(new FileSystemChangeEvent
        {
            Path = e.FullPath,
            ChangeType = FileSystemChangeType.Created,
            IsDirectory = Directory.Exists(e.FullPath)
        });

        watcher.Deleted += (s, e) => QueueEvent(new FileSystemChangeEvent
        {
            Path = e.FullPath,
            ChangeType = FileSystemChangeType.Deleted
        });

        watcher.Changed += (s, e) => QueueEvent(new FileSystemChangeEvent
        {
            Path = e.FullPath,
            ChangeType = FileSystemChangeType.Modified,
            IsDirectory = Directory.Exists(e.FullPath)
        });

        watcher.Renamed += (s, e) => QueueEvent(new FileSystemChangeEvent
        {
            Path = e.FullPath,
            OldPath = e.OldFullPath,
            ChangeType = FileSystemChangeType.Renamed,
            IsDirectory = Directory.Exists(e.FullPath)
        });

        watcher.Error += (s, e) =>
            _logger.LogError(e.GetException(), "File watcher error for {Path}", path);

        watcher.EnableRaisingEvents = true;
        _logger.LogDebug("Started watching directory: {Path}", path);

        return new FileWatcherDisposable(watcher, debounceTimer, () =>
            _logger.LogDebug("Stopped watching directory: {Path}", path));
    }

    /// <summary>
    /// Disposable wrapper for FileSystemWatcher and timer cleanup.
    /// </summary>
    private sealed class FileWatcherDisposable : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly System.Timers.Timer _timer;
        private readonly Action _onDispose;
        private bool _disposed;

        public FileWatcherDisposable(FileSystemWatcher watcher, System.Timers.Timer timer, Action onDispose)
        {
            _watcher = watcher;
            _timer = timer;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _watcher.EnableRaisingEvents = false;
            _timer.Stop();
            _watcher.Dispose();
            _timer.Dispose();
            _onDispose();
        }
    }

    #endregion

    #region Utilities

    /// <inheritdoc/>
    public string GetRelativePath(string fullPath, string basePath)
        => Path.GetRelativePath(basePath, fullPath);

    /// <inheritdoc/>
    public bool IsTextFile(string path)
    {
        if (!File.Exists(path)) return false;

        // Check extension first (fast path)
        var extension = Path.GetExtension(path);
        if (!string.IsNullOrEmpty(extension) && TextExtensions.Contains(extension))
            return true;

        // Check file header for binary signatures
        try
        {
            using var stream = File.OpenRead(path);
            var buffer = new byte[8];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead == 0) return true; // Empty file is text

            foreach (var signature in BinarySignatures)
            {
                if (bytesRead >= signature.Length &&
                    buffer.Take(signature.Length).SequenceEqual(signature))
                    return false; // Binary signature found
            }

            // Check for null bytes in first 8KB (common in binary files)
            stream.Position = 0;
            var checkBuffer = new byte[Math.Min(8192, stream.Length)];
            bytesRead = stream.Read(checkBuffer, 0, checkBuffer.Length);

            return !checkBuffer.Take(bytesRead).Contains((byte)0);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking if file is text: {Path}", path);
            return false;
        }
    }

    /// <inheritdoc/>
    public long GetFileSize(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        return new FileInfo(path).Length;
    }

    /// <inheritdoc/>
    public async Task<int> GetLineCountAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        var lineCount = 0;
        await foreach (var _ in File.ReadLinesAsync(path, cancellationToken))
        {
            lineCount++;
        }

        return lineCount;
    }

    #endregion

    #region Ignore Patterns

    /// <inheritdoc/>
    public bool ShouldIgnore(string path, string basePath, IReadOnlyList<string> ignorePatterns)
    {
        if (ignorePatterns.Count == 0) return false;

        var relativePath = GetRelativePath(path, basePath);
        var isDirectory = Directory.Exists(path);

        // Normalize path separators
        relativePath = relativePath.Replace('\\', '/');
        if (isDirectory && !relativePath.EndsWith('/'))
            relativePath += '/';

        // Track last match result (gitignore uses last-match-wins)
        bool? shouldIgnore = null;

        foreach (var pattern in ignorePatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern) || pattern.TrimStart().StartsWith('#'))
                continue;

            var trimmed = pattern.Trim();
            var isNegation = trimmed.StartsWith('!');
            if (isNegation) trimmed = trimmed[1..];

            var regex = ConvertGitIgnorePatternToRegex(trimmed);
            if (regex.IsMatch(relativePath))
                shouldIgnore = !isNegation;
        }

        return shouldIgnore ?? false;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> LoadGitIgnorePatternsAsync(
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        var patterns = new List<string>();

        // Load root .gitignore
        var gitignorePath = Path.Combine(workspacePath, ".gitignore");
        if (File.Exists(gitignorePath))
        {
            _logger.LogDebug("Loading .gitignore from: {Path}", gitignorePath);
            var lines = await File.ReadAllLinesAsync(gitignorePath, cancellationToken);
            patterns.AddRange(lines);
        }

        // Add default patterns
        patterns.AddRange(DefaultIgnorePatterns);

        return patterns.Distinct().ToList();
    }

    /// <summary>
    /// Converts a gitignore pattern to a compiled regex.
    /// </summary>
    private static Regex ConvertGitIgnorePatternToRegex(string pattern)
    {
        var regexPattern = new StringBuilder("^");

        var isDirectoryOnly = pattern.EndsWith('/');
        if (isDirectoryOnly) pattern = pattern[..^1];

        var hasLeadingSlash = pattern.StartsWith('/');
        if (hasLeadingSlash) pattern = pattern[1..];

        // Escape regex special characters except * and ?
        pattern = Regex.Escape(pattern)
            .Replace("\\*\\*", ".*")     // ** matches everything
            .Replace("\\*", "[^/]*")     // * matches except /
            .Replace("\\?", "[^/]");     // ? matches single char

        if (!hasLeadingSlash && !pattern.Contains('/'))
            regexPattern.Append("(.*/)?" ).Append(pattern);
        else
            regexPattern.Append(pattern);

        regexPattern.Append(isDirectoryOnly ? "/" : "(/|$)");

        return new Regex(regexPattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Checks if a file system item is hidden.
    /// </summary>
    private static bool IsHidden(FileSystemInfo info)
    {
        if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            return true;

        // Unix-style hidden files (start with .)
        return info.Name.StartsWith('.');
    }

    /// <summary>
    /// Checks if a directory has any children.
    /// </summary>
    private static bool HasChildren(DirectoryInfo dirInfo)
    {
        try
        {
            return dirInfo.EnumerateFileSystemInfos().Any();
        }
        catch
        {
            // Access denied or other error
            return false;
        }
    }

    #endregion
}
