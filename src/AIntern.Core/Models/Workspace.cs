namespace AIntern.Core.Models;

/// <summary>
/// Represents an open project workspace (folder).
/// Tracks UI state such as open files, active file, and expanded folders.
/// </summary>
/// <remarks>
/// <para>
/// A workspace is the fundamental unit of file system context in the application.
/// It represents a project folder that the user has opened, along with all the
/// UI state needed to restore their session (open files, folder expansion, etc.).
/// </para>
/// <para>
/// Workspaces are persisted to the database as RecentWorkspaceEntity records,
/// allowing the application to restore the last workspace on startup.
/// </para>
/// </remarks>
public sealed class Workspace
{
    /// <summary>
    /// Gets the unique identifier for the workspace.
    /// Auto-generated on creation.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets an optional custom name for the workspace.
    /// If empty, <see cref="DisplayName"/> falls back to the folder name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the absolute path to the workspace root directory.
    /// </summary>
    /// <remarks>
    /// This is the canonical path used for all file operations within the workspace.
    /// It should be an absolute path that exists on the file system.
    /// </remarks>
    public required string RootPath { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the workspace was first opened.
    /// </summary>
    public DateTime OpenedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when the workspace was last accessed.
    /// Updated each time the workspace is opened or interacted with.
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the list of currently open file paths.
    /// Paths are relative to <see cref="RootPath"/>.
    /// </summary>
    /// <remarks>
    /// Used to restore open editor tabs when reopening the workspace.
    /// </remarks>
    public IReadOnlyList<string> OpenFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the currently active/focused file path.
    /// Path is relative to <see cref="RootPath"/>. Null if no file is active.
    /// </summary>
    /// <remarks>
    /// Used to restore the active editor tab when reopening the workspace.
    /// </remarks>
    public string? ActiveFilePath { get; set; }

    /// <summary>
    /// Gets or sets the list of expanded folder paths in the file explorer.
    /// Paths are relative to <see cref="RootPath"/>.
    /// </summary>
    /// <remarks>
    /// Used to restore the file explorer tree expansion state when reopening.
    /// </remarks>
    public IReadOnlyList<string> ExpandedFolders { get; set; } = [];

    /// <summary>
    /// Gets or sets whether this workspace is pinned in the recent workspaces list.
    /// Pinned workspaces are not removed when the recent list is trimmed.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Gets or sets the git ignore patterns loaded from .gitignore files in the workspace.
    /// Used for filtering the file explorer tree.
    /// </summary>
    public IReadOnlyList<string> GitIgnorePatterns { get; set; } = [];

    /// <summary>
    /// Gets the display name for UI rendering.
    /// Returns the custom <see cref="Name"/> if set, otherwise the folder name from <see cref="RootPath"/>.
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Name)
        ? Path.GetFileName(RootPath) ?? RootPath
        : Name;

    /// <summary>
    /// Gets whether the workspace root directory exists on the file system.
    /// </summary>
    public bool Exists => Directory.Exists(RootPath);

    /// <summary>
    /// Converts a relative path to an absolute path within this workspace.
    /// </summary>
    /// <param name="relativePath">Path relative to the workspace root.</param>
    /// <returns>Absolute path combining RootPath and relativePath.</returns>
    public string GetAbsolutePath(string relativePath)
        => Path.GetFullPath(Path.Combine(RootPath, relativePath));

    /// <summary>
    /// Converts an absolute path to a relative path within this workspace.
    /// </summary>
    /// <param name="absolutePath">Absolute file path.</param>
    /// <returns>Path relative to the workspace root.</returns>
    public string GetRelativePath(string absolutePath)
        => Path.GetRelativePath(RootPath, absolutePath);

    /// <summary>
    /// Checks if an absolute path is within this workspace.
    /// </summary>
    /// <param name="absolutePath">Absolute path to check.</param>
    /// <returns>True if the path is within the workspace root.</returns>
    public bool ContainsPath(string absolutePath)
    {
        // Normalize paths for comparison
        var normalizedRoot = Path.GetFullPath(RootPath).TrimEnd(Path.DirectorySeparatorChar);
        var normalizedPath = Path.GetFullPath(absolutePath);

        return normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
               || normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Updates the <see cref="LastAccessedAt"/> timestamp to the current UTC time.
    /// Call this when the user interacts with the workspace.
    /// </summary>
    public void Touch() => LastAccessedAt = DateTime.UtcNow;
}
