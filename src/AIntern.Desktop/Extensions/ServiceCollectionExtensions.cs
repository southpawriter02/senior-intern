using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AIntern.Core.Interfaces;
using AIntern.Data;
using AIntern.Data.Repositories;
using AIntern.Desktop.ViewModels;
using AIntern.Services;

namespace AIntern.Desktop.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAInternServices(this IServiceCollection services)
    {
        // Database
        services.AddDbContextFactory<AInternDbContext>(options =>
            options.UseSqlite(DatabasePathResolver.GetConnectionString()));

        services.AddSingleton<DatabaseInitializer>();

        // Repositories
        services.AddSingleton<IConversationRepository, ConversationRepository>();
        services.AddSingleton<ISystemPromptRepository, SystemPromptRepository>();
        services.AddSingleton<IInferencePresetRepository, InferencePresetRepository>();

        // Core services (singletons - shared state)
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILlmService, LlmService>();
        services.AddSingleton<IConversationService, DatabaseConversationService>();
        services.AddSingleton<IInferenceSettingsService, InferenceSettingsService>();
        services.AddSingleton<ISystemPromptService, SystemPromptService>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IMigrationService, MigrationService>();

        // ViewModels (transient - created as needed)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<ModelSelectorViewModel>();
        services.AddTransient<ConversationListViewModel>();
        services.AddTransient<InferenceSettingsViewModel>();
        services.AddTransient<SystemPromptEditorViewModel>();
        services.AddTransient<SystemPromptSelectorViewModel>();

        return services;
    }

    /// <summary>
    /// Initializes the database (creates tables, seeds defaults).
    /// Call this at application startup.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        var initializer = services.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync(ct);
    }
}
