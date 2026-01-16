namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TREE BUILDING OPTIONS (v0.4.4d)                                          │
// │ Configuration options for building hierarchical tree structures.         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Configuration options for building hierarchical tree structures
/// from flat file operation lists.
/// </summary>
/// <param name="SortDirectoriesFirst">Whether to sort directories before files. Default: true.</param>
/// <param name="ExpandByDefault">Whether directories are expanded by default. Default: true.</param>
/// <param name="HideCommonRootPath">Whether to hide the common root path. Default: false.</param>
/// <param name="SelectByDefault">Whether files are selected by default. Default: true.</param>
/// <param name="SortAlphabetically">Whether to sort items alphabetically. Default: true.</param>
public record TreeBuildingOptions(
    bool SortDirectoriesFirst = true,
    bool ExpandByDefault = true,
    bool HideCommonRootPath = false,
    bool SelectByDefault = true,
    bool SortAlphabetically = true)
{
    /// <summary>
    /// Default tree building options.
    /// </summary>
    public static readonly TreeBuildingOptions Default = new();
}
