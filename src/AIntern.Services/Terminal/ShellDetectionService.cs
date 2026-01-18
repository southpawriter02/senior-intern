using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using AIntern.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIntern.Services.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL DETECTION SERVICE (v0.5.3a)                                       │
// │ Cross-platform shell discovery and identification.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Cross-platform shell detection service.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1e. Extended in v0.5.3a.</para>
/// <para>
/// Detects available shells on Windows, macOS, and Linux using
/// platform-specific mechanisms. Features include:
/// <list type="bullet">
///   <item>Automatic default shell detection</item>
///   <item>Version detection via --version</item>
///   <item>Result caching for performance</item>
///   <item>Shell executable validation</item>
/// </list>
/// </para>
/// </remarks>
public sealed partial class ShellDetectionService : IShellDetectionService
{
    // ─────────────────────────────────────────────────────────────────────
    // Constants
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Timeout for shell validation operations (in seconds).
    /// </summary>
    private const int ValidationTimeoutSeconds = 5;

    /// <summary>
    /// Timeout for version detection operations (in seconds).
    /// </summary>
    private const int VersionTimeoutSeconds = 2;

    // ─────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────

    private readonly ILogger<ShellDetectionService> _logger;

    /// <summary>
    /// Cached default shell path.
    /// </summary>
    private string? _cachedDefaultShell;

    /// <summary>
    /// Cached list of available shells.
    /// </summary>
    private IReadOnlyList<ShellInfo>? _cachedShells;

    /// <summary>
    /// Lock for thread-safe cache access.
    /// </summary>
    private readonly object _cacheLock = new();

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new shell detection service.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public ShellDetectionService(ILogger<ShellDetectionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogDebug("ShellDetectionService initialized");
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellDetectionService Implementation - Default Shell
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ShellInfo> DetectDefaultShellAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Detecting default shell for platform: {Platform}",
            RuntimeInformation.OSDescription);

        // Get default shell path first
        var shellPath = await GetDefaultShellAsync(cancellationToken);

        // Get the full list to find the matching ShellInfo
        var availableShells = await GetAvailableShellsAsync(cancellationToken);

        // Find the default shell in the list
        var defaultShell = availableShells.FirstOrDefault(s =>
            NormalizePath(s.Path) == NormalizePath(shellPath));

        if (defaultShell != null)
        {
            _logger.LogInformation("Detected default shell: {Shell} ({Type})",
                defaultShell.Name, defaultShell.ShellType);
            return defaultShell;
        }

        // Create a fallback ShellInfo if not found in list
        var shellType = DetermineShellType(shellPath);
        var fallbackShell = new ShellInfo
        {
            Name = shellType.ToString(),
            Path = shellPath,
            ShellType = shellType,
            DefaultArguments = GetDefaultArguments(shellType),
            IsDefault = true
        };

        _logger.LogInformation("Using fallback default shell: {Path} ({Type})",
            fallbackShell.Path, fallbackShell.ShellType);

        return fallbackShell;
    }

    /// <inheritdoc />
    public async Task<string> GetDefaultShellAsync(CancellationToken cancellationToken = default)
    {
        // Return cached result if available
        lock (_cacheLock)
        {
            if (_cachedDefaultShell != null)
            {
                _logger.LogDebug("Returning cached default shell: {Path}", _cachedDefaultShell);
                return _cachedDefaultShell;
            }
        }

        _logger.LogDebug("Detecting default shell (no cache)");

        string? defaultShell = null;

        // Platform-specific detection
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            defaultShell = await GetWindowsDefaultShellAsync(cancellationToken);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            defaultShell = await GetMacDefaultShellAsync(cancellationToken);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            defaultShell = await GetLinuxDefaultShellAsync(cancellationToken);
        }

        // Use fallback if detection failed
        if (string.IsNullOrEmpty(defaultShell))
        {
            _logger.LogWarning("Could not detect default shell, using platform fallback");
            defaultShell = GetFallbackShell();
        }

        _logger.LogInformation("Default shell detected: {Shell}", defaultShell);

        // Cache the result
        lock (_cacheLock)
        {
            _cachedDefaultShell = defaultShell;
        }

        return defaultShell;
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellDetectionService Implementation - Available Shells
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShellInfo>> GetAvailableShellsAsync(
        CancellationToken cancellationToken = default)
    {
        // Return cached result if available
        lock (_cacheLock)
        {
            if (_cachedShells != null)
            {
                _logger.LogDebug("Returning cached shells list ({Count} shells)",
                    _cachedShells.Count);
                return _cachedShells;
            }
        }

        _logger.LogDebug("Enumerating available shells (no cache)");

        var shells = new List<ShellInfo>();
        var defaultShellPath = await GetDefaultShellAsync(cancellationToken);

        // Platform-specific enumeration
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await AddWindowsShellsAsync(shells, defaultShellPath, cancellationToken);
        }
        else
        {
            await AddUnixShellsAsync(shells, defaultShellPath, cancellationToken);
        }

        // Sort with default shell first, then alphabetically
        shells.Sort((a, b) =>
        {
            if (a.IsDefault && !b.IsDefault) return -1;
            if (!a.IsDefault && b.IsDefault) return 1;
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });

        _logger.LogDebug("Found {Count} available shells", shells.Count);

        // Cache the result
        IReadOnlyList<ShellInfo> result = shells.AsReadOnly();
        lock (_cacheLock)
        {
            _cachedShells = result;
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellDetectionService Implementation - Shell Validation
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<bool> IsShellAvailableAsync(string path, CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogDebug("IsShellAvailableAsync called with null/empty path");
            return false;
        }

        try
        {
            // Check file exists
            if (!File.Exists(path))
            {
                _logger.LogDebug("Shell not found at path: {Path}", path);
                return false;
            }

            // Special case: cmd.exe doesn't have --version
            var fileName = Path.GetFileName(path).ToLowerInvariant();
            if (fileName == "cmd.exe")
            {
                _logger.LogDebug("Skipping version check for cmd.exe");
                return true;
            }

            // Try to execute --version to verify it's a working shell
            _logger.LogDebug("Validating shell at: {Path}", path);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Wait with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(ValidationTimeoutSeconds));

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
                _logger.LogDebug("Shell validation succeeded: {Path}", path);
                return true;
            }
            catch (OperationCanceledException)
            {
                // Timeout or cancellation
                _logger.LogDebug("Shell validation timed out: {Path}", path);
                try { process.Kill(); } catch { /* Ignore kill errors */ }
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking shell availability at: {Path}", path);
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellDetectionService Implementation - v0.5.3a Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ShellType DetectShellType(string shellPath)
    {
        if (string.IsNullOrWhiteSpace(shellPath))
        {
            _logger.LogDebug("DetectShellType called with null/empty path, returning Unknown");
            return ShellType.Unknown;
        }

        var result = DetermineShellType(shellPath);
        _logger.LogDebug("DetectShellType for '{Path}' returned: {Type}", shellPath, result);
        return result;
    }

    /// <inheritdoc />
    public bool ValidateShellPath(string shellPath)
    {
        if (string.IsNullOrWhiteSpace(shellPath))
        {
            _logger.LogDebug("ValidateShellPath called with null/empty path");
            return false;
        }

        try
        {
            // Check that path is to an existing file (not directory)
            var exists = File.Exists(shellPath);
            _logger.LogDebug("ValidateShellPath for '{Path}': {Result}", shellPath, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ValidateShellPath error for '{Path}'", shellPath);
            return false;
        }
    }

    /// <inheritdoc />
    public string? FindInPath(string executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName))
        {
            _logger.LogDebug("FindInPath called with null/empty name");
            return null;
        }

        var result = FindExecutable(executableName);
        _logger.LogDebug("FindInPath for '{Name}': {Result}", executableName, result ?? "(not found)");
        return result;
    }

    /// <inheritdoc />
    public async Task<string?> GetShellVersionAsync(
        string shellPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shellPath))
        {
            _logger.LogDebug("GetShellVersionAsync called with null/empty path");
            return null;
        }

        if (!ValidateShellPath(shellPath))
        {
            _logger.LogDebug("GetShellVersionAsync: path does not exist '{Path}'", shellPath);
            return null;
        }

        return await GetVersionAsync(shellPath, "--version", cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Windows Detection
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the default shell on Windows.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Path to the default shell, or null if not found.</returns>
    private async Task<string?> GetWindowsDefaultShellAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Detecting Windows default shell");

        // Check for PowerShell Core first (preferred)
        var pwshPath = FindExecutable("pwsh");
        if (pwshPath != null && await IsShellAvailableAsync(pwshPath, cancellationToken))
        {
            _logger.LogDebug("Found PowerShell Core at: {Path}", pwshPath);
            return pwshPath;
        }

        // Fall back to Windows PowerShell
        var windowsPowerShell = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");

        if (File.Exists(windowsPowerShell))
        {
            _logger.LogDebug("Found Windows PowerShell at: {Path}", windowsPowerShell);
            return windowsPowerShell;
        }

        // Ultimate fallback to cmd.exe
        var cmdPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "cmd.exe");

        if (File.Exists(cmdPath))
        {
            _logger.LogDebug("Falling back to cmd.exe at: {Path}", cmdPath);
            return cmdPath;
        }

        return null;
    }

    /// <summary>
    /// Enumerates available shells on Windows.
    /// </summary>
    private async Task AddWindowsShellsAsync(
        List<ShellInfo> shells,
        string defaultShell,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Enumerating Windows shells");

        // PowerShell Core
        var pwshPath = FindExecutable("pwsh");
        if (pwshPath != null && await IsShellAvailableAsync(pwshPath, cancellationToken))
        {
            _logger.LogDebug("Adding PowerShell Core: {Path}", pwshPath);
            shells.Add(new ShellInfo
            {
                Name = "PowerShell",
                Path = pwshPath,
                ShellType = ShellType.PowerShellCore,
                DefaultArguments = ["-NoLogo"],
                IsDefault = NormalizePath(pwshPath) == NormalizePath(defaultShell),
                Version = await GetVersionAsync(pwshPath, "--version", cancellationToken)
            });
        }

        // Windows PowerShell
        var windowsPowerShell = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");

        if (File.Exists(windowsPowerShell) &&
            NormalizePath(windowsPowerShell) != NormalizePath(pwshPath))
        {
            _logger.LogDebug("Adding Windows PowerShell: {Path}", windowsPowerShell);
            shells.Add(new ShellInfo
            {
                Name = "Windows PowerShell",
                Path = windowsPowerShell,
                ShellType = ShellType.PowerShell,
                DefaultArguments = ["-NoLogo"],
                IsDefault = NormalizePath(windowsPowerShell) == NormalizePath(defaultShell)
            });
        }

        // Command Prompt
        var cmdPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "cmd.exe");

        if (File.Exists(cmdPath))
        {
            _logger.LogDebug("Adding Command Prompt: {Path}", cmdPath);
            shells.Add(new ShellInfo
            {
                Name = "Command Prompt",
                Path = cmdPath,
                ShellType = ShellType.Cmd,
                DefaultArguments = [],
                IsDefault = NormalizePath(cmdPath) == NormalizePath(defaultShell)
            });
        }

        // Git Bash - check common installation locations
        var gitBashPaths = new[]
        {
            @"C:\Program Files\Git\bin\bash.exe",
            @"C:\Program Files (x86)\Git\bin\bash.exe",
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Git", "bin", "bash.exe")
        };

        foreach (var gitBash in gitBashPaths)
        {
            if (File.Exists(gitBash))
            {
                _logger.LogDebug("Adding Git Bash: {Path}", gitBash);
                shells.Add(new ShellInfo
                {
                    Name = "Git Bash",
                    Path = gitBash,
                    ShellType = ShellType.Bash,
                    DefaultArguments = ["--login"],
                    IsDefault = NormalizePath(gitBash) == NormalizePath(defaultShell),
                    Version = await GetVersionAsync(gitBash, "--version", cancellationToken)
                });
                break; // Only add once
            }
        }

        // WSL (Windows Subsystem for Linux)
        var wslPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "wsl.exe");

        if (File.Exists(wslPath))
        {
            _logger.LogDebug("Adding WSL: {Path}", wslPath);
            shells.Add(new ShellInfo
            {
                Name = "WSL",
                Path = wslPath,
                ShellType = ShellType.Bash,
                DefaultArguments = [],
                IsDefault = NormalizePath(wslPath) == NormalizePath(defaultShell)
            });
        }

        // Nushell
        var nuPath = FindExecutable("nu");
        if (nuPath != null && await IsShellAvailableAsync(nuPath, cancellationToken))
        {
            _logger.LogDebug("Adding Nushell: {Path}", nuPath);
            shells.Add(new ShellInfo
            {
                Name = "Nushell",
                Path = nuPath,
                ShellType = ShellType.Nushell,
                DefaultArguments = [],
                IsDefault = NormalizePath(nuPath) == NormalizePath(defaultShell),
                Version = await GetVersionAsync(nuPath, "--version", cancellationToken)
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // macOS Detection
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the default shell on macOS.
    /// </summary>
    private async Task<string?> GetMacDefaultShellAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Detecting macOS default shell");

        // Try SHELL environment variable first
        var shell = Environment.GetEnvironmentVariable("SHELL");
        if (!string.IsNullOrEmpty(shell) && await IsShellAvailableAsync(shell, cancellationToken))
        {
            _logger.LogDebug("Found shell from $SHELL: {Shell}", shell);
            return shell;
        }

        // Try dscl to get user's configured shell
        try
        {
            _logger.LogDebug("Querying dscl for user shell");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dscl",
                    Arguments = $". -read /Users/{Environment.UserName} UserShell",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            // Output format: "UserShell: /bin/zsh"
            var parts = output.Split(':', 2);
            if (parts.Length == 2)
            {
                shell = parts[1].Trim();
                if (await IsShellAvailableAsync(shell, cancellationToken))
                {
                    _logger.LogDebug("Found shell from dscl: {Shell}", shell);
                    return shell;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get shell from dscl");
        }

        // macOS defaults to zsh since Catalina (10.15)
        const string zshPath = "/bin/zsh";
        if (File.Exists(zshPath))
        {
            _logger.LogDebug("Falling back to /bin/zsh");
            return zshPath;
        }

        return null;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Linux Detection
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the default shell on Linux.
    /// </summary>
    private async Task<string?> GetLinuxDefaultShellAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Detecting Linux default shell");

        // Try SHELL environment variable
        var shell = Environment.GetEnvironmentVariable("SHELL");
        if (!string.IsNullOrEmpty(shell) && await IsShellAvailableAsync(shell, cancellationToken))
        {
            _logger.LogDebug("Found shell from $SHELL: {Shell}", shell);
            return shell;
        }

        // Try to read from /etc/passwd
        try
        {
            _logger.LogDebug("Reading /etc/passwd for user shell");
            var passwd = await File.ReadAllTextAsync("/etc/passwd", cancellationToken);
            var username = Environment.UserName;
            var lines = passwd.Split('\n');

            foreach (var line in lines)
            {
                if (line.StartsWith(username + ":", StringComparison.Ordinal))
                {
                    var parts = line.Split(':');
                    if (parts.Length >= 7)
                    {
                        shell = parts[6].Trim();
                        if (await IsShellAvailableAsync(shell, cancellationToken))
                        {
                            _logger.LogDebug("Found shell from /etc/passwd: {Shell}", shell);
                            return shell;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get shell from /etc/passwd");
        }

        // Fall back to bash
        const string bashPath = "/bin/bash";
        if (File.Exists(bashPath))
        {
            _logger.LogDebug("Falling back to /bin/bash");
            return bashPath;
        }

        return null;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Unix Shell Enumeration (shared by macOS and Linux)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Enumerates available shells on Unix systems (macOS and Linux).
    /// </summary>
    private async Task AddUnixShellsAsync(
        List<ShellInfo> shells,
        string defaultShell,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Enumerating Unix shells");

        // Track added paths to avoid duplicates
        var addedPaths = new HashSet<string>(StringComparer.Ordinal);

        // Well-known shell paths to check
        var shellPaths = new (string Name, string Path, ShellType Type)[]
        {
            ("Bash", "/bin/bash", ShellType.Bash),
            ("Bash", "/usr/bin/bash", ShellType.Bash),
            ("Zsh", "/bin/zsh", ShellType.Zsh),
            ("Zsh", "/usr/bin/zsh", ShellType.Zsh),
            ("Fish", "/usr/bin/fish", ShellType.Fish),
            ("Fish", "/usr/local/bin/fish", ShellType.Fish),
            ("Fish", "/opt/homebrew/bin/fish", ShellType.Fish),
            ("Sh", "/bin/sh", ShellType.Sh),
            ("Nushell", "/usr/bin/nu", ShellType.Nushell),
            ("Nushell", "/usr/local/bin/nu", ShellType.Nushell),
            ("Nushell", "/opt/homebrew/bin/nu", ShellType.Nushell),
        };

        foreach (var (name, path, type) in shellPaths)
        {
            // Skip if path doesn't exist
            if (!File.Exists(path))
                continue;

            // Normalize and check for duplicates
            var normalizedPath = NormalizePath(path);
            if (normalizedPath == null || addedPaths.Contains(normalizedPath))
                continue;

            // Validate the shell
            if (await IsShellAvailableAsync(path, cancellationToken))
            {
                addedPaths.Add(normalizedPath);
                _logger.LogDebug("Adding {Name}: {Path}", name, path);
                shells.Add(new ShellInfo
                {
                    Name = name,
                    Path = path,
                    ShellType = type,
                    DefaultArguments = GetDefaultArguments(type),
                    IsDefault = NormalizePath(path) == NormalizePath(defaultShell),
                    Version = await GetVersionAsync(path, "--version", cancellationToken)
                });
            }
        }

        // Check for PowerShell Core on Unix
        var pwshPaths = new[]
        {
            "/usr/bin/pwsh",
            "/usr/local/bin/pwsh",
            "/opt/homebrew/bin/pwsh",
            "/snap/bin/pwsh"
        };

        foreach (var pwshPath in pwshPaths)
        {
            if (await IsShellAvailableAsync(pwshPath, cancellationToken))
            {
                _logger.LogDebug("Adding PowerShell: {Path}", pwshPath);
                shells.Add(new ShellInfo
                {
                    Name = "PowerShell",
                    Path = pwshPath,
                    ShellType = ShellType.PowerShellCore,
                    DefaultArguments = ["-NoLogo"],
                    IsDefault = NormalizePath(pwshPath) == NormalizePath(defaultShell),
                    Version = await GetVersionAsync(pwshPath, "--version", cancellationToken)
                });
                break; // Only add once
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helper Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the platform-appropriate fallback shell.
    /// </summary>
    /// <returns>Path to the fallback shell.</returns>
    private static string GetFallbackShell()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "cmd.exe");
        }

        return "/bin/sh";
    }

    /// <summary>
    /// Finds an executable in the PATH environment variable.
    /// </summary>
    /// <param name="name">Name of the executable (without extension on Windows).</param>
    /// <returns>Full path if found, null otherwise.</returns>
    private static string? FindExecutable(string name)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        var paths = pathEnv.Split(Path.PathSeparator);
        var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";

        foreach (var path in paths)
        {
            try
            {
                var fullPath = Path.Combine(path, name + extension);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            catch
            {
                // Skip invalid paths (e.g., containing invalid characters)
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the version string from a shell executable.
    /// </summary>
    /// <param name="path">Path to the shell.</param>
    /// <param name="args">Arguments to get version (usually --version).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Version string if detected, null otherwise.</returns>
    private async Task<string?> GetVersionAsync(
        string path,
        string args,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting version for: {Path}", path);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Read first line of output
            var output = await process.StandardOutput.ReadLineAsync(cancellationToken);

            // Wait with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(VersionTimeoutSeconds));

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(); } catch { /* Ignore kill errors */ }
            }

            var version = ParseVersionString(output);
            _logger.LogDebug("Version detected for {Path}: {Version}", path, version ?? "(none)");
            return version;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get version for shell: {Path}", path);
            return null;
        }
    }

    /// <summary>
    /// Extracts a clean version string from shell --version output.
    /// </summary>
    /// <param name="output">Raw output from --version command.</param>
    /// <returns>Cleaned version string, or null if not parseable.</returns>
    private static string? ParseVersionString(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        // Get first line, trimmed
        var cleaned = output.Trim();
        var firstLine = cleaned.Split('\n')[0].Trim();

        // If it's reasonably short, return as-is
        // Example outputs:
        //   "GNU bash, version 5.2.15(1)-release"
        //   "zsh 5.9 (x86_64-apple-darwin23.0)"
        //   "PowerShell 7.4.0"
        //   "fish, version 3.6.1"
        if (firstLine.Length <= 50)
            return firstLine;

        // Otherwise, try to extract just the version number
        var versionMatch = Regex.Match(firstLine, @"\d+\.\d+(\.\d+)?");
        return versionMatch.Success ? versionMatch.Value : null;
    }

    /// <summary>
    /// Normalizes a path for comparison (resolves case differences on Windows).
    /// </summary>
    /// <param name="path">Path to normalize.</param>
    /// <returns>Normalized path, or null if path is invalid.</returns>
    private static string? NormalizePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        try
        {
            var fullPath = Path.GetFullPath(path);
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? fullPath.ToLowerInvariant()
                : fullPath;
        }
        catch
        {
            return path;
        }
    }

    /// <summary>
    /// Determines the shell type from an executable path.
    /// </summary>
    /// <param name="path">Path to the shell executable.</param>
    /// <returns>The determined shell type.</returns>
    private static ShellType DetermineShellType(string path)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();

        // v0.5.3a: Added Tcsh, Ksh, and Wsl types
        return name switch
        {
            "bash" or "bash.exe" => ShellType.Bash,
            "zsh" or "zsh.exe" => ShellType.Zsh,
            "sh" => ShellType.Sh,
            "fish" or "fish.exe" => ShellType.Fish,
            "cmd.exe" => ShellType.Cmd,
            "powershell.exe" => ShellType.PowerShell,
            "pwsh" or "pwsh.exe" => ShellType.PowerShellCore,
            "nu" or "nu.exe" => ShellType.Nushell,
            "tcsh" or "tcsh.exe" or "csh" or "csh.exe" => ShellType.Tcsh,
            "ksh" or "ksh.exe" or "ksh93" or "mksh" => ShellType.Ksh,
            "wsl" or "wsl.exe" => ShellType.Wsl,
            _ => ShellType.Unknown
        };
    }

    /// <summary>
    /// Gets the default arguments for a given shell type.
    /// </summary>
    /// <param name="shellType">The shell type.</param>
    /// <returns>Default arguments array, or null if none.</returns>
    private static string[]? GetDefaultArguments(ShellType shellType)
    {
        return shellType switch
        {
            ShellType.Bash => ["--login"],
            ShellType.Zsh => ["--login"],
            ShellType.PowerShell => ["-NoLogo"],
            ShellType.PowerShellCore => ["-NoLogo"],
            _ => null
        };
    }
}
