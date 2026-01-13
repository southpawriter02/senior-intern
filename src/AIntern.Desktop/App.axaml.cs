using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Data;
using AIntern.Desktop.Extensions;
using AIntern.Desktop.ViewModels;
using AIntern.Desktop.Views;

namespace AIntern.Desktop;

/// <summary>
/// The main Avalonia application class.
/// Responsible for initializing the DI container, creating the main window,
/// and handling application lifecycle events.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the application's service provider for dependency injection.
    /// Static to allow access from anywhere in the application.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Initializes the application by loading XAML resources.
    /// Called before OnFrameworkInitializationCompleted.
    /// </summary>
    public override void Initialize()
    {
        // Load App.axaml resources (styles, themes, etc.)
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Called when the Avalonia framework initialization is complete.
    /// Sets up the DI container, creates the main window, and subscribes to lifecycle events.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        // CommunityToolkit.MVVM adds its own data validator, which conflicts with Avalonia's
        // built-in DataAnnotationsValidationPlugin. Remove duplicates to prevent double validation.
        // See: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation
        var dataValidationPlugins = BindingPlugins.DataValidators;
        for (var i = dataValidationPlugins.Count - 1; i >= 0; i--)
        {
            if (dataValidationPlugins[i] is DataAnnotationsValidationPlugin)
            {
                dataValidationPlugins.RemoveAt(i);
            }
        }

        // Build the dependency injection container with all services and ViewModels
        var services = new ServiceCollection();
        services.AddAInternServices();
        Services = services.BuildServiceProvider();

        // Initialize the database (apply migrations and seed data).
        // This must happen before the main window opens to ensure data is available.
        // Using GetAwaiter().GetResult() is safe here because this runs before
        // the Avalonia dispatcher is fully active.
        InitializeDatabaseAsync().GetAwaiter().GetResult();

        // Configure the desktop application lifetime
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Create the main window with its ViewModel injected from DI
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };

            // Subscribe to shutdown event for cleanup
            desktop.ShutdownRequested += OnShutdownRequested;
        }

        // Call base implementation to complete initialization
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Initializes the database by applying pending migrations and seeding default data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is called during application startup before the main window is shown.
    /// It ensures the database schema is up-to-date and that required seed data
    /// (system prompts, inference presets) exists.
    /// </para>
    /// <para>
    /// If initialization fails, the error is logged but the application continues.
    /// This prevents the app from crashing due to database issues while still
    /// making the problem visible in logs for debugging.
    /// </para>
    /// </remarks>
    private static async Task InitializeDatabaseAsync()
    {
        var logger = Services.GetRequiredService<ILogger<App>>();

        try
        {
            // Create a scope to properly handle scoped services like DbContext.
            // The scope ensures the DbContext is disposed after initialization.
            using var scope = Services.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

            await initializer.InitializeAsync();

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (DatabaseInitializationException ex)
        {
            // Log the failure but don't crash - allow app to start even if DB has issues.
            // User will see errors when trying to use features that require the database.
            logger.LogError(
                ex,
                "Database initialization failed: {Message}",
                ex.Message);
        }
        catch (Exception ex)
        {
            // Catch any unexpected errors during initialization.
            logger.LogError(
                ex,
                "Unexpected error during database initialization: {Message}",
                ex.Message);
        }
    }

    /// <summary>
    /// Handles the application shutdown event.
    /// Disposes of the LLM service and saves settings.
    /// </summary>
    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        // Release LLM resources (unload model, free VRAM)
        var llmService = Services.GetRequiredService<ILlmService>();
        await llmService.DisposeAsync();

        // Persist current settings to disk for next session
        var settingsService = Services.GetRequiredService<ISettingsService>();
        await settingsService.SaveSettingsAsync(settingsService.CurrentSettings);
    }
}
