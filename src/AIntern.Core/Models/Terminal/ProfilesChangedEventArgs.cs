namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PROFILES CHANGED EVENT ARGS (v0.5.3d)                                   │
// │ Event arguments for profile modification notifications.                 │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Event arguments for profile changes.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3d.</para>
/// <para>
/// Provides details about profile modifications including:
/// <list type="bullet">
///   <item>Type of change (Added, Updated, Deleted, etc.)</item>
///   <item>ID of the affected profile (null for Reset)</item>
///   <item>The profile after modification (null for Deleted/Reset)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProfilesChangedEventArgs : EventArgs
{
    /// <summary>
    /// Type of change that occurred.
    /// </summary>
    public ProfileChangeType ChangeType { get; init; }

    /// <summary>
    /// ID of the affected profile.
    /// </summary>
    /// <remarks>
    /// Null for <see cref="ProfileChangeType.Reset"/> events.
    /// </remarks>
    public Guid? ProfileId { get; init; }

    /// <summary>
    /// The profile after the change.
    /// </summary>
    /// <remarks>
    /// Null for <see cref="ProfileChangeType.Deleted"/> and
    /// <see cref="ProfileChangeType.Reset"/> events.
    /// </remarks>
    public ShellProfile? Profile { get; init; }

    /// <summary>
    /// Returns a string representation of the event.
    /// </summary>
    public override string ToString() => ProfileId.HasValue
        ? $"ProfilesChanged({ChangeType}, {ProfileId.Value})"
        : $"ProfilesChanged({ChangeType})";
}
