namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ EDITOR REFRESH EVENT ARGS (v0.4.3i)                                      │
// │ Event arguments for editor refresh requests.                            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Event arguments for editor refresh requests.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3i.</para>
/// </remarks>
public sealed class EditorRefreshEventArgs : EventArgs
{
    /// <summary>Gets the absolute path to the file.</summary>
    public required string FilePath { get; init; }

    /// <summary>Gets the relative path from the workspace root.</summary>
    public string? RelativePath { get; init; }

    /// <summary>Gets the reason for the refresh.</summary>
    public required RefreshReason Reason { get; init; }

    /// <summary>Gets the new content of the file, if available.</summary>
    public string? NewContent { get; init; }

    /// <summary>Gets whether this was a user-initiated action.</summary>
    public bool IsUserInitiated { get; init; } = true;

    /// <summary>Gets the change ID for correlation with undo operations.</summary>
    public Guid? ChangeId { get; init; }
}
