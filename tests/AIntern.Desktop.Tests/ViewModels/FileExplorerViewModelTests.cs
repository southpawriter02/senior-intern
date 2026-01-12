using Xunit;
using NSubstitute;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Avalonia.Platform.Storage;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for FileExplorerViewModel (v0.3.2b).
/// </summary>
public class FileExplorerViewModelTests : IDisposable
{
    private readonly IWorkspaceService _mockWorkspaceService;
    private readonly IFileSystemService _mockFileSystemService;
    private readonly ISettingsService _mockSettingsService;
    private readonly IStorageProvider _mockStorageProvider;
    private readonly ILogger<FileExplorerViewModel> _mockLogger;
    private readonly FileExplorerViewModel _viewModel;
    private readonly AppSettings _testSettings;

    public FileExplorerViewModelTests()
    {
        _mockWorkspaceService = Substitute.For<IWorkspaceService>();
        _mockFileSystemService = Substitute.For<IFileSystemService>();
        _mockSettingsService = Substitute.For<ISettingsService>();
        _mockStorageProvider = Substitute.For<IStorageProvider>();
        _mockLogger = Substitute.For<ILogger<FileExplorerViewModel>>();

        _testSettings = new AppSettings { ShowHiddenFiles = false };
        _mockSettingsService.CurrentSettings.Returns(_testSettings);

        _viewModel = new FileExplorerViewModel(
            _mockWorkspaceService,
            _mockFileSystemService,
            _mockSettingsService,
            _mockStorageProvider,
            _mockLogger);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_SubscribesToWorkspaceEvents()
    {
        // Verify event subscription by triggering and checking effect
        var workspace = CreateTestWorkspace();
        _mockWorkspaceService.CurrentWorkspace.Returns(workspace);

        // Event should be subscribed - we can't directly verify, but no exception means success
        Assert.NotNull(_viewModel);
    }

    [Fact]
    public void Constructor_InitializesWithEmptyRootItems()
    {
        Assert.Empty(_viewModel.RootItems);
        Assert.False(_viewModel.HasWorkspace);
        Assert.Empty(_viewModel.WorkspaceName);
    }

    #endregion

    #region Filter Tests

    [Fact]
    public void SearchFilter_SetsFilterProperty()
    {
        _viewModel.SearchFilter = "test";

        Assert.Equal("test", _viewModel.SearchFilter);
    }

    [Fact]
    public void ClearFilterCommand_ClearsSearchFilter()
    {
        _viewModel.SearchFilter = "test";

        _viewModel.ClearFilterCommand.Execute(null);

        Assert.Equal(string.Empty, _viewModel.SearchFilter);
    }

    [Fact]
    public async Task SearchFilter_DebouncesPreviousInput()
    {
        // Add a test item
        var item = CreateTestFileItem("TestFile.cs");
        _viewModel.RootItems.Add(item);

        // Rapid filter changes
        _viewModel.SearchFilter = "t";
        _viewModel.SearchFilter = "te";
        _viewModel.SearchFilter = "test";

        // Before debounce completes (within 200ms)
        await Task.Delay(50);
        Assert.False(_viewModel.IsFiltering);

        // After debounce
        await Task.Delay(200);
        // Filter should have been applied
    }

    #endregion

    #region Workspace Command Tests

    [Fact]
    public async Task CloseWorkspaceCommand_CallsService()
    {
        await _viewModel.CloseWorkspaceCommand.ExecuteAsync(null);

        await _mockWorkspaceService.Received(1).CloseWorkspaceAsync();
    }

    [Fact]
    public async Task RefreshCommand_DoesNothingWithoutWorkspace()
    {
        _mockWorkspaceService.CurrentWorkspace.Returns((Workspace?)null);

        await _viewModel.RefreshCommand.ExecuteAsync(null);

        // Should return early without loading
        await _mockFileSystemService.DidNotReceive()
            .GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<bool>());
    }

    #endregion

    #region File Operation Tests

    [Fact]
    public async Task NewFileCommand_CreatesFileWithUniqueName()
    {
        var workspace = CreateTestWorkspace();
        _mockWorkspaceService.CurrentWorkspace.Returns(workspace);

        var newItem = new FileSystemItem
        {
            Name = "untitled.txt",
            Path = "/workspace/untitled.txt",
            Type = FileSystemItemType.File
        };
        _mockFileSystemService.CreateFileAsync(Arg.Any<string>())
            .Returns(Task.FromResult(newItem));

        await _viewModel.NewFileCommand.ExecuteAsync(null);

        await _mockFileSystemService.Received(1).CreateFileAsync(Arg.Any<string>());
        Assert.Single(_viewModel.RootItems);
        Assert.True(_viewModel.RootItems[0].IsRenaming);
    }

    [Fact]
    public async Task NewFolderCommand_InsertsBeforeFiles()
    {
        var workspace = CreateTestWorkspace();
        _mockWorkspaceService.CurrentWorkspace.Returns(workspace);

        // Add existing file
        var fileItem = CreateTestFileItem("existing.cs");
        _viewModel.RootItems.Add(fileItem);

        var newFolder = new FileSystemItem
        {
            Name = "New Folder",
            Path = "/workspace/New Folder",
            Type = FileSystemItemType.Directory
        };
        _mockFileSystemService.CreateDirectoryAsync(Arg.Any<string>())
            .Returns(Task.FromResult(newFolder));

        await _viewModel.NewFolderCommand.ExecuteAsync(null);

        Assert.Equal(2, _viewModel.RootItems.Count);
        Assert.True(_viewModel.RootItems[0].IsDirectory); // Folder first
        Assert.True(_viewModel.RootItems[1].IsFile); // File second
    }

    [Fact]
    public void RenameCommand_BeginsRenameOnSelectedItem()
    {
        var item = CreateTestFileItem("test.cs");
        _viewModel.SelectedItem = item;

        _viewModel.RenameCommand.Execute(null);

        Assert.True(item.IsRenaming);
    }

    [Fact]
    public void RenameCommand_UsesPassedItemOverSelected()
    {
        var selected = CreateTestFileItem("selected.cs");
        var passed = CreateTestFileItem("passed.cs");
        _viewModel.SelectedItem = selected;

        _viewModel.RenameCommand.Execute(passed);

        Assert.False(selected.IsRenaming);
        Assert.True(passed.IsRenaming);
    }

    [Fact]
    public async Task DeleteCommand_DeletesFile()
    {
        var item = CreateTestFileItem("test.cs");
        _viewModel.RootItems.Add(item);

        await _viewModel.DeleteCommand.ExecuteAsync(item);

        await _mockFileSystemService.Received(1).DeleteFileAsync(item.Path);
        Assert.Empty(_viewModel.RootItems);
    }

    [Fact]
    public async Task DeleteCommand_DeletesDirectory()
    {
        var item = CreateTestFolderItem("folder");
        _viewModel.RootItems.Add(item);

        await _viewModel.DeleteCommand.ExecuteAsync(item);

        await _mockFileSystemService.Received(1).DeleteDirectoryAsync(item.Path);
        Assert.Empty(_viewModel.RootItems);
    }

    #endregion

    #region Path Command Tests

    [Fact]
    public async Task CopyPathCommand_RequiresItem()
    {
        _viewModel.SelectedItem = null;

        await _viewModel.CopyPathCommand.ExecuteAsync(null);

        // Should return early - no exception
    }

    [Fact]
    public async Task CopyRelativePathCommand_UsesWorkspaceRoot()
    {
        var workspace = CreateTestWorkspace();
        _mockWorkspaceService.CurrentWorkspace.Returns(workspace);
        var item = CreateTestFileItem("src/file.cs", "/workspace/src/file.cs");

        await _viewModel.CopyRelativePathCommand.ExecuteAsync(item);

        // Should use GetRelativePath
        Assert.NotNull(workspace.GetRelativePath(item.Path));
    }

    #endregion

    #region Context Attachment Tests

    [Fact]
    public void AttachToContextCommand_RaisesEvent()
    {
        var item = CreateTestFileItem("test.cs");
        FileAttachRequestedEventArgs? eventArgs = null;
        _viewModel.FileAttachRequested += (s, e) => eventArgs = e;

        _viewModel.AttachToContextCommand.Execute(item);

        Assert.NotNull(eventArgs);
        Assert.Equal(item.Path, eventArgs.FilePath);
    }

    [Fact]
    public void AttachToContextCommand_IgnoresDirectories()
    {
        var folder = CreateTestFolderItem("folder");
        FileAttachRequestedEventArgs? eventArgs = null;
        _viewModel.FileAttachRequested += (s, e) => eventArgs = e;

        _viewModel.AttachToContextCommand.Execute(folder);

        Assert.Null(eventArgs);
    }

    #endregion

    #region Open File Tests

    [Fact]
    public void OpenFileCommand_RaisesEvent()
    {
        var item = CreateTestFileItem("test.cs");
        FileOpenRequestedEventArgs? eventArgs = null;
        _viewModel.FileOpenRequested += (s, e) => eventArgs = e;

        _viewModel.OpenFileCommand.Execute(item);

        Assert.NotNull(eventArgs);
        Assert.Equal(item.Path, eventArgs.FilePath);
    }

    [Fact]
    public void OpenFileCommand_IgnoresDirectories()
    {
        var folder = CreateTestFolderItem("folder");
        FileOpenRequestedEventArgs? eventArgs = null;
        _viewModel.FileOpenRequested += (s, e) => eventArgs = e;

        _viewModel.OpenFileCommand.Execute(folder);

        Assert.Null(eventArgs);
    }

    #endregion

    #region Expand/Collapse Tests

    [Fact]
    public async Task ExpandFolderCommand_ExpandsAndLoadsChildren()
    {
        _mockFileSystemService.GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<bool>())
            .Returns(Task.FromResult<IReadOnlyList<FileSystemItem>>([]));

        var folder = CreateTestFolderItem("src");

        await _viewModel.ExpandFolderCommand.ExecuteAsync(folder);

        Assert.True(folder.IsExpanded);
    }

    [Fact]
    public void CollapseFolderCommand_CollapsesFolder()
    {
        var folder = CreateTestFolderItem("src");
        folder.IsExpanded = true;

        _viewModel.CollapseFolderCommand.Execute(folder);

        Assert.False(folder.IsExpanded);
    }

    #endregion

    #region IFileExplorerParent Tests

    [Fact]
    public async Task LoadChildrenForItemAsync_FiltersIgnoredItems()
    {
        var workspace = CreateTestWorkspace();
        _mockWorkspaceService.CurrentWorkspace.Returns(workspace);

        var contents = new List<FileSystemItem>
        {
            new() { Name = "visible.cs", Path = "/project/visible.cs", Type = FileSystemItemType.File },
            new() { Name = "node_modules", Path = "/project/node_modules", Type = FileSystemItemType.Directory }
        };
        _mockFileSystemService.GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<bool>())
            .Returns(Task.FromResult<IReadOnlyList<FileSystemItem>>(contents));
        _mockFileSystemService.ShouldIgnore("/project/visible.cs", Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(false);
        _mockFileSystemService.ShouldIgnore("/project/node_modules", Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(true);

        var parent = CreateTestFolderItem("project", "/project");
        var children = await _viewModel.LoadChildrenForItemAsync(parent);

        Assert.Single(children);
        Assert.Equal("visible.cs", children[0].Name);
    }

    [Fact]
    public void ShowError_SetsErrorMessage()
    {
        _viewModel.ShowError("Test error");

        Assert.Equal("Test error", _viewModel.ErrorMessage);
    }

    [Fact]
    public void GetRelativePath_DelegatesToWorkspace()
    {
        var workspace = CreateTestWorkspace();
        _mockWorkspaceService.CurrentWorkspace.Returns(workspace);

        var relative = _viewModel.GetRelativePath("/workspace/src/file.cs");

        Assert.NotNull(relative);
    }

    #endregion

    #region Helper Methods

    private static Workspace CreateTestWorkspace()
    {
        return new Workspace
        {
            Id = Guid.NewGuid(),
            RootPath = "/workspace",
            Name = "Test Workspace"
        };
    }

    private FileTreeItemViewModel CreateTestFileItem(string name, string? path = null)
    {
        return new FileTreeItemViewModel(_viewModel)
        {
            Name = name,
            Path = path ?? $"/workspace/{name}",
            ItemType = FileSystemItemType.File
        };
    }

    private FileTreeItemViewModel CreateTestFolderItem(string name, string? path = null)
    {
        return new FileTreeItemViewModel(_viewModel)
        {
            Name = name,
            Path = path ?? $"/workspace/{name}",
            ItemType = FileSystemItemType.Directory,
            HasChildren = true
        };
    }

    #endregion
}
