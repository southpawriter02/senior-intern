using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
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
    /// <b>Services:</b> Registered as singletons to maintain shared state:
    /// <list type="bullet">
    /// <item><see cref="ISettingsService"/> - Persistent application settings</item>
    /// <item><see cref="ILlmService"/> - LLM model management and inference</item>
    /// <item><see cref="IConversationService"/> - Conversation state management</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>ViewModels:</b> Registered as transient (new instance per request):
    /// <list type="bullet">
    /// <item><see cref="MainWindowViewModel"/></item>
    /// <item><see cref="ChatViewModel"/></item>
    /// <item><see cref="ModelSelectorViewModel"/></item>
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
        // │ CORE SERVICES (Singletons - shared state across app)            │
        // └─────────────────────────────────────────────────────────────────┘
        
        // Settings: persists and retrieves app configuration
        services.AddSingleton<ISettingsService, SettingsService>();
        
        // LLM: manages model loading, inference, and resource cleanup
        services.AddSingleton<ILlmService, LlmService>();
        
        // Conversation: maintains current chat state and message history
        services.AddSingleton<IConversationService, ConversationService>();

        // ┌─────────────────────────────────────────────────────────────────┐
        // │ VIEWMODELS (Transient - created fresh when requested)           │
        // └─────────────────────────────────────────────────────────────────┘
        
        // Main window: coordinates child ViewModels and status bar
        services.AddTransient<MainWindowViewModel>();
        
        // Chat panel: handles messages and streaming generation
        services.AddTransient<ChatViewModel>();
        
        // Model selector: file picker and model loading UI
        services.AddTransient<ModelSelectorViewModel>();

        return services;
    }
}
