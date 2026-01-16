namespace AIntern.Services;

using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TREE BUILDING SERVICE (v0.4.4d)                                          │
// │ Builds hierarchical tree structures from flat file operation lists.      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Default implementation of <see cref="ITreeBuildingService"/>.
/// Builds hierarchical tree structures from flat file operation lists.
/// </summary>
/// <remarks>
/// <para>
/// The service takes a flat list of file operations and organizes them into a
/// hierarchical tree structure suitable for display in tree views. It handles:
/// </para>
/// <list type="bullet">
/// <item>Grouping files by directory</item>
/// <item>Creating intermediate directory nodes</item>
/// <item>Sorting (directories first, then alphabetically)</item>
/// <item>Applying configuration options</item>
/// </list>
/// </remarks>
public sealed class TreeBuildingService : ITreeBuildingService
{
    private readonly ILogger<TreeBuildingService> _logger;

    /// <summary>
    /// Creates a new TreeBuildingService.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public TreeBuildingService(ILogger<TreeBuildingService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<TreeNode> BuildTree(
        FileTreeProposal proposal,
        TreeBuildingOptions? options = null)
    {
        _logger.LogDebug(
            "Building tree from proposal with {Count} operations",
            proposal.Operations.Count);

        return BuildTreeFromOperations(proposal.Operations, options);
    }

    /// <inheritdoc />
    public IReadOnlyList<TreeNode> BuildTreeFromOperations(
        IEnumerable<FileOperation> operations,
        TreeBuildingOptions? options = null)
    {
        options ??= TreeBuildingOptions.Default;

        var operationsList = operations.ToList();
        if (operationsList.Count == 0)
        {
            _logger.LogDebug("No operations to build tree from");
            return [];
        }

        _logger.LogDebug(
            "Building tree from {Count} operations with options: SortDirsFirst={SortDirs}, Expand={Expand}",
            operationsList.Count,
            options.SortDirectoriesFirst,
            options.ExpandByDefault);

        var result = new List<TreeNode>();
        var directoryNodes = new Dictionary<string, TreeNode>(
            StringComparer.OrdinalIgnoreCase);

        // Group operations by directory
        var grouped = operationsList
            .GroupBy(o => GetDirectoryPath(o.Path))
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            // Create directory nodes for the path
            var parent = EnsureDirectoryPath(
                group.Key,
                directoryNodes,
                result,
                options);

            // Add file nodes
            var fileItems = group
                .OrderBy(o => o.FileName)
                .Select(op => CreateFileNode(op, options));

            foreach (var fileNode in fileItems)
            {
                if (parent != null)
                {
                    parent.Children.Add(fileNode);
                }
                else
                {
                    result.Add(fileNode);
                }
            }
        }

        // Sort the result
        if (options.SortAlphabetically)
        {
            SortTree(result, options.SortDirectoriesFirst);
        }

        _logger.LogInformation(
            "Built tree with {RootCount} root items and {TotalDirs} directories",
            result.Count,
            directoryNodes.Count);

        return result.AsReadOnly();
    }

    /// <inheritdoc />
    public IEnumerable<TreeNode> FlattenTree(
        IEnumerable<TreeNode> items,
        bool includeDirectories = true)
    {
        foreach (var item in items)
        {
            if (includeDirectories || !item.IsDirectory)
            {
                yield return item;
            }

            foreach (var child in FlattenTree(item.Children, includeDirectories))
            {
                yield return child;
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<TreeNode> FindAll(
        IEnumerable<TreeNode> items,
        Func<TreeNode, bool> predicate)
    {
        return FlattenTree(items).Where(predicate);
    }

    /// <inheritdoc />
    public TreeNode? FindByPath(IEnumerable<TreeNode> items, string path)
    {
        return FlattenTree(items)
            .FirstOrDefault(n => n.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the directory path from a file path.
    /// </summary>
    private static string GetDirectoryPath(string filePath)
    {
        return Path.GetDirectoryName(filePath)?.Replace('\\', '/') ?? string.Empty;
    }

    /// <summary>
    /// Ensures all directory nodes exist for a path, creating them as needed.
    /// </summary>
    private TreeNode? EnsureDirectoryPath(
        string directoryPath,
        Dictionary<string, TreeNode> directoryNodes,
        List<TreeNode> rootItems,
        TreeBuildingOptions options)
    {
        if (string.IsNullOrEmpty(directoryPath))
            return null;

        // Normalize path separators
        directoryPath = directoryPath.Replace('\\', '/');

        // Check if already exists
        if (directoryNodes.TryGetValue(directoryPath, out var existingNode))
            return existingNode;

        // Split path into parts
        var parts = directoryPath.Split('/')
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        TreeNode? parent = null;

        // Create directory nodes for each path segment
        for (int i = 0; i < parts.Count; i++)
        {
            var partPath = string.Join("/", parts.Take(i + 1));

            if (!directoryNodes.TryGetValue(partPath, out var dirNode))
            {
                _logger.LogTrace("Creating directory node: {Path}", partPath);

                dirNode = new TreeNode
                {
                    Name = parts[i],
                    Path = partPath,
                    IsDirectory = true
                };

                directoryNodes[partPath] = dirNode;

                if (parent == null)
                {
                    rootItems.Add(dirNode);
                }
                else
                {
                    parent.Children.Add(dirNode);
                }
            }

            parent = dirNode;
        }

        return parent;
    }

    /// <summary>
    /// Creates a file node from a FileOperation.
    /// </summary>
    private static TreeNode CreateFileNode(
        FileOperation operation,
        TreeBuildingOptions options)
    {
        return new TreeNode
        {
            Name = operation.FileName,
            Path = operation.Path,
            IsDirectory = false,
            Operation = operation
        };
    }

    /// <summary>
    /// Recursively sorts a tree structure.
    /// </summary>
    private static void SortTree(List<TreeNode> items, bool directoriesFirst)
    {
        items.Sort((a, b) =>
        {
            if (directoriesFirst)
            {
                if (a.IsDirectory != b.IsDirectory)
                    return a.IsDirectory ? -1 : 1;
            }
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var item in items.Where(i => i.IsDirectory))
        {
            SortTree(item.Children, directoriesFirst);
        }
    }
}
