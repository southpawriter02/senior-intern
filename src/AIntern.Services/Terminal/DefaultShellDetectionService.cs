using System.Runtime.InteropServices;
using AIntern.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIntern.Services.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DEFAULT SHELL DETECTION SERVICE (v0.5.1d, updated v0.5.3a)              │
// │ Simple shell detection fallback without version detection or caching.  │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Simple shell detection service without caching or version detection.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1d. Updated in v0.5.1e. Extended in v0.5.3a.</para>
/// <para>
/// This provides a lightweight implementation for cases where the full
/// <see cref="ShellDetectionService"/> is not needed. It is faster but
/// does not provide caching, version detection, or shell validation.
/// </para>
/// <para>
/// For production use, prefer <see cref="ShellDetectionService"/>.
/// </para>
/// </remarks>
public sealed class DefaultShellDetectionService : IShellDetectionService
{
    // ─────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────

    private readonly ILogger<DefaultShellDetectionService> _logger;

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new default shell detection service.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public DefaultShellDetectionService(ILogger<DefaultShellDetectionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellDetectionService Implementation
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<ShellInfo> DetectDefaultShellAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Detecting default shell for platform: {Platform}",
            RuntimeInformation.OSDescription);

        ShellInfo shell;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            shell = DetectWindowsDefaultShell();
        }
        else
        {
            shell = DetectUnixDefaultShell();
        }

        _logger.LogInformation("Detected default shell: {ShellPath} ({ShellType})",
            shell.Path, shell.ShellType);

        return Task.FromResult(shell);
    }

    /// <inheritdoc />
    public async Task<string> GetDefaultShellAsync(CancellationToken cancellationToken = default)
    {
        var shellInfo = await DetectDefaultShellAsync(cancellationToken);
        return shellInfo.Path;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ShellInfo>> GetAvailableShellsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting available shells for platform: {Platform}",
            RuntimeInformation.OSDescription);

        List<ShellInfo> shells;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            shells = GetWindowsShells();
        }
        else
        {
            shells = GetUnixShells();
        }

        _logger.LogDebug("Found {Count} available shells", shells.Count);
        return Task.FromResult<IReadOnlyList<ShellInfo>>(shells.AsReadOnly());
    }

    /// <inheritdoc />
    public Task<bool> IsShellAvailableAsync(string path, CancellationToken cancellationToken = default)
    {
        // Simple file existence check without validation
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(false);
        }

        var exists = File.Exists(path);
        _logger.LogDebug("Shell availability check for {Path}: {Exists}", path, exists);
        return Task.FromResult(exists);
    }

    // ─────────────────────────────────────────────────────────────────────
    // IShellDetectionService v0.5.3a Methods
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public ShellType DetectShellType(string shellPath)
    {
        if (string.IsNullOrWhiteSpace(shellPath))
        {
            return ShellType.Unknown;
        }

        return DetermineShellType(shellPath);
    }

    /// <inheritdoc />
    public bool ValidateShellPath(string shellPath)
    {
        if (string.IsNullOrWhiteSpace(shellPath))
        {
            return false;
        }

        return File.Exists(shellPath);
    }

    /// <inheritdoc />
    public string? FindInPath(string executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName))
        {
            return null;
        }

        return FindExecutable(executableName);
    }

    /// <inheritdoc />
    public Task<string?> GetShellVersionAsync(
        string shellPath,
        CancellationToken cancellationToken = default)
    {
        // DefaultShellDetectionService does not support version detection
        _logger.LogDebug("GetShellVersionAsync: version detection not supported in default implementation");
        return Task.FromResult<string?>(null);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Windows
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the default shell on Windows.
    /// </summary>
    private ShellInfo DetectWindowsDefaultShell()
    {
        // Check for PowerShell Core first
        var pwshPath = FindExecutable("pwsh.exe");
        if (pwshPath != null)
        {
            return new ShellInfo
            {
                Name = "PowerShell",
                Path = pwshPath,
                ShellType = ShellType.PowerShellCore,
                DefaultArguments = ["-NoLogo"],
                IsDefault = true
            };
        }

        // Fall back to Windows PowerShell
        var powershellPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");

        if (File.Exists(powershellPath))
        {
            return new ShellInfo
            {
                Name = "Windows PowerShell",
                Path = powershellPath,
                ShellType = ShellType.PowerShell,
                DefaultArguments = ["-NoLogo"],
                IsDefault = true
            };
        }

        // Last resort: cmd.exe
        var comspec = Environment.GetEnvironmentVariable("COMSPEC");
        var cmdPath = comspec ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

        return new ShellInfo
        {
            Name = "Command Prompt",
            Path = cmdPath,
            ShellType = ShellType.Cmd,
            DefaultArguments = [],
            IsDefault = true
        };
    }

    /// <summary>
    /// Gets available shells on Windows.
    /// </summary>
    private List<ShellInfo> GetWindowsShells()
    {
        var shells = new List<ShellInfo>();
        var defaultShell = DetectWindowsDefaultShell();

        // cmd.exe
        var comspec = Environment.GetEnvironmentVariable("COMSPEC");
        var cmdPath = comspec ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

        if (File.Exists(cmdPath))
        {
            shells.Add(new ShellInfo
            {
                Name = "Command Prompt",
                Path = cmdPath,
                ShellType = ShellType.Cmd,
                DefaultArguments = [],
                IsDefault = NormalizePath(cmdPath) == NormalizePath(defaultShell.Path)
            });
        }

        // Windows PowerShell
        var powershellPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");

        if (File.Exists(powershellPath))
        {
            shells.Add(new ShellInfo
            {
                Name = "Windows PowerShell",
                Path = powershellPath,
                ShellType = ShellType.PowerShell,
                DefaultArguments = ["-NoLogo"],
                IsDefault = NormalizePath(powershellPath) == NormalizePath(defaultShell.Path)
            });
        }

        // PowerShell Core
        var pwshPath = FindExecutable("pwsh.exe");
        if (pwshPath != null)
        {
            shells.Add(new ShellInfo
            {
                Name = "PowerShell",
                Path = pwshPath,
                ShellType = ShellType.PowerShellCore,
                DefaultArguments = ["-NoLogo"],
                IsDefault = NormalizePath(pwshPath) == NormalizePath(defaultShell.Path)
            });
        }

        // Git Bash
        var gitBashPath = @"C:\Program Files\Git\bin\bash.exe";
        if (File.Exists(gitBashPath))
        {
            shells.Add(new ShellInfo
            {
                Name = "Git Bash",
                Path = gitBashPath,
                ShellType = ShellType.Bash,
                DefaultArguments = ["--login"],
                IsDefault = NormalizePath(gitBashPath) == NormalizePath(defaultShell.Path)
            });
        }

        return shells;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Unix
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the default shell on Unix-like systems.
    /// </summary>
    private ShellInfo DetectUnixDefaultShell()
    {
        // Check SHELL environment variable
        var shellEnv = Environment.GetEnvironmentVariable("SHELL");

        if (!string.IsNullOrEmpty(shellEnv) && File.Exists(shellEnv))
        {
            var shellType = DetermineShellType(shellEnv);
            return new ShellInfo
            {
                Name = shellType.ToString(),
                Path = shellEnv,
                ShellType = shellType,
                DefaultArguments = GetDefaultArguments(shellType),
                IsDefault = true
            };
        }

        // Common shell locations in order of preference
        var shellPaths = new[]
        {
            "/bin/zsh",
            "/bin/bash",
            "/bin/sh"
        };

        foreach (var path in shellPaths)
        {
            if (File.Exists(path))
            {
                var shellType = DetermineShellType(path);
                return new ShellInfo
                {
                    Name = shellType.ToString(),
                    Path = path,
                    ShellType = shellType,
                    DefaultArguments = GetDefaultArguments(shellType),
                    IsDefault = true
                };
            }
        }

        // Last resort
        return new ShellInfo
        {
            Name = "Sh",
            Path = "/bin/sh",
            ShellType = ShellType.Sh,
            DefaultArguments = [],
            IsDefault = true
        };
    }

    /// <summary>
    /// Gets available shells on Unix-like systems.
    /// </summary>
    private List<ShellInfo> GetUnixShells()
    {
        var shells = new List<ShellInfo>();
        var defaultShell = DetectUnixDefaultShell();

        // Common shell locations
        var shellPaths = new Dictionary<string, ShellType>
        {
            { "/bin/bash", ShellType.Bash },
            { "/bin/zsh", ShellType.Zsh },
            { "/bin/sh", ShellType.Sh },
            { "/usr/bin/fish", ShellType.Fish },
            { "/opt/homebrew/bin/fish", ShellType.Fish },
        };

        foreach (var (path, type) in shellPaths)
        {
            if (File.Exists(path))
            {
                shells.Add(new ShellInfo
                {
                    Name = type.ToString(),
                    Path = path,
                    ShellType = type,
                    DefaultArguments = GetDefaultArguments(type),
                    IsDefault = NormalizePath(path) == NormalizePath(defaultShell.Path)
                });
            }
        }

        return shells;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private Methods - Helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Determines shell type from executable path.
    /// </summary>
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
            "cmd.exe" or "cmd" => ShellType.Cmd,
            "powershell.exe" or "powershell" => ShellType.PowerShell,
            "pwsh.exe" or "pwsh" => ShellType.PowerShellCore,
            "nu" or "nu.exe" => ShellType.Nushell,
            "tcsh" or "tcsh.exe" or "csh" or "csh.exe" => ShellType.Tcsh,
            "ksh" or "ksh.exe" or "ksh93" or "mksh" => ShellType.Ksh,
            "wsl" or "wsl.exe" => ShellType.Wsl,
            _ => ShellType.Unknown
        };
    }

    /// <summary>
    /// Gets default arguments for a shell type.
    /// </summary>
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

    /// <summary>
    /// Finds an executable in the system PATH.
    /// </summary>
    private static string? FindExecutable(string execName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
        {
            return null;
        }

        var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

        foreach (var dir in pathEnv.Split(separator))
        {
            var fullPath = Path.Combine(dir, execName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Normalizes a path for comparison.
    /// </summary>
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
}
