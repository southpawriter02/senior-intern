using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

/// <summary>
/// Handles migration between application versions.
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// Checks if migration is required and performs it if needed.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Migration result with status and steps.</returns>
    Task<MigrationResult> MigrateIfNeededAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current application version from the database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current version, or v0.1.0 if not found.</returns>
    Task<Version> GetCurrentVersionAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if migration is required without performing it.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if migration is required.</returns>
    Task<bool> IsMigrationRequiredAsync(CancellationToken ct = default);
}
