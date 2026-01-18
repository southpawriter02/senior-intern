namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL PROFILE SERVICE INTERFACE (v0.5.3d)                               │
// │ CRUD operations, persistence, and default profile management.           │
// └─────────────────────────────────────────────────────────────────────────┘

using AIntern.Core.Models.Terminal;

/// <summary>
/// Service for managing shell profiles with persistence.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.3d.</para>
/// <para>
/// Provides comprehensive profile management:
/// <list type="bullet">
///   <item>CRUD operations for profiles</item>
///   <item>JSON persistence to disk</item>
///   <item>Default profile selection and resolution</item>
///   <item>Built-in profile generation from detected shells</item>
///   <item>Import/export functionality</item>
///   <item>Effective settings resolution (profile + app defaults)</item>
/// </list>
/// </para>
/// </remarks>
public interface IShellProfileService
{
    // ─────────────────────────────────────────────────────────────────────
    // Events
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when profiles are modified.
    /// </summary>
    /// <remarks>
    /// Subscribe to receive notifications for Add, Update, Delete,
    /// DefaultChanged, and Reset operations.
    /// </remarks>
    event EventHandler<ProfilesChangedEventArgs>? ProfilesChanged;

    // ─────────────────────────────────────────────────────────────────────
    // Read Operations
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets all profiles (built-in and custom).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Read-only list of all profiles.</returns>
    Task<IReadOnlyList<ShellProfile>> GetAllProfilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets visible profiles, sorted by SortOrder then Name.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Read-only list of visible profiles.</returns>
    /// <remarks>
    /// Excludes profiles where <see cref="ShellProfile.IsHidden"/> is true.
    /// </remarks>
    Task<IReadOnlyList<ShellProfile>> GetVisibleProfilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a profile by ID.
    /// </summary>
    /// <param name="id">Profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The profile, or null if not found.</returns>
    Task<ShellProfile?> GetProfileAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the default profile (never null).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The default profile.</returns>
    /// <remarks>
    /// <para>Resolution order:</para>
    /// <list type="number">
    ///   <item>AppSettings.DefaultShellProfileId (if exists)</item>
    ///   <item>First profile with IsDefault = true</item>
    ///   <item>First profile in the list</item>
    /// </list>
    /// </remarks>
    Task<ShellProfile> GetDefaultProfileAsync(CancellationToken ct = default);

    // ─────────────────────────────────────────────────────────────────────
    // Write Operations
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new profile.
    /// </summary>
    /// <param name="profile">Profile to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created profile.</returns>
    /// <exception cref="ArgumentException">ShellPath is empty or invalid.</exception>
    /// <remarks>
    /// Auto-detects ShellType if set to Unknown.
    /// </remarks>
    Task<ShellProfile> CreateProfileAsync(ShellProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing profile.
    /// </summary>
    /// <param name="profile">Profile with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Profile not found or is built-in.</exception>
    Task UpdateProfileAsync(ShellProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Deletes a profile.
    /// </summary>
    /// <param name="id">Profile ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Profile is built-in.</exception>
    /// <remarks>
    /// Silently ignores non-existent profile IDs.
    /// Clears DefaultShellProfileId if deleted profile was default.
    /// </remarks>
    Task DeleteProfileAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Sets a profile as the default.
    /// </summary>
    /// <param name="id">Profile ID to set as default.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Profile not found.</exception>
    /// <remarks>
    /// Clears IsDefault on all other profiles.
    /// Updates AppSettings.DefaultShellProfileId.
    /// </remarks>
    Task SetDefaultProfileAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Duplicates a profile using Clone().
    /// </summary>
    /// <param name="id">Profile ID to duplicate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new profile.</returns>
    /// <exception cref="InvalidOperationException">Profile not found.</exception>
    Task<ShellProfile> DuplicateProfileAsync(Guid id, CancellationToken ct = default);

    // ─────────────────────────────────────────────────────────────────────
    // Bulk Operations
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resets all profiles to auto-detected defaults.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Removes all custom profiles and regenerates built-in profiles
    /// from detected shells.
    /// </remarks>
    Task ResetToDefaultsAsync(CancellationToken ct = default);

    /// <summary>
    /// Imports profiles from JSON.
    /// </summary>
    /// <param name="json">JSON string containing profiles.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Count of profiles imported.</returns>
    /// <remarks>
    /// Creates new IDs for imported profiles.
    /// Skips profiles with invalid shell paths.
    /// </remarks>
    Task<int> ImportProfilesAsync(string json, CancellationToken ct = default);

    /// <summary>
    /// Exports profiles to JSON.
    /// </summary>
    /// <param name="profileIds">Profile IDs to export, or null for non-built-in.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>JSON string of profiles.</returns>
    Task<string> ExportProfilesAsync(IEnumerable<Guid>? profileIds = null, CancellationToken ct = default);

    // ─────────────────────────────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets effective settings (profile overrides + app defaults).
    /// </summary>
    /// <param name="profile">Profile to resolve settings for.</param>
    /// <returns>Resolved settings with all values populated.</returns>
    /// <remarks>
    /// Nullable profile properties are resolved from AppSettings.
    /// </remarks>
    ShellProfileDefaults GetEffectiveSettings(ShellProfile profile);
}
