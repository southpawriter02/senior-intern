using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Core.Events;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="FileExplorerViewModel"/>.
/// </summary>
public class FileExplorerViewModelTests : IDisposable
{
    private readonly Mock<IWorkspaceService> _mockWorkspaceService;
    private readonly Mock<IFileSystemService> _mockFileSystemService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<FileExplorerViewModel>> _mockLogger;
    private FileExplorerViewModel? _viewModel;

    public FileExplorerViewModelTests()
    {
        _mockWorkspaceService = new Mock<IWorkspaceService>();
        _mockFileSystemService = new Mock<IFileSystemService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<FileExplorerViewModel>>();

        // Setup default settings
        _mockSettingsService.Setup(s => s.CurrentSettings)
            .Returns(new AppSettings());
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }

    private FileExplorerViewModel CreateViewModel()
    {
        _viewModel = new FileExplorerViewModel(
            _mockWorkspaceService.Object,
            _mockFileSystemService.Object,
            _mockSettingsService.Object,
            null, // StorageProvider not available in tests
            _mockLogger.Object);
        return _viewModel;
    }

    #region Initial State Tests

    [Fact]
    public void Constructor_InitializesDefaultState()
    {
        var vm = CreateViewModel();

        Assert.False(vm.HasWorkspace);
        Assert.Empty(vm.WorkspaceName);
        Assert.Empty(vm.WorkspacePath);
        Assert.False(vm.IsLoading);
        Assert.Empty(vm.SearchFilter);
        Assert.Empty(vm.RootItems);
    }

    [Fact]
    public void RootItems_IsNotNull()
    {
        var vm = CreateViewModel();
        Assert.NotNull(vm.RootItems);
    }

    [Fact]
    public void SelectedItem_IsNullByDefault()
    {
        var vm = CreateViewModel();
        Assert.Null(vm.SelectedItem);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void HasWorkspace_DefaultsFalse()
    {
        var vm = CreateViewModel();
        Assert.False(vm.HasWorkspace);
    }

    [Fact]
    public void IsLoading_DefaultsFalse()
    {
        var vm = CreateViewModel();
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void FilteredItemCount_DefaultsZero()
    {
        var vm = CreateViewModel();
        Assert.Equal(0, vm.FilteredItemCount);
    }

    #endregion

    #region Event Args Tests

    [Fact]
    public void FileOpenRequestedEventArgs_StoresPath()
    {
        var args = new FileOpenRequestedEventArgs("/test/path.cs");
        Assert.Equal("/test/path.cs", args.FilePath);
    }

    [Fact]
    public void FileAttachRequestedEventArgs_StoresPath()
    {
        var args = new FileAttachRequestedEventArgs("/test/data.json");
        Assert.Equal("/test/data.json", args.FilePath);
    }

    [Fact]
    public void DeleteConfirmationEventArgs_StoresProperties()
    {
        var args = new DeleteConfirmationEventArgs("/test/folder", true);
        Assert.Equal("/test/folder", args.Path);
        Assert.True(args.IsDirectory);
        Assert.False(args.Confirmed);
    }

    #endregion

    #region IFileTreeItemParent Tests

    [Fact]
    public void GetRelativePath_DelegatesToWorkspace()
    {
        var workspace = new Workspace { RootPath = "/project" };
        _mockWorkspaceService.Setup(w => w.CurrentWorkspace).Returns(workspace);

        var vm = CreateViewModel();
        var result = vm.GetRelativePath("/project/src/test.cs");

        Assert.Equal("src/test.cs", result);
    }

    [Fact]
    public void GetRelativePath_ReturnsAbsolutePath_WhenNoWorkspace()
    {
        _mockWorkspaceService.Setup(w => w.CurrentWorkspace).Returns((Workspace?)null);

        var vm = CreateViewModel();
        var result = vm.GetRelativePath("/project/src/test.cs");

        Assert.Equal("/project/src/test.cs", result);
    }

    [Fact]
    public void ShowError_SetsErrorMessage()
    {
        var vm = CreateViewModel();

        vm.ShowError("Test error");

        Assert.Equal("Test error", vm.ErrorMessage);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledTwice()
    {
        var vm = CreateViewModel();

        vm.Dispose();
        vm.Dispose(); // Should not throw
    }

    #endregion
}
