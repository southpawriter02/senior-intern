namespace AIntern.Desktop.Tests.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalPanelViewModelTests (v0.5.2d)                                        │
// │ Unit tests for TerminalPanelViewModel commands and tab navigation.          │
// └─────────────────────────────────────────────────────────────────────────────┘

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.ViewModels;

/// <summary>
/// Unit tests for <see cref="TerminalPanelViewModel"/>.
/// </summary>
/// <remarks>
/// Tests cover:
/// <list type="bullet">
///   <item><description>Session activation and management</description></item>
///   <item><description>Tab navigation (circular)</description></item>
///   <item><description>Panel visibility and maximize state</description></item>
/// </list>
/// Note: Event handling tests require UI thread, so they use synchronous patterns.
/// Added in v0.5.2d.
/// </remarks>
public class TerminalPanelViewModelTests
{
    #region Test Helpers

    /// <summary>
    /// Creates a mock ITerminalService for testing.
    /// </summary>
    private static Mock<ITerminalService> CreateMockTerminalService()
    {
        var mock = new Mock<ITerminalService>();

        // Setup to allow event subscriptions without throwing
        mock.SetupAdd(s => s.SessionCreated += It.IsAny<EventHandler<TerminalSessionEventArgs>>());
        mock.SetupAdd(s => s.SessionClosed += It.IsAny<EventHandler<TerminalSessionEventArgs>>());
        mock.SetupAdd(s => s.SessionStateChanged += It.IsAny<EventHandler<TerminalSessionStateEventArgs>>());
        mock.SetupAdd(s => s.TitleChanged += It.IsAny<EventHandler<TerminalTitleEventArgs>>());

        return mock;
    }

    /// <summary>
    /// Creates a TerminalPanelViewModel with mock sessions.
    /// </summary>
    private static TerminalPanelViewModel CreateViewModelWithSessions(
        Mock<ITerminalService> mockService,
        int sessionCount)
    {
        var vm = new TerminalPanelViewModel(mockService.Object);

        // Add mock sessions directly to the collection
        for (int i = 0; i < sessionCount; i++)
        {
            var session = new TerminalSession
            {
                Id = Guid.NewGuid(),
                Name = $"Terminal {i + 1}",
                ShellPath = "/bin/bash",
                State = TerminalSessionState.Running
            };
            var sessionVm = new TerminalSessionViewModel(session);
            vm.Sessions.Add(sessionVm);
        }

        // Activate the first session if any exist
        if (vm.Sessions.Count > 0)
        {
            vm.ActivateSessionCommand.Execute(vm.Sessions[0]);
        }

        return vm;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullService()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TerminalPanelViewModel(null!));
    }

    [Fact]
    public void Constructor_InitializesEmptySessions()
    {
        // Arrange
        var mockService = CreateMockTerminalService();

        // Act
        var vm = new TerminalPanelViewModel(mockService.Object);

        // Assert
        Assert.Empty(vm.Sessions);
        Assert.Null(vm.ActiveSession);
        Assert.False(vm.HasActiveSession);
    }

    [Fact]
    public void Constructor_SetsDefaultPanelState()
    {
        // Arrange
        var mockService = CreateMockTerminalService();

        // Act
        var vm = new TerminalPanelViewModel(mockService.Object);

        // Assert
        Assert.False(vm.IsVisible);
        Assert.False(vm.IsMaximized);
        Assert.Equal(300, vm.PanelHeight);
    }

    [Fact]
    public void Constructor_SetsDefaultTerminalSettings()
    {
        // Arrange
        var mockService = CreateMockTerminalService();

        // Act
        var vm = new TerminalPanelViewModel(mockService.Object);

        // Assert
        Assert.Equal("Cascadia Mono", vm.FontFamily);
        Assert.Equal(14, vm.FontSize);
        Assert.Equal(TerminalTheme.Dark.Name, vm.Theme.Name);
    }

    #endregion

    #region ActivateSession Tests

    [Fact]
    public void ActivateSession_SetsActiveSession()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 2);
        var session = vm.Sessions[1];

        // Act
        vm.ActivateSessionCommand.Execute(session);

        // Assert
        Assert.Equal(session, vm.ActiveSession);
        Assert.True(session.IsActive);
        Assert.False(vm.Sessions[0].IsActive);
    }

    [Fact]
    public void ActivateSession_DeactivatesPreviousSession()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 2);
        var first = vm.Sessions[0];
        var second = vm.Sessions[1];

        // First is already active from setup
        Assert.True(first.IsActive);

        // Act
        vm.ActivateSessionCommand.Execute(second);

        // Assert
        Assert.False(first.IsActive);
        Assert.True(second.IsActive);
    }

    [Fact]
    public void ActivateSession_RaisesActiveSessionChangedEvent()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 2);
        TerminalSessionViewModel? eventSession = null;
        vm.ActiveSessionChanged += (_, s) => eventSession = s;

        // Act
        vm.ActivateSessionCommand.Execute(vm.Sessions[1]);

        // Assert
        Assert.Equal(vm.Sessions[1], eventSession);
    }

    [Fact]
    public void ActivateSession_IgnoresNull()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 1);
        var original = vm.ActiveSession;

        // Act
        vm.ActivateSessionCommand.Execute(null);

        // Assert
        Assert.Equal(original, vm.ActiveSession);
    }

    [Fact]
    public void ActivateSession_IgnoresSameSession()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 1);
        var callCount = 0;
        vm.ActiveSessionChanged += (_, _) => callCount++;

        // Already activated once in setup
        var initialCount = callCount;

        // Act - activate same session again
        vm.ActivateSessionCommand.Execute(vm.Sessions[0]);

        // Assert - no additional event
        Assert.Equal(initialCount, callCount);
    }

    #endregion

    #region Tab Navigation Tests

    [Fact]
    public void NextTab_AdvancesToNextSession()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 3);
        vm.ActivateSessionCommand.Execute(vm.Sessions[0]);

        // Act
        vm.NextTabCommand.Execute(null);

        // Assert
        Assert.Equal(vm.Sessions[1], vm.ActiveSession);
    }

    [Fact]
    public void NextTab_WrapsToBeginning()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 3);
        vm.ActivateSessionCommand.Execute(vm.Sessions[2]); // Last tab

        // Act
        vm.NextTabCommand.Execute(null);

        // Assert
        Assert.Equal(vm.Sessions[0], vm.ActiveSession);
    }

    [Fact]
    public void NextTab_DoesNothingWithSingleSession()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 1);
        var original = vm.ActiveSession;

        // Act
        vm.NextTabCommand.Execute(null);

        // Assert
        Assert.Equal(original, vm.ActiveSession);
    }

    [Fact]
    public void NextTab_DoesNothingWithNoSessions()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = new TerminalPanelViewModel(mockService.Object);

        // Act
        vm.NextTabCommand.Execute(null);

        // Assert
        Assert.Null(vm.ActiveSession);
    }

    [Fact]
    public void PreviousTab_GoesToPreviousSession()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 3);
        vm.ActivateSessionCommand.Execute(vm.Sessions[2]);

        // Act
        vm.PreviousTabCommand.Execute(null);

        // Assert
        Assert.Equal(vm.Sessions[1], vm.ActiveSession);
    }

    [Fact]
    public void PreviousTab_WrapsToEnd()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 3);
        vm.ActivateSessionCommand.Execute(vm.Sessions[0]); // First tab

        // Act
        vm.PreviousTabCommand.Execute(null);

        // Assert
        Assert.Equal(vm.Sessions[2], vm.ActiveSession);
    }

    [Fact]
    public void PreviousTab_DoesNothingWithSingleSession()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 1);
        var original = vm.ActiveSession;

        // Act
        vm.PreviousTabCommand.Execute(null);

        // Assert
        Assert.Equal(original, vm.ActiveSession);
    }

    #endregion

    #region Panel Visibility Tests

    [Fact]
    public void TogglePanel_TogglesVisibility()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = new TerminalPanelViewModel(mockService.Object);
        Assert.False(vm.IsVisible);

        // Act - toggle on
        vm.TogglePanelCommand.Execute(null);

        // Assert
        Assert.True(vm.IsVisible);

        // Act - toggle off
        vm.TogglePanelCommand.Execute(null);

        // Assert
        Assert.False(vm.IsVisible);
    }

    [Fact]
    public void ShowPanel_SetsVisibleTrue()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = new TerminalPanelViewModel(mockService.Object);

        // Act
        vm.ShowPanelCommand.Execute(null);

        // Assert
        Assert.True(vm.IsVisible);
    }

    [Fact]
    public void HidePanel_SetsVisibleFalse()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = new TerminalPanelViewModel(mockService.Object);
        vm.IsVisible = true;

        // Act
        vm.HidePanelCommand.Execute(null);

        // Assert
        Assert.False(vm.IsVisible);
    }

    [Fact]
    public void HidePanel_ResetsMaximizeState()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = new TerminalPanelViewModel(mockService.Object);
        vm.IsVisible = true;
        vm.IsMaximized = true;

        // Act
        vm.HidePanelCommand.Execute(null);

        // Assert
        Assert.False(vm.IsMaximized);
    }

    [Fact]
    public void ToggleMaximize_TogglesMaximizeState()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = new TerminalPanelViewModel(mockService.Object);
        Assert.False(vm.IsMaximized);

        // Act
        vm.ToggleMaximizeCommand.Execute(null);

        // Assert
        Assert.True(vm.IsMaximized);

        // Act
        vm.ToggleMaximizeCommand.Execute(null);

        // Assert
        Assert.False(vm.IsMaximized);
    }

    #endregion

    #region HasActiveSession Tests

    [Fact]
    public void HasActiveSession_ReturnsTrueWhenActive()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 1);

        // Assert
        Assert.True(vm.HasActiveSession);
    }

    [Fact]
    public void HasActiveSession_ReturnsFalseWhenNoSessions()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = new TerminalPanelViewModel(mockService.Object);

        // Assert
        Assert.False(vm.HasActiveSession);
    }

    #endregion

    #region Panel Height Tests

    [Fact]
    public void PanelHeight_DefaultIs300()
    {
        // Arrange
        var mockService = CreateMockTerminalService();

        // Act
        var vm = new TerminalPanelViewModel(mockService.Object);

        // Assert
        Assert.Equal(300, vm.PanelHeight);
    }

    [Fact]
    public void PanelHeight_CanBeChanged()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = new TerminalPanelViewModel(mockService.Object);

        // Act
        vm.PanelHeight = 400;

        // Assert
        Assert.Equal(400, vm.PanelHeight);
    }

    #endregion

    #region CloseActiveSession CanExecute Tests

    [Fact]
    public void CloseActiveSessionCommand_CanExecuteWhenActive()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = CreateViewModelWithSessions(mockService, 1);

        // Assert
        Assert.True(vm.CloseActiveSessionCommand.CanExecute(null));
    }

    [Fact]
    public void CloseActiveSessionCommand_CannotExecuteWhenNoActive()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = new TerminalPanelViewModel(mockService.Object);

        // Assert
        Assert.False(vm.CloseActiveSessionCommand.CanExecute(null));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        var mockService = CreateMockTerminalService();
        var vm = new TerminalPanelViewModel(mockService.Object);

        // Act
        vm.Dispose();

        // Assert - verify event unsubscription was called
        mockService.VerifyRemove(
            s => s.SessionCreated -= It.IsAny<EventHandler<TerminalSessionEventArgs>>(),
            Times.Once);
        mockService.VerifyRemove(
            s => s.SessionClosed -= It.IsAny<EventHandler<TerminalSessionEventArgs>>(),
            Times.Once);
    }

    #endregion
}
