using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Data;
using AIntern.Data.Repositories;
using AIntern.Desktop.Services;
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
    /// <item><see cref="IInferenceSettingsService"/> - Inference parameters and presets</item>
    /// <item><see cref="ISystemPromptService"/> - System prompt management (v0.2.4b)</item>
    /// <item><see cref="ISearchService"/> - Full-text search with suggestions (v0.2.5b)</item>
    /// <item><see cref="IExportService"/> - Conversation export to multiple formats (v0.2.5c)</item>
    /// <item><see cref="IMigrationService"/> - Version migration and legacy settings import (v0.2.5d)</item>
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
    /// <item><see cref="SystemPromptEditorViewModel"/> - Transient (per editor window)</item>
    /// <item><see cref="SystemPromptSelectorViewModel"/> - Singleton (shared state)</item>
    /// <item><see cref="SearchViewModel"/> - Transient (per search dialog, v0.2.5e)</item>
    /// <item><see cref="ExportViewModel"/> - Created manually (requires runtime conversationId, v0.2.5f)</item>
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

        // System Prompt: manages system prompts, templates, and current selection.
        // Uses a factory to resolve scoped repository dependencies.
        // The service maintains prompt selection state and fires events on changes.
        // Added in v0.2.4b.
        services.AddSingleton<ISystemPromptService>(sp =>
        {
            // Create a scope to resolve scoped services (repositories).
            var scope = sp.CreateScope();
            return new SystemPromptService(
                scope.ServiceProvider.GetRequiredService<ISystemPromptRepository>(),
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<ILogger<SystemPromptService>>());
        });

        // Search: provides full-text search with recent search suggestions.
        // Uses a factory to resolve scoped DbContext dependency.
        // The service wraps FTS5 search operations from AInternDbContext.
        // Added in v0.2.5b.
        services.AddSingleton<ISearchService>(sp =>
        {
            // Create a scope to resolve scoped services (DbContext).
            var scope = sp.CreateScope();
            return new SearchService(
                scope.ServiceProvider.GetRequiredService<AInternDbContext>(),
                sp.GetRequiredService<ILogger<SearchService>>());
        });

        // Export: provides conversation export to multiple formats.
        // Uses a factory to resolve scoped repository dependency.
        // The service supports Markdown, JSON, PlainText, and HTML formats.
        // Added in v0.2.5c.
        services.AddSingleton<IExportService>(sp =>
        {
            // Create a scope to resolve scoped services (repositories).
            var scope = sp.CreateScope();
            return new ExportService(
                scope.ServiceProvider.GetRequiredService<IConversationRepository>(),
                sp.GetRequiredService<ILogger<ExportService>>());
        });

        // Migration: provides version migration and legacy settings import.
        // Uses a factory to resolve scoped DbContext dependency.
        // The service handles v0.1.0 → v0.2.0 migrations automatically.
        // Added in v0.2.5d.
        services.AddSingleton<IMigrationService>(sp =>
        {
            // Create a scope to resolve scoped services (DbContext).
            var scope = sp.CreateScope();
            return new MigrationService(
                scope.ServiceProvider.GetRequiredService<AInternDbContext>(),
                scope.ServiceProvider.GetRequiredService<DatabasePathResolver>(),
                sp.GetRequiredService<ILogger<MigrationService>>());
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
        // │ WORKSPACE SERVICES (v0.3.1e-f)                                  │
        // └─────────────────────────────────────────────────────────────────┘

        // File System: workspace-aware file operations with watching and .gitignore.
        // Singleton for shared file watcher state.
        // Added in v0.3.1d.
        services.AddSingleton<IFileSystemService, FileSystemService>();

        // Workspace: manages workspace lifecycle, state, and recent history.
        // Uses a factory to resolve scoped repository dependency.
        // Added in v0.3.1e, enhanced in v0.3.1f.
        services.AddSingleton<IWorkspaceService>(sp =>
        {
            // Create a scope to resolve scoped services (repositories).
            var scope = sp.CreateScope();
            return new WorkspaceService(
                scope.ServiceProvider.GetRequiredService<IWorkspaceRepository>(),
                sp.GetRequiredService<IFileSystemService>(),
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<ILogger<WorkspaceService>>());
        });

        // File Index: workspace file indexing with fuzzy search.
        // Added in v0.3.5c.
        services.AddSingleton<IFileIndexService, FileIndexService>();

        // Keyboard Shortcuts: centralized shortcut management (v0.3.5g).
        services.AddSingleton<IKeyboardShortcutService, KeyboardShortcutService>();

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

        // System Prompt Editor: manages prompt editing state with service integration.
        // Transient so each editor window gets its own instance with independent state.
        // Subscribes to ISystemPromptService events for list synchronization.
        // Added in v0.2.4c.
        services.AddTransient<SystemPromptEditorViewModel>();

        // System Prompt Selector: quick selector for the chat header dropdown.
        // Singleton to share selection state across the application.
        // Subscribes to ISystemPromptService events for automatic synchronization.
        // Added in v0.2.4c.\n        services.AddSingleton<SystemPromptSelectorViewModel>();

        // File Explorer: workspace navigation and file operations sidebar.
        // Singleton to persist expansion state and selection across application lifecycle.
        // Subscribes to IWorkspaceService events for workspace state synchronization.
        // Added in v0.3.2g.
        services.AddSingleton<FileExplorerViewModel>();

        // Search: spotlight-style search dialog ViewModel with debounced search.
        // Transient so each dialog gets its own instance with fresh state.
        // Uses ISearchService for FTS5 search operations.
        // Added in v0.2.5e.
        services.AddTransient<SearchViewModel>();

        // Welcome: welcome screen ViewModel shown when no workspace is open.
        // Transient so each display gets fresh recent workspaces.
        // Added in v0.3.5h.
        services.AddTransient<WelcomeViewModel>();

        return services;
    }
}
