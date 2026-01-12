using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using AIntern.Core.Interfaces;
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
