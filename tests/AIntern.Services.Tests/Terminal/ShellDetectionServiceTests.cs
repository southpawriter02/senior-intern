using AIntern.Core.Interfaces;
using AIntern.Services.Terminal;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Services.Tests.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SHELL DETECTION SERVICE TESTS (v0.5.1e)                                 │
// │ Unit tests for the ShellDetectionService implementation.                │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="ShellDetectionService"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1e.</para>
/// <para>
/// These tests verify the shell detection service behavior including
/// default shell detection, shell enumeration, caching, and validation.
/// </para>
/// </remarks>
public class ShellDetectionServiceTests
{
    // ─────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────

    private readonly Mock<ILogger<ShellDetectionService>> _mockLogger;
    private readonly ShellDetectionService _service;

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new test instance.
    /// </summary>
    public ShellDetectionServiceTests()
    {
        _mockLogger = new Mock<ILogger<ShellDetectionService>>();
        _service = new ShellDetectionService(_mockLogger.Object);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Constructor Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> Constructor throws ArgumentNullException when logger is null.<br/>
    /// <b>Arrange:</b> Null logger.<br/>
    /// <b>Act:</b> Attempt to create ShellDetectionService.<br/>
    /// <b>Assert:</b> ArgumentNullException is thrown.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger<ShellDetectionService>? logger = null;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(
            () => new ShellDetectionService(logger!));
        Assert.Equal("logger", ex.ParamName);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetDefaultShellAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetDefaultShellAsync returns non-empty path.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call GetDefaultShellAsync.<br/>
    /// <b>Assert:</b> Returns non-empty string.
    /// </summary>
    [Fact]
    public async Task GetDefaultShellAsync_ReturnsNonEmptyPath()
    {
        // Act
        var result = await _service.GetDefaultShellAsync();

        // Assert
        Assert.False(string.IsNullOrEmpty(result));
    }

    /// <summary>
    /// <b>Unit Test:</b> GetDefaultShellAsync returns path to existing file.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call GetDefaultShellAsync.<br/>
    /// <b>Assert:</b> Returned path exists on filesystem.
    /// </summary>
    [Fact]
    public async Task GetDefaultShellAsync_ReturnsExistingFile()
    {
        // Act
        var result = await _service.GetDefaultShellAsync();

        // Assert
        Assert.True(File.Exists(result), $"Expected file to exist: {result}");
    }

    /// <summary>
    /// <b>Unit Test:</b> GetDefaultShellAsync returns cached result on subsequent calls.<br/>
    /// <b>Arrange:</b> Call GetDefaultShellAsync twice.<br/>
    /// <b>Act:</b> Get results from both calls.<br/>
    /// <b>Assert:</b> Both calls return the same path.
    /// </summary>
    [Fact]
    public async Task GetDefaultShellAsync_ReturnsCachedResult()
    {
        // Act
        var result1 = await _service.GetDefaultShellAsync();
        var result2 = await _service.GetDefaultShellAsync();

        // Assert
        Assert.Equal(result1, result2);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetDefaultShellAsync respects cancellation.<br/>
    /// <b>Arrange:</b> Pre-cancelled token.<br/>
    /// <b>Act:</b> Call GetDefaultShellAsync with cancelled token.<br/>
    /// <b>Assert:</b> Returns result (cached) or throws OperationCanceledException.
    /// </summary>
    [Fact]
    public async Task GetDefaultShellAsync_CancellationToken_Works()
    {
        // First call to populate cache
        var result = await _service.GetDefaultShellAsync();

        // Act - subsequent call with cancellation should return cached
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should return cached result even with cancelled token
        var result2 = await _service.GetDefaultShellAsync(cts.Token);
        Assert.Equal(result, result2);
    }

    // ─────────────────────────────────────────────────────────────────────
    // DetectDefaultShellAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> DetectDefaultShellAsync returns valid ShellInfo.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call DetectDefaultShellAsync.<br/>
    /// <b>Assert:</b> Returns ShellInfo with valid properties.
    /// </summary>
    [Fact]
    public async Task DetectDefaultShellAsync_ReturnsValidShellInfo()
    {
        // Act
        var result = await _service.DetectDefaultShellAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Name));
        Assert.False(string.IsNullOrEmpty(result.Path));
        Assert.NotEqual(ShellType.Unknown, result.ShellType);
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectDefaultShellAsync returns IsDefault true.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call DetectDefaultShellAsync.<br/>
    /// <b>Assert:</b> ShellInfo has IsDefault set to true.
    /// </summary>
    [Fact]
    public async Task DetectDefaultShellAsync_ReturnsIsDefaultTrue()
    {
        // Act
        var result = await _service.DetectDefaultShellAsync();

        // Assert
        Assert.True(result.IsDefault);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetAvailableShellsAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetAvailableShellsAsync returns at least one shell.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call GetAvailableShellsAsync.<br/>
    /// <b>Assert:</b> Returns at least one shell.
    /// </summary>
    [Fact]
    public async Task GetAvailableShellsAsync_ReturnsAtLeastOneShell()
    {
        // Act
        var result = await _service.GetAvailableShellsAsync();

        // Assert
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetAvailableShellsAsync includes the default shell.<br/>
    /// <b>Arrange:</b> Get default shell and available shells.<br/>
    /// <b>Act:</b> Check if available shells includes default.<br/>
    /// <b>Assert:</b> Default shell is in available shells list.
    /// </summary>
    [Fact]
    public async Task GetAvailableShellsAsync_IncludesDefaultShell()
    {
        // Arrange
        var defaultShellPath = await _service.GetDefaultShellAsync();

        // Act
        var availableShells = await _service.GetAvailableShellsAsync();

        // Assert
        var paths = availableShells.Select(s => s.Path).ToList();
        Assert.Contains(defaultShellPath, paths);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetAvailableShellsAsync returns cached result.<br/>
    /// <b>Arrange:</b> Call GetAvailableShellsAsync twice.<br/>
    /// <b>Act:</b> Compare references.<br/>
    /// <b>Assert:</b> Same list instance returned.
    /// </summary>
    [Fact]
    public async Task GetAvailableShellsAsync_ReturnsCachedResult()
    {
        // Act
        var result1 = await _service.GetAvailableShellsAsync();
        var result2 = await _service.GetAvailableShellsAsync();

        // Assert - should be same reference (cached)
        Assert.Same(result1, result2);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetAvailableShellsAsync marks exactly one shell as default.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call GetAvailableShellsAsync.<br/>
    /// <b>Assert:</b> Exactly one shell has IsDefault true.
    /// </summary>
    [Fact]
    public async Task GetAvailableShellsAsync_ExactlyOneDefaultShell()
    {
        // Act
        var result = await _service.GetAvailableShellsAsync();

        // Assert
        var defaultShells = result.Where(s => s.IsDefault).ToList();
        Assert.Single(defaultShells);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetAvailableShellsAsync has default shell first.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call GetAvailableShellsAsync.<br/>
    /// <b>Assert:</b> First shell in list is the default.
    /// </summary>
    [Fact]
    public async Task GetAvailableShellsAsync_DefaultShellIsFirst()
    {
        // Act
        var result = await _service.GetAvailableShellsAsync();

        // Assert
        Assert.True(result.Count > 0);
        Assert.True(result[0].IsDefault);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetAvailableShellsAsync returns shells with valid properties.<br/>
    /// <b>Arrange:</b> Shell detection service.<br/>
    /// <b>Act:</b> Call GetAvailableShellsAsync.<br/>
    /// <b>Assert:</b> All shells have valid names and paths.
    /// </summary>
    [Fact]
    public async Task GetAvailableShellsAsync_AllShellsHaveValidProperties()
    {
        // Act
        var result = await _service.GetAvailableShellsAsync();

        // Assert
        foreach (var shell in result)
        {
            Assert.False(string.IsNullOrEmpty(shell.Name), "Shell Name should not be empty");
            Assert.False(string.IsNullOrEmpty(shell.Path), "Shell Path should not be empty");
            Assert.True(File.Exists(shell.Path), $"Shell path should exist: {shell.Path}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // IsShellAvailableAsync Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> IsShellAvailableAsync returns true for default shell.<br/>
    /// <b>Arrange:</b> Get default shell path.<br/>
    /// <b>Act:</b> Call IsShellAvailableAsync with default shell.<br/>
    /// <b>Assert:</b> Returns true.
    /// </summary>
    [Fact]
    public async Task IsShellAvailableAsync_DefaultShell_ReturnsTrue()
    {
        // Arrange
        var defaultShell = await _service.GetDefaultShellAsync();

        // Act
        var result = await _service.IsShellAvailableAsync(defaultShell);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> IsShellAvailableAsync returns false for null path.<br/>
    /// <b>Arrange:</b> Null path.<br/>
    /// <b>Act:</b> Call IsShellAvailableAsync with null.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public async Task IsShellAvailableAsync_NullPath_ReturnsFalse()
    {
        // Act
        var result = await _service.IsShellAvailableAsync(null!);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> IsShellAvailableAsync returns false for empty path.<br/>
    /// <b>Arrange:</b> Empty string path.<br/>
    /// <b>Act:</b> Call IsShellAvailableAsync with empty string.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public async Task IsShellAvailableAsync_EmptyPath_ReturnsFalse()
    {
        // Act
        var result = await _service.IsShellAvailableAsync("");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> IsShellAvailableAsync returns false for whitespace path.<br/>
    /// <b>Arrange:</b> Whitespace path.<br/>
    /// <b>Act:</b> Call IsShellAvailableAsync with whitespace.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public async Task IsShellAvailableAsync_WhitespacePath_ReturnsFalse()
    {
        // Act
        var result = await _service.IsShellAvailableAsync("   ");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> IsShellAvailableAsync returns false for non-existent path.<br/>
    /// <b>Arrange:</b> Non-existent path.<br/>
    /// <b>Act:</b> Call IsShellAvailableAsync.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public async Task IsShellAvailableAsync_NonExistentPath_ReturnsFalse()
    {
        // Act
        var result = await _service.IsShellAvailableAsync("/nonexistent/shell/path");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> IsShellAvailableAsync returns false for directory path.<br/>
    /// <b>Arrange:</b> Path to a directory.<br/>
    /// <b>Act:</b> Call IsShellAvailableAsync.<br/>
    /// <b>Assert:</b> Returns false.
    /// </summary>
    [Fact]
    public async Task IsShellAvailableAsync_DirectoryPath_ReturnsFalse()
    {
        // Arrange - using a known directory
        var tempDir = Path.GetTempPath();

        // Act
        var result = await _service.IsShellAvailableAsync(tempDir);

        // Assert
        Assert.False(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ShellInfo Record Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ShellInfo ToString includes name.<br/>
    /// <b>Arrange:</b> ShellInfo with name.<br/>
    /// <b>Act:</b> Call ToString.<br/>
    /// <b>Assert:</b> Result contains name.
    /// </summary>
    [Fact]
    public void ShellInfo_ToString_IncludesName()
    {
        // Arrange
        var shellInfo = new ShellInfo
        {
            Name = "TestShell",
            Path = "/bin/test",
            ShellType = ShellType.Bash
        };

        // Act
        var result = shellInfo.ToString();

        // Assert
        Assert.Contains("TestShell", result);
    }

    /// <summary>
    /// <b>Unit Test:</b> ShellInfo ToString includes version when present.<br/>
    /// <b>Arrange:</b> ShellInfo with version.<br/>
    /// <b>Act:</b> Call ToString.<br/>
    /// <b>Assert:</b> Result contains version.
    /// </summary>
    [Fact]
    public void ShellInfo_ToString_IncludesVersionWhenPresent()
    {
        // Arrange
        var shellInfo = new ShellInfo
        {
            Name = "TestShell",
            Path = "/bin/test",
            ShellType = ShellType.Bash,
            Version = "5.2.15"
        };

        // Act
        var result = shellInfo.ToString();

        // Assert
        Assert.Contains("5.2.15", result);
        Assert.Equal("TestShell (5.2.15)", result);
    }

    /// <summary>
    /// <b>Unit Test:</b> ShellInfo ToString without version returns just name.<br/>
    /// <b>Arrange:</b> ShellInfo without version.<br/>
    /// <b>Act:</b> Call ToString.<br/>
    /// <b>Assert:</b> Result is just the name.
    /// </summary>
    [Fact]
    public void ShellInfo_ToString_WithoutVersion_ReturnsName()
    {
        // Arrange
        var shellInfo = new ShellInfo
        {
            Name = "TestShell",
            Path = "/bin/test",
            ShellType = ShellType.Bash
        };

        // Act
        var result = shellInfo.ToString();

        // Assert
        Assert.Equal("TestShell", result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ShellType Enum Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ShellType enum has expected values.<br/>
    /// <b>Arrange:</b> Check enum values.<br/>
    /// <b>Act:</b> Compare to expected.<br/>
    /// <b>Assert:</b> All expected values exist.
    /// </summary>
    [Fact]
    public void ShellType_HasExpectedValues()
    {
        // Assert - verify expected values exist
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.Unknown));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.Bash));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.Zsh));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.Sh));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.Fish));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.Cmd));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.PowerShell));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.PowerShellCore));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.Nushell));
    }

    /// <summary>
    /// <b>Unit Test:</b> ShellType Unknown has value 0.<br/>
    /// <b>Arrange:</b> ShellType.Unknown.<br/>
    /// <b>Act:</b> Cast to int.<br/>
    /// <b>Assert:</b> Value is 0.
    /// </summary>
    [Fact]
    public void ShellType_Unknown_HasValueZero()
    {
        // Assert
        Assert.Equal(0, (int)ShellType.Unknown);
    }

    // ─────────────────────────────────────────────────────────────────────
    // DetectShellType Tests (v0.5.3a)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> DetectShellType returns Bash for bash path.<br/>
    /// <b>Arrange:</b> Path to bash executable.<br/>
    /// <b>Act:</b> Call DetectShellType.<br/>
    /// <b>Assert:</b> Returns ShellType.Bash.
    /// </summary>
    [Theory]
    [InlineData("/bin/bash", ShellType.Bash)]
    [InlineData("/usr/bin/bash", ShellType.Bash)]
    [InlineData("bash.exe", ShellType.Bash)]
    public void DetectShellType_Bash_ReturnsBash(string path, ShellType expected)
    {
        // Act
        var result = _service.DetectShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectShellType returns Zsh for zsh path.<br/>
    /// </summary>
    [Theory]
    [InlineData("/bin/zsh", ShellType.Zsh)]
    [InlineData("zsh.exe", ShellType.Zsh)]
    public void DetectShellType_Zsh_ReturnsZsh(string path, ShellType expected)
    {
        // Act
        var result = _service.DetectShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectShellType returns PowerShellCore for pwsh path.<br/>
    /// </summary>
    [Theory]
    [InlineData("pwsh", ShellType.PowerShellCore)]
    [InlineData("pwsh.exe", ShellType.PowerShellCore)]
    public void DetectShellType_Pwsh_ReturnsPowerShellCore(string path, ShellType expected)
    {
        // Act
        var result = _service.DetectShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectShellType returns PowerShell for powershell.exe.<br/>
    /// </summary>
    [Fact]
    public void DetectShellType_PowerShell_ReturnsPowerShell()
    {
        // Act
        var result = _service.DetectShellType("powershell.exe");

        // Assert
        Assert.Equal(ShellType.PowerShell, result);
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectShellType returns Cmd for cmd.exe.<br/>
    /// </summary>
    [Fact]
    public void DetectShellType_Cmd_ReturnsCmd()
    {
        // Act
        var result = _service.DetectShellType("cmd.exe");

        // Assert
        Assert.Equal(ShellType.Cmd, result);
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectShellType returns Tcsh for tcsh path (v0.5.3a).<br/>
    /// </summary>
    [Theory]
    [InlineData("tcsh", ShellType.Tcsh)]
    [InlineData("tcsh.exe", ShellType.Tcsh)]
    [InlineData("csh", ShellType.Tcsh)]
    public void DetectShellType_Tcsh_ReturnsTcsh(string path, ShellType expected)
    {
        // Act
        var result = _service.DetectShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectShellType returns Ksh for ksh path (v0.5.3a).<br/>
    /// </summary>
    [Theory]
    [InlineData("ksh", ShellType.Ksh)]
    [InlineData("ksh.exe", ShellType.Ksh)]
    [InlineData("ksh93", ShellType.Ksh)]
    [InlineData("mksh", ShellType.Ksh)]
    public void DetectShellType_Ksh_ReturnsKsh(string path, ShellType expected)
    {
        // Act
        var result = _service.DetectShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectShellType returns Wsl for wsl path (v0.5.3a).<br/>
    /// </summary>
    [Theory]
    [InlineData("wsl", ShellType.Wsl)]
    [InlineData("wsl.exe", ShellType.Wsl)]
    public void DetectShellType_Wsl_ReturnsWsl(string path, ShellType expected)
    {
        // Act
        var result = _service.DetectShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectShellType returns Unknown for unrecognized name.<br/>
    /// </summary>
    [Theory]
    [InlineData("/bin/unknown_shell")]
    [InlineData("some_random.exe")]
    public void DetectShellType_Unknown_ReturnsUnknown(string path)
    {
        // Act
        var result = _service.DetectShellType(path);

        // Assert
        Assert.Equal(ShellType.Unknown, result);
    }

    /// <summary>
    /// <b>Unit Test:</b> DetectShellType returns Unknown for null/empty path.<br/>
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DetectShellType_NullOrEmpty_ReturnsUnknown(string? path)
    {
        // Act
        var result = _service.DetectShellType(path!);

        // Assert
        Assert.Equal(ShellType.Unknown, result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ValidateShellPath Tests (v0.5.3a)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ValidateShellPath returns true for existing file.<br/>
    /// </summary>
    [Fact]
    public async Task ValidateShellPath_ExistingFile_ReturnsTrue()
    {
        // Arrange - use default shell which should exist
        var defaultShell = await _service.GetDefaultShellAsync();

        // Act
        var result = _service.ValidateShellPath(defaultShell);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> ValidateShellPath returns false for null path.<br/>
    /// </summary>
    [Fact]
    public void ValidateShellPath_NullPath_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateShellPath(null!);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> ValidateShellPath returns false for empty path.<br/>
    /// </summary>
    [Fact]
    public void ValidateShellPath_EmptyPath_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateShellPath("");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> ValidateShellPath returns false for whitespace path.<br/>
    /// </summary>
    [Fact]
    public void ValidateShellPath_WhitespacePath_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateShellPath("   ");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> ValidateShellPath returns false for non-existent path.<br/>
    /// </summary>
    [Fact]
    public void ValidateShellPath_NonExistentPath_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateShellPath("/nonexistent/shell/path");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> ValidateShellPath returns false for directory path.<br/>
    /// </summary>
    [Fact]
    public void ValidateShellPath_DirectoryPath_ReturnsFalse()
    {
        // Arrange
        var tempDir = Path.GetTempPath();

        // Act
        var result = _service.ValidateShellPath(tempDir);

        // Assert
        Assert.False(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // FindInPath Tests (v0.5.3a)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> FindInPath returns null for null input.<br/>
    /// </summary>
    [Fact]
    public void FindInPath_NullName_ReturnsNull()
    {
        // Act
        var result = _service.FindInPath(null!);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> FindInPath returns null for empty input.<br/>
    /// </summary>
    [Fact]
    public void FindInPath_EmptyName_ReturnsNull()
    {
        // Act
        var result = _service.FindInPath("");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> FindInPath returns null for whitespace input.<br/>
    /// </summary>
    [Fact]
    public void FindInPath_WhitespaceName_ReturnsNull()
    {
        // Act
        var result = _service.FindInPath("   ");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> FindInPath returns null for non-existent executable.<br/>
    /// </summary>
    [Fact]
    public void FindInPath_NonExistentExecutable_ReturnsNull()
    {
        // Act
        var result = _service.FindInPath("definitely_not_a_real_executable_12345");

        // Assert
        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetShellVersionAsync Tests (v0.5.3a)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> GetShellVersionAsync returns null for null path.<br/>
    /// </summary>
    [Fact]
    public async Task GetShellVersionAsync_NullPath_ReturnsNull()
    {
        // Act
        var result = await _service.GetShellVersionAsync(null!);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetShellVersionAsync returns null for empty path.<br/>
    /// </summary>
    [Fact]
    public async Task GetShellVersionAsync_EmptyPath_ReturnsNull()
    {
        // Act
        var result = await _service.GetShellVersionAsync("");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetShellVersionAsync returns null for non-existent path.<br/>
    /// </summary>
    [Fact]
    public async Task GetShellVersionAsync_NonExistentPath_ReturnsNull()
    {
        // Act
        var result = await _service.GetShellVersionAsync("/nonexistent/shell");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// <b>Unit Test:</b> GetShellVersionAsync respects cancellation.<br/>
    /// </summary>
    [Fact]
    public async Task GetShellVersionAsync_Cancellation_Respected()
    {
        // Arrange
        var defaultShell = await _service.GetDefaultShellAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - should not throw, just return null or cached value
        var result = await _service.GetShellVersionAsync(defaultShell, cts.Token);
        // No exception thrown is success
    }

    // ─────────────────────────────────────────────────────────────────────
    // ShellType v0.5.3a Enum Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> ShellType has v0.5.3a values (Tcsh, Ksh, Wsl).<br/>
    /// </summary>
    [Fact]
    public void ShellType_HasNewV053aValues()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.Tcsh));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.Ksh));
        Assert.True(Enum.IsDefined(typeof(ShellType), ShellType.Wsl));
    }

    /// <summary>
    /// <b>Unit Test:</b> ShellType.Wsl is distinct from ShellType.Bash.<br/>
    /// </summary>
    [Fact]
    public void ShellType_Wsl_NotBash()
    {
        // Assert
        Assert.NotEqual(ShellType.Bash, ShellType.Wsl);
    }
}
