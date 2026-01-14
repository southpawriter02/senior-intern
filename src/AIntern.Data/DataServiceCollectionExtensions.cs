using AIntern.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIntern.Data;

/// <summary>
/// Extension methods for registering data layer services with dependency injection.
/// </summary>
/// <remarks>
/// <para>
/// This class provides extension methods for <see cref="IServiceCollection"/>
/// to register all data layer components with a single method call.
/// </para>
/// <para>
/// <b>Registered Services:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="AInternDbContext"/> - Scoped (one per request)</description></item>
///   <item><description><see cref="IConversationRepository"/> - Scoped</description></item>
///   <item><description><see cref="ISystemPromptRepository"/> - Scoped</description></item>
///   <item><description><see cref="IInferencePresetRepository"/> - Scoped</description></item>
///   <item><description><see cref="IWorkspaceRepository"/> - Scoped (v0.3.1e-f)</description></item>
///   <item><description><see cref="DatabaseInitializer"/> - Scoped</description></item>
/// </list>
/// <para>
/// All services are registered with scoped lifetime to ensure proper DbContext
/// lifecycle management and avoid threading issues.
/// </para>
/// </remarks>
public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Adds the data layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers all data layer services with production configuration:
    /// SQLite database at the standard application data path.
    /// </para>
    /// <para>
    /// <b>Debug Configuration:</b> In debug builds, enables sensitive data logging
    /// and detailed error messages for easier troubleshooting.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddDataServices();
    /// </code>
    /// </example>
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        // Register DatabasePathResolver as a singleton.
        // This creates the instance early, ensuring directories exist,
        // and provides consistent paths throughout the application lifetime.
        services.AddSingleton<DatabasePathResolver>();

        // Register DbContext with SQLite configuration.
        // We use a factory pattern to resolve the path resolver at runtime.
        services.AddDbContext<AInternDbContext>((serviceProvider, options) =>
        {
            var pathResolver = serviceProvider.GetRequiredService<DatabasePathResolver>();

            options.UseSqlite(pathResolver.ConnectionString, sqliteOptions =>
            {
                // Set a reasonable command timeout.
                // 30 seconds is enough for most operations but not so long
                // that users wait forever if something goes wrong.
                sqliteOptions.CommandTimeout(30);
            });

#if DEBUG
            // Enable detailed logging in debug builds.
            // This helps with troubleshooting but exposes potentially
            // sensitive data in logs, so only enabled in debug.
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();

            // Route EF Core logs through the application's logging system.
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            if (loggerFactory is not null)
            {
                options.UseLoggerFactory(loggerFactory);
            }
#endif
        });

        // Register repositories with scoped lifetime.
        // Scoped ensures they share the same DbContext instance within a request
        // but get a fresh instance for each new request.
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<ISystemPromptRepository, SystemPromptRepository>();
        services.AddScoped<IInferencePresetRepository, InferencePresetRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>(); // v0.3.1e-f

        // Register the database initializer.
        // This is called during application startup to apply migrations and seed data.
        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    /// <summary>
    /// Adds the data layer services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure DbContext options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this overload for testing with in-memory SQLite or custom configuration.
    /// The custom configuration replaces the default SQLite setup.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // For testing with in-memory SQLite database
    /// services.AddDataServices(options =>
    ///     options.UseSqlite("DataSource=:memory:"));
    /// </code>
    /// </example>
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Register DbContext with custom options.
        // This allows tests to use in-memory databases or custom configurations.
        services.AddDbContext<AInternDbContext>(configureOptions);

        // Register repositories (same as production).
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<ISystemPromptRepository, SystemPromptRepository>();
        services.AddScoped<IInferencePresetRepository, InferencePresetRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>(); // v0.3.1e-f

        // Register the database initializer.
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
