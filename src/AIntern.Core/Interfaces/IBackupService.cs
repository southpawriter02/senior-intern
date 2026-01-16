namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ BACKUP SERVICE INTERFACE (v0.4.3b)                                       │
// │ Service for managing file backups for undo support.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for managing file backups for undo support.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.3b.</para>
/// </remarks>
public interface IBackupService
{
    /// <summary>
    /// Create a backup of a file.
    /// </summary>
    /// <param name="filePath">The absolute path to the file to backup.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The path to the backup file, or null if backup failed.</returns>
    Task<string?> CreateBackupAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Restore a file from backup.
    /// </summary>
    /// <param name="backupPath">The path to the backup file.</param>
    /// <param name="targetPath">The path to restore to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if restore succeeded.</returns>
    Task<bool> RestoreBackupAsync(string backupPath, string targetPath, CancellationToken ct = default);

    /// <summary>
    /// Delete a backup file.
    /// </summary>
    /// <param name="backupPath">The path to the backup file.</param>
    /// <returns>True if deletion succeeded.</returns>
    bool DeleteBackup(string backupPath);

    /// <summary>
    /// Check if a backup exists.
    /// </summary>
    /// <param name="backupPath">The path to the backup file.</param>
    /// <returns>True if the backup exists.</returns>
    bool BackupExists(string backupPath);

    /// <summary>
    /// Clean up expired backups.
    /// </summary>
    /// <param name="maxAge">Maximum age of backups to keep.</param>
    /// <returns>Number of backups deleted.</returns>
    int CleanupExpiredBackups(TimeSpan maxAge);
}
