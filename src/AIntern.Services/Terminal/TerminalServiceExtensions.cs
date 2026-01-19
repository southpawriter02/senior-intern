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
    ///   <item><see cref="ITerminalSearchService"/> → <see cref="TerminalSearchService"/> (v0.5.5b)</item>
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
    ///   <item>
    ///     <b>TerminalSearchService:</b> Stateless service with compiled regex patterns.
    ///     Singleton for consistent logging and configuration.
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

        // ─────────────────────────────────────────────────────────────────────
        // COMMAND EXTRACTOR SERVICE (v0.5.4b)
        // ─────────────────────────────────────────────────────────────────────
        //
        // Extracts executable commands from AI-generated markdown.
        // Features:
        //   • Fenced code block parsing with language detection
        //   • Shell type mapping (bash, powershell, cmd, etc.)
        //   • Heuristic command detection for unlabeled blocks
        //   • Dangerous command pattern detection (12 patterns)
        //   • Inline command extraction after indicator phrases
        //   • Confidence scoring (0.40-0.95)
        //
        // Registered as singleton because:
        //   1. Stateless service with no per-request data
        //   2. Static pattern dictionaries shared across all requests
        //   3. Source-generated regex patterns compiled once
        //
        services.AddSingleton<ICommandExtractorService, CommandExtractorService>();

        // ─────────────────────────────────────────────────────────────────────
        // COMMAND EXECUTION SERVICE (v0.5.4c)
        // ─────────────────────────────────────────────────────────────────────
        //
        // Executes commands in terminal sessions with status tracking.
        // Features:
        //   • Clipboard integration for copy operations
        //   • Send-to-terminal without execute (user presses Enter)
        //   • Execute with Enter key (command + newline)
        //   • Sequential multi-command execution
        //   • Execution cancellation via SIGINT
        //   • Status tracking with event notifications
        //   • Session management with shell type preferences
        //
        // Registered as singleton because:
        //   1. Maintains status tracking dictionary across requests
        //   2. Shares session reuse logic across the application
        //   3. Event subscribers expect consistent source
        //
        services.AddSingleton<ICommandExecutionService, CommandExecutionService>();

        // ─────────────────────────────────────────────────────────────────────
        // OUTPUT CAPTURE SERVICE (v0.5.4d)
        // ─────────────────────────────────────────────────────────────────────
        //
        // Captures terminal output for AI context with processing.
        // Features:
        //   • Stream capture during command execution
        //   • On-demand buffer capture (full, last N lines, selection)
        //   • ANSI escape sequence stripping
        //   • Line ending normalization
        //   • Intelligent truncation (KeepStart, KeepEnd, KeepBoth)
        //   • Per-session capture history with automatic pruning
        //
        // Registered as singleton because:
        //   1. Subscribes to terminal output events at startup
        //   2. Maintains capture history across requests
        //   3. Must be same instance that started capture to stop it
        //
        services.AddSingleton<IOutputCaptureService, OutputCaptureService>();

        // ─────────────────────────────────────────────────────────────────────
        // TERMINAL SEARCH SERVICE (v0.5.5b)
        // ─────────────────────────────────────────────────────────────────────
        //
        // Searches terminal buffer content with pattern matching and navigation.
        // Features:
        //   • Plain text and regex pattern matching
        //   • Case-sensitive and case-insensitive modes
        //   • Background thread execution for responsive UI
        //   • Cancellation support for long-running searches
        //   • Result navigation with wrap-around
        //   • Viewport filtering for rendering optimization
        //   • Regex pattern validation with timeout protection
        //
        // Registered as singleton because:
        //   1. Stateless service - no per-request data
        //   2. Consistent logging configuration
        //   3. Compiled regex patterns can be reused
        //
        services.AddSingleton<ITerminalSearchService, TerminalSearchService>();

        // ─────────────────────────────────────────────────────────────────────
        // KEYBOARD SHORTCUT SERVICE (v0.5.5d)
        // ─────────────────────────────────────────────────────────────────────
        //
        // Manages terminal keyboard shortcuts with registry and customization.
        // Features:
        //   • 35+ default bindings across 6 categories
        //   • Custom binding management with persistence
        //   • Conflict detection and resolution
        //   • PTY pass-through for shell shortcuts (Ctrl+C, Ctrl+Z, etc.)
        //   • Platform-aware key formatting (⌘ on macOS, Ctrl on others)
        //   • Category-based organization for settings UI
        //
        // Registered as singleton because:
        //   1. Bindings should be consistent across the application
        //   2. Settings persistence requires single source of truth
        //   3. BindingsChanged event requires stable instance
        //
        services.AddSingleton<ITerminalShortcutService, TerminalShortcutService>();

        // ─────────────────────────────────────────────────────────────────────
        // FONT SERVICE (v0.5.5e)
        // ─────────────────────────────────────────────────────────────────────
        //
        // Detects and manages available system fonts for terminal use.
        // Features:
        //   • Lazy detection of available monospace fonts
        //   • Platform-specific font recommendations
        //   • Font fallback chain resolution
        //   • Caching for performance
        //
        // Registered as singleton because:
        //   1. Font detection is expensive (system API calls)
        //   2. Results are cached and don't change during runtime
        //   3. Single source of truth for font availability
        //
        services.AddSingleton<IFontService, FontService>();

        return services;
    }
}
