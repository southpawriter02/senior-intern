using AIntern.Core.Utilities;

namespace AIntern.Core.Models;

/// <summary>
/// Represents a file or directory in the file system.
/// </summary>
public sealed class FileSystemItem
{
    /// <summary>Absolute path to the item.</summary>
    public required string Path { get; init; }

    /// <summary>File or folder name.</summary>
    public required string Name { get; init; }

    /// <summary>Type of file system item.</summary>
    public required FileSystemItemType Type { get; init; }

    /// <summary>File size in bytes (null for directories).</summary>
    public long? Size { get; init; }

    /// <summary>Last modified timestamp.</summary>
    public DateTime ModifiedAt { get; init; }

    /// <summary>Last accessed timestamp.</summary>
    public DateTime AccessedAt { get; init; }

    /// <summary>Creation timestamp.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Whether the item is hidden (starts with . or has hidden attribute).</summary>
    public bool IsHidden { get; init; }

    /// <summary>Whether the item is read-only.</summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// For directories: whether the directory has any children.
    /// Used for lazy loading indicator in tree view.
    /// </summary>
    public bool HasChildren { get; init; }

    /// <summary>For directories: current expansion state in tree view.</summary>
    public bool IsExpanded { get; set; }

    /// <summary>For directories: loaded children (null if not loaded yet).</summary>
    public IReadOnlyList<FileSystemItem>? Children { get; set; }

    /// <summary>
    /// File extension including the dot (e.g., ".cs").
    /// Empty string for directories.
    /// </summary>
    public string Extension => Type == FileSystemItemType.File
        ? System.IO.Path.GetExtension(Path)
        : string.Empty;

    /// <summary>
    /// Detected programming language based on extension.
    /// Uses LanguageDetector (stub until v0.3.1c).
    /// </summary>
    public string? Language => Type == FileSystemItemType.File
        ? LanguageDetector.DetectByExtension(Extension)
        : null;

    /// <summary>Whether this is a directory.</summary>
    public bool IsDirectory => Type == FileSystemItemType.Directory;

    /// <summary>Whether this is a file.</summary>
    public bool IsFile => Type == FileSystemItemType.File;

    /// <summary>Human-readable file size (e.g., "1.2 KB").</summary>
    public string FormattedSize => Size.HasValue
        ? FormatFileSize(Size.Value)
        : string.Empty;

    /// <summary>Gets the parent directory path.</summary>
    public string? ParentPath => System.IO.Path.GetDirectoryName(Path);

    /// <summary>Creates a FileSystemItem from a FileInfo.</summary>
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
            IsReadOnly = info.IsReadOnly
        };
    }

    /// <summary>Creates a FileSystemItem from a DirectoryInfo.</summary>
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

        return suffixIndex == 0
            ? $"{size:F0} {suffixes[suffixIndex]}"
            : $"{size:F1} {suffixes[suffixIndex]}";
    }
}

/// <summary>Type of file system item.</summary>
public enum FileSystemItemType
{
    /// <summary>Regular file.</summary>
    File,

    /// <summary>Directory/folder.</summary>
    Directory,

    /// <summary>Symbolic link.</summary>
    SymbolicLink
}
