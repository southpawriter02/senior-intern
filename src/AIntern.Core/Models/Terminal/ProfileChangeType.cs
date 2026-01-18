namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PROFILE CHANGE TYPE (v0.5.3d)                                           │
// │ Enumeration for profile modification events.                            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Type of profile change event.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3d.</para>
/// <para>
/// Used with <see cref="ProfilesChangedEventArgs"/> to indicate
/// what kind of modification occurred to the profile collection.
/// </para>
/// </remarks>
public enum ProfileChangeType
{
    /// <summary>
    /// A new profile was created.
    /// </summary>
    Added,

    /// <summary>
    /// An existing profile was modified.
    /// </summary>
    Updated,

    /// <summary>
    /// A profile was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// The default profile selection changed.
    /// </summary>
    DefaultChanged,

    /// <summary>
    /// All profiles were reset to defaults.
    /// </summary>
    Reset
}
