using Microsoft.EntityFrameworkCore;
using AIntern.Core.Entities;
using AIntern.Core.Templates;

namespace AIntern.Data;

/// <summary>
/// Handles database initialization, creation, and seeding of default data.
/// </summary>
public sealed class DatabaseInitializer
{
    private readonly IDbContextFactory<AInternDbContext> _contextFactory;

    public DatabaseInitializer(IDbContextFactory<AInternDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    /// Initializes the database, creating it if necessary and seeding default data.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Ensure database and tables are created
        await context.Database.EnsureCreatedAsync(ct);

        // Seed default data if not already present
        await SeedDefaultsAsync(context, ct);
    }

    /// <summary>
    /// Seeds default system prompts and inference presets if they don't exist.
    /// </summary>
    private static async Task SeedDefaultsAsync(AInternDbContext context, CancellationToken ct)
    {
        await SeedSystemPromptsAsync(context, ct);
        await SeedInferencePresetsAsync(context, ct);
    }

    private static async Task SeedSystemPromptsAsync(AInternDbContext context, CancellationToken ct)
    {
        // Check if any built-in prompts exist (use IsBuiltIn to allow user prompts to coexist)
        if (await context.SystemPrompts.AnyAsync(sp => sp.IsBuiltIn, ct))
        {
            return; // Already seeded
        }

        var templates = SystemPromptTemplates.GetAllTemplates();
        context.SystemPrompts.AddRange(templates);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedInferencePresetsAsync(AInternDbContext context, CancellationToken ct)
    {
        if (await context.InferencePresets.AnyAsync(ct))
        {
            return; // Already seeded
        }

        var defaultPresets = new List<InferencePresetEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Balanced",
                Temperature = 0.7f,
                TopP = 0.9f,
                MaxTokens = 2048,
                ContextSize = 4096,
                IsDefault = true,
                IsBuiltIn = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Precise",
                Temperature = 0.3f,
                TopP = 0.8f,
                MaxTokens = 2048,
                ContextSize = 4096,
                IsDefault = false,
                IsBuiltIn = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Creative",
                Temperature = 1.2f,
                TopP = 0.95f,
                MaxTokens = 2048,
                ContextSize = 4096,
                IsDefault = false,
                IsBuiltIn = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Long-form",
                Temperature = 0.7f,
                TopP = 0.9f,
                MaxTokens = 4096,
                ContextSize = 8192,
                IsDefault = false,
                IsBuiltIn = true
            }
        };

        context.InferencePresets.AddRange(defaultPresets);
        await context.SaveChangesAsync(ct);
    }
}
