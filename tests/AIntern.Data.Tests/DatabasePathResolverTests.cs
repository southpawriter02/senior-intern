using Xunit;

namespace AIntern.Data.Tests;

/// <summary>
/// Unit tests for the <see cref="DatabasePathResolver"/> class.
/// Verifies correct path resolution across platforms, XDG compliance on Linux,
/// directory creation, and connection string formatting.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the DatabasePathResolver correctly handles:
/// </para>
/// <list type="bullet">
/// <item><description>Platform-specific path resolution (Windows, macOS, Linux)</description></item>
/// <item><description>XDG Base Directory Specification compliance on Linux</description></item>
/// <item><description>Automatic directory creation</description></item>
/// <item><description>Connection string formatting for Entity Framework Core</description></item>
/// <item><description>Backup and log file path generation</description></item>
/// </list>
/// <para>
/// Platform-specific tests are skipped when not running on the target platform
/// to allow the test suite to pass on all development machines.
/// </para>
/// </remarks>
public class DatabasePathResolverTests
{
    #region Constructor Tests

    /// <summary>
    /// Verifies that the DatabasePathResolver can be instantiated without throwing.
    /// </summary>
    [Fact]
    public void Constructor_DoesNotThrow()
    {
        // Arrange & Act
        var resolver = new DatabasePathResolver();

        // Assert
        Assert.NotNull(resolver);
    }

    /// <summary>
    /// Verifies that the constructor creates the app data directory.
    /// </summary>
    [Fact]
    public void Constructor_CreatesAppDataDirectory()
    {
        // Arrange & Act
        var resolver = new DatabasePathResolver();

        // Assert
        Assert.True(Directory.Exists(resolver.AppDataDirectory),
            $"Expected directory to exist: {resolver.AppDataDirectory}");
    }

    /// <summary>
    /// Verifies that the constructor creates the logs subdirectory.
    /// </summary>
    [Fact]
    public void Constructor_CreatesLogsDirectory()
    {
        // Arrange & Act
        var resolver = new DatabasePathResolver();

        // Assert
        Assert.True(Directory.Exists(resolver.LogsDirectory),
            $"Expected logs directory to exist: {resolver.LogsDirectory}");
    }

    /// <summary>
    /// Verifies that the constructor creates the backups subdirectory.
    /// </summary>
    [Fact]
    public void Constructor_CreatesBackupsDirectory()
    {
        // Arrange & Act
        var resolver = new DatabasePathResolver();

        // Assert
        Assert.True(Directory.Exists(resolver.BackupsDirectory),
            $"Expected backups directory to exist: {resolver.BackupsDirectory}");
    }

    #endregion

    #region AppDataDirectory Tests

    /// <summary>
    /// Verifies that the AppDataDirectory property returns a non-empty path.
    /// </summary>
    [Fact]
    public void AppDataDirectory_ReturnsNonEmptyPath()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var appDataDir = resolver.AppDataDirectory;

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(appDataDir),
            "AppDataDirectory should not be null or empty");
    }

    /// <summary>
    /// Verifies that the AppDataDirectory ends with the application name.
    /// </summary>
    [Fact]
    public void AppDataDirectory_EndsWithAIntern()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var appDataDir = resolver.AppDataDirectory;

        // Assert
        Assert.EndsWith("AIntern", appDataDir);
    }

    /// <summary>
    /// Verifies that the AppDataDirectory is an absolute (rooted) path.
    /// </summary>
    [Fact]
    public void AppDataDirectory_IsAbsolutePath()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var appDataDir = resolver.AppDataDirectory;

        // Assert
        Assert.True(Path.IsPathRooted(appDataDir),
            $"Expected an absolute path, but got: {appDataDir}");
    }

    #endregion

    #region DatabasePath Tests

    /// <summary>
    /// Verifies that DatabasePath returns a non-empty path.
    /// </summary>
    [Fact]
    public void DatabasePath_ReturnsNonEmptyPath()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var dbPath = resolver.DatabasePath;

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(dbPath),
            "DatabasePath should not be null or empty");
    }

    /// <summary>
    /// Verifies that DatabasePath ends with the expected database filename.
    /// </summary>
    [Fact]
    public void DatabasePath_EndsWithDatabaseFileName()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var dbPath = resolver.DatabasePath;

        // Assert
        Assert.EndsWith("aintern.db", dbPath);
    }

    /// <summary>
    /// Verifies that DatabasePath is within the AppDataDirectory.
    /// </summary>
    [Fact]
    public void DatabasePath_IsWithinAppDataDirectory()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var dbPath = resolver.DatabasePath;
        var expectedDir = resolver.AppDataDirectory;
        var actualDir = Path.GetDirectoryName(dbPath);

        // Assert
        Assert.Equal(expectedDir, actualDir);
    }

    /// <summary>
    /// Verifies that DatabasePath is an absolute (rooted) path.
    /// </summary>
    [Fact]
    public void DatabasePath_IsAbsolutePath()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var dbPath = resolver.DatabasePath;

        // Assert
        Assert.True(Path.IsPathRooted(dbPath),
            $"Expected an absolute path, but got: {dbPath}");
    }

    #endregion

    #region ConnectionString Tests

    /// <summary>
    /// Verifies that ConnectionString has the correct SQLite format.
    /// </summary>
    [Fact]
    public void ConnectionString_StartsWithDataSource()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var connectionString = resolver.ConnectionString;

        // Assert
        Assert.StartsWith("Data Source=", connectionString);
    }

    /// <summary>
    /// Verifies that ConnectionString contains the database filename.
    /// </summary>
    [Fact]
    public void ConnectionString_ContainsDatabaseFileName()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var connectionString = resolver.ConnectionString;

        // Assert
        Assert.Contains("aintern.db", connectionString);
    }

    /// <summary>
    /// Verifies that ConnectionString exactly matches the expected format.
    /// </summary>
    [Fact]
    public void ConnectionString_MatchesExpectedFormat()
    {
        // Arrange
        var resolver = new DatabasePathResolver();
        var expectedPath = resolver.DatabasePath;

        // Act
        var connectionString = resolver.ConnectionString;

        // Assert
        Assert.Equal($"Data Source={expectedPath}", connectionString);
    }

    #endregion

    #region LogsDirectory Tests

    /// <summary>
    /// Verifies that LogsDirectory is within the AppDataDirectory.
    /// </summary>
    [Fact]
    public void LogsDirectory_IsWithinAppDataDirectory()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var logsDir = resolver.LogsDirectory;
        var appDataDir = resolver.AppDataDirectory;

        // Assert
        Assert.StartsWith(appDataDir, logsDir);
    }

    /// <summary>
    /// Verifies that LogsDirectory ends with 'logs'.
    /// </summary>
    [Fact]
    public void LogsDirectory_EndsWithLogs()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var logsDir = resolver.LogsDirectory;

        // Assert
        Assert.EndsWith("logs", logsDir);
    }

    #endregion

    #region BackupsDirectory Tests

    /// <summary>
    /// Verifies that BackupsDirectory is within the AppDataDirectory.
    /// </summary>
    [Fact]
    public void BackupsDirectory_IsWithinAppDataDirectory()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var backupsDir = resolver.BackupsDirectory;
        var appDataDir = resolver.AppDataDirectory;

        // Assert
        Assert.StartsWith(appDataDir, backupsDir);
    }

    /// <summary>
    /// Verifies that BackupsDirectory ends with 'backups'.
    /// </summary>
    [Fact]
    public void BackupsDirectory_EndsWithBackups()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var backupsDir = resolver.BackupsDirectory;

        // Assert
        Assert.EndsWith("backups", backupsDir);
    }

    #endregion

    #region GetBackupPath Tests

    /// <summary>
    /// Verifies that GetBackupPath returns a path within the backups directory.
    /// </summary>
    [Fact]
    public void GetBackupPath_IsWithinBackupsDirectory()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var backupPath = resolver.GetBackupPath();
        var backupsDir = resolver.BackupsDirectory;

        // Assert
        Assert.StartsWith(backupsDir, backupPath);
    }

    /// <summary>
    /// Verifies that GetBackupPath returns a path with .db extension.
    /// </summary>
    [Fact]
    public void GetBackupPath_HasDbExtension()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var backupPath = resolver.GetBackupPath();

        // Assert
        Assert.EndsWith(".db", backupPath);
    }

    /// <summary>
    /// Verifies that GetBackupPath includes a timestamp in the filename.
    /// </summary>
    [Fact]
    public void GetBackupPath_ContainsTimestamp()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var backupPath = resolver.GetBackupPath();
        var fileName = Path.GetFileName(backupPath);

        // Assert - should contain "backup" and a date pattern
        Assert.Contains("backup", fileName);
        Assert.Matches(@"aintern_backup_\d{8}_\d{6}\.db", fileName);
    }

    /// <summary>
    /// Verifies that consecutive calls to GetBackupPath return different paths.
    /// </summary>
    [Fact]
    public void GetBackupPath_ReturnsDifferentPathsWhenCalledWithDelay()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var backupPath1 = resolver.GetBackupPath();
        Thread.Sleep(1100); // Wait just over a second for timestamp to change
        var backupPath2 = resolver.GetBackupPath();

        // Assert
        Assert.NotEqual(backupPath1, backupPath2);
    }

    #endregion

    #region GetLogFilePath Tests

    /// <summary>
    /// Verifies that GetLogFilePath returns a path within the logs directory.
    /// </summary>
    [Fact]
    public void GetLogFilePath_IsWithinLogsDirectory()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var logPath = resolver.GetLogFilePath();
        var logsDir = resolver.LogsDirectory;

        // Assert
        Assert.StartsWith(logsDir, logPath);
    }

    /// <summary>
    /// Verifies that GetLogFilePath returns a path with .log extension.
    /// </summary>
    [Fact]
    public void GetLogFilePath_HasLogExtension()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var logPath = resolver.GetLogFilePath();

        // Assert
        Assert.EndsWith(".log", logPath);
    }

    /// <summary>
    /// Verifies that GetLogFilePath includes today's date by default.
    /// </summary>
    [Fact]
    public void GetLogFilePath_ContainsTodaysDate()
    {
        // Arrange
        var resolver = new DatabasePathResolver();
        var today = DateTime.Now.ToString("yyyyMMdd");

        // Act
        var logPath = resolver.GetLogFilePath();

        // Assert
        Assert.Contains(today, logPath);
    }

    /// <summary>
    /// Verifies that GetLogFilePath uses the specified date.
    /// </summary>
    [Fact]
    public void GetLogFilePath_UsesSpecifiedDate()
    {
        // Arrange
        var resolver = new DatabasePathResolver();
        var specificDate = new DateTime(2024, 6, 15);

        // Act
        var logPath = resolver.GetLogFilePath(specificDate);

        // Assert
        Assert.Contains("20240615", logPath);
    }

    #endregion

    #region DatabaseExists Tests

    /// <summary>
    /// Verifies that DatabaseExists returns false when database has not been created.
    /// </summary>
    [Fact]
    public void DatabaseExists_ReturnsFalse_WhenDatabaseNotCreated()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Clean up if test database exists from previous runs
        if (File.Exists(resolver.DatabasePath))
        {
            File.Delete(resolver.DatabasePath);
        }

        // Act
        var exists = resolver.DatabaseExists;

        // Assert
        Assert.False(exists,
            $"Expected DatabaseExists to be false when database file does not exist");
    }

    /// <summary>
    /// Verifies that DatabaseExists returns true when database file exists.
    /// </summary>
    [Fact]
    public void DatabaseExists_ReturnsTrue_WhenDatabaseFileExists()
    {
        // Arrange
        var resolver = new DatabasePathResolver();

        // Create an empty database file for testing
        File.WriteAllText(resolver.DatabasePath, string.Empty);

        try
        {
            // Act
            var exists = resolver.DatabaseExists;

            // Assert
            Assert.True(exists,
                $"Expected DatabaseExists to be true when database file exists");
        }
        finally
        {
            // Cleanup
            if (File.Exists(resolver.DatabasePath))
            {
                File.Delete(resolver.DatabasePath);
            }
        }
    }

    #endregion

    #region Platform-Specific Tests

    /// <summary>
    /// Verifies Windows-specific path conventions when running on Windows.
    /// The path should contain AppData\Roaming.
    /// </summary>
    [Fact]
    [Trait("Platform", "Windows")]
    public void Windows_AppDataDirectory_ContainsAppDataRoaming()
    {
        // Skip on non-Windows platforms
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var appDataDir = resolver.AppDataDirectory;

        // Assert - Windows paths contain AppData\Roaming or AppData/Roaming
        Assert.True(
            appDataDir.Contains("AppData\\Roaming") || appDataDir.Contains("AppData/Roaming"),
            $"Expected Windows path to contain AppData\\Roaming, but got: {appDataDir}");
    }

    /// <summary>
    /// Verifies macOS-specific path conventions when running on macOS.
    /// The path should contain Library/Application Support.
    /// </summary>
    [Fact]
    [Trait("Platform", "macOS")]
    public void MacOS_AppDataDirectory_ContainsApplicationSupport()
    {
        // Skip on non-macOS platforms
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var appDataDir = resolver.AppDataDirectory;

        // Assert
        Assert.Contains("Library/Application Support", appDataDir);
    }

    /// <summary>
    /// Verifies Linux-specific path conventions when running on Linux.
    /// The path should follow XDG specification (contain .local/share or use XDG_DATA_HOME).
    /// </summary>
    [Fact]
    [Trait("Platform", "Linux")]
    public void Linux_AppDataDirectory_FollowsXdgSpec()
    {
        // Skip on non-Linux platforms
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        // Arrange
        var resolver = new DatabasePathResolver();

        // Act
        var appDataDir = resolver.AppDataDirectory;
        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

        // Assert
        if (!string.IsNullOrWhiteSpace(xdgDataHome))
        {
            // If XDG_DATA_HOME is set, path should start with it
            Assert.StartsWith(xdgDataHome, appDataDir);
        }
        else
        {
            // Otherwise, should use ~/.local/share default
            Assert.Contains(".local/share", appDataDir);
        }
    }

    #endregion

    #region Consistency Tests

    /// <summary>
    /// Verifies that multiple instantiations return consistent paths.
    /// </summary>
    [Fact]
    public void MultipleInstances_ReturnConsistentPaths()
    {
        // Arrange
        var resolver1 = new DatabasePathResolver();
        var resolver2 = new DatabasePathResolver();

        // Act & Assert
        Assert.Equal(resolver1.AppDataDirectory, resolver2.AppDataDirectory);
        Assert.Equal(resolver1.DatabasePath, resolver2.DatabasePath);
        Assert.Equal(resolver1.ConnectionString, resolver2.ConnectionString);
        Assert.Equal(resolver1.LogsDirectory, resolver2.LogsDirectory);
        Assert.Equal(resolver1.BackupsDirectory, resolver2.BackupsDirectory);
    }

    /// <summary>
    /// Verifies that all paths use consistent path separators for the current platform.
    /// </summary>
    [Fact]
    public void AllPaths_UseConsistentPathSeparators()
    {
        // Arrange
        var resolver = new DatabasePathResolver();
        var expectedSeparator = Path.DirectorySeparatorChar;

        // Act
        var paths = new[]
        {
            resolver.AppDataDirectory,
            resolver.DatabasePath,
            resolver.LogsDirectory,
            resolver.BackupsDirectory
        };

        // Assert - all paths should be valid and use consistent separators
        foreach (var path in paths)
        {
            Assert.True(Path.IsPathRooted(path), $"Path should be absolute: {path}");
            // Path.GetFullPath normalizes separators
            var normalizedPath = Path.GetFullPath(path);
            Assert.Equal(path, normalizedPath);
        }
    }

    #endregion
}
