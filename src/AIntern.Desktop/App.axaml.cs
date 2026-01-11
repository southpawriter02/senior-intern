using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SeniorIntern.Core.Interfaces;
using SeniorIntern.Desktop.Extensions;
using SeniorIntern.Desktop.ViewModels;
using SeniorIntern.Desktop.Views;

namespace SeniorIntern.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Remove duplicate data validators when using CommunityToolkit
        // https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
        var dataValidationPlugins = BindingPlugins.DataValidators;
        for (var i = dataValidationPlugins.Count - 1; i >= 0; i--)
        {
            if (dataValidationPlugins[i] is DataAnnotationsValidationPlugin)
            {
                dataValidationPlugins.RemoveAt(i);
            }
        }

        // Build DI container
        var services = new ServiceCollection();
        services.AddSeniorInternServices();
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };

            desktop.ShutdownRequested += OnShutdownRequested;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        // Clean up LLM resources
        var llmService = Services.GetRequiredService<ILlmService>();
        await llmService.DisposeAsync();

        // Save settings
        var settingsService = Services.GetRequiredService<ISettingsService>();
        await settingsService.SaveSettingsAsync(settingsService.CurrentSettings);
    }
}
