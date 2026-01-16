namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TREE NODE SELECTION BEHAVIOR (v0.4.4d)                                   │
// │ Defines how selection state propagates between parent and child nodes.   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Defines how selection state propagates between parent and child nodes
/// in a hierarchical tree structure.
/// </summary>
/// <remarks>
/// Different use cases may require different selection behaviors:
/// <list type="bullet">
/// <item><see cref="Independent"/> - For file lists where each item is independent</item>
/// <item><see cref="CascadeToChildren"/> - For batch selection via parent</item>
/// <item><see cref="CascadeToParent"/> - For showing partial selection state</item>
/// <item><see cref="CascadeBoth"/> - For full hierarchical checkbox behavior</item>
/// </list>
/// </remarks>
public enum TreeNodeSelectionBehavior
{
    /// <summary>
    /// Each item's selection is independent of others.
    /// </summary>
    /// <remarks>
    /// Selecting or deselecting a parent has no effect on children.
    /// Child selection does not affect parent state.
    /// </remarks>
    Independent,

    /// <summary>
    /// Selecting a parent automatically selects all children.
    /// </summary>
    /// <remarks>
    /// When a parent is selected, all descendant files are selected.
    /// When a parent is deselected, all descendant files are deselected.
    /// Child selection does not affect parent state.
    /// </remarks>
    CascadeToChildren,

    /// <summary>
    /// Child selection updates parent state (None/Some/All).
    /// </summary>
    /// <remarks>
    /// Parent selection state reflects the aggregate of children.
    /// Selecting a parent does not affect children.
    /// </remarks>
    CascadeToParent,

    /// <summary>
    /// Selection cascades in both directions.
    /// </summary>
    /// <remarks>
    /// Combines <see cref="CascadeToChildren"/> and <see cref="CascadeToParent"/>.
    /// Parent changes propagate to children, and child changes update parent state.
    /// This provides full hierarchical checkbox behavior.
    /// </remarks>
    CascadeBoth
}
