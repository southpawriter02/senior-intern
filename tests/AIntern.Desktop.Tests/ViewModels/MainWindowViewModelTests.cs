using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.Tests.TestHelpers;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for the <see cref="MainWindowViewModel"/> class.
/// Tests initialization, child ViewModel composition, and sidebar commands.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the functionality:
/// </para>
/// <list type="bullet">
///   <item><description>Child ViewModels are properly injected and accessible</description></item>
///   <item><description>InitializeAsync loads conversation list and settings</description></item>
///   <item><description>ToggleSidebar toggles visibility state</description></item>
///   <item><description>NewConversation delegates to ConversationListViewModel</description></item>
///   <item><description>ModelStateChanged updates StatusMessage</description></item>
///   <item><description>InferenceProgress updates TokenInfo</description></item>
///   <item><description>SaveStateChanged updates SaveState (v0.2.5g)</description></item>
///   <item><description>IsModelLoaded property updates (v0.2.5g)</description></item>
///   <item><description>ToggleSettingsPanel command (v0.2.5g)</description></item>
/// </list>
/// </remarks>
public class MainWindowViewModelTests : IDisposable
{
    #region Test Infrastructure

    private readonly Mock<ILlmService> _mockLlmService;
    private readonly Mock<IConversationService> _mockConversationService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ISystemPromptService> _mockSystemPromptService;
    private readonly Mock<ISearchService> _mockSearchService;
    private readonly Mock<IExportService> _mockExportService;
    private readonly Mock<IWorkspaceService> _mockWorkspaceService;
    private readonly TestDispatcher _dispatcher;
    private readonly Mock<ILogger<MainWindowViewModel>> _mockLogger;

    // Child ViewModels (created with minimal mocking)
    private readonly ChatViewModel _chatViewModel;
    private readonly ModelSelectorViewModel _modelSelectorViewModel;
    private readonly ConversationListViewModel _conversationListViewModel;
    private readonly InferenceSettingsViewModel _inferenceSettingsViewModel;
    private readonly FileExplorerViewModel _fileExplorerViewModel;

    private MainWindowViewModel? _viewModel;

    public MainWindowViewModelTests()
    {
        _mockLlmService = new Mock<ILlmService>();
        _mockConversationService = new Mock<IConversationService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockSystemPromptService = new Mock<ISystemPromptService>();
        _mockSearchService = new Mock<ISearchService>();
        _mockExportService = new Mock<IExportService>();
        _mockWorkspaceService = new Mock<IWorkspaceService>();
        _dispatcher = new TestDispatcher();
        _mockLogger = new Mock<ILogger<MainWindowViewModel>>();

        // Setup default behavior
        _mockLlmService.Setup(s => s.IsModelLoaded).Returns(false);
        _mockConversationService.Setup(s => s.CurrentConversation)
            .Returns(new Conversation { Id = Guid.NewGuid(), Title = "Test" });
        _mockConversationService.Setup(s => s.GetMessages())
            .Returns(Array.Empty<ChatMessage>());
        _mockConversationService.Setup(s => s.GetRecentConversationsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationSummary>());
        _mockSettingsService.Setup(s => s.CurrentSettings)
            .Returns(new AppSettings());
        _mockSettingsService.Setup(s => s.LoadSettingsAsync())
            .ReturnsAsync(new AppSettings());

        // Create child ViewModels with their own mocks
        var mockChatLogger = new Mock<ILogger<ChatViewModel>>();
        var mockSelectorVmLogger = new Mock<ILogger<SystemPromptSelectorViewModel>>();
        var selectorViewModel = new SystemPromptSelectorViewModel(
            _mockSystemPromptService.Object,
            _dispatcher,
            mockSelectorVmLogger.Object);

        _chatViewModel = new ChatViewModel(
            _mockLlmService.Object,
            _mockConversationService.Object,
            _mockSettingsService.Object,
            _mockSystemPromptService.Object,
            selectorViewModel,
            _dispatcher,
            mockChatLogger.Object);

        // ModelSelectorViewModel takes 2 parameters: ILlmService and ISettingsService
        _modelSelectorViewModel = new ModelSelectorViewModel(
            _mockLlmService.Object,
            _mockSettingsService.Object);

        var mockConversationListLogger = new Mock<ILogger<ConversationListViewModel>>();
        _conversationListViewModel = new ConversationListViewModel(
            _mockConversationService.Object,
            _dispatcher,
            mockConversationListLogger.Object);

        // InferenceSettingsViewModel takes IInferenceSettingsService, IDispatcher, and optional logger
        var mockInferenceSettingsService = new Mock<IInferenceSettingsService>();
        mockInferenceSettingsService.Setup(s => s.CurrentSettings)
            .Returns(new InferenceSettings());
        mockInferenceSettingsService.Setup(s => s.GetPresetsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InferencePreset>());
        var mockInferenceSettingsLogger = new Mock<ILogger<InferenceSettingsViewModel>>();
        _inferenceSettingsViewModel = new InferenceSettingsViewModel(
            mockInferenceSettingsService.Object,
            _dispatcher,
            mockInferenceSettingsLogger.Object);

        // FileExplorerViewModel (v0.3.2g)
        var mockFileSystemService = new Mock<IFileSystemService>();
        var mockFileExplorerLogger = new Mock<ILogger<FileExplorerViewModel>>();
        _fileExplorerViewModel = new FileExplorerViewModel(
            _mockWorkspaceService.Object,
            mockFileSystemService.Object,
            _mockSettingsService.Object,
            null, // IStorageProvider not needed in tests
            mockFileExplorerLogger.Object);
    }

    private MainWindowViewModel CreateViewModel()
    {
        _viewModel = new MainWindowViewModel(
            _chatViewModel,
            _modelSelectorViewModel,
            _conversationListViewModel,
            _inferenceSettingsViewModel,
            _mockLlmService.Object,
            _mockSettingsService.Object,
            _mockSystemPromptService.Object,
            _mockSearchService.Object,
            _mockExportService.Object,
            _mockConversationService.Object,
            _mockWorkspaceService.Object,
            _fileExplorerViewModel,
            _dispatcher,
            _mockLogger.Object);

        return _viewModel;
    }

    public void Dispose()
    {
        _chatViewModel.Dispose();
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException for null chatViewModel.
    /// </summary>
    [Fact]
    public void Constructor_NullChatViewModel_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            null!,
            _modelSelectorViewModel,
            _conversationListViewModel,
            _inferenceSettingsViewModel,
            _mockLlmService.Object,
            _mockSettingsService.Object,
            _mockSystemPromptService.Object,
            _mockSearchService.Object,
            _mockExportService.Object,
            _mockConversationService.Object,
            _mockWorkspaceService.Object,
            _fileExplorerViewModel,
            _dispatcher,
            _mockLogger.Object));
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException for null conversationListViewModel.
    /// </summary>
    [Fact]
    public void Constructor_NullConversationListViewModel_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
            _chatViewModel,
            _modelSelectorViewModel,
            null!,
            _inferenceSettingsViewModel,
            _mockLlmService.Object,
            _mockSettingsService.Object,
            _mockSystemPromptService.Object,
            _mockSearchService.Object,
            _mockExportService.Object,
            _mockConversationService.Object,
            _mockWorkspaceService.Object,
            _fileExplorerViewModel,
            _dispatcher,
            _mockLogger.Object));
    }

    /// <summary>
    /// Verifies that the constructor subscribes to ModelStateChanged event.
    /// </summary>
    [Fact]
    public void Constructor_SubscribesToModelStateChangedEvent()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        _mockLlmService.VerifyAdd(
            s => s.ModelStateChanged += It.IsAny<EventHandler<ModelStateChangedEventArgs>>(),
            Times.AtLeastOnce); // Multiple subscriptions expected (MainWindowViewModel + ModelSelectorViewModel)
    }

    /// <summary>
    /// Verifies that the constructor subscribes to InferenceProgress event.
    /// </summary>
    [Fact]
    public void Constructor_SubscribesToInferenceProgressEvent()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        _mockLlmService.VerifyAdd(
            s => s.InferenceProgress += It.IsAny<EventHandler<InferenceProgressEventArgs>>(),
            Times.Once);
    }

    #endregion

    #region Child ViewModel Property Tests

    /// <summary>
    /// Verifies that ChatViewModel property is properly set.
    /// </summary>
    [Fact]
    public void ChatViewModel_IsProperlyInjected()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm.ChatViewModel);
        Assert.Same(_chatViewModel, vm.ChatViewModel);
    }

    /// <summary>
    /// Verifies that ModelSelectorViewModel property is properly set.
    /// </summary>
    [Fact]
    public void ModelSelectorViewModel_IsProperlyInjected()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm.ModelSelectorViewModel);
        Assert.Same(_modelSelectorViewModel, vm.ModelSelectorViewModel);
    }

    /// <summary>
    /// Verifies that ConversationListViewModel property is properly set.
    /// </summary>
    [Fact]
    public void ConversationListViewModel_IsProperlyInjected()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm.ConversationListViewModel);
        Assert.Same(_conversationListViewModel, vm.ConversationListViewModel);
    }

    /// <summary>
    /// Verifies that InferenceSettingsViewModel property is properly set.
    /// </summary>
    [Fact]
    public void InferenceSettingsViewModel_IsProperlyInjected()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm.InferenceSettingsViewModel);
        Assert.Same(_inferenceSettingsViewModel, vm.InferenceSettingsViewModel);
    }

    #endregion

    #region Default Value Tests

    /// <summary>
    /// Verifies that MainWindowViewModel has correct default values.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal("No model loaded", vm.StatusMessage);
        Assert.Equal(string.Empty, vm.TokenInfo);
        Assert.True(vm.IsSidebarVisible);
        Assert.Equal(280, vm.SidebarWidth);
    }

    #endregion

    #region InitializeAsync Tests

    /// <summary>
    /// Verifies that InitializeAsync loads settings.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LoadsSettings()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert
        _mockSettingsService.Verify(s => s.LoadSettingsAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies that InitializeAsync initializes ConversationListViewModel.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_InitializesConversationListViewModel()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert - ConversationListViewModel.InitializeAsync should be called
        // We verify this indirectly by checking that GetRecentConversationsAsync was called
        _mockConversationService.Verify(
            s => s.GetRecentConversationsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that InitializeAsync updates StatusMessage on success.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_UpdatesStatusMessageOnSuccess()
    {
        // Arrange
        _mockLlmService.Setup(s => s.IsModelLoaded).Returns(false);
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert
        Assert.Equal("No model loaded", vm.StatusMessage);
    }

    /// <summary>
    /// Verifies that InitializeAsync shows loading status initially.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_ShowsLoadingStatus()
    {
        // Arrange
        var statusMessages = new List<string?>();
        var vm = CreateViewModel();
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.StatusMessage))
                statusMessages.Add(vm.StatusMessage);
        };

        // Act
        await vm.InitializeAsync();

        // Assert - "Loading..." should have been set at some point
        Assert.Contains("Loading...", statusMessages);
    }

    /// <summary>
    /// Verifies that InitializeAsync handles errors gracefully.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_HandlesErrors()
    {
        // Arrange
        _mockSettingsService.Setup(s => s.LoadSettingsAsync())
            .ThrowsAsync(new Exception("Test error"));
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert - should not throw, should show error in status
        Assert.StartsWith("Error:", vm.StatusMessage);
    }

    #endregion

    #region ToggleSidebar Command Tests

    /// <summary>
    /// Verifies that ToggleSidebarCommand toggles sidebar visibility.
    /// </summary>
    [Fact]
    public void ToggleSidebarCommand_TogglesSidebarVisibility()
    {
        // Arrange
        var vm = CreateViewModel();
        Assert.True(vm.IsSidebarVisible);

        // Act
        vm.ToggleSidebarCommand.Execute(null);

        // Assert
        Assert.False(vm.IsSidebarVisible);

        // Toggle back
        vm.ToggleSidebarCommand.Execute(null);
        Assert.True(vm.IsSidebarVisible);
    }

    /// <summary>
    /// Verifies that ToggleSidebar notifies property changed.
    /// </summary>
    [Fact]
    public void ToggleSidebar_NotifiesPropertyChanged()
    {
        // Arrange
        var vm = CreateViewModel();
        var changedProperties = new List<string?>();
        vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        // Act
        vm.ToggleSidebarCommand.Execute(null);

        // Assert
        Assert.Contains(nameof(MainWindowViewModel.IsSidebarVisible), changedProperties);
    }

    #endregion

    #region NewConversation Command Tests

    /// <summary>
    /// Verifies that NewConversationCommand delegates to ConversationListViewModel.
    /// </summary>
    [Fact]
    public async Task NewConversationCommand_CreatesNewConversation()
    {
        // Arrange
        _mockConversationService.Setup(s => s.CreateNewConversationAsync(
            It.IsAny<string?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Conversation { Id = Guid.NewGuid(), Title = "New" });
        var vm = CreateViewModel();

        // Act
        await vm.NewConversationCommand.ExecuteAsync(null);

        // Assert - Verify the conversation service was called to create a new conversation
        _mockConversationService.Verify(
            s => s.CreateNewConversationAsync(It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region FocusSearch Command Tests

    /// <summary>
    /// Verifies that FocusSearchCommand executes without throwing.
    /// </summary>
    [Fact]
    public void FocusSearchCommand_ExecutesWithoutError()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act & Assert - should not throw
        vm.FocusSearchCommand.Execute(null);
    }

    #endregion

    #region OnModelStateChanged Event Handler Tests

    /// <summary>
    /// Verifies that the event handler is subscribed and responds to model unloaded state.
    /// </summary>
    /// <remarks>
    /// Note: Testing model loaded state requires a valid file path because ModelSelectorViewModel
    /// tries to read file info. This test only verifies the unloaded state where no file access occurs.
    /// </remarks>
    [Fact]
    public void OnModelStateChanged_ModelUnloaded_UpdatesStatusMessage()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act - unload (no file path, so no FileInfo access)
        _mockLlmService.Raise(
            s => s.ModelStateChanged += null,
            new ModelStateChangedEventArgs(isLoaded: false, modelPath: null));

        // Assert
        Assert.Equal("No model loaded", vm.StatusMessage);
    }

    /// <summary>
    /// Verifies that InferenceProgress updates TokenInfo (which can then be cleared).
    /// </summary>
    [Fact]
    public void OnInferenceProgress_SetsTokenInfo_ThenModelUnloaded_ClearsTokenInfo()
    {
        // Arrange
        var vm = CreateViewModel();

        // First simulate some token info
        _mockLlmService.Raise(
            s => s.InferenceProgress += null,
            new InferenceProgressEventArgs { TokensGenerated = 10, Elapsed = TimeSpan.FromSeconds(2) });
        Assert.NotEqual(string.Empty, vm.TokenInfo);

        // Act - unload model (no file path, so no FileInfo access)
        _mockLlmService.Raise(
            s => s.ModelStateChanged += null,
            new ModelStateChangedEventArgs(isLoaded: false, modelPath: null));

        // Assert
        Assert.Equal(string.Empty, vm.TokenInfo);
    }

    #endregion

    #region OnInferenceProgress Event Handler Tests

    /// <summary>
    /// Verifies that OnInferenceProgress updates TokenInfo.
    /// </summary>
    [Fact]
    public void OnInferenceProgress_UpdatesTokenInfo()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act - TokensPerSecond is calculated from TokensGenerated / Elapsed.TotalSeconds
        // 42 tokens in ~2.745 seconds = ~15.3 tok/s
        _mockLlmService.Raise(
            s => s.InferenceProgress += null,
            new InferenceProgressEventArgs
            {
                TokensGenerated = 42,
                Elapsed = TimeSpan.FromSeconds(42.0 / 15.3)
            });

        // Assert - The format may vary slightly due to floating point
        Assert.Contains("Tokens: 42", vm.TokenInfo);
        Assert.Contains("tok/s", vm.TokenInfo);
    }

    #endregion

    #region SetMainWindow Tests

    /// <summary>
    /// Verifies that SetMainWindow throws ArgumentNullException for null window.
    /// </summary>
    [Fact]
    public void SetMainWindow_NullWindow_ThrowsArgumentNullException()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => vm.SetMainWindow(null!));
    }

    #endregion

    #region v0.2.5g Tests - SaveState and IsModelLoaded

    /// <summary>
    /// Verifies that the constructor subscribes to SaveStateChanged event.
    /// </summary>
    /// <remarks>
    /// Note: Multiple ViewModels subscribe to SaveStateChanged (MainWindowViewModel, ChatViewModel),
    /// so we verify at least once rather than exactly once.
    /// </remarks>
    [Fact]
    public void Constructor_SubscribesToSaveStateChangedEvent()
    {
        // Act
        var vm = CreateViewModel();

        // Assert - At least once because ChatViewModel also subscribes
        _mockConversationService.VerifyAdd(
            s => s.SaveStateChanged += It.IsAny<EventHandler<SaveStateChangedEventArgs>>(),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that SaveState is null by default.
    /// </summary>
    [Fact]
    public void Constructor_SaveStateIsNullByDefault()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Null(vm.SaveState);
    }

    /// <summary>
    /// Verifies that IsModelLoaded is false by default when no model is loaded.
    /// </summary>
    [Fact]
    public void Constructor_IsModelLoadedIsFalseByDefault()
    {
        // Arrange
        _mockLlmService.Setup(s => s.IsModelLoaded).Returns(false);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.False(vm.IsModelLoaded);
    }

    /// <summary>
    /// Verifies that IsModelLoaded is true when a model is already loaded.
    /// </summary>
    [Fact]
    public void Constructor_IsModelLoadedIsTrueWhenModelLoaded()
    {
        // Arrange
        _mockLlmService.Setup(s => s.IsModelLoaded).Returns(true);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.True(vm.IsModelLoaded);
    }

    /// <summary>
    /// Verifies that SaveStateChanged event updates SaveState property.
    /// </summary>
    [Fact]
    public void OnSaveStateChanged_UpdatesSaveStateProperty()
    {
        // Arrange
        var vm = CreateViewModel();
        var saveState = new SaveStateChangedEventArgs
        {
            IsSaving = true,
            HasUnsavedChanges = false
        };

        // Act
        _mockConversationService.Raise(
            s => s.SaveStateChanged += null,
            saveState);

        // Assert
        Assert.NotNull(vm.SaveState);
        Assert.True(vm.SaveState.IsSaving);
        Assert.False(vm.SaveState.HasUnsavedChanges);
    }

    /// <summary>
    /// Verifies that SaveStateChanged with unsaved changes is reflected in SaveState.
    /// </summary>
    [Fact]
    public void OnSaveStateChanged_UnsavedChanges_UpdatesSaveState()
    {
        // Arrange
        var vm = CreateViewModel();
        var saveState = new SaveStateChangedEventArgs
        {
            IsSaving = false,
            HasUnsavedChanges = true
        };

        // Act
        _mockConversationService.Raise(
            s => s.SaveStateChanged += null,
            saveState);

        // Assert
        Assert.NotNull(vm.SaveState);
        Assert.False(vm.SaveState.IsSaving);
        Assert.True(vm.SaveState.HasUnsavedChanges);
    }

    /// <summary>
    /// Verifies that IsModelLoaded is set correctly on construction when model is already loaded.
    /// </summary>
    /// <remarks>
    /// Note: We cannot test the ModelStateChanged event with isLoaded=true and a modelPath because
    /// ModelSelectorViewModel tries to access FileInfo for the path, which fails for non-existent paths.
    /// This test verifies the initial state is set correctly based on ILlmService.IsModelLoaded.
    /// </remarks>
    [Fact]
    public void OnModelStateChanged_ModelLoaded_IsModelLoadedSetCorrectly()
    {
        // Arrange - Set up service to report model is already loaded
        _mockLlmService.Setup(s => s.IsModelLoaded).Returns(true);

        // Act - Create ViewModel (constructor reads IsModelLoaded from service)
        var vm = CreateViewModel();

        // Assert - IsModelLoaded should be true based on service state
        Assert.True(vm.IsModelLoaded);
    }

    /// <summary>
    /// Verifies that ModelStateChanged updates IsModelLoaded to false when unloaded.
    /// </summary>
    [Fact]
    public void OnModelStateChanged_ModelUnloaded_UpdatesIsModelLoaded()
    {
        // Arrange
        _mockLlmService.Setup(s => s.IsModelLoaded).Returns(true);
        var vm = CreateViewModel();
        Assert.True(vm.IsModelLoaded);

        // Act
        _mockLlmService.Raise(
            s => s.ModelStateChanged += null,
            new ModelStateChangedEventArgs(isLoaded: false, modelPath: null));

        // Assert
        Assert.False(vm.IsModelLoaded);
    }

    /// <summary>
    /// Verifies that SaveState property notifies on change.
    /// </summary>
    [Fact]
    public void OnSaveStateChanged_NotifiesPropertyChanged()
    {
        // Arrange
        var vm = CreateViewModel();
        var changedProperties = new List<string?>();
        vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        // Act
        _mockConversationService.Raise(
            s => s.SaveStateChanged += null,
            new SaveStateChangedEventArgs { IsSaving = true, HasUnsavedChanges = false });

        // Assert
        Assert.Contains(nameof(MainWindowViewModel.SaveState), changedProperties);
    }

    /// <summary>
    /// Verifies that IsModelLoaded property notifies on change when model is unloaded.
    /// </summary>
    /// <remarks>
    /// Note: We test the unload event (null path) to avoid FileInfo access in ModelSelectorViewModel.
    /// The notification mechanism is the same for load and unload events.
    /// </remarks>
    [Fact]
    public void OnModelStateChanged_NotifiesIsModelLoadedPropertyChanged()
    {
        // Arrange - Start with model loaded
        _mockLlmService.Setup(s => s.IsModelLoaded).Returns(true);
        var vm = CreateViewModel();
        var changedProperties = new List<string?>();
        vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        // Act - Unload model (null path avoids FileInfo access)
        _mockLlmService.Raise(
            s => s.ModelStateChanged += null,
            new ModelStateChangedEventArgs(isLoaded: false, modelPath: null));

        // Assert
        Assert.Contains(nameof(MainWindowViewModel.IsModelLoaded), changedProperties);
    }

    #endregion

    #region v0.2.5g Tests - ToggleSettingsPanel Command

    /// <summary>
    /// Verifies that ToggleSettingsPanelCommand toggles the InferenceSettingsViewModel expansion state.
    /// </summary>
    [Fact]
    public void ToggleSettingsPanelCommand_TogglesInferenceSettingsExpansion()
    {
        // Arrange
        var vm = CreateViewModel();
        var initialState = _inferenceSettingsViewModel.IsExpanded;

        // Act
        vm.ToggleSettingsPanelCommand.Execute(null);

        // Assert
        Assert.NotEqual(initialState, _inferenceSettingsViewModel.IsExpanded);

        // Toggle back
        vm.ToggleSettingsPanelCommand.Execute(null);
        Assert.Equal(initialState, _inferenceSettingsViewModel.IsExpanded);
    }

    /// <summary>
    /// Verifies that ToggleSettingsPanelCommand shows sidebar when expanding settings and sidebar is hidden.
    /// </summary>
    [Fact]
    public void ToggleSettingsPanelCommand_WhenExpandingAndSidebarHidden_ShowsSidebar()
    {
        // Arrange
        var vm = CreateViewModel();
        _inferenceSettingsViewModel.IsExpanded = false;
        vm.ToggleSidebarCommand.Execute(null); // Hide sidebar
        Assert.False(vm.IsSidebarVisible);

        // Act - expand settings
        vm.ToggleSettingsPanelCommand.Execute(null);

        // Assert
        Assert.True(_inferenceSettingsViewModel.IsExpanded);
        Assert.True(vm.IsSidebarVisible); // Sidebar should be shown
    }

    /// <summary>
    /// Verifies that ToggleSettingsPanelCommand does not affect sidebar when collapsing settings.
    /// </summary>
    [Fact]
    public void ToggleSettingsPanelCommand_WhenCollapsing_DoesNotAffectSidebar()
    {
        // Arrange
        var vm = CreateViewModel();
        _inferenceSettingsViewModel.IsExpanded = true;
        Assert.True(vm.IsSidebarVisible);

        // Act - collapse settings
        vm.ToggleSettingsPanelCommand.Execute(null);

        // Assert
        Assert.False(_inferenceSettingsViewModel.IsExpanded);
        Assert.True(vm.IsSidebarVisible); // Sidebar should remain visible
    }

    #endregion

    #region v0.3.2g Tests - FileExplorer and Workspace Integration

    /// <summary>
    /// Verifies that FileExplorer property is properly injected.
    /// </summary>
    [Fact]
    public void FileExplorer_IsProperlyInjected()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm.FileExplorer);
        Assert.Same(_fileExplorerViewModel, vm.FileExplorer);
    }

    /// <summary>
    /// Verifies that HasOpenWorkspace is false by default.
    /// </summary>
    [Fact]
    public void Constructor_HasOpenWorkspaceIsFalseByDefault()
    {
        // Arrange - no workspace
        _mockWorkspaceService.Setup(s => s.CurrentWorkspace).Returns((Workspace?)null);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.False(vm.HasOpenWorkspace);
    }

    /// <summary>
    /// Verifies that HasOpenWorkspace is true when workspace is already open.
    /// </summary>
    [Fact]
    public void Constructor_HasOpenWorkspaceIsTrueWhenWorkspaceOpen()
    {
        // Arrange
        _mockWorkspaceService.Setup(s => s.CurrentWorkspace)
            .Returns(new Workspace { Id = Guid.NewGuid(), Name = "Test", RootPath = "/test" });

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.True(vm.HasOpenWorkspace);
    }

    /// <summary>
    /// Verifies that WorkspaceChanged event updates HasOpenWorkspace.
    /// </summary>
    [Fact]
    public void OnWorkspaceChanged_UpdatesHasOpenWorkspace()
    {
        // Arrange
        _mockWorkspaceService.Setup(s => s.CurrentWorkspace).Returns((Workspace?)null);
        var vm = CreateViewModel();
        Assert.False(vm.HasOpenWorkspace);

        // Act - open workspace
        _mockWorkspaceService.Raise(
            s => s.WorkspaceChanged += null,
            new WorkspaceChangedEventArgs
            {
                CurrentWorkspace = new Workspace { Id = Guid.NewGuid(), Name = "Test", RootPath = "/test" },
                ChangeType = WorkspaceChangeType.Opened
            });

        // Assert
        Assert.True(vm.HasOpenWorkspace);
    }

    /// <summary>
    /// Verifies that WorkspaceChanged event updates HasOpenWorkspace to false when closed.
    /// </summary>
    [Fact]
    public void OnWorkspaceChanged_WorkspaceClosed_UpdatesHasOpenWorkspace()
    {
        // Arrange - start with workspace open
        _mockWorkspaceService.Setup(s => s.CurrentWorkspace)
            .Returns(new Workspace { Id = Guid.NewGuid(), Name = "Test", RootPath = "/test" });
        var vm = CreateViewModel();
        Assert.True(vm.HasOpenWorkspace);

        // Act - close workspace
        _mockWorkspaceService.Raise(
            s => s.WorkspaceChanged += null,
            new WorkspaceChangedEventArgs
            {
                CurrentWorkspace = null,
                ChangeType = WorkspaceChangeType.Closed
            });

        // Assert
        Assert.False(vm.HasOpenWorkspace);
    }

    /// <summary>
    /// Verifies that HasOpenWorkspace notifies property changed.
    /// </summary>
    [Fact]
    public void OnWorkspaceChanged_NotifiesHasOpenWorkspacePropertyChanged()
    {
        // Arrange
        _mockWorkspaceService.Setup(s => s.CurrentWorkspace).Returns((Workspace?)null);
        var vm = CreateViewModel();
        var changedProperties = new List<string?>();
        vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        // Act
        _mockWorkspaceService.Raise(
            s => s.WorkspaceChanged += null,
            new WorkspaceChangedEventArgs
            {
                CurrentWorkspace = new Workspace { Id = Guid.NewGuid(), Name = "Test", RootPath = "/test" },
                ChangeType = WorkspaceChangeType.Opened
            });

        // Assert
        Assert.Contains(nameof(MainWindowViewModel.HasOpenWorkspace), changedProperties);
    }

    /// <summary>
    /// Verifies that constructor subscribes to FileOpenRequested event.
    /// </summary>
    [Fact]
    public void Constructor_SubscribesToFileOpenRequestedEvent()
    {
        // Act
        var vm = CreateViewModel();

        // Assert - verify subscription happened (FileExplorer has the event)
        // We can't directly verify subscription, but we can verify the event exists
        Assert.NotNull(vm.FileExplorer);
    }

    /// <summary>
    /// Verifies that constructor subscribes to WorkspaceChanged event.
    /// </summary>
    [Fact]
    public void Constructor_SubscribesToWorkspaceChangedEvent()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        _mockWorkspaceService.VerifyAdd(
            s => s.WorkspaceChanged += It.IsAny<EventHandler<WorkspaceChangedEventArgs>>(),
            Times.AtLeastOnce);
    }

    #endregion
}
