namespace AIntern.Core.Models;

/// <summary>
/// Result of a migration operation.
/// </summary>
public sealed record MigrationResult(
    bool Success,
    Version FromVersion,
    Version ToVersion,
    IReadOnlyList<string> MigrationSteps,
    string? ErrorMessage)
{
    /// <summary>Creates a successful result indicating no migration was needed.</summary>
    public static MigrationResult NoMigrationNeeded(Version current) => new(
        Success: true,
        FromVersion: current,
        ToVersion: current,
        MigrationSteps: ["No migration needed"],
        ErrorMessage: null);

    /// <summary>Creates a failed migration result with error details.</summary>
    public static MigrationResult Failed(
        Version from,
        Version to,
        IReadOnlyList<string> steps,
        string error) => new(
        Success: false,
        FromVersion: from,
        ToVersion: to,
        MigrationSteps: steps,
        ErrorMessage: error);
}
