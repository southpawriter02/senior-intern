using Microsoft.Extensions.DependencyInjection;
using SeniorIntern.Core.Interfaces;
using SeniorIntern.Desktop.ViewModels;
using SeniorIntern.Services;

namespace SeniorIntern.Desktop.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSeniorInternServices(this IServiceCollection services)
    {
        // Core services (singletons - shared state)
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILlmService, LlmService>();
        services.AddSingleton<IConversationService, ConversationService>();

        // ViewModels (transient - created as needed)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<ModelSelectorViewModel>();

        return services;
    }
}
