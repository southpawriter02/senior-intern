namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SELECTION STATE (v0.4.4d)                                                │
// │ Represents the selection state of a directory based on its children.     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Represents the aggregate selection state of a directory node
/// based on its descendant file items.
/// </summary>
/// <remarks>
/// Used to implement tri-state checkboxes in tree views where a directory's
/// checkbox state reflects whether none, some, or all of its child files
/// are selected.
/// </remarks>
public enum SelectionState
{
    /// <summary>
    /// No children are selected.
    /// </summary>
    /// <remarks>
    /// Displayed as an unchecked checkbox.
    /// </remarks>
    None,

    /// <summary>
    /// Some (but not all) children are selected.
    /// </summary>
    /// <remarks>
    /// Displayed as an indeterminate/partial checkbox.
    /// </remarks>
    Some,

    /// <summary>
    /// All children are selected.
    /// </summary>
    /// <remarks>
    /// Displayed as a checked checkbox.
    /// </remarks>
    All
}
