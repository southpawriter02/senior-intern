using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AIntern.Core.Entities;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Data;

namespace AIntern.Services;

/// <summary>
/// Handles migration from v0.1.0 (JSON settings) to v0.2.0 (SQLite database).
/// </summary>
public sealed class MigrationService : IMigrationService
{
    private static readonly Version CurrentVersion = new(0, 2, 0);
    private static readonly Version LegacyVersion = new(0, 1, 0);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IDbContextFactory<AInternDbContext> _contextFactory;
    private readonly DatabaseInitializer _databaseInitializer;
    private readonly ILogger<MigrationService> _logger;
    private readonly string _settingsPath;
    private readonly string _backupPath;

    public MigrationService(
        IDbContextFactory<AInternDbContext> contextFactory,
        DatabaseInitializer databaseInitializer,
        ILogger<MigrationService> logger)
    {
        _contextFactory = contextFactory;
        _databaseInitializer = databaseInitializer;
        _logger = logger;

        // Legacy settings location (SeniorIntern folder)
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "SeniorIntern");
        _settingsPath = Path.Combine(appFolder, "settings.json");
        _backupPath = Path.Combine(appFolder, "settings.v1.json.bak");
    }

    public async Task<MigrationResult> MigrateIfNeededAsync(CancellationToken ct = default)
    {
        var steps = new List<string>();

        try
        {
            if (!await IsMigrationRequiredAsync(ct))
            {
                _logger.LogInformation("No migration required");
                return MigrationResult.NoMigrationNeeded(CurrentVersion);
            }

            _logger.LogInformation("Starting migration from v0.1.0 to v0.2.0");

            // Step 1: Backup legacy settings
            steps.Add("Backing up v0.1.0 settings");
            await BackupLegacySettingsAsync(ct);

            // Step 2: Read legacy settings
            steps.Add("Reading legacy settings");
            var legacySettings = await ReadLegacySettingsAsync(ct);

            // Step 3: Initialize database (creates tables, seeds defaults)
            steps.Add("Initializing database and seeding defaults");
            await _databaseInitializer.InitializeAsync(ct);

            // Step 4: Migrate settings to new format
            steps.Add("Migrating settings to new format");
            await MigrateSettingsAsync(legacySettings, ct);

            // Step 5: Stamp version in database
            steps.Add("Stamping version in database");
            await StampVersionAsync(ct);

            _logger.LogInformation("Migration completed successfully");

            return new MigrationResult(
                Success: true,
                FromVersion: LegacyVersion,
                ToVersion: CurrentVersion,
                MigrationSteps: steps,
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            steps.Add($"FAILED: {ex.Message}");
            return MigrationResult.Failed(LegacyVersion, CurrentVersion, steps, ex.Message);
        }
    }

    public async Task<Version> GetCurrentVersionAsync(CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            if (!await context.Database.CanConnectAsync(ct))
                return LegacyVersion;

            var versionRecord = await context.AppVersions.FirstOrDefaultAsync(ct);
            return versionRecord?.ToVersion() ?? LegacyVersion;
        }
        catch
        {
            return LegacyVersion;
        }
    }

    public async Task<bool> IsMigrationRequiredAsync(CancellationToken ct = default)
    {
        // No legacy settings file = fresh install, no migration needed
        if (!File.Exists(_settingsPath))
        {
            _logger.LogDebug("No legacy settings file found, skipping migration");
            return false;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            // Database doesn't exist or can't connect = needs migration
            if (!await context.Database.CanConnectAsync(ct))
            {
                _logger.LogDebug("Database not accessible, migration required");
                return true;
            }

            // Check version stamp
            var currentVersion = await GetCurrentVersionAsync(ct);
            var needsMigration = currentVersion < CurrentVersion;

            _logger.LogDebug(
                "Current version: {Current}, Target: {Target}, Migration required: {Required}",
                currentVersion, CurrentVersion, needsMigration);

            return needsMigration;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking migration status, assuming migration required");
            return true;
        }
    }

    #region Private Helpers

    private async Task BackupLegacySettingsAsync(CancellationToken ct)
    {
        if (!File.Exists(_settingsPath))
            return;

        try
        {
            var content = await File.ReadAllTextAsync(_settingsPath, ct);
            await File.WriteAllTextAsync(_backupPath, content, ct);
            _logger.LogInformation("Backed up legacy settings to {Path}", _backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to backup legacy settings, continuing with migration");
        }
    }

    private async Task<LegacySettings> ReadLegacySettingsAsync(CancellationToken ct)
    {
        if (!File.Exists(_settingsPath))
            return new LegacySettings();

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath, ct);
            return JsonSerializer.Deserialize<LegacySettings>(json, JsonOptions) ?? new LegacySettings();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read legacy settings, using defaults");
            return new LegacySettings();
        }
    }

    private async Task MigrateSettingsAsync(LegacySettings legacy, CancellationToken ct)
    {
        // Map legacy values to current AppSettings format
        var settings = new AppSettings
        {
            LastModelPath = legacy.LastModelPath,
            DefaultContextSize = legacy.DefaultContextSize,
            DefaultGpuLayers = legacy.DefaultGpuLayers,
            Temperature = legacy.Temperature,
            Theme = legacy.Theme,
            // Use defaults for new v0.2.0 fields
            TopP = 0.9f,
            MaxTokens = 2048,
            DefaultBatchSize = 512,
            SidebarWidth = 280,
            WindowWidth = 1200,
            WindowHeight = 800
        };

        // Write updated settings back
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_settingsPath, json, ct);

        _logger.LogInformation("Migrated settings with legacy values preserved");
    }

    private async Task StampVersionAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Remove any existing version records
        var existing = await context.AppVersions.ToListAsync(ct);
        if (existing.Count > 0)
        {
            context.AppVersions.RemoveRange(existing);
        }

        // Add current version
        context.AppVersions.Add(new AppVersionEntity
        {
            Id = 1,
            Major = CurrentVersion.Major,
            Minor = CurrentVersion.Minor,
            Patch = CurrentVersion.Build,
            MigratedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(ct);
        _logger.LogInformation("Stamped database version as {Version}", CurrentVersion);
    }

    #endregion

    #region Legacy Settings Model

    /// <summary>
    /// v0.1.0 settings format for migration purposes.
    /// </summary>
    private sealed class LegacySettings
    {
        public string? LastModelPath { get; set; }
        public uint DefaultContextSize { get; set; } = 4096;
        public int DefaultGpuLayers { get; set; } = -1;
        public float Temperature { get; set; } = 0.7f;
        public string Theme { get; set; } = "Dark";
    }

    #endregion
}
