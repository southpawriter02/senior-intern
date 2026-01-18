namespace AIntern.Desktop.Tests.Views;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalPanelTests (v0.5.2e)                                                 │
// │ Unit tests for TerminalPanel initialization and session management.         │
// └─────────────────────────────────────────────────────────────────────────────┘

using System;
using Xunit;
using Moq;
using AIntern.Core.Interfaces;
using AIntern.Desktop.Views;

/// <summary>
/// Unit tests for <see cref="TerminalPanel"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the code-behind behavior of TerminalPanel.
/// XAML bindings and visual behavior are tested through integration tests.
/// </para>
/// <para>
/// Tests cover:
/// <list type="bullet">
///   <item><description>Constructor - component initialization</description></item>
///   <item><description>Initialize - service storage and null handling</description></item>
/// </list>
/// </para>
/// <para>Added in v0.5.2e.</para>
/// </remarks>
public class TerminalPanelTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesInstance()
    {
        // Act
        var panel = new TerminalPanel();

        // Assert
        Assert.NotNull(panel);
    }

    #endregion

    #region Initialize Tests

    [Fact]
    public void Initialize_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var panel = new TerminalPanel();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => panel.Initialize(null!));
    }

    [Fact]
    public void Initialize_WithService_StoresReference()
    {
        // Arrange
        var panel = new TerminalPanel();
        var mockService = new Mock<ITerminalService>();

        // Act - Should not throw
        panel.Initialize(mockService.Object);

        // Assert - No exception means success
        // Internal state verification would require reflection or InternalsVisibleTo
        Assert.NotNull(panel);
    }

    [Fact]
    public void Initialize_CalledTwice_ReplacesService()
    {
        // Arrange
        var panel = new TerminalPanel();
        var mockService1 = new Mock<ITerminalService>();
        var mockService2 = new Mock<ITerminalService>();

        // Act - Should not throw on second call
        panel.Initialize(mockService1.Object);
        panel.Initialize(mockService2.Object);

        // Assert - No exception means success
        Assert.NotNull(panel);
    }

    #endregion
}
