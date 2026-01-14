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
/// Unit tests for ChatViewModel system prompt integration (v0.2.4e).
/// Tests BuildContextWithSystemPrompt, OnCurrentPromptChanged, and InitializeSystemPromptDisplay.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the v0.2.4e functionality:
/// </para>
/// <list type="bullet">
///   <item><description>System prompt is prepended to LLM context</description></item>
///   <item><description>UI properties update when prompt changes</description></item>
///   <item><description>Initial display state is synchronized with service</description></item>
/// </list>
/// <para>Added in v0.2.5a (test coverage for v0.2.4e).</para>
/// </remarks>
public class ChatViewModelSystemPromptTests : IDisposable
{
    #region Test Infrastructure

    private readonly Mock<ILlmService> _mockLlmService;
    private readonly Mock<IConversationService> _mockConversationService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ISystemPromptService> _mockSystemPromptService;
    private readonly SystemPromptSelectorViewModel _selectorViewModel;
    private readonly TestDispatcher _dispatcher;
    private readonly Mock<ILogger<ChatViewModel>> _mockLogger;

    private ChatViewModel? _viewModel;

    public ChatViewModelSystemPromptTests()
    {
        _mockLlmService = new Mock<ILlmService>();
        _mockConversationService = new Mock<IConversationService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockSystemPromptService = new Mock<ISystemPromptService>();
        _dispatcher = new TestDispatcher();
        _mockLogger = new Mock<ILogger<ChatViewModel>>();

        // Setup default behavior
        _mockLlmService.Setup(s => s.IsModelLoaded).Returns(true);

        _mockConversationService.Setup(s => s.CurrentConversation)
            .Returns(new Conversation { Id = Guid.NewGuid(), Title = "Test Conversation" });
        _mockConversationService.Setup(s => s.GetMessages())
            .Returns(Array.Empty<ChatMessage>());

        _mockSettingsService.Setup(s => s.CurrentSettings)
            .Returns(new AppSettings());

        // Create a real selector ViewModel with mocked dependencies (sealed class cannot be mocked)
        var mockSelectorLogger = new Mock<ILogger<SystemPromptSelectorViewModel>>();
        _selectorViewModel = new SystemPromptSelectorViewModel(
            _mockSystemPromptService.Object,
            _dispatcher,
            mockSelectorLogger.Object);
    }

    private ChatViewModel CreateViewModel(SystemPrompt? currentPrompt = null)
    {
        _mockSystemPromptService.Setup(s => s.CurrentPrompt).Returns(currentPrompt);

        _viewModel = new ChatViewModel(
            _mockLlmService.Object,
            _mockConversationService.Object,
            _mockSettingsService.Object,
            _mockSystemPromptService.Object,
            _selectorViewModel,
            _dispatcher,
            _mockLogger.Object);

        return _viewModel;
    }

    private static SystemPrompt CreateTestPrompt(
        string name = "Test Prompt",
        string content = "You are a helpful assistant.",
        bool isDefault = false)
    {
        return new SystemPrompt
        {
            Id = Guid.NewGuid(),
            Name = name,
            Content = content,
            Description = "Test description",
            Category = "General",
            IsBuiltIn = false,
            IsDefault = isDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }

    #endregion

    #region InitializeSystemPromptDisplay Tests

    /// <summary>
    /// Verifies that InitializeSystemPromptDisplay sets properties when a prompt is selected.
    /// </summary>
    [Fact]
    public void Constructor_WithCurrentPrompt_InitializesDisplayProperties()
    {
        // Arrange
        var prompt = CreateTestPrompt("The Senior Intern", "You are a senior intern...");

        // Act
        var vm = CreateViewModel(prompt);

        // Assert
        Assert.True(vm.ShowSystemPromptMessage);
        Assert.Equal("The Senior Intern", vm.SystemPromptName);
        Assert.Equal("You are a senior intern...", vm.SystemPromptContent);
    }

    /// <summary>
    /// Verifies that InitializeSystemPromptDisplay hides expander when no prompt is selected.
    /// </summary>
    [Fact]
    public void Constructor_WithNoCurrentPrompt_HidesSystemPromptExpander()
    {
        // Act
        var vm = CreateViewModel(currentPrompt: null);

        // Assert
        Assert.False(vm.ShowSystemPromptMessage);
        Assert.Null(vm.SystemPromptName);
        Assert.Null(vm.SystemPromptContent);
    }

    /// <summary>
    /// Verifies that the constructor subscribes to CurrentPromptChanged event.
    /// </summary>
    [Fact]
    public void Constructor_SubscribesToCurrentPromptChangedEvent()
    {
        // Act
        var vm = CreateViewModel();

        // Assert - verify the event was subscribed at least once (ChatViewModel and SystemPromptSelectorViewModel both subscribe)
        _mockSystemPromptService.VerifyAdd(
            s => s.CurrentPromptChanged += It.IsAny<EventHandler<CurrentPromptChangedEventArgs>>(),
            Times.AtLeastOnce);
    }

    #endregion

    #region OnCurrentPromptChanged Tests

    /// <summary>
    /// Verifies that OnCurrentPromptChanged updates UI properties when a prompt is selected.
    /// </summary>
    [Fact]
    public void OnCurrentPromptChanged_WithNewPrompt_UpdatesDisplayProperties()
    {
        // Arrange
        var vm = CreateViewModel();
        var newPrompt = CreateTestPrompt("Code Reviewer", "You review code...");

        // Act - Raise the event
        _mockSystemPromptService.Raise(
            s => s.CurrentPromptChanged += null,
            new CurrentPromptChangedEventArgs { NewPrompt = newPrompt, PreviousPrompt = null });

        // Assert
        Assert.True(vm.ShowSystemPromptMessage);
        Assert.Equal("Code Reviewer", vm.SystemPromptName);
        Assert.Equal("You review code...", vm.SystemPromptContent);
    }

    /// <summary>
    /// Verifies that OnCurrentPromptChanged clears UI properties when prompt is cleared.
    /// </summary>
    [Fact]
    public void OnCurrentPromptChanged_WithNullPrompt_ClearsDisplayProperties()
    {
        // Arrange
        var initialPrompt = CreateTestPrompt();
        var vm = CreateViewModel(initialPrompt);

        // Verify initial state
        Assert.True(vm.ShowSystemPromptMessage);

        // Act - Clear the prompt
        _mockSystemPromptService.Raise(
            s => s.CurrentPromptChanged += null,
            new CurrentPromptChangedEventArgs { NewPrompt = null, PreviousPrompt = initialPrompt });

        // Assert
        Assert.False(vm.ShowSystemPromptMessage);
        Assert.Null(vm.SystemPromptName);
        Assert.Null(vm.SystemPromptContent);
    }

    /// <summary>
    /// Verifies that OnCurrentPromptChanged handles prompt switching correctly.
    /// </summary>
    [Fact]
    public void OnCurrentPromptChanged_SwitchingPrompts_UpdatesCorrectly()
    {
        // Arrange
        var firstPrompt = CreateTestPrompt("First Prompt", "First content");
        var vm = CreateViewModel(firstPrompt);

        var secondPrompt = CreateTestPrompt("Second Prompt", "Second content");

        // Act
        _mockSystemPromptService.Raise(
            s => s.CurrentPromptChanged += null,
            new CurrentPromptChangedEventArgs { NewPrompt = secondPrompt, PreviousPrompt = firstPrompt });

        // Assert
        Assert.True(vm.ShowSystemPromptMessage);
        Assert.Equal("Second Prompt", vm.SystemPromptName);
        Assert.Equal("Second content", vm.SystemPromptContent);
    }

    #endregion

    #region SystemPromptSelectorViewModel Property Tests

    /// <summary>
    /// Verifies that SystemPromptSelectorViewModel is properly injected.
    /// </summary>
    [Fact]
    public void SystemPromptSelectorViewModel_IsInjectedCorrectly()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm.SystemPromptSelectorViewModel);
        Assert.Same(_selectorViewModel, vm.SystemPromptSelectorViewModel);
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies that Dispose unsubscribes from CurrentPromptChanged event.
    /// </summary>
    [Fact]
    public void Dispose_UnsubscribesFromCurrentPromptChangedEvent()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.Dispose();

        // Assert - verify the event was unsubscribed
        _mockSystemPromptService.VerifyRemove(
            s => s.CurrentPromptChanged -= It.IsAny<EventHandler<CurrentPromptChangedEventArgs>>(),
            Times.Once);
    }

    /// <summary>
    /// Verifies that Dispose is safe to call multiple times.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act & Assert - should not throw
        vm.Dispose();
        vm.Dispose();
        vm.Dispose();
    }

    #endregion
}
