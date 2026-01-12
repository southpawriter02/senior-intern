namespace AIntern.Core.Models;

/// <summary>
/// Represents an open project workspace (folder).
/// </summary>
public sealed class Workspace
{
    /// <summary>Unique identifier for the workspace.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Optional custom name for the workspace.
    /// If empty, DisplayName falls back to the folder name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Absolute path to the workspace root directory.</summary>
    public required string RootPath { get; init; }

    /// <summary>When the workspace was first opened.</summary>
    public DateTime OpenedAt { get; init; } = DateTime.UtcNow;

    /// <summary>When the workspace was last accessed.</summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>List of currently open file paths (relative to RootPath).</summary>
    public IReadOnlyList<string> OpenFiles { get; set; } = [];

    /// <summary>Currently active/focused file path (relative to RootPath).</summary>
    public string? ActiveFilePath { get; set; }

    /// <summary>
    /// List of expanded folder paths in the explorer (relative to RootPath).
    /// Used to restore tree state.
    /// </summary>
    public IReadOnlyList<string> ExpandedFolders { get; set; } = [];

    /// <summary>Whether this workspace is pinned in the recent list.</summary>
    public bool IsPinned { get; set; }

    /// <summary>Git ignore patterns loaded from .gitignore files in the workspace.</summary>
    public IReadOnlyList<string> GitIgnorePatterns { get; set; } = [];

    /// <summary>Display name for UI (custom name or folder name).</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Name)
        ? Path.GetFileName(RootPath) ?? RootPath
        : Name;

    /// <summary>Whether the workspace root directory exists.</summary>
    public bool Exists => Directory.Exists(RootPath);

    /// <summary>Gets the absolute path for a relative file path.</summary>
    public string GetAbsolutePath(string relativePath)
        => Path.GetFullPath(Path.Combine(RootPath, relativePath));

    /// <summary>Gets the relative path for an absolute file path.</summary>
    public string GetRelativePath(string absolutePath)
        => Path.GetRelativePath(RootPath, absolutePath);

    /// <summary>Checks if a path is within this workspace.</summary>
    public bool ContainsPath(string absolutePath)
        => absolutePath.StartsWith(RootPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>Updates the last accessed timestamp to now.</summary>
    public void Touch() => LastAccessedAt = DateTime.UtcNow;
}
