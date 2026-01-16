using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ EDITOR REFRESH SERVICE INTERFACE (v0.4.3i)                               │
// │ Coordinates editor refresh when files change.                           │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Coordinates editor refresh when files change.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3i.</para>
/// </remarks>
public interface IEditorRefreshService : IDisposable
{
    /// <summary>Raised when an editor should refresh its content.</summary>
    event EventHandler<EditorRefreshEventArgs>? RefreshRequested;

    /// <summary>Manually requests a refresh for a specific file.</summary>
    void RequestRefresh(string filePath, RefreshReason reason, string? newContent = null);

    /// <summary>Suspends refresh notifications temporarily.</summary>
    IDisposable SuspendNotifications();

    /// <summary>Gets whether notifications are currently suspended.</summary>
    bool IsSuspended { get; }
}
