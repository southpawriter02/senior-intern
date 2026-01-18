namespace AIntern.Desktop.Tests.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalSessionViewModelTests (v0.5.2d)                                      │
// │ Unit tests for TerminalSessionViewModel shell type detection and properties. │
// └─────────────────────────────────────────────────────────────────────────────┘

using System;
using Xunit;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.ViewModels;

/// <summary>
/// Unit tests for <see cref="TerminalSessionViewModel"/>.
/// </summary>
/// <remarks>
/// Tests cover:
/// <list type="bullet">
///   <item><description>Shell type detection from various paths</description></item>
///   <item><description>Property initialization and updating</description></item>
///   <item><description>Computed properties (HasExited, ExitCode)</description></item>
/// </list>
/// Added in v0.5.2d.
/// </remarks>
public class TerminalSessionViewModelTests
{
    #region Test Helpers

    /// <summary>
    /// Creates a mock TerminalSession for testing.
    /// </summary>
    private static TerminalSession CreateMockSession(
        string shellPath = "/bin/bash",
        string name = "Terminal",
        TerminalSessionState state = TerminalSessionState.Running,
        string workingDirectory = "/home/user",
        string? title = null)
    {
        return new TerminalSession
        {
            Id = Guid.NewGuid(),
            ShellPath = shellPath,
            Name = name,
            State = state,
            WorkingDirectory = workingDirectory,
            Title = title
        };
    }

    #endregion

    #region Shell Type Detection Tests

    [Theory]
    [InlineData("/bin/bash", "bash")]
    [InlineData("/usr/bin/bash", "bash")]
    [InlineData("C:\\Git\\bin\\bash.exe", "bash")]
    public void GetShellType_BashPaths_ReturnsBash(string path, string expected)
    {
        // Act
        var result = TerminalSessionViewModel.GetShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/bin/zsh", "zsh")]
    [InlineData("/usr/local/bin/zsh", "zsh")]
    public void GetShellType_ZshPaths_ReturnsZsh(string path, string expected)
    {
        // Act
        var result = TerminalSessionViewModel.GetShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/usr/bin/fish", "fish")]
    [InlineData("/opt/homebrew/bin/fish", "fish")]
    public void GetShellType_FishPaths_ReturnsFish(string path, string expected)
    {
        // Act
        var result = TerminalSessionViewModel.GetShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/usr/bin/pwsh", "powershell")]
    [InlineData("C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe", "powershell")]
    [InlineData("C:\\Program Files\\PowerShell\\7\\pwsh.exe", "powershell")]
    public void GetShellType_PowerShellPaths_ReturnsPowershell(string path, string expected)
    {
        // Act
        var result = TerminalSessionViewModel.GetShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("C:\\Windows\\System32\\cmd.exe", "cmd")]
    public void GetShellType_CmdPaths_ReturnsCmd(string path, string expected)
    {
        // Act
        var result = TerminalSessionViewModel.GetShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/usr/bin/nu", "nushell")]
    [InlineData("C:\\Program Files\\nu\\nu.exe", "nushell")]
    public void GetShellType_NuPaths_ReturnsNushell(string path, string expected)
    {
        // Act
        var result = TerminalSessionViewModel.GetShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/usr/bin/unknown", "terminal")]
    [InlineData("C:\\Program Files\\custom-shell.exe", "terminal")]
    [InlineData("", "terminal")]
    [InlineData(null, "terminal")]
    public void GetShellType_UnknownOrEmptyPaths_ReturnsTerminal(string? path, string expected)
    {
        // Act
        var result = TerminalSessionViewModel.GetShellType(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Constructor_SetsShellTypeFromPath()
    {
        // Arrange
        var session = CreateMockSession(shellPath: "/bin/zsh");

        // Act
        var vm = new TerminalSessionViewModel(session);

        // Assert
        Assert.Equal("zsh", vm.ShellType);
    }

    #endregion

    #region Property Initialization Tests

    [Fact]
    public void Constructor_InitializesPropertiesFromSession()
    {
        // Arrange
        var session = CreateMockSession(
            name: "Test Terminal",
            state: TerminalSessionState.Running,
            workingDirectory: "/projects/myapp");

        // Act
        var vm = new TerminalSessionViewModel(session);

        // Assert
        Assert.Equal(session.Id, vm.Id);
        Assert.Equal("Test Terminal", vm.Name);
        Assert.Equal(TerminalSessionState.Running, vm.State);
        Assert.Equal("/projects/myapp", vm.WorkingDirectory);
    }

    [Fact]
    public void Constructor_UsesTitleOverName()
    {
        // Arrange
        var session = CreateMockSession(name: "Terminal", title: "vim ~/project");

        // Act
        var vm = new TerminalSessionViewModel(session);

        // Assert
        Assert.Equal("vim ~/project", vm.Name);
    }

    [Fact]
    public void Constructor_ThrowsOnNullSession()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TerminalSessionViewModel(null!));
    }

    #endregion

    #region HasExited Tests

    [Theory]
    [InlineData(TerminalSessionState.Starting, false)]
    [InlineData(TerminalSessionState.Running, false)]
    [InlineData(TerminalSessionState.Exited, true)]
    [InlineData(TerminalSessionState.Error, true)]
    [InlineData(TerminalSessionState.Closing, false)]
    public void HasExited_ReflectsState(TerminalSessionState state, bool expected)
    {
        // Arrange
        var session = CreateMockSession(state: state);
        var vm = new TerminalSessionViewModel(session);

        // Act - State might have been updated in constructor, so we set it explicitly
        vm.State = state;

        // Assert
        Assert.Equal(expected, vm.HasExited);
    }

    #endregion

    #region UpdateFromSession Tests

    [Fact]
    public void UpdateFromSession_SyncsPropertiesFromModel()
    {
        // Arrange
        var session = CreateMockSession(
            name: "Original",
            state: TerminalSessionState.Running,
            workingDirectory: "/home");
        var vm = new TerminalSessionViewModel(session);

        // Modify the underlying session
        session.Title = "Updated Title";
        session.State = TerminalSessionState.Exited;
        session.WorkingDirectory = "/tmp";

        // Act
        vm.UpdateFromSession();

        // Assert
        Assert.Equal("Updated Title", vm.Name);
        Assert.Equal(TerminalSessionState.Exited, vm.State);
        Assert.Equal("/tmp", vm.WorkingDirectory);
    }

    #endregion

    #region ExitCode Tests

    [Fact]
    public void ExitCode_ReturnsNullWhenRunning()
    {
        // Arrange
        var session = CreateMockSession(state: TerminalSessionState.Running);

        // Act
        var vm = new TerminalSessionViewModel(session);

        // Assert
        Assert.Null(vm.ExitCode);
    }

    [Fact]
    public void ExitCode_ReturnsValueWhenExited()
    {
        // Arrange
        var session = CreateMockSession(state: TerminalSessionState.Exited);
        session.ExitCode = 0;

        // Act
        var vm = new TerminalSessionViewModel(session);

        // Assert
        Assert.Equal(0, vm.ExitCode);
    }

    #endregion

    #region IsActive Tests

    [Fact]
    public void IsActive_DefaultFalse()
    {
        // Arrange
        var session = CreateMockSession();

        // Act
        var vm = new TerminalSessionViewModel(session);

        // Assert
        Assert.False(vm.IsActive);
    }

    [Fact]
    public void IsActive_CanBeSet()
    {
        // Arrange
        var session = CreateMockSession();
        var vm = new TerminalSessionViewModel(session);

        // Act
        vm.IsActive = true;

        // Assert
        Assert.True(vm.IsActive);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var session = CreateMockSession(name: "MyTerminal", state: TerminalSessionState.Running);
        var vm = new TerminalSessionViewModel(session);

        // Act
        var result = vm.ToString();

        // Assert
        Assert.Contains("TerminalSessionVM", result);
        Assert.Contains("MyTerminal", result);
        Assert.Contains("Running", result);
    }

    #endregion
}
