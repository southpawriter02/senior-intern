using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIntern.Data;

/// <summary>
/// Resolves cross-platform file system paths for the SQLite database and application data.
/// Follows platform conventions: Windows uses AppData, macOS uses Application Support,
/// and Linux follows the XDG Base Directory Specification.
/// </summary>
/// <remarks>
/// <para>
/// This class provides consistent, platform-appropriate paths for storing application data
/// including the SQLite database file. The resolver automatically creates the application
/// data directory if it does not exist, ensuring a smooth first-run experience.
/// </para>
/// <para>
/// Database locations by platform:
/// </para>
/// <list type="table">
/// <listheader>
///   <term>Platform</term>
///   <description>Database Path</description>
/// </listheader>
/// <item>
///   <term>Windows</term>
///   <description><c>%APPDATA%\AIntern\aintern.db</c> (e.g., C:\Users\{user}\AppData\Roaming\AIntern\aintern.db)</description>
/// </item>
/// <item>
///   <term>macOS</term>
///   <description><c>~/Library/Application Support/AIntern/aintern.db</c></description>
/// </item>
/// <item>
///   <term>Linux</term>
///   <description><c>$XDG_DATA_HOME/AIntern/aintern.db</c> or <c>~/.local/share/AIntern/aintern.db</c> if XDG_DATA_HOME is not set</description>
/// </item>
/// </list>
/// <para>
/// The XDG Base Directory Specification is followed on Linux to ensure compatibility with
/// standard Linux desktop environments and user expectations. See
/// <see href="https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html">XDG Base Directory Specification</see>
/// for more information.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a resolver instance (automatically creates app data directory)
/// var resolver = new DatabasePathResolver();
///
/// // Get the connection string for Entity Framework Core
/// var connectionString = resolver.ConnectionString;
/// // Returns: "Data Source=/Users/{user}/Library/Application Support/AIntern/aintern.db" (on macOS)
///
/// // Access individual path components
/// Console.WriteLine($"App Data: {resolver.AppDataDirectory}");
/// Console.WriteLine($"Database: {resolver.DatabasePath}");
/// </code>
/// </example>
public sealed class DatabasePathResolver
{
    #region Constants

    /// <summary>
    /// The application name used for creating the data directory.
    /// This name is used as the folder name in the platform-specific app data location.
    /// </summary>
    private const string AppName = "AIntern";

    /// <summary>
    /// The SQLite database filename.
    /// </summary>
    private const string DatabaseFileName = "aintern.db";

    /// <summary>
    /// The XDG environment variable name for user data directory on Linux.
    /// Per XDG spec, this defines the base directory for user-specific data files.
    /// </summary>
    private const string XdgDataHomeEnvVar = "XDG_DATA_HOME";

    /// <summary>
    /// The default XDG data directory path relative to home directory.
    /// Used when XDG_DATA_HOME environment variable is not set.
    /// Per XDG spec, defaults to $HOME/.local/share
    /// </summary>
    private const string XdgDataHomeDefault = ".local/share";

    /// <summary>
    /// The subdirectory name for storing log files within the app data directory.
    /// </summary>
    private const string LogsDirectoryName = "logs";

    /// <summary>
    /// The subdirectory name for storing backup files within the app data directory.
    /// </summary>
    private const string BackupsDirectoryName = "backups";

    #endregion

    #region Fields

    /// <summary>
    /// Logger instance for diagnostic output.
    /// </summary>
    private readonly ILogger<DatabasePathResolver> _logger;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the full path to the application data directory.
    /// The directory is created automatically if it does not exist.
    /// </summary>
    /// <value>
    /// The platform-specific application data directory path.
    /// </value>
    /// <example>
    /// <list type="bullet">
    /// <item><description>Windows: <c>C:\Users\{user}\AppData\Roaming\AIntern</c></description></item>
    /// <item><description>macOS: <c>/Users/{user}/Library/Application Support/AIntern</c></description></item>
    /// <item><description>Linux: <c>/home/{user}/.local/share/AIntern</c></description></item>
    /// </list>
    /// </example>
    public string AppDataDirectory { get; }

    /// <summary>
    /// Gets the full path to the SQLite database file.
    /// </summary>
    /// <value>
    /// The complete file path including directory and filename.
    /// The file may not exist yet if the database has not been created.
    /// </value>
    public string DatabasePath { get; }

    /// <summary>
    /// Gets the full path to the logs directory.
    /// The directory is created automatically if it does not exist.
    /// </summary>
    /// <value>
    /// The path to the logs subdirectory within the app data directory.
    /// </value>
    public string LogsDirectory { get; }

    /// <summary>
    /// Gets the full path to the backups directory.
    /// The directory is created automatically if it does not exist.
    /// </summary>
    /// <value>
    /// The path to the backups subdirectory within the app data directory.
    /// </value>
    public string BackupsDirectory { get; }

    /// <summary>
    /// Gets the EF Core connection string for the SQLite database.
    /// </summary>
    /// <value>
    /// A connection string in the format <c>Data Source={DatabasePath}</c>.
    /// This can be used directly with <see cref="Microsoft.EntityFrameworkCore.DbContextOptionsBuilder"/>.
    /// </value>
    /// <example>
    /// <code>
    /// var resolver = new DatabasePathResolver();
    /// services.AddDbContext&lt;AppDbContext&gt;(options =&gt;
    ///     options.UseSqlite(resolver.ConnectionString));
    /// </code>
    /// </example>
    public string ConnectionString => $"Data Source={DatabasePath}";

    /// <summary>
    /// Gets a value indicating whether the database file exists.
    /// </summary>
    /// <value>
    /// <c>true</c> if the database file exists on disk; otherwise, <c>false</c>.
    /// </value>
    public bool DatabaseExists => File.Exists(DatabasePath);

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabasePathResolver"/> class.
    /// Determines the appropriate data directory based on the current platform
    /// and ensures the directory structure exists.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when running on an unsupported operating system (not Windows, macOS, or Linux).
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the application does not have permission to create the data directory.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when an I/O error occurs while creating the data directory.
    /// </exception>
    /// <remarks>
    /// The constructor automatically creates the following directory structure:
    /// <list type="bullet">
    /// <item><description>Application data directory (e.g., <c>~/.local/share/AIntern</c>)</description></item>
    /// <item><description>Logs subdirectory (e.g., <c>~/.local/share/AIntern/logs</c>)</description></item>
    /// <item><description>Backups subdirectory (e.g., <c>~/.local/share/AIntern/backups</c>)</description></item>
    /// </list>
    /// </remarks>
    public DatabasePathResolver() : this(NullLogger<DatabasePathResolver>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabasePathResolver"/> class with logging support.
    /// Determines the appropriate data directory based on the current platform
    /// and ensures the directory structure exists.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when running on an unsupported operating system (not Windows, macOS, or Linux).
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the application does not have permission to create the data directory.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when an I/O error occurs while creating the data directory.
    /// </exception>
    public DatabasePathResolver(ILogger<DatabasePathResolver> logger)
    {
        _logger = logger ?? NullLogger<DatabasePathResolver>.Instance;

        _logger.LogDebug("Initializing DatabasePathResolver");

        // Determine platform-specific base directory
        AppDataDirectory = GetPlatformDataDirectory();
        _logger.LogInformation("Resolved application data directory: {AppDataDirectory}", AppDataDirectory);

        // Build paths for subdirectories and database
        DatabasePath = Path.Combine(AppDataDirectory, DatabaseFileName);
        LogsDirectory = Path.Combine(AppDataDirectory, LogsDirectoryName);
        BackupsDirectory = Path.Combine(AppDataDirectory, BackupsDirectoryName);

        _logger.LogDebug("Database path: {DatabasePath}", DatabasePath);
        _logger.LogDebug("Logs directory: {LogsDirectory}", LogsDirectory);
        _logger.LogDebug("Backups directory: {BackupsDirectory}", BackupsDirectory);

        // Ensure all directories exist (no-op if already exists)
        EnsureDirectoriesExist();

        _logger.LogInformation(
            "DatabasePathResolver initialized successfully. Database exists: {DatabaseExists}",
            DatabaseExists);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Generates a timestamped backup file path for the database.
    /// </summary>
    /// <returns>
    /// A full path to a backup file with timestamp suffix.
    /// The file does not exist yet; this method only generates the path.
    /// </returns>
    /// <example>
    /// <code>
    /// var resolver = new DatabasePathResolver();
    /// var backupPath = resolver.GetBackupPath();
    /// // Returns: "/Users/{user}/Library/Application Support/AIntern/backups/aintern_backup_20250112_143022.db"
    ///
    /// // Copy current database to backup location
    /// if (resolver.DatabaseExists)
    /// {
    ///     File.Copy(resolver.DatabasePath, backupPath);
    /// }
    /// </code>
    /// </example>
    public string GetBackupPath()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"aintern_backup_{timestamp}.db";
        var backupPath = Path.Combine(BackupsDirectory, backupFileName);

        _logger.LogDebug("Generated backup path: {BackupPath}", backupPath);

        return backupPath;
    }

    /// <summary>
    /// Gets the path to a log file for the specified date.
    /// </summary>
    /// <param name="date">The date for which to generate the log file path. Defaults to today.</param>
    /// <returns>
    /// A full path to a log file for the specified date.
    /// </returns>
    /// <example>
    /// <code>
    /// var resolver = new DatabasePathResolver();
    /// var logPath = resolver.GetLogFilePath();
    /// // Returns: "/Users/{user}/Library/Application Support/AIntern/logs/aintern_20250112.log"
    /// </code>
    /// </example>
    public string GetLogFilePath(DateTime? date = null)
    {
        var logDate = date ?? DateTime.Now;
        var logFileName = $"aintern_{logDate:yyyyMMdd}.log";
        var logPath = Path.Combine(LogsDirectory, logFileName);

        _logger.LogDebug("Generated log file path for {Date}: {LogPath}", logDate.Date, logPath);

        return logPath;
    }

    #endregion

    #region Private Methods - Platform Detection

    /// <summary>
    /// Determines the platform-appropriate data directory.
    /// </summary>
    /// <returns>The full path to the application data directory.</returns>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when the current platform is not Windows, macOS, or Linux.
    /// </exception>
    private string GetPlatformDataDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            _logger.LogDebug("Detected Windows platform");
            return GetWindowsDataDirectory();
        }

        if (OperatingSystem.IsMacOS())
        {
            _logger.LogDebug("Detected macOS platform");
            return GetMacOsDataDirectory();
        }

        if (OperatingSystem.IsLinux())
        {
            _logger.LogDebug("Detected Linux platform");
            return GetLinuxDataDirectory();
        }

        _logger.LogError("Unsupported operating system detected");
        throw new PlatformNotSupportedException(
            $"AIntern only supports Windows, macOS, and Linux operating systems. " +
            $"Current platform is not supported.");
    }

    /// <summary>
    /// Gets the data directory path for Windows.
    /// Uses the Roaming AppData folder (%APPDATA%).
    /// </summary>
    /// <returns>The Windows application data directory path.</returns>
    /// <remarks>
    /// Windows path: <c>C:\Users\{user}\AppData\Roaming\AIntern</c>
    ///
    /// The Roaming folder is used (rather than Local) to support scenarios where
    /// users may want their data to follow their profile across machines in
    /// enterprise environments.
    /// </remarks>
    private string GetWindowsDataDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var path = Path.Combine(appData, AppName);

        _logger.LogDebug(
            "Windows data directory resolved using ApplicationData special folder. Base: {AppData}, Full: {Path}",
            appData, path);

        return path;
    }

    /// <summary>
    /// Gets the data directory path for macOS.
    /// Uses ~/Library/Application Support following Apple's guidelines.
    /// </summary>
    /// <returns>The macOS application data directory path.</returns>
    /// <remarks>
    /// macOS path: <c>/Users/{user}/Library/Application Support/AIntern</c>
    ///
    /// This follows Apple's File System Programming Guide for storing
    /// application-specific data files.
    /// </remarks>
    private string GetMacOsDataDirectory()
    {
        // On macOS, SpecialFolder.ApplicationData maps to ~/Library/Application Support
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var path = Path.Combine(appData, AppName);

        _logger.LogDebug(
            "macOS data directory resolved using ApplicationData special folder. Base: {AppData}, Full: {Path}",
            appData, path);

        return path;
    }

    /// <summary>
    /// Gets the data directory path for Linux following XDG Base Directory Specification.
    /// Uses $XDG_DATA_HOME if set, otherwise falls back to ~/.local/share.
    /// </summary>
    /// <returns>The Linux application data directory path.</returns>
    /// <remarks>
    /// <para>
    /// Per the XDG Base Directory Specification:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <c>$XDG_DATA_HOME</c> defines the base directory for user-specific data files.
    /// If set, the path will be <c>$XDG_DATA_HOME/AIntern</c>.
    /// </description></item>
    /// <item><description>
    /// If <c>$XDG_DATA_HOME</c> is not set or empty, it defaults to <c>$HOME/.local/share</c>,
    /// resulting in <c>~/.local/share/AIntern</c>.
    /// </description></item>
    /// </list>
    /// <para>
    /// See <see href="https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html">
    /// XDG Base Directory Specification</see> for more information.
    /// </para>
    /// </remarks>
    private string GetLinuxDataDirectory()
    {
        // Check for XDG_DATA_HOME environment variable
        var xdgDataHome = Environment.GetEnvironmentVariable(XdgDataHomeEnvVar);

        if (!string.IsNullOrWhiteSpace(xdgDataHome))
        {
            // Use XDG_DATA_HOME if set: $XDG_DATA_HOME/AIntern
            var path = Path.Combine(xdgDataHome, AppName);

            _logger.LogDebug(
                "Linux data directory resolved using XDG_DATA_HOME environment variable. " +
                "XDG_DATA_HOME: {XdgDataHome}, Full: {Path}",
                xdgDataHome, path);

            return path;
        }

        // Fallback to XDG default: ~/.local/share/AIntern
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var fallbackPath = Path.Combine(homeDir, XdgDataHomeDefault, AppName);

        _logger.LogDebug(
            "Linux data directory resolved using XDG default fallback. " +
            "XDG_DATA_HOME not set, using: {HomeDir}/{Default}/{AppName} = {Path}",
            homeDir, XdgDataHomeDefault, AppName, fallbackPath);

        return fallbackPath;
    }

    #endregion

    #region Private Methods - Directory Management

    /// <summary>
    /// Ensures all required directories exist in the app data structure.
    /// Creates directories if they do not exist.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the application does not have permission to create directories.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when an I/O error occurs while creating directories.
    /// </exception>
    private void EnsureDirectoriesExist()
    {
        _logger.LogDebug("Ensuring required directories exist");

        try
        {
            // CreateDirectory is a no-op if directory already exists
            if (!Directory.Exists(AppDataDirectory))
            {
                Directory.CreateDirectory(AppDataDirectory);
                _logger.LogInformation("Created application data directory: {AppDataDirectory}", AppDataDirectory);
            }
            else
            {
                _logger.LogDebug("Application data directory already exists: {AppDataDirectory}", AppDataDirectory);
            }

            if (!Directory.Exists(LogsDirectory))
            {
                Directory.CreateDirectory(LogsDirectory);
                _logger.LogInformation("Created logs directory: {LogsDirectory}", LogsDirectory);
            }
            else
            {
                _logger.LogDebug("Logs directory already exists: {LogsDirectory}", LogsDirectory);
            }

            if (!Directory.Exists(BackupsDirectory))
            {
                Directory.CreateDirectory(BackupsDirectory);
                _logger.LogInformation("Created backups directory: {BackupsDirectory}", BackupsDirectory);
            }
            else
            {
                _logger.LogDebug("Backups directory already exists: {BackupsDirectory}", BackupsDirectory);
            }

            _logger.LogDebug("All required directories verified");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex,
                "Permission denied when creating directories. AppDataDirectory: {AppDataDirectory}",
                AppDataDirectory);
            throw;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex,
                "I/O error when creating directories. AppDataDirectory: {AppDataDirectory}",
                AppDataDirectory);
            throw;
        }
    }

    #endregion
}
