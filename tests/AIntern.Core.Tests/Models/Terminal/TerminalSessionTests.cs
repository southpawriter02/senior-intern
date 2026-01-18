using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalSession"/>.
/// </summary>
public sealed class TerminalSessionTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var session = new TerminalSession();

        // Assert
        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal("Terminal", session.Name);
        Assert.Equal(string.Empty, session.ShellPath);
        Assert.Equal(string.Empty, session.WorkingDirectory);
        Assert.Equal(TerminalSessionState.Starting, session.State);
        Assert.Equal(TerminalSize.Default, session.Size);
        Assert.True(session.IsInteractive);
        Assert.Null(session.ExitCode);
        Assert.Null(session.ClosedAt);
        Assert.Null(session.WorkspaceId);
        Assert.Null(session.Title);
    }

    [Fact]
    public void CreatedAt_IsSetToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var session = new TerminalSession();

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(session.CreatedAt, before, after);
    }

    [Fact]
    public void DisplayTitle_ReturnsTitle_WhenSet()
    {
        // Arrange
        var session = new TerminalSession
        {
            Name = "Default",
            Title = "Custom Title"
        };

        // Act & Assert
        Assert.Equal("Custom Title", session.DisplayTitle);
    }

    [Fact]
    public void DisplayTitle_ReturnsName_WhenTitleNull()
    {
        // Arrange
        var session = new TerminalSession
        {
            Name = "MyTerminal",
            Title = null
        };

        // Act & Assert
        Assert.Equal("MyTerminal", session.DisplayTitle);
    }

    [Theory]
    [InlineData(TerminalSessionState.Starting, false)]
    [InlineData(TerminalSessionState.Running, false)]
    [InlineData(TerminalSessionState.Exited, true)]
    [InlineData(TerminalSessionState.Error, true)]
    [InlineData(TerminalSessionState.Closing, true)]
    public void IsTerminated_ReturnsExpectedValue(TerminalSessionState state, bool expected)
    {
        // Arrange
        var session = new TerminalSession { State = state };

        // Act & Assert
        Assert.Equal(expected, session.IsTerminated);
    }

    [Fact]
    public void Duration_IsNonNegative_WhenRunning()
    {
        // Arrange
        var session = new TerminalSession
        {
            State = TerminalSessionState.Running
        };

        // Act
        var duration = session.Duration;

        // Assert
        Assert.True(duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task DisposeAsync_CallsOnDisposeAsync()
    {
        // Arrange
        var wasCalled = false;
        var session = new TerminalSession();
        session.OnDisposeAsync = () =>
        {
            wasCalled = true;
            return ValueTask.CompletedTask;
        };

        // Act
        await session.DisposeAsync();

        // Assert
        Assert.True(wasCalled);
    }

    [Fact]
    public async Task DisposeAsync_NoCallback_DoesNotThrow()
    {
        // Arrange
        var session = new TerminalSession();

        // Act - should not throw
        await session.DisposeAsync();
    }

    [Fact]
    public void Environment_CanStoreVariables()
    {
        // Arrange
        var session = new TerminalSession
        {
            Environment =
            {
                ["TERM"] = "xterm-256color",
                ["COLORTERM"] = "truecolor"
            }
        };

        // Assert
        Assert.True(session.Environment.ContainsKey("TERM"));
        Assert.Equal("xterm-256color", session.Environment["TERM"]);
        Assert.Equal("truecolor", session.Environment["COLORTERM"]);
    }

    [Fact]
    public void ToString_ContainsKeyInfo()
    {
        // Arrange
        var session = new TerminalSession
        {
            Name = "Test",
            State = TerminalSessionState.Running
        };

        // Act
        var result = session.ToString();

        // Assert
        Assert.Contains("TerminalSession", result);
        Assert.Contains("Test", result);
        Assert.Contains("Running", result);
    }
}
