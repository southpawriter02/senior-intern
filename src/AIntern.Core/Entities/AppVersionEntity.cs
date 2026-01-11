namespace AIntern.Core.Entities;

/// <summary>
/// Tracks application version for migration purposes.
/// </summary>
public sealed class AppVersionEntity
{
    /// <summary>Primary key (single row expected).</summary>
    public int Id { get; set; }

    /// <summary>Major version component.</summary>
    public int Major { get; set; }

    /// <summary>Minor version component.</summary>
    public int Minor { get; set; }

    /// <summary>Patch version component.</summary>
    public int Patch { get; set; }

    /// <summary>When the migration was applied.</summary>
    public DateTime MigratedAt { get; set; }

    /// <summary>Gets the version as a Version object.</summary>
    public Version ToVersion() => new(Major, Minor, Patch);
}
