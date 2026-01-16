namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ REFRESH REASON (v0.4.3i)                                                 │
// │ Specifies the reason for an editor refresh request.                     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Specifies the reason for an editor refresh request.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3i.</para>
/// </remarks>
public enum RefreshReason
{
    /// <summary>File was modified through the apply workflow.</summary>
    FileModified = 0,

    /// <summary>A previous change was undone.</summary>
    Undone = 1,

    /// <summary>File was modified by an external process.</summary>
    ExternalChange = 2,

    /// <summary>File was created as a new file.</summary>
    FileCreated = 3,

    /// <summary>File was deleted.</summary>
    FileDeleted = 4
}
