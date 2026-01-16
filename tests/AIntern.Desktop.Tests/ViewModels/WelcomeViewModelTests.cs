namespace AIntern.Desktop.Tests.ViewModels;

using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="WelcomeViewModel"/> (v0.3.5h).
/// </summary>
public class WelcomeViewModelTests
{
    private readonly Mock<IWorkspaceService> _mockWorkspaceService = new();
    private readonly Mock<ILogger<WelcomeViewModel>> _mockLogger = new();

    private WelcomeViewModel CreateViewModel()
    {
        return new WelcomeViewModel(_mockWorkspaceService.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies constructor throws for null workspaceService.
    /// </summary>
    [Fact]
    public void Constructor_NullWorkspaceService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WelcomeViewModel(null!));
    }

    /// <summary>
    /// Verifies constructor creates empty recent workspaces.
    /// </summary>
    [Fact]
    public void Constructor_InitializesEmptyRecentWorkspaces()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm.RecentWorkspaces);
        Assert.Empty(vm.RecentWorkspaces);
        Assert.False(vm.HasRecentWorkspaces);
    }

    #endregion

    #region LoadAsync Tests

    /// <summary>
    /// Verifies LoadAsync populates recent workspaces.
    /// </summary>
    [Fact]
    public async Task LoadAsync_PopulatesRecentWorkspaces()
    {
        // Arrange
        var workspaces = new List<Workspace>
        {
            new() { Id = Guid.NewGuid(), RootPath = "/path/one", Name = "one" },
            new() { Id = Guid.NewGuid(), RootPath = "/path/two", Name = "two" }
        };
        _mockWorkspaceService
            .Setup(s => s.GetRecentWorkspacesAsync(5, default))
            .ReturnsAsync(workspaces);

        var vm = CreateViewModel();

        // Act
        await vm.LoadAsync();

        // Assert
        Assert.Equal(2, vm.RecentWorkspaces.Count);
        Assert.True(vm.HasRecentWorkspaces);
    }

    /// <summary>
    /// Verifies LoadAsync handles empty list.
    /// </summary>
    [Fact]
    public async Task LoadAsync_NoWorkspaces_SetsHasRecentWorkspacesFalse()
    {
        // Arrange
        _mockWorkspaceService
            .Setup(s => s.GetRecentWorkspacesAsync(5, default))
            .ReturnsAsync(new List<Workspace>());

        var vm = CreateViewModel();

        // Act
        await vm.LoadAsync();

        // Assert
        Assert.Empty(vm.RecentWorkspaces);
        Assert.False(vm.HasRecentWorkspaces);
    }

    #endregion

    #region Command Tests

    /// <summary>
    /// Verifies OpenFolderCommand raises event.
    /// </summary>
    [Fact]
    public void OpenFolderCommand_RaisesEvent()
    {
        // Arrange
        var vm = CreateViewModel();
        string? receivedPath = null;
        vm.WorkspaceOpenRequested += (_, path) => receivedPath = path;

        // Act
        vm.OpenFolderCommand.Execute(null);

        // Assert
        Assert.Equal(string.Empty, receivedPath);
    }

    /// <summary>
    /// Verifies NewFileCommand raises event.
    /// </summary>
    [Fact]
    public void NewFileCommand_RaisesEvent()
    {
        // Arrange
        var vm = CreateViewModel();
        var eventRaised = false;
        vm.NewFileRequested += (_, _) => eventRaised = true;

        // Act
        vm.NewFileCommand.Execute(null);

        // Assert
        Assert.True(eventRaised);
    }

    /// <summary>
    /// Verifies OpenRecentCommand raises event with path.
    /// </summary>
    [Fact]
    public void OpenRecentCommand_RaisesEventWithPath()
    {
        // Arrange
        var vm = CreateViewModel();
        string? receivedPath = null;
        vm.WorkspaceOpenRequested += (_, path) => receivedPath = path;

        // Act
        vm.OpenRecentCommand.Execute("/test/path");

        // Assert
        Assert.Equal("/test/path", receivedPath);
    }

    #endregion
}
