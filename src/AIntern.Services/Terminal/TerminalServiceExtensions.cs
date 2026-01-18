using Microsoft.Extensions.DependencyInjection;
using AIntern.Core.Interfaces;

namespace AIntern.Services.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TERMINAL SERVICE EXTENSIONS (v0.5.1f)                                   │
// │ Extension methods for registering terminal services with DI.            │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Extension methods for registering terminal services.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1f.</para>
/// <para>
/// This class provides a single point of registration for all terminal
/// subsystem services. Call <see cref="AddTerminalServices"/> from
/// your application's service configuration to enable terminal functionality.
/// </para>
/// </remarks>
public static class TerminalServiceExtensions
{
    /// <summary>
    /// Adds terminal services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This registers the following services as singletons:
    /// <list type="bullet">
    ///   <item><see cref="IShellDetectionService"/> → <see cref="ShellDetectionService"/></item>
    ///   <item><see cref="ITerminalService"/> → <see cref="TerminalService"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Why Singletons?</b>
    /// <list type="bullet">
    ///   <item>
    ///     <b>ShellDetectionService:</b> Caches detection results for the application
    ///     lifetime. Re-detecting shells on every request would be wasteful.
    ///   </item>
    ///   <item>
    ///     <b>TerminalService:</b> Maintains a registry of active terminal sessions
    ///     that must be shared across the application. Each session manages a PTY
    ///     process that should not be duplicated.
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b>
    /// <code>
    /// // In your application startup
    /// services.AddTerminalServices();
    /// </code>
    /// </para>
    /// <para>
    /// <b>Dependencies:</b>
    /// These services require logging infrastructure. Ensure logging is configured
    /// before adding terminal services:
    /// <code>
    /// services.AddLogging(builder => builder.AddSerilog());
    /// services.AddTerminalServices();
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Full integration in App.axaml.cs
    /// public static IServiceCollection AddAInternServices(this IServiceCollection services)
    /// {
    ///     // ... other services ...
    ///     services.AddTerminalServices();
    ///     return services;
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddTerminalServices(this IServiceCollection services)
    {
        // ─────────────────────────────────────────────────────────────────────
        // SHELL DETECTION SERVICE
        // ─────────────────────────────────────────────────────────────────────
        //
        // Detects available shells on Windows, macOS, and Linux.
        // Features:
        //   • Platform-specific detection strategies
        //   • Result caching for performance
        //   • Version detection via --version
        //   • Executable validation with timeouts
        //
        // The service is registered as a singleton because:
        //   1. Shell detection results don't change during app lifetime
        //   2. Caching is internal and shared across all consumers
        //   3. ITerminalService depends on it for session creation
        //
        services.AddSingleton<IShellDetectionService, ShellDetectionService>();

        // ─────────────────────────────────────────────────────────────────────
        // TERMINAL SERVICE
        // ─────────────────────────────────────────────────────────────────────
        //
        // Manages terminal session lifecycle and PTY I/O.
        // Features:
        //   • Session creation with shell detection
        //   • PTY I/O routing via Pty.Net
        //   • Input/output events for UI binding
        //   • Session state tracking
        //   • Graceful session termination
        //
        // The service is registered as a singleton because:
        //   1. Sessions must be shared across the application
        //   2. Each session wraps a unique PTY process
        //   3. The service tracks the active session for focus management
        //
        services.AddSingleton<ITerminalService, TerminalService>();

        return services;
    }
}
