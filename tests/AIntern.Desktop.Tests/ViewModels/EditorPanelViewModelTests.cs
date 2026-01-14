using AIntern.Core.Interfaces;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="EditorPanelViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests cover:
/// </para>
/// <list type="bullet">
///   <item><description>File operations (open, new, save)</description></item>
///   <item><description>Tab management (close, navigate)</description></item>
///   <item><description>Editor commands (undo, redo, find)</description></item>
///   <item><description>Unsaved changes prompt flow</description></item>
/// </list>
/// <para>Added in v0.3.3b.</para>
/// </remarks>
public class EditorPanelViewModelTests : IDisposable
{
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly Mock<IDialogService> _mockDialog;
    private readonly Mock<ILogger<EditorPanelViewModel>> _mockLogger;
    private EditorPanelViewModel? _viewModel;

    public EditorPanelViewModelTests()
    {
        _mockFileSystem = new Mock<IFileSystemService>();
        _mockDialog = new Mock<IDialogService>();
        _mockLogger = new Mock<ILogger<EditorPanelViewModel>>();
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }

    private EditorPanelViewModel CreateViewModel()
    {
        _viewModel = new EditorPanelViewModel(
            _mockFileSystem.Object,
            _mockDialog.Object,
            _mockLogger.Object);
        return _viewModel;
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies constructor initializes empty tabs collection.
    /// </summary>
    [Fact]
    public void Constructor_InitializesEmptyTabsCollection()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm.Tabs);
        Assert.Empty(vm.Tabs);
        Assert.Null(vm.ActiveTab);
    }

    /// <summary>
    /// Verifies constructor throws on null fileSystemService.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsOnNullFileSystemService()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EditorPanelViewModel(null!, _mockDialog.Object));
    }

    /// <summary>
    /// Verifies constructor throws on null dialogService.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsOnNullDialogService()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EditorPanelViewModel(_mockFileSystem.Object, null!));
    }

    #endregion

    #region NewFile Tests

    /// <summary>
    /// Verifies NewFile creates tab with counter.
    /// </summary>
    [Fact]
    public void NewFile_CreatesTabWithCounter()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.NewFile();

        // Assert
        Assert.Single(vm.Tabs);
        Assert.Equal("Untitled-1", vm.Tabs[0].FileName);
        Assert.Equal(vm.Tabs[0], vm.ActiveTab);
    }

    /// <summary>
    /// Verifies NewFile increments counter.
    /// </summary>
    [Fact]
    public void NewFile_IncrementsCounter()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.NewFile();
        vm.NewFile();
        vm.NewFile();

        // Assert
        Assert.Equal(3, vm.Tabs.Count);
        Assert.Equal("Untitled-1", vm.Tabs[0].FileName);
        Assert.Equal("Untitled-2", vm.Tabs[1].FileName);
        Assert.Equal("Untitled-3", vm.Tabs[2].FileName);
    }

    /// <summary>
    /// Verifies NewFile sets tab as active.
    /// </summary>
    [Fact]
    public void NewFile_SetsTabAsActive()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.NewFile();

        // Assert
        Assert.NotNull(vm.ActiveTab);
        Assert.True(vm.ActiveTab.IsActive);
    }

    /// <summary>
    /// Verifies NewFile raises TabOpened event.
    /// </summary>
    [Fact]
    public void NewFile_RaisesTabOpenedEvent()
    {
        // Arrange
        var vm = CreateViewModel();
        EditorTabViewModel? openedTab = null;
        vm.TabOpened += (s, e) => openedTab = e;

        // Act
        vm.NewFile();

        // Assert
        Assert.NotNull(openedTab);
        Assert.Equal("Untitled-1", openedTab.FileName);
    }

    #endregion

    #region OpenFile Tests

    /// <summary>
    /// Verifies OpenFileAsync creates new tab for new file.
    /// </summary>
    [Fact]
    public async Task OpenFileAsync_CreatesNewTab()
    {
        // Arrange
        var vm = CreateViewModel();
        _mockFileSystem.Setup(f => f.ReadFileAsync("/path/to/file.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync("class Foo {}");

        // Act
        await vm.OpenFileAsync("/path/to/file.cs");

        // Assert
        Assert.Single(vm.Tabs);
        Assert.Equal("file.cs", vm.Tabs[0].FileName);
        Assert.Equal("/path/to/file.cs", vm.Tabs[0].FilePath);
    }

    /// <summary>
    /// Verifies OpenFileAsync activates existing tab.
    /// </summary>
    [Fact]
    public async Task OpenFileAsync_ActivatesExistingTab()
    {
        // Arrange
        var vm = CreateViewModel();
        _mockFileSystem.Setup(f => f.ReadFileAsync("/path/to/file.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync("content");

        await vm.OpenFileAsync("/path/to/file.cs");
        vm.NewFile(); // Create another tab, making it active

        // Act - open same file again
        await vm.OpenFileAsync("/path/to/file.cs");

        // Assert
        Assert.Equal(2, vm.Tabs.Count); // Still 2 tabs
        Assert.Equal("file.cs", vm.ActiveTab?.FileName);
        _mockFileSystem.Verify(f => f.ReadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies OpenFileAsync shows error on failure.
    /// </summary>
    [Fact]
    public async Task OpenFileAsync_ShowsErrorOnFailure()
    {
        // Arrange
        var vm = CreateViewModel();
        _mockFileSystem.Setup(f => f.ReadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("File not found"));

        // Act
        await vm.OpenFileAsync("/missing/file.cs");

        // Assert
        Assert.Empty(vm.Tabs);
        _mockDialog.Verify(d => d.ShowErrorAsync("Error Opening File", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Tab Navigation Tests

    /// <summary>
    /// Verifies NextTab moves to next tab.
    /// </summary>
    [Fact]
    public void NextTab_MovesToNextTab()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        vm.NewFile();
        vm.NewFile();
        vm.ActiveTab = vm.Tabs[0]; // Start at first

        // Act
        vm.NextTab();

        // Assert
        Assert.Equal(vm.Tabs[1], vm.ActiveTab);
    }

    /// <summary>
    /// Verifies NextTab wraps around.
    /// </summary>
    [Fact]
    public void NextTab_WrapsAround()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        vm.NewFile();
        vm.ActiveTab = vm.Tabs[1]; // Start at last

        // Act
        vm.NextTab();

        // Assert
        Assert.Equal(vm.Tabs[0], vm.ActiveTab);
    }

    /// <summary>
    /// Verifies PreviousTab moves to previous tab.
    /// </summary>
    [Fact]
    public void PreviousTab_MovesToPreviousTab()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        vm.NewFile();
        vm.NewFile();
        vm.ActiveTab = vm.Tabs[2]; // Start at last

        // Act
        vm.PreviousTab();

        // Assert
        Assert.Equal(vm.Tabs[1], vm.ActiveTab);
    }

    /// <summary>
    /// Verifies PreviousTab wraps around.
    /// </summary>
    [Fact]
    public void PreviousTab_WrapsAround()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        vm.NewFile();
        vm.ActiveTab = vm.Tabs[0]; // Start at first

        // Act
        vm.PreviousTab();

        // Assert
        Assert.Equal(vm.Tabs[1], vm.ActiveTab);
    }

    /// <summary>
    /// Verifies navigation does nothing with single tab.
    /// </summary>
    [Fact]
    public void NextTab_DoesNothingWithSingleTab()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        var originalTab = vm.ActiveTab;

        // Act
        vm.NextTab();
        vm.PreviousTab();

        // Assert
        Assert.Equal(originalTab, vm.ActiveTab);
    }

    #endregion

    #region MoveTab Tests

    /// <summary>
    /// Verifies MoveTab reorders tabs.
    /// </summary>
    [Fact]
    public void MoveTab_ReordersTabs()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        vm.NewFile();
        vm.NewFile();
        var tab1 = vm.Tabs[0];
        var tab2 = vm.Tabs[1];
        var tab3 = vm.Tabs[2];

        // Act
        vm.MoveTab(0, 2);

        // Assert
        Assert.Equal(tab2, vm.Tabs[0]);
        Assert.Equal(tab3, vm.Tabs[1]);
        Assert.Equal(tab1, vm.Tabs[2]);
    }

    /// <summary>
    /// Verifies MoveTab ignores invalid indices.
    /// </summary>
    [Fact]
    public void MoveTab_IgnoresInvalidIndices()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        vm.NewFile();
        var originalOrder = vm.Tabs.ToList();

        // Act
        vm.MoveTab(-1, 0);
        vm.MoveTab(0, 10);
        vm.MoveTab(10, 0);

        // Assert - order unchanged
        Assert.Equal(originalOrder[0], vm.Tabs[0]);
        Assert.Equal(originalOrder[1], vm.Tabs[1]);
    }

    #endregion

    #region CloseTab Tests

    /// <summary>
    /// Verifies CloseTabAsync closes clean tab without prompt.
    /// </summary>
    [Fact]
    public async Task CloseTabAsync_ClosesCleanTabWithoutPrompt()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        var tab = vm.Tabs[0];

        // Act
        await vm.CloseTabAsync(tab);

        // Assert
        Assert.Empty(vm.Tabs);
        Assert.Null(vm.ActiveTab);
        _mockDialog.Verify(d => d.ShowConfirmDialogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies CloseTabAsync activates adjacent tab.
    /// </summary>
    [Fact]
    public async Task CloseTabAsync_ActivatesAdjacentTab()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        vm.NewFile();
        vm.NewFile();
        vm.ActiveTab = vm.Tabs[1]; // Middle tab

        // Act
        await vm.CloseTabAsync(vm.Tabs[1]);

        // Assert
        Assert.Equal(2, vm.Tabs.Count);
        Assert.NotNull(vm.ActiveTab);
    }

    /// <summary>
    /// Verifies CloseTabAsync raises TabClosed event.
    /// </summary>
    [Fact]
    public async Task CloseTabAsync_RaisesTabClosedEvent()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        var tab = vm.Tabs[0];
        EditorTabViewModel? closedTab = null;
        vm.TabClosed += (s, e) => closedTab = e;

        // Act
        await vm.CloseTabAsync(tab);

        // Assert
        Assert.Equal(tab, closedTab);
    }

    #endregion

    #region Computed Properties Tests

    /// <summary>
    /// Verifies HasOpenTabs is false when empty.
    /// </summary>
    [Fact]
    public void HasOpenTabs_FalseWhenEmpty()
    {
        // Arrange
        var vm = CreateViewModel();

        // Assert
        Assert.False(vm.HasOpenTabs);
    }

    /// <summary>
    /// Verifies HasOpenTabs is true with tabs.
    /// </summary>
    [Fact]
    public void HasOpenTabs_TrueWithTabs()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();

        // Assert
        Assert.True(vm.HasOpenTabs);
    }

    /// <summary>
    /// Verifies HasUnsavedChanges tracks dirty tabs.
    /// </summary>
    [Fact]
    public void HasUnsavedChanges_TracksDirtyTabs()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        Assert.False(vm.HasUnsavedChanges);

        // Act
        vm.Tabs[0].Document.Text = "some content";

        // Assert
        Assert.True(vm.HasUnsavedChanges);
        Assert.Equal(1, vm.UnsavedTabsCount);
    }

    #endregion

    #region Editor Command Tests

    /// <summary>
    /// Verifies Undo raises UndoRequested event.
    /// </summary>
    [Fact]
    public void Undo_RaisesUndoRequestedEvent()
    {
        // Arrange
        var vm = CreateViewModel();
        var raised = false;
        vm.UndoRequested += (s, e) => raised = true;

        // Act
        vm.Undo();

        // Assert
        Assert.True(raised);
    }

    /// <summary>
    /// Verifies Redo raises RedoRequested event.
    /// </summary>
    [Fact]
    public void Redo_RaisesRedoRequestedEvent()
    {
        // Arrange
        var vm = CreateViewModel();
        var raised = false;
        vm.RedoRequested += (s, e) => raised = true;

        // Act
        vm.Redo();

        // Assert
        Assert.True(raised);
    }

    /// <summary>
    /// Verifies Find raises FindRequested event.
    /// </summary>
    [Fact]
    public void Find_RaisesFindRequestedEvent()
    {
        // Arrange
        var vm = CreateViewModel();
        var raised = false;
        vm.FindRequested += (s, e) => raised = true;

        // Act
        vm.Find();

        // Assert
        Assert.True(raised);
    }

    /// <summary>
    /// Verifies Replace raises ReplaceRequested event.
    /// </summary>
    [Fact]
    public void Replace_RaisesReplaceRequestedEvent()
    {
        // Arrange
        var vm = CreateViewModel();
        var raised = false;
        vm.ReplaceRequested += (s, e) => raised = true;

        // Act
        vm.Replace();

        // Assert
        Assert.True(raised);
    }

    #endregion

    #region GetTabByPath Tests

    /// <summary>
    /// Verifies GetTabByPath finds open tab.
    /// </summary>
    [Fact]
    public async Task GetTabByPath_FindsOpenTab()
    {
        // Arrange
        var vm = CreateViewModel();
        _mockFileSystem.Setup(f => f.ReadFileAsync("/path/to/file.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync("content");
        await vm.OpenFileAsync("/path/to/file.cs");

        // Act
        var tab = vm.GetTabByPath("/path/to/file.cs");

        // Assert
        Assert.NotNull(tab);
        Assert.Equal("file.cs", tab.FileName);
    }

    /// <summary>
    /// Verifies GetTabByPath returns null for unopened file.
    /// </summary>
    [Fact]
    public void GetTabByPath_ReturnsNullForUnopenedFile()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        var tab = vm.GetTabByPath("/missing/file.cs");

        // Assert
        Assert.Null(tab);
    }

    /// <summary>
    /// Verifies IsFileOpen returns correct value.
    /// </summary>
    [Fact]
    public async Task IsFileOpen_ReturnsCorrectValue()
    {
        // Arrange
        var vm = CreateViewModel();
        _mockFileSystem.Setup(f => f.ReadFileAsync("/path/to/file.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync("content");
        await vm.OpenFileAsync("/path/to/file.cs");

        // Assert
        Assert.True(vm.IsFileOpen("/path/to/file.cs"));
        Assert.False(vm.IsFileOpen("/other/file.cs"));
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies Dispose clears all tabs.
    /// </summary>
    [Fact]
    public void Dispose_ClearsAllTabs()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NewFile();
        vm.NewFile();
        vm.NewFile();

        // Act
        vm.Dispose();

        // Assert
        Assert.Empty(vm.Tabs);
    }

    #endregion
}
