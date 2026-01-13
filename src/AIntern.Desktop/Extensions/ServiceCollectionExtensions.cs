using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Data;
using AIntern.Data.Repositories;
using AIntern.Desktop.ViewModels;
using AIntern.Services;
using Serilog;

namespace AIntern.Desktop.Extensions;

/// <summary>
/// Extension methods for configuring the application's dependency injection container.
/// </summary>
/// <remarks>
/// This class centralizes all DI registrations for the application.
/// Called from <see cref="App.OnFrameworkInitializationCompleted"/>.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all AIntern services, ViewModels, and logging into the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// <b>Logging:</b> Replaces the default logging providers with Serilog.
    /// </para>
    /// <para>
    /// <b>Data Layer:</b> Registered via <see cref="DataServiceCollectionExtensions.AddDataServices"/>:
    /// <list type="bullet">
    /// <item><see cref="AInternDbContext"/> - Database context (scoped)</item>
    /// <item>Repository interfaces - Data access (scoped)</item>
    /// <item><see cref="DatabaseInitializer"/> - Migration and seeding (scoped)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Services:</b> Registered as singletons to maintain shared state:
    /// <list type="bullet">
    /// <item><see cref="ISettingsService"/> - Persistent application settings</item>
    /// <item><see cref="ILlmService"/> - LLM model management and inference</item>
    /// <item><see cref="IConversationService"/> - Conversation state management</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>ViewModels:</b> Registered with appropriate lifetimes:
    /// <list type="bullet">
    /// <item><see cref="MainWindowViewModel"/> - Transient</item>
    /// <item><see cref="ChatViewModel"/> - Transient</item>
    /// <item><see cref="ModelSelectorViewModel"/> - Transient</item>
    /// <item><see cref="ConversationListViewModel"/> - Transient</item>
    /// <item><see cref="InferenceSettingsViewModel"/> - Singleton (maintains state)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddAInternServices(this IServiceCollection services)
    {
        // ┌─────────────────────────────────────────────────────────────────┐
        // │ LOGGING CONFIGURATION                                           │
        // └─────────────────────────────────────────────────────────────────┘
        
        // Configure Microsoft.Extensions.Logging to use Serilog
        services.AddLogging(builder =>
        {
            // Remove default console/debug providers
            builder.ClearProviders();
            
            // Add Serilog as the sole logging provider
            // dispose: true ensures Serilog is properly disposed on app shutdown
            builder.AddSerilog(dispose: true);
        });

        // ┌─────────────────────────────────────────────────────────────────┐
        // │ DATA LAYER (Scoped - shared DbContext per request)              │
        // └─────────────────────────────────────────────────────────────────┘
        
        // Register DbContext, repositories, and database initializer.
        // This enables conversation persistence, system prompts, and inference presets.
        services.AddDataServices();

        // ┌─────────────────────────────────────────────────────────────────┐
        // │ CORE SERVICES (Singletons - shared state across app)            │
        // └─────────────────────────────────────────────────────────────────┘
        
        // Settings: persists and retrieves app configuration
        services.AddSingleton<ISettingsService, SettingsService>();

        // Conversation: manages current chat state with database persistence.
        // Uses a factory to resolve scoped repository dependencies.
        // The service maintains a long-lived singleton while creating scoped
        // DbContext instances for each database operation.
        services.AddSingleton<IConversationService>(sp =>
        {
            // Create a scope to resolve scoped services (repositories).
            // The service will manage its own scopes for database operations.
            var scope = sp.CreateScope();
            return new DatabaseConversationService(
                scope.ServiceProvider.GetRequiredService<IConversationRepository>(),
                scope.ServiceProvider.GetRequiredService<ISystemPromptRepository>(),
                sp.GetRequiredService<ILogger<DatabaseConversationService>>());
        });

        // Inference Settings: manages live inference parameters with preset support.
        // Uses a factory to resolve scoped repository dependencies.
        // The service coordinates between repository (presets), settings service
        // (persistence), and consumers (LlmService, ViewModels).
        services.AddSingleton<IInferenceSettingsService>(sp =>
        {
            // Create a scope to resolve scoped services (repositories).
            var scope = sp.CreateScope();
            return new InferenceSettingsService(
                scope.ServiceProvider.GetRequiredService<IInferencePresetRepository>(),
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<ILogger<InferenceSettingsService>>());
        });

        // LLM: manages model loading, inference, and resource cleanup.
        // v0.2.3e: Updated to inject IInferenceSettingsService for user-configured parameters.
        // Must be registered after IInferenceSettingsService.
        services.AddSingleton<ILlmService>(sp =>
        {
            return new LlmService(
                sp.GetRequiredService<IInferenceSettingsService>());
        });

        // ┌─────────────────────────────────────────────────────────────────┐
        // │ UI INFRASTRUCTURE                                                │
        // └─────────────────────────────────────────────────────────────────┘
        
        // Dispatcher: UI thread abstraction for testable ViewModels.
        // Singleton because Avalonia.Threading.Dispatcher is thread-safe.
        services.AddSingleton<IDispatcher, AvaloniaDispatcher>();

        // ┌─────────────────────────────────────────────────────────────────┐
        // │ VIEWMODELS (Transient - created fresh when requested)           │
        // └─────────────────────────────────────────────────────────────────┘
        
        // Main window: coordinates child ViewModels and status bar
        services.AddTransient<MainWindowViewModel>();
        
        // Chat panel: handles messages and streaming generation
        services.AddTransient<ChatViewModel>();
        
        // Model selector: file picker and model loading UI
        services.AddTransient<ModelSelectorViewModel>();
        
        // Conversation list: sidebar with grouped conversations
        services.AddTransient<ConversationListViewModel>();

        // Inference settings: parameter sliders and preset management.
        // Singleton to maintain state across the application lifecycle.
        // Subscribes to IInferenceSettingsService events for two-way sync.
        services.AddSingleton<InferenceSettingsViewModel>();

        return services;
    }
}
