using Avalonia;
using Serilog;
using Serilog.Events;

namespace AIntern.Desktop;

/// <summary>
/// Application entry point.
/// Configures Serilog logging and starts the Avalonia application.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// Configures logging sinks and handles global exceptions.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <remarks>
    /// <para>
    /// <b>Console Sink:</b> Outputs to standard output with compact format.
    /// </para>
    /// <para>
    /// <b>File Sink:</b> Rolling daily logs to <c>logs/aintern-{date}.log</c>
    /// with 7-day retention.
    /// </para>
    /// <para>
    /// Log levels are configured to reduce noise from framework libraries:
    /// <list type="bullet">
    /// <item>Default: Debug</item>
    /// <item>Microsoft.*: Information</item>
    /// <item>Avalonia.*: Warning</item>
    /// </list>
    /// </para>
    /// </remarks>
    [STAThread]
    public static void Main(string[] args)
    {
        // Configure Serilog with console and file sinks
        Log.Logger = new LoggerConfiguration()
            // Set base minimum level for all loggers
            .MinimumLevel.Debug()
            // Override noisy framework namespaces
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Avalonia", LogEventLevel.Warning)
            // Add contextual properties to log events
            .Enrich.FromLogContext()
            // Console sink: human-readable format for development
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            // File sink: rolling daily logs with retention policy
            .WriteTo.File(
                path: "logs/aintern-.log",
                rollingInterval: RollingInterval.Day,  // Create new file each day
                retainedFileCountLimit: 7,             // Keep last 7 days of logs
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            // Log application startup
            Log.Information("Starting AIntern application");
            
            // Build and run the Avalonia application
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // Log any unhandled exceptions that crash the app
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            // Ensure all log messages are flushed before exit
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Builds and configures the Avalonia application.
    /// </summary>
    /// <returns>A configured <see cref="AppBuilder"/> instance.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()   // Auto-detect Windows/macOS/Linux rendering
            .WithInterFont()       // Use Inter font family
            .LogToTrace();         // Route Avalonia logs to trace listeners
}
