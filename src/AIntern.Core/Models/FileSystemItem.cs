namespace AIntern.Core.Models;

using AIntern.Core.Utilities;

/// <summary>
/// Represents a file or directory in the file system.
/// Used for file explorer tree views and file operations.
/// </summary>
/// <remarks>
/// <para>
/// FileSystemItem is a unified model for both files and directories,
/// with type-specific computed properties and factory methods.
/// </para>
/// <para>
/// Directory children are loaded lazily - the <see cref="HasChildren"/>
/// property indicates whether expansion is possible without loading all children.
/// </para>
/// </remarks>
public sealed class FileSystemItem
{
    /// <summary>
    /// Gets the absolute path to the item.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the file or folder name (without path).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the type of file system item.
    /// </summary>
    public required FileSystemItemType Type { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// Null for directories.
    /// </summary>
    public long? Size { get; init; }

    /// <summary>
    /// Gets the last modified timestamp (UTC).
    /// </summary>
    public DateTime ModifiedAt { get; init; }

    /// <summary>
    /// Gets the last accessed timestamp (UTC).
    /// </summary>
    public DateTime AccessedAt { get; init; }

    /// <summary>
    /// Gets the creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets whether the item is hidden.
    /// True if the name starts with '.' or has the hidden file attribute.
    /// </summary>
    public bool IsHidden { get; init; }

    /// <summary>
    /// Gets whether the item is read-only.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Gets whether the directory has any children.
    /// Used for lazy loading indicator in tree view (shows expand arrow).
    /// Always false for files.
    /// </summary>
    public bool HasChildren { get; init; }

    /// <summary>
    /// Gets or sets the current expansion state in tree view.
    /// Only meaningful for directories.
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Gets or sets the loaded children.
    /// Null if not yet loaded (lazy loading).
    /// Empty collection for directories with no children.
    /// </summary>
    public IReadOnlyList<FileSystemItem>? Children { get; set; }

    /// <summary>
    /// Gets the file extension including the leading dot (e.g., ".cs").
    /// Returns empty string for directories.
    /// </summary>
    public string Extension => Type == FileSystemItemType.File
        ? System.IO.Path.GetExtension(Path)
        : string.Empty;

    /// <summary>
    /// Gets the detected programming language based on file extension.
    /// Null for directories or unrecognized extensions.
    /// </summary>
    public string? Language => Type == FileSystemItemType.File
        ? LanguageDetector.DetectByExtension(Extension)
        : null;

    /// <summary>
    /// Gets whether this is a directory.
    /// </summary>
    public bool IsDirectory => Type == FileSystemItemType.Directory;

    /// <summary>
    /// Gets whether this is a file.
    /// </summary>
    public bool IsFile => Type == FileSystemItemType.File;

    /// <summary>
    /// Gets a human-readable file size (e.g., "1.2 KB").
    /// Empty string for directories.
    /// </summary>
    public string FormattedSize => Size.HasValue
        ? FormatFileSize(Size.Value)
        : string.Empty;

    /// <summary>
    /// Gets the parent directory path.
    /// Null for root items.
    /// </summary>
    public string? ParentPath => System.IO.Path.GetDirectoryName(Path);

    /// <summary>
    /// Creates a FileSystemItem from a <see cref="FileInfo"/>.
    /// </summary>
    /// <param name="info">File information to convert.</param>
    /// <returns>A new FileSystemItem representing the file.</returns>
    public static FileSystemItem FromFileInfo(FileInfo info)
    {
        return new FileSystemItem
        {
            Path = info.FullName,
            Name = info.Name,
            Type = FileSystemItemType.File,
            Size = info.Length,
            ModifiedAt = info.LastWriteTimeUtc,
            AccessedAt = info.LastAccessTimeUtc,
            CreatedAt = info.CreationTimeUtc,
            IsHidden = info.Name.StartsWith('.') || info.Attributes.HasFlag(FileAttributes.Hidden),
            IsReadOnly = info.IsReadOnly,
            HasChildren = false
        };
    }

    /// <summary>
    /// Creates a FileSystemItem from a <see cref="DirectoryInfo"/>.
    /// </summary>
    /// <param name="info">Directory information to convert.</param>
    /// <param name="hasChildren">Whether the directory has any children.</param>
    /// <returns>A new FileSystemItem representing the directory.</returns>
    public static FileSystemItem FromDirectoryInfo(DirectoryInfo info, bool hasChildren = false)
    {
        return new FileSystemItem
        {
            Path = info.FullName,
            Name = info.Name,
            Type = FileSystemItemType.Directory,
            Size = null,
            ModifiedAt = info.LastWriteTimeUtc,
            AccessedAt = info.LastAccessTimeUtc,
            CreatedAt = info.CreationTimeUtc,
            IsHidden = info.Name.StartsWith('.') || info.Attributes.HasFlag(FileAttributes.Hidden),
            IsReadOnly = info.Attributes.HasFlag(FileAttributes.ReadOnly),
            HasChildren = hasChildren
        };
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable string.
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        // Use F0 for bytes to avoid decimal, F1 for larger units
        return suffixIndex == 0
            ? $"{size:F0} {suffixes[suffixIndex]}"
            : $"{size:F1} {suffixes[suffixIndex]}";
    }
}

/// <summary>
/// Type of file system item.
/// </summary>
public enum FileSystemItemType
{
    /// <summary>
    /// Regular file.
    /// </summary>
    File,

    /// <summary>
    /// Directory/folder.
    /// </summary>
    Directory,

    /// <summary>
    /// Symbolic link.
    /// </summary>
    SymbolicLink
}
