using System.Diagnostics;
using AIntern.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIntern.Data.Repositories;

/// <summary>
/// Repository implementation for managing inference presets.
/// </summary>
/// <remarks>
/// <para>
/// This repository provides a clean abstraction over Entity Framework Core operations
/// for inference presets, with comprehensive logging support.
/// </para>
/// <para>
/// <b>Key Implementation Details:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Built-in protection:</b> Prevents deletion of IsBuiltIn presets</description></item>
///   <item><description><b>Atomic default switching:</b> Uses two ExecuteUpdateAsync calls to atomically change the default preset</description></item>
///   <item><description><b>Duplication:</b> Creates a user-owned copy of any preset with a new name</description></item>
///   <item><description><b>Default reassignment:</b> Automatically assigns a new default if the current default is deleted</description></item>
/// </list>
/// <para>
/// <b>Logging Behavior:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Debug:</b> Entry/exit for all operations with parameters and timing</description></item>
///   <item><description><b>Warning:</b> When built-in protection triggers or preset not found</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is not thread-safe. Each request should use its own
/// instance via dependency injection with scoped lifetime.
/// </para>
/// </remarks>
public sealed class InferencePresetRepository : IInferencePresetRepository
{
    #region Fields

    /// <summary>
    /// The database context for Entity Framework operations.
    /// </summary>
    private readonly AInternDbContext _context;

    /// <summary>
    /// Logger instance for diagnostic output.
    /// </summary>
    private readonly ILogger<InferencePresetRepository> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="InferencePresetRepository"/>.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <remarks>
    /// If no logger is provided, a <see cref="NullLogger{T}"/> is used.
    /// </remarks>
    public InferencePresetRepository(AInternDbContext context, ILogger<InferencePresetRepository>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? NullLogger<InferencePresetRepository>.Instance;
        _logger.LogDebug("InferencePresetRepository instance created");
    }

    #endregion

    #region Read Operations

    /// <inheritdoc />
    public async Task<InferencePresetEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting inference preset by ID: {PresetId}", id);

        var preset = await _context.InferencePresets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (preset == null)
        {
            _logger.LogDebug("Inference preset not found: {PresetId}", id);
        }

        return preset;
    }

    /// <inheritdoc />
    public async Task<InferencePresetEntity?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting default inference preset");

        var preset = await _context.InferencePresets
            .AsNoTracking()
            .Where(p => p.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

        if (preset == null)
        {
            _logger.LogDebug("No default inference preset found");
        }
        else
        {
            _logger.LogDebug("Default inference preset found: {PresetId} ({Name})", preset.Id, preset.Name);
        }

        return preset;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferencePresetEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all inference presets");

        var presets = await _context.InferencePresets
            .AsNoTracking()
            .OrderByDescending(p => p.IsBuiltIn)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} inference presets", presets.Count);

        return presets;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferencePresetEntity>> GetBuiltInAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting built-in inference presets");

        var presets = await _context.InferencePresets
            .AsNoTracking()
            .Where(p => p.IsBuiltIn)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} built-in inference presets", presets.Count);

        return presets;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferencePresetEntity>> GetUserCreatedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting user-created inference presets");

        var presets = await _context.InferencePresets
            .AsNoTracking()
            .Where(p => !p.IsBuiltIn)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} user-created inference presets", presets.Count);

        return presets;
    }

    /// <inheritdoc />
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if inference preset name exists: '{Name}'", name);

        var query = _context.InferencePresets.Where(p => p.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        var exists = await query.AnyAsync(cancellationToken);

        _logger.LogDebug("Inference preset name '{Name}' exists: {Exists}", name, exists);

        return exists;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InferencePresetEntity>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] GetByCategoryAsync - Category: {Category}", category);

        var presets = await _context.InferencePresets
            .AsNoTracking()
            .Where(p => p.Category == category)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] GetByCategoryAsync - Category: {Category}, Count: {Count}, Duration: {DurationMs}ms",
            category, presets.Count, stopwatch.ElapsedMilliseconds);

        return presets;
    }

    #endregion

    #region Write Operations

    /// <inheritdoc />
    public async Task<InferencePresetEntity> CreateAsync(InferencePresetEntity preset, CancellationToken cancellationToken = default)
    {
        if (preset.Id == Guid.Empty)
        {
            preset.Id = Guid.NewGuid();
            _logger.LogDebug("Generated new ID for inference preset: {PresetId}", preset.Id);
        }

        _logger.LogDebug("Creating inference preset: {PresetId} with name '{Name}'", preset.Id, preset.Name);

        _context.InferencePresets.Add(preset);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created inference preset: {PresetId}", preset.Id);

        return preset;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(InferencePresetEntity preset, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating inference preset: {PresetId}", preset.Id);

        _context.InferencePresets.Update(preset);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated inference preset: {PresetId}", preset.Id);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] DeleteAsync - PresetId: {PresetId}", id);

        // Load the preset to check IsBuiltIn and IsDefault flags.
        // Built-in presets are protected from deletion.
        var preset = await _context.InferencePresets.FindAsync([id], cancellationToken);

        if (preset == null)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "DeleteAsync - Preset not found: {PresetId}, Duration: {DurationMs}ms",
                id, stopwatch.ElapsedMilliseconds);
            return;
        }

        // Guard clause: Protect built-in presets from deletion.
        // Built-in presets are seeded during database initialization and should
        // never be removed to ensure users always have baseline options.
        if (preset.IsBuiltIn)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "DeleteAsync BLOCKED - Cannot delete built-in preset {PresetId}: {Name}, Duration: {DurationMs}ms",
                id, preset.Name, stopwatch.ElapsedMilliseconds);
            return;
        }

        // Track whether this was the default before deletion.
        // If so, we need to reassign the default to another preset.
        var wasDefault = preset.IsDefault;
        var deletedName = preset.Name;

        _context.InferencePresets.Remove(preset);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Deleted preset {PresetId}: {Name}", id, deletedName);

        // Automatic default reassignment: If we just deleted the default preset,
        // select a new default. Priority: built-in presets first, then by name.
        if (wasDefault)
        {
            _logger.LogDebug("Reassigning default after deleting default preset");

            var newDefault = await _context.InferencePresets
                .OrderByDescending(p => p.IsBuiltIn)
                .ThenBy(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (newDefault != null)
            {
                await _context.InferencePresets
                    .Where(p => p.Id == newDefault.Id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.IsDefault, true)
                        .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);

                _logger.LogDebug(
                    "Reassigned default to preset {PresetId}: {Name}",
                    newDefault.Id, newDefault.Name);
            }
            else
            {
                _logger.LogWarning("No presets remain to assign as default");
            }
        }

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] DeleteAsync - PresetId: {PresetId}, Duration: {DurationMs}ms",
            id, stopwatch.ElapsedMilliseconds);
    }

    /// <inheritdoc />
    public async Task SetAsDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SetAsDefaultAsync - PresetId: {PresetId}", id);

        // Atomic default switching using two ExecuteUpdateAsync calls.
        // Step 1: Clear the IsDefault flag from all currently-default presets.
        var clearedCount = await _context.InferencePresets
            .Where(p => p.IsDefault)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsDefault, false)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        _logger.LogDebug(
            "SetAsDefaultAsync - Cleared IsDefault from {Count} presets",
            clearedCount);

        // Step 2: Set the new preset as default.
        var affectedRows = await _context.InferencePresets
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsDefault, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        stopwatch.Stop();

        if (affectedRows == 0)
        {
            _logger.LogWarning(
                "SetAsDefaultAsync affected 0 rows - preset may not exist: {PresetId}",
                id);
        }
        else
        {
            _logger.LogDebug(
                "[EXIT] SetAsDefaultAsync - PresetId: {PresetId}, Duration: {DurationMs}ms",
                id, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    public async Task<InferencePresetEntity?> DuplicateAsync(Guid id, string newName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "[ENTER] DuplicateAsync - SourcePresetId: {PresetId}, NewName: {NewName}",
            id, newName);

        // Load the source preset to copy its properties.
        // UseAsNoTracking() since we're creating a new entity, not modifying the source.
        var source = await _context.InferencePresets
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (source == null)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "DuplicateAsync - Source preset not found: {PresetId}, Duration: {DurationMs}ms",
                id, stopwatch.ElapsedMilliseconds);
            return null;
        }

        // Create a new preset with the source's parameter values.
        // The duplicate is always user-created (IsBuiltIn = false) and not the default.
        // This allows users to customize built-in presets without losing the originals.
        var duplicate = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = newName,
            Description = source.Description,
            Category = source.Category,
            Temperature = source.Temperature,
            TopP = source.TopP,
            TopK = source.TopK,
            RepeatPenalty = source.RepeatPenalty,
            Seed = source.Seed,
            MaxTokens = source.MaxTokens,
            ContextSize = source.ContextSize,
            IsDefault = false,   // Duplicates are never default
            IsBuiltIn = false,   // Duplicates are always user-created
            UsageCount = 0       // Start fresh usage count for duplicates
        };

        _context.InferencePresets.Add(duplicate);
        await _context.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] DuplicateAsync - SourceId: {SourceId}, DuplicateId: {DuplicateId}, Name: {Name}, Duration: {DurationMs}ms",
            id, duplicate.Id, newName, stopwatch.ElapsedMilliseconds);

        return duplicate;
    }

    /// <inheritdoc />
    public async Task IncrementUsageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] IncrementUsageAsync - PresetId: {PresetId}", id);

        // Use ExecuteUpdateAsync for efficiency - no need to load the full entity.
        // This is safe to call frequently without performance concerns.
        var affected = await _context.InferencePresets
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.UsageCount, p => p.UsageCount + 1)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        stopwatch.Stop();

        if (affected == 0)
        {
            _logger.LogWarning(
                "[EXIT] IncrementUsageAsync - Preset not found: {PresetId}, Duration: {DurationMs}ms",
                id, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogDebug(
                "[EXIT] IncrementUsageAsync - PresetId: {PresetId}, Duration: {DurationMs}ms",
                id, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    public async Task SeedBuiltInPresetsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SeedBuiltInPresetsAsync");

        // Idempotent check: Only seed if no built-in presets exist.
        // This makes it safe to call during every application startup.
        var hasBuiltIn = await _context.InferencePresets
            .AnyAsync(p => p.IsBuiltIn, cancellationToken);

        if (hasBuiltIn)
        {
            stopwatch.Stop();
            _logger.LogDebug(
                "[SKIP] SeedBuiltInPresetsAsync - Built-in presets already exist, Duration: {DurationMs}ms",
                stopwatch.ElapsedMilliseconds);
            return;
        }

        _logger.LogDebug("[INFO] SeedBuiltInPresetsAsync - No built-in presets found, creating 5 presets");

        var now = DateTime.UtcNow;

        // Create 5 built-in presets with well-known GUIDs for stable references.
        // These IDs match InferencePreset.cs well-known preset IDs.
        var builtInPresets = new List<InferencePresetEntity>
        {
            // Precise: Low temperature for factual, deterministic responses
            new()
            {
                Id = new Guid("00000001-0000-0000-0000-000000000001"),
                Name = "Precise",
                Description = "Low temperature for factual and deterministic responses. Best for tasks requiring accuracy and consistency.",
                Category = "Code",
                Temperature = 0.2f,
                TopP = 0.85f,
                TopK = 30,
                RepeatPenalty = 1.1f,
                Seed = -1,
                MaxTokens = 2048,
                ContextSize = 4096,
                IsDefault = false,
                IsBuiltIn = true,
                UsageCount = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            // Balanced: Default preset for general use
            new()
            {
                Id = new Guid("00000001-0000-0000-0000-000000000002"),
                Name = "Balanced",
                Description = "Well-rounded settings for general conversations and tasks. The default preset for new conversations.",
                Category = "General",
                Temperature = 0.7f,
                TopP = 0.9f,
                TopK = 40,
                RepeatPenalty = 1.1f,
                Seed = -1,
                MaxTokens = 2048,
                ContextSize = 4096,
                IsDefault = true,  // This is the default preset
                IsBuiltIn = true,
                UsageCount = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            // Creative: High temperature for brainstorming and creative writing
            new()
            {
                Id = new Guid("00000001-0000-0000-0000-000000000003"),
                Name = "Creative",
                Description = "Higher temperature for brainstorming and creative writing. Produces more varied and imaginative responses.",
                Category = "Creative",
                Temperature = 1.2f,
                TopP = 0.95f,
                TopK = 50,
                RepeatPenalty = 1.05f,
                Seed = -1,
                MaxTokens = 4096,
                ContextSize = 4096,
                IsDefault = false,
                IsBuiltIn = true,
                UsageCount = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            // Long-form: Extended context for detailed work
            new()
            {
                Id = new Guid("00000001-0000-0000-0000-000000000004"),
                Name = "Long-form",
                Description = "Extended context window for working with longer documents and detailed conversations.",
                Category = "Technical",
                Temperature = 0.7f,
                TopP = 0.9f,
                TopK = 40,
                RepeatPenalty = 1.1f,
                Seed = -1,
                MaxTokens = 4096,
                ContextSize = 16384,
                IsDefault = false,
                IsBuiltIn = true,
                UsageCount = 0,
                CreatedAt = now,
                UpdatedAt = now
            },
            // Code Review: Optimized for code analysis
            new()
            {
                Id = new Guid("00000001-0000-0000-0000-000000000005"),
                Name = "Code Review",
                Description = "Optimized for code review and analysis. Low temperature for consistent feedback, extended context for reviewing larger files.",
                Category = "Code",
                Temperature = 0.3f,
                TopP = 0.85f,
                TopK = 30,
                RepeatPenalty = 1.1f,
                Seed = -1,
                MaxTokens = 2048,
                ContextSize = 8192,
                IsDefault = false,
                IsBuiltIn = true,
                UsageCount = 0,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        _context.InferencePresets.AddRange(builtInPresets);
        await _context.SaveChangesAsync(cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
            "[EXIT] SeedBuiltInPresetsAsync - Created {Count} built-in presets, Duration: {DurationMs}ms",
            builtInPresets.Count, stopwatch.ElapsedMilliseconds);
    }

    #endregion
}
