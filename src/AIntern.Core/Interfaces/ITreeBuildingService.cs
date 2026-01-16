namespace AIntern.Core.Interfaces;

using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TREE BUILDING SERVICE INTERFACE (v0.4.4d)                                │
// │ Service for building hierarchical tree structures from flat operations.  │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for building hierarchical tree structures from flat file operation lists.
/// </summary>
/// <remarks>
/// Converts a flat list of <see cref="FileOperation"/> objects into a hierarchical
/// tree structure suitable for display in tree views. The service handles:
/// <list type="bullet">
/// <item>Grouping files by directory</item>
/// <item>Creating intermediate directory nodes</item>
/// <item>Sorting (directories first, then alphabetically)</item>
/// <item>Applying configuration options</item>
/// </list>
/// </remarks>
public interface ITreeBuildingService
{
    /// <summary>
    /// Builds a tree structure from a FileTreeProposal.
    /// </summary>
    /// <param name="proposal">The proposal containing file operations.</param>
    /// <param name="options">Optional tree building configuration.</param>
    /// <returns>A list of root-level tree items.</returns>
    IReadOnlyList<TreeNode> BuildTree(
        FileTreeProposal proposal,
        TreeBuildingOptions? options = null);

    /// <summary>
    /// Builds a tree structure from a collection of file operations.
    /// </summary>
    /// <param name="operations">The file operations to organize.</param>
    /// <param name="options">Optional tree building configuration.</param>
    /// <returns>A list of root-level tree items.</returns>
    IReadOnlyList<TreeNode> BuildTreeFromOperations(
        IEnumerable<FileOperation> operations,
        TreeBuildingOptions? options = null);

    /// <summary>
    /// Flattens a tree structure into a sequential enumeration.
    /// </summary>
    /// <param name="items">The root items to flatten.</param>
    /// <param name="includeDirectories">Whether to include directory nodes.</param>
    /// <returns>A flat enumeration of all items.</returns>
    IEnumerable<TreeNode> FlattenTree(
        IEnumerable<TreeNode> items,
        bool includeDirectories = true);

    /// <summary>
    /// Finds all items matching a predicate.
    /// </summary>
    /// <param name="items">The root items to search.</param>
    /// <param name="predicate">The matching condition.</param>
    /// <returns>All matching items.</returns>
    IEnumerable<TreeNode> FindAll(
        IEnumerable<TreeNode> items,
        Func<TreeNode, bool> predicate);

    /// <summary>
    /// Finds a single item by path.
    /// </summary>
    /// <param name="items">The root items to search.</param>
    /// <param name="path">The path to find.</param>
    /// <returns>The matching item, or null if not found.</returns>
    TreeNode? FindByPath(IEnumerable<TreeNode> items, string path);
}

/// <summary>
/// Represents a node in a file tree structure.
/// </summary>
/// <remarks>
/// This is a simple data model used by <see cref="ITreeBuildingService"/>.
/// ViewModels should wrap this in their own observable types.
/// </remarks>
public sealed class TreeNode
{
    /// <summary>
    /// Display name of the item (file or directory name without path).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Relative path from the workspace root.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Whether this item represents a directory.
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>
    /// The underlying FileOperation for file items. Null for directory items.
    /// </summary>
    public FileOperation? Operation { get; init; }

    /// <summary>
    /// Child nodes (files and subdirectories).
    /// </summary>
    public List<TreeNode> Children { get; init; } = [];

    /// <summary>
    /// Whether this item is a file (not a directory).
    /// </summary>
    public bool IsFile => !IsDirectory;

    /// <summary>
    /// Whether this item has child items.
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Depth level in the tree (0 = root).
    /// </summary>
    public int Depth => Path.Count(c => c == '/' || c == '\\');

    /// <summary>
    /// File extension without the dot.
    /// </summary>
    public string Extension => System.IO.Path.GetExtension(Path).TrimStart('.');
}
