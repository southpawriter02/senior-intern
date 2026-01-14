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
/// Unit tests for SystemPromptSelectorViewModel (v0.2.4c/v0.2.4e).
/// Tests initialization, prompt selection, and event handling.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>InitializeAsync loads prompts and syncs selection</description></item>
///   <item><description>SelectPromptAsync updates service and fires events</description></item>
///   <item><description>Event handlers properly sync state</description></item>
///   <item><description>Computed properties return correct values</description></item>
/// </list>
/// <para>Added in v0.2.5a (test coverage for v0.2.4c/v0.2.4e).</para>
/// </remarks>
public class SystemPromptSelectorViewModelTests : IDisposable
{
    #region Test Infrastructure

    private readonly Mock<ISystemPromptService> _mockPromptService;
    private readonly TestDispatcher _dispatcher;
    private readonly Mock<ILogger<SystemPromptSelectorViewModel>> _mockLogger;

    private SystemPromptSelectorViewModel? _viewModel;

    public SystemPromptSelectorViewModelTests()
    {
        _mockPromptService = new Mock<ISystemPromptService>();
        _dispatcher = new TestDispatcher();
        _mockLogger = new Mock<ILogger<SystemPromptSelectorViewModel>>();

        // Default setup - empty prompt list
        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SystemPrompt>());
    }

    private SystemPromptSelectorViewModel CreateViewModel()
    {
        _viewModel = new SystemPromptSelectorViewModel(
            _mockPromptService.Object,
            _dispatcher,
            _mockLogger.Object);

        return _viewModel;
    }

    private static SystemPrompt CreateTestPrompt(
        string name = "Test Prompt",
        string content = "You are a helpful assistant.",
        bool isDefault = false,
        bool isBuiltIn = false,
        string category = "General")
    {
        return new SystemPrompt
        {
            Id = Guid.NewGuid(),
            Name = name,
            Content = content,
            Description = "Test description",
            Category = category,
            IsBuiltIn = isBuiltIn,
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

    #region Constructor Tests

    /// <summary>
    /// Verifies constructor throws when promptService is null.
    /// </summary>
    [Fact]
    public void Constructor_NullPromptService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SystemPromptSelectorViewModel(null!, _dispatcher, _mockLogger.Object));
    }

    /// <summary>
    /// Verifies constructor throws when dispatcher is null.
    /// </summary>
    [Fact]
    public void Constructor_NullDispatcher_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SystemPromptSelectorViewModel(_mockPromptService.Object, null!, _mockLogger.Object));
    }

    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SystemPromptSelectorViewModel(_mockPromptService.Object, _dispatcher, null!));
    }

    /// <summary>
    /// Verifies constructor subscribes to service events.
    /// </summary>
    [Fact]
    public void Constructor_SubscribesToServiceEvents()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        _mockPromptService.VerifyAdd(
            s => s.PromptListChanged += It.IsAny<EventHandler<PromptListChangedEventArgs>>(),
            Times.Once);
        _mockPromptService.VerifyAdd(
            s => s.CurrentPromptChanged += It.IsAny<EventHandler<CurrentPromptChangedEventArgs>>(),
            Times.Once);
    }

    #endregion

    #region InitializeAsync Tests

    /// <summary>
    /// Verifies InitializeAsync loads available prompts.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LoadsAvailablePrompts()
    {
        // Arrange
        var prompts = new List<SystemPrompt>
        {
            CreateTestPrompt("Prompt 1"),
            CreateTestPrompt("Prompt 2")
        };
        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompts);

        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert - Should have "No prompt" + 2 user prompts = 3 total
        Assert.Equal(3, vm.AvailablePrompts.Count);
        Assert.Equal("No system prompt", vm.AvailablePrompts[0].Name);
        Assert.Equal("Prompt 1", vm.AvailablePrompts[1].Name);
        Assert.Equal("Prompt 2", vm.AvailablePrompts[2].Name);
    }

    /// <summary>
    /// Verifies InitializeAsync adds "No prompt" option first.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_AddsNoPromptOptionFirst()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert
        Assert.Single(vm.AvailablePrompts);
        Assert.Equal("No system prompt", vm.AvailablePrompts[0].Name);
        Assert.Equal(Guid.Empty, vm.AvailablePrompts[0].Id);
    }

    /// <summary>
    /// Verifies InitializeAsync syncs selection with service current prompt.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SyncsSelectionWithService()
    {
        // Arrange
        var prompt = CreateTestPrompt("Selected Prompt");
        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.CurrentPrompt).Returns(prompt);

        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert
        Assert.NotNull(vm.SelectedPrompt);
        Assert.Equal(prompt.Id, vm.SelectedPrompt.Id);
        Assert.Equal("Selected Prompt", vm.SelectedPrompt.Name);
    }

    /// <summary>
    /// Verifies InitializeAsync selects "No prompt" when no current prompt.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_NoCurrentPrompt_SelectsNoPromptOption()
    {
        // Arrange
        _mockPromptService.Setup(s => s.CurrentPrompt).Returns((SystemPrompt?)null);
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();

        // Assert
        Assert.NotNull(vm.SelectedPrompt);
        Assert.Equal(Guid.Empty, vm.SelectedPrompt.Id);
        Assert.Equal("No system prompt", vm.SelectedPrompt.Name);
    }

    /// <summary>
    /// Verifies InitializeAsync is idempotent.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_CalledMultipleTimes_IsIdempotent()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.InitializeAsync();
        await vm.InitializeAsync();
        await vm.InitializeAsync();

        // Assert - GetAllPromptsAsync should only be called once
        _mockPromptService.Verify(
            s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies InitializeAsync sets IsLoading during operation.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SetsIsLoadingDuringOperation()
    {
        // Arrange
        var loadingStates = new List<bool>();
        var tcs = new TaskCompletionSource<IReadOnlyList<SystemPrompt>>();

        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = CreateViewModel();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SystemPromptSelectorViewModel.IsLoading))
            {
                loadingStates.Add(vm.IsLoading);
            }
        };

        // Act
        var initTask = vm.InitializeAsync();

        // Should be loading now
        Assert.True(vm.IsLoading);

        // Complete the operation
        tcs.SetResult(Array.Empty<SystemPrompt>());
        await initTask;

        // Assert - IsLoading should have transitioned: false -> true -> false
        Assert.False(vm.IsLoading);
    }

    #endregion

    #region SelectPromptAsync Tests

    /// <summary>
    /// Verifies SelectPromptAsync calls service SetCurrentPromptAsync.
    /// </summary>
    [Fact]
    public async Task SelectPromptAsync_CallsServiceSetCurrentPrompt()
    {
        // Arrange
        var prompt = CreateTestPrompt("Test Prompt");
        var promptVm = new SystemPromptViewModel(prompt);

        var vm = CreateViewModel();

        // Act
        await vm.SelectPromptCommand.ExecuteAsync(promptVm);

        // Assert
        _mockPromptService.Verify(
            s => s.SetCurrentPromptAsync(prompt.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies SelectPromptAsync with "No prompt" option clears selection.
    /// </summary>
    [Fact]
    public async Task SelectPromptAsync_NoPromptOption_ClearsSelection()
    {
        // Arrange
        var noPromptVm = new SystemPromptViewModel
        {
            Id = Guid.Empty,
            Name = "No system prompt"
        };

        var vm = CreateViewModel();

        // Act
        await vm.SelectPromptCommand.ExecuteAsync(noPromptVm);

        // Assert
        _mockPromptService.Verify(
            s => s.SetCurrentPromptAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies SelectPromptAsync with null clears selection.
    /// </summary>
    [Fact]
    public async Task SelectPromptAsync_NullPrompt_ClearsSelection()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.SelectPromptCommand.ExecuteAsync(null);

        // Assert
        _mockPromptService.Verify(
            s => s.SetCurrentPromptAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Computed Properties Tests

    /// <summary>
    /// Verifies HasPromptSelected returns true when prompt is selected.
    /// </summary>
    [Fact]
    public async Task HasPromptSelected_WithPromptSelected_ReturnsTrue()
    {
        // Arrange
        var prompt = CreateTestPrompt("Test Prompt");
        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.CurrentPrompt).Returns(prompt);

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Assert
        Assert.True(vm.HasPromptSelected);
    }

    /// <summary>
    /// Verifies HasPromptSelected returns false for "No prompt" option.
    /// </summary>
    [Fact]
    public async Task HasPromptSelected_NoPromptSelected_ReturnsFalse()
    {
        // Arrange
        _mockPromptService.Setup(s => s.CurrentPrompt).Returns((SystemPrompt?)null);

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Assert
        Assert.False(vm.HasPromptSelected);
    }

    /// <summary>
    /// Verifies DisplayText returns prompt name when selected.
    /// </summary>
    [Fact]
    public async Task DisplayText_WithPromptSelected_ReturnsPromptName()
    {
        // Arrange
        var prompt = CreateTestPrompt("The Senior Intern");
        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.CurrentPrompt).Returns(prompt);

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Assert
        Assert.Equal("The Senior Intern", vm.DisplayText);
    }

    /// <summary>
    /// Verifies DisplayText returns "No system prompt" when nothing selected.
    /// </summary>
    [Fact]
    public async Task DisplayText_NoPromptSelected_ReturnsNoSystemPrompt()
    {
        // Arrange
        _mockPromptService.Setup(s => s.CurrentPrompt).Returns((SystemPrompt?)null);

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Assert
        Assert.Equal("No system prompt", vm.DisplayText);
    }

    /// <summary>
    /// Verifies ContentPreview returns prompt content preview.
    /// </summary>
    [Fact]
    public async Task ContentPreview_WithPromptSelected_ReturnsPreview()
    {
        // Arrange
        var prompt = CreateTestPrompt("Test", "You are a helpful assistant.");
        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.CurrentPrompt).Returns(prompt);

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Assert
        Assert.Contains("helpful assistant", vm.ContentPreview);
    }

    /// <summary>
    /// Verifies SelectedCategory returns prompt category.
    /// </summary>
    [Fact]
    public async Task SelectedCategory_WithPromptSelected_ReturnsCategory()
    {
        // Arrange
        var prompt = CreateTestPrompt("Test", category: "Code");
        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.CurrentPrompt).Returns(prompt);

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Assert
        Assert.Equal("Code", vm.SelectedCategory);
    }

    /// <summary>
    /// Verifies IsBuiltInSelected returns true for built-in prompts.
    /// </summary>
    [Fact]
    public async Task IsBuiltInSelected_WithBuiltInPrompt_ReturnsTrue()
    {
        // Arrange
        var prompt = CreateTestPrompt("Template", isBuiltIn: true);
        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.CurrentPrompt).Returns(prompt);

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Assert
        Assert.True(vm.IsBuiltInSelected);
    }

    #endregion

    #region Event Handler Tests

    /// <summary>
    /// Verifies OnCurrentPromptChanged syncs selection.
    /// </summary>
    [Fact]
    public async Task OnCurrentPromptChanged_SyncsSelection()
    {
        // Arrange
        var prompt1 = CreateTestPrompt("Prompt 1");
        var prompt2 = CreateTestPrompt("Prompt 2");

        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt1, prompt2 });
        _mockPromptService.Setup(s => s.CurrentPrompt).Returns(prompt1);

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        Assert.Equal(prompt1.Id, vm.SelectedPrompt?.Id);

        // Act - Update current prompt
        _mockPromptService.Setup(s => s.CurrentPrompt).Returns(prompt2);
        _mockPromptService.Raise(
            s => s.CurrentPromptChanged += null,
            new CurrentPromptChangedEventArgs { NewPrompt = prompt2, PreviousPrompt = prompt1 });

        // Assert
        Assert.Equal(prompt2.Id, vm.SelectedPrompt?.Id);
    }

    /// <summary>
    /// Verifies OnPromptListChanged refreshes prompt list.
    /// </summary>
    [Fact]
    public async Task OnPromptListChanged_RefreshesPromptList()
    {
        // Arrange
        var prompt1 = CreateTestPrompt("Prompt 1");

        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt1 });

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        Assert.Equal(2, vm.AvailablePrompts.Count); // No prompt + 1

        // Act - Add a new prompt
        var prompt2 = CreateTestPrompt("Prompt 2");
        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt1, prompt2 });

        _mockPromptService.Raise(
            s => s.PromptListChanged += null,
            new PromptListChangedEventArgs
            {
                ChangeType = PromptListChangeType.PromptCreated,
                AffectedPromptId = prompt2.Id,
                AffectedPromptName = prompt2.Name
            });

        // Assert - should now have 3 prompts
        Assert.Equal(3, vm.AvailablePrompts.Count);
    }

    #endregion

    #region RefreshPromptsAsync Tests

    /// <summary>
    /// Verifies RefreshPromptsAsync reloads prompts from service.
    /// </summary>
    [Fact]
    public async Task RefreshPromptsAsync_ReloadsFromService()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        _mockPromptService.Setup(s => s.GetAllPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { CreateTestPrompt("New Prompt") });

        // Act
        await vm.RefreshPromptsCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(2, vm.AvailablePrompts.Count);
        Assert.Equal("New Prompt", vm.AvailablePrompts[1].Name);
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies Dispose unsubscribes from service events.
    /// </summary>
    [Fact]
    public void Dispose_UnsubscribesFromServiceEvents()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.Dispose();

        // Assert
        _mockPromptService.VerifyRemove(
            s => s.PromptListChanged -= It.IsAny<EventHandler<PromptListChangedEventArgs>>(),
            Times.Once);
        _mockPromptService.VerifyRemove(
            s => s.CurrentPromptChanged -= It.IsAny<EventHandler<CurrentPromptChangedEventArgs>>(),
            Times.Once);
    }

    /// <summary>
    /// Verifies Dispose is safe to call multiple times.
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
