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
/// Unit tests for <see cref="SystemPromptEditorViewModel"/> (v0.2.4d).
/// Tests CRUD operations, dirty tracking, validation, and computed properties.
/// </summary>
/// <remarks>
/// <para>
/// These tests cover:
/// </para>
/// <list type="bullet">
///   <item><description>Constructor validation and event subscription</description></item>
///   <item><description>InitializeAsync loading and selection</description></item>
///   <item><description>CreateNewPromptAsync editor initialization</description></item>
///   <item><description>SavePromptAsync create and update flows</description></item>
///   <item><description>DeletePromptAsync, DuplicatePromptAsync, SetAsDefaultAsync</description></item>
///   <item><description>DiscardChangesAsync reverts changes</description></item>
///   <item><description>Dirty tracking (IsDirty computed from changes)</description></item>
///   <item><description>Validation (CanSave, CanDelete, CanEdit, CanSetDefault)</description></item>
///   <item><description>Computed properties (CharacterCount, TokenCount)</description></item>
///   <item><description>Dispose unsubscribes from events</description></item>
/// </list>
/// <para>Added in v0.2.5a (test coverage for v0.2.4d).</para>
/// </remarks>
public class SystemPromptEditorViewModelTests : IDisposable
{
    #region Test Infrastructure

    private readonly Mock<ISystemPromptService> _mockPromptService;
    private readonly TestDispatcher _dispatcher;
    private readonly Mock<ILogger<SystemPromptEditorViewModel>> _mockLogger;

    private SystemPromptEditorViewModel? _viewModel;

    public SystemPromptEditorViewModelTests()
    {
        _mockPromptService = new Mock<ISystemPromptService>();
        _dispatcher = new TestDispatcher();
        _mockLogger = new Mock<ILogger<SystemPromptEditorViewModel>>();

        // Setup default behavior
        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt>());
        _mockPromptService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt>());
    }

    private SystemPromptEditorViewModel CreateViewModel()
    {
        _viewModel = new SystemPromptEditorViewModel(
            _mockPromptService.Object,
            _dispatcher,
            _mockLogger.Object);

        return _viewModel;
    }

    private static SystemPrompt CreateTestPrompt(
        string name = "Test Prompt",
        string content = "You are a helpful assistant.",
        bool isBuiltIn = false,
        bool isDefault = false)
    {
        return new SystemPrompt
        {
            Id = Guid.NewGuid(),
            Name = name,
            Content = content,
            Description = "Test description",
            Category = "General",
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
    /// Verifies that constructor throws for null prompt service.
    /// </summary>
    [Fact]
    public void Constructor_NullPromptService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SystemPromptEditorViewModel(
            null!,
            _dispatcher,
            _mockLogger.Object));
    }

    /// <summary>
    /// Verifies that constructor throws for null dispatcher.
    /// </summary>
    [Fact]
    public void Constructor_NullDispatcher_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SystemPromptEditorViewModel(
            _mockPromptService.Object,
            null!,
            _mockLogger.Object));
    }

    /// <summary>
    /// Verifies that constructor subscribes to PromptListChanged event.
    /// </summary>
    [Fact]
    public void Constructor_SubscribesToPromptListChangedEvent()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        _mockPromptService.VerifyAdd(
            s => s.PromptListChanged += It.IsAny<EventHandler<PromptListChangedEventArgs>>(),
            Times.Once);
    }

    /// <summary>
    /// Verifies that constructor allows null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var vm = new SystemPromptEditorViewModel(
            _mockPromptService.Object,
            _dispatcher,
            null);

        vm.Dispose();
    }

    #endregion

    #region InitializeAsync Tests

    /// <summary>
    /// Verifies that InitializeAsync loads prompts.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LoadsPrompts()
    {
        // Arrange
        var userPrompt = CreateTestPrompt("User Prompt");
        var template = CreateTestPrompt("Template", isBuiltIn: true);

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { userPrompt });
        _mockPromptService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { template });
        _mockPromptService.Setup(s => s.GetByIdAsync(userPrompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPrompt);

        var vm = CreateViewModel();

        // Act
        await vm.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.Single(vm.UserPrompts);
        Assert.Single(vm.Templates);
        Assert.Equal("User Prompt", vm.UserPrompts[0].Name);
        Assert.Equal("Template", vm.Templates[0].Name);
    }

    /// <summary>
    /// Verifies that InitializeAsync selects first user prompt.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SelectsFirstUserPrompt()
    {
        // Arrange
        var userPrompt = CreateTestPrompt("User Prompt");

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { userPrompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(userPrompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPrompt);

        var vm = CreateViewModel();

        // Act
        await vm.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(vm.SelectedPrompt);
        Assert.Equal(userPrompt.Id, vm.SelectedPrompt.Id);
    }

    /// <summary>
    /// Verifies that InitializeAsync selects first template when no user prompts.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_NoUserPrompts_SelectsFirstTemplate()
    {
        // Arrange
        var template = CreateTestPrompt("Template", isBuiltIn: true);

        _mockPromptService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { template });
        _mockPromptService.Setup(s => s.GetByIdAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var vm = CreateViewModel();

        // Act
        await vm.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(vm.SelectedPrompt);
        Assert.Equal(template.Id, vm.SelectedPrompt.Id);
    }

    /// <summary>
    /// Verifies that InitializeAsync sets IsLoading during operation.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SetsIsLoadingDuringOperation()
    {
        // Arrange
        var vm = CreateViewModel();
        var wasLoading = false;

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                wasLoading = vm.IsLoading;
                return Task.FromResult<IReadOnlyList<SystemPrompt>>(new List<SystemPrompt>());
            });

        // Act
        await vm.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.True(wasLoading);
        Assert.False(vm.IsLoading); // Should be false after completion
    }

    #endregion

    #region CreateNewPromptAsync Tests

    /// <summary>
    /// Verifies that CreateNewPromptAsync sets IsNewPrompt to true.
    /// </summary>
    [Fact]
    public async Task CreateNewPromptAsync_SetsIsNewPromptTrue()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Assert
        Assert.True(vm.IsNewPrompt);
    }

    /// <summary>
    /// Verifies that CreateNewPromptAsync clears selection.
    /// </summary>
    [Fact]
    public async Task CreateNewPromptAsync_ClearsSelection()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Assert
        Assert.Null(vm.SelectedPrompt);
    }

    /// <summary>
    /// Verifies that CreateNewPromptAsync sets default name.
    /// </summary>
    [Fact]
    public async Task CreateNewPromptAsync_SetsDefaultName()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("New Prompt", vm.PromptName);
    }

    /// <summary>
    /// Verifies that CreateNewPromptAsync sets IsEditing to true.
    /// </summary>
    [Fact]
    public async Task CreateNewPromptAsync_SetsIsEditingTrue()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Assert
        Assert.True(vm.IsEditing);
    }

    /// <summary>
    /// Verifies that CreateNewPromptAsync clears editor content.
    /// </summary>
    [Fact]
    public async Task CreateNewPromptAsync_ClearsEditorContent()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Assert
        Assert.Empty(vm.EditorContent);
        Assert.Empty(vm.PromptDescription);
        Assert.Equal("General", vm.PromptCategory);
    }

    #endregion

    #region SavePromptAsync Tests

    /// <summary>
    /// Verifies that SavePromptAsync creates new prompt when IsNewPrompt is true.
    /// </summary>
    [Fact]
    public async Task SavePromptAsync_WhenNewPrompt_CreatesPrompt()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        vm.PromptName = "My New Prompt";
        vm.EditorContent = "You are a new assistant.";

        var createdPrompt = CreateTestPrompt("My New Prompt", "You are a new assistant.");
        _mockPromptService.Setup(s => s.CreatePromptAsync(
            "My New Prompt",
            "You are a new assistant.",
            null,
            "General",
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPrompt);
        _mockPromptService.Setup(s => s.GetByIdAsync(createdPrompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPrompt);

        // Act
        await vm.SavePromptCommand.ExecuteAsync(null);

        // Assert
        _mockPromptService.Verify(s => s.CreatePromptAsync(
            "My New Prompt",
            "You are a new assistant.",
            null,
            "General",
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that SavePromptAsync updates existing prompt when not new.
    /// </summary>
    [Fact]
    public async Task SavePromptAsync_WhenExistingPrompt_UpdatesPrompt()
    {
        // Arrange
        var existingPrompt = CreateTestPrompt("Existing Prompt", "Original content");

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { existingPrompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(existingPrompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPrompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        // Modify the prompt
        vm.EditorContent = "Updated content";

        var updatedPrompt = CreateTestPrompt("Existing Prompt", "Updated content");

        _mockPromptService.Setup(s => s.UpdatePromptAsync(
            existingPrompt.Id,
            "Existing Prompt",
            "Updated content",
            "Test description",
            "General",
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedPrompt);

        // Act
        await vm.SavePromptCommand.ExecuteAsync(null);

        // Assert
        _mockPromptService.Verify(s => s.UpdatePromptAsync(
            existingPrompt.Id,
            "Existing Prompt",
            "Updated content",
            "Test description",
            "General",
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that SavePromptAsync sets IsDirty to false after save.
    /// </summary>
    [Fact]
    public async Task SavePromptAsync_SetsIsDirtyFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        vm.PromptName = "Test";
        vm.EditorContent = "Content";

        var createdPrompt = CreateTestPrompt("Test", "Content");
        _mockPromptService.Setup(s => s.CreatePromptAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<IEnumerable<string>?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPrompt);
        _mockPromptService.Setup(s => s.GetByIdAsync(createdPrompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPrompt);

        // Ensure IsDirty is true before save
        Assert.True(vm.IsDirty);

        // Act
        await vm.SavePromptCommand.ExecuteAsync(null);

        // Assert
        Assert.False(vm.IsDirty);
    }

    #endregion

    #region DeletePromptAsync Tests

    /// <summary>
    /// Verifies that DeletePromptAsync calls service delete method.
    /// </summary>
    [Fact]
    public async Task DeletePromptAsync_CallsServiceDelete()
    {
        // Arrange
        var prompt = CreateTestPrompt("To Delete");

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(prompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);
        _mockPromptService.Setup(s => s.DeletePromptAsync(prompt.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        // Act
        await vm.DeletePromptCommand.ExecuteAsync(null);

        // Assert
        _mockPromptService.Verify(s => s.DeletePromptAsync(prompt.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that DeletePromptAsync does not delete built-in prompts.
    /// </summary>
    [Fact]
    public async Task DeletePromptAsync_BuiltInPrompt_DoesNotDelete()
    {
        // Arrange
        var builtInPrompt = CreateTestPrompt("Built-in", isBuiltIn: true);

        _mockPromptService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { builtInPrompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(builtInPrompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(builtInPrompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        // Assert - CanDelete should be false for built-in prompts
        Assert.False(vm.CanDelete);
    }

    #endregion

    #region DuplicatePromptAsync Tests

    /// <summary>
    /// Verifies that DuplicatePromptAsync calls service duplicate method.
    /// </summary>
    [Fact]
    public async Task DuplicatePromptAsync_CallsServiceDuplicate()
    {
        // Arrange
        var prompt = CreateTestPrompt("Original");
        var duplicate = CreateTestPrompt("Original (Copy)");

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(prompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);
        _mockPromptService.Setup(s => s.DuplicatePromptAsync(prompt.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicate);
        _mockPromptService.Setup(s => s.GetByIdAsync(duplicate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicate);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        // Act
        await vm.DuplicatePromptCommand.ExecuteAsync(null);

        // Assert
        _mockPromptService.Verify(s => s.DuplicatePromptAsync(prompt.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SetAsDefaultAsync Tests

    /// <summary>
    /// Verifies that SetAsDefaultAsync calls service set default method.
    /// </summary>
    [Fact]
    public async Task SetAsDefaultAsync_CallsServiceSetDefault()
    {
        // Arrange
        var prompt = CreateTestPrompt("Not Default", isDefault: false);

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(prompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);
        _mockPromptService.Setup(s => s.SetAsDefaultAsync(prompt.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        // Act
        await vm.SetAsDefaultCommand.ExecuteAsync(null);

        // Assert
        _mockPromptService.Verify(s => s.SetAsDefaultAsync(prompt.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that CanSetDefault is false for already default prompts.
    /// </summary>
    [Fact]
    public async Task CanSetDefault_WhenAlreadyDefault_ReturnsFalse()
    {
        // Arrange
        var defaultPrompt = CreateTestPrompt("Default", isDefault: true);

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { defaultPrompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(defaultPrompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultPrompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.False(vm.CanSetDefault);
    }

    #endregion

    #region DiscardChangesAsync Tests

    /// <summary>
    /// Verifies that DiscardChangesAsync reverts to original values.
    /// </summary>
    [Fact]
    public async Task DiscardChangesAsync_RevertsToOriginalValues()
    {
        // Arrange
        var prompt = CreateTestPrompt("Original Name", "Original content");

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(prompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        // Make changes
        vm.PromptName = "Modified Name";
        vm.EditorContent = "Modified content";

        Assert.True(vm.IsDirty);

        // Act
        await vm.DiscardChangesCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Original Name", vm.PromptName);
        Assert.Equal("Original content", vm.EditorContent);
        Assert.False(vm.IsDirty);
    }

    /// <summary>
    /// Verifies that DiscardChangesAsync clears new prompt and selects first available.
    /// </summary>
    [Fact]
    public async Task DiscardChangesAsync_WhenNewPrompt_ClearsAndSelectsFirst()
    {
        // Arrange
        var existingPrompt = CreateTestPrompt("Existing");

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { existingPrompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(existingPrompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPrompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        vm.PromptName = "New prompt name";
        vm.EditorContent = "New prompt content";

        // Act
        await vm.DiscardChangesCommand.ExecuteAsync(null);

        // Assert
        Assert.False(vm.IsNewPrompt);
        Assert.NotNull(vm.SelectedPrompt);
        Assert.Equal(existingPrompt.Id, vm.SelectedPrompt.Id);
    }

    #endregion

    #region Dirty Tracking Tests

    /// <summary>
    /// Verifies that IsDirty becomes true when PromptName changes.
    /// </summary>
    [Fact]
    public async Task IsDirty_WhenPromptNameChanges_BecomesTrue()
    {
        // Arrange
        var prompt = CreateTestPrompt("Original");

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(prompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        Assert.False(vm.IsDirty);

        // Act
        vm.PromptName = "Modified";

        // Assert
        Assert.True(vm.IsDirty);
    }

    /// <summary>
    /// Verifies that IsDirty becomes true when EditorContent changes.
    /// </summary>
    [Fact]
    public async Task IsDirty_WhenEditorContentChanges_BecomesTrue()
    {
        // Arrange
        var prompt = CreateTestPrompt("Test", "Original content");

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(prompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        Assert.False(vm.IsDirty);

        // Act
        vm.EditorContent = "Modified content";

        // Assert
        Assert.True(vm.IsDirty);
    }

    /// <summary>
    /// Verifies that IsDirty returns to false when content matches original.
    /// </summary>
    [Fact]
    public async Task IsDirty_WhenContentMatchesOriginal_ReturnsFalse()
    {
        // Arrange
        var prompt = CreateTestPrompt("Test", "Original content");

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(prompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        // Make a change
        vm.EditorContent = "Modified content";
        Assert.True(vm.IsDirty);

        // Act - revert to original
        vm.EditorContent = "Original content";

        // Assert
        Assert.False(vm.IsDirty);
    }

    /// <summary>
    /// Verifies that IsDirty becomes true when new prompt content is modified.
    /// </summary>
    [Fact]
    public async Task IsDirty_NewPromptWithContent_IsTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Initial state after create new - IsDirty is explicitly false
        // The ViewModel sets IsDirty = false at the end of CreateNewPromptAsync
        Assert.False(vm.IsDirty);

        // Act - modify content from the initial state
        vm.EditorContent = "Some new content";

        // Assert - now dirty because content differs from original
        Assert.True(vm.IsDirty);
    }

    #endregion

    #region Computed Properties Tests

    /// <summary>
    /// Verifies CharacterCount returns correct length.
    /// </summary>
    [Fact]
    public async Task CharacterCount_ReturnsContentLength()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.EditorContent = "Hello World"; // 11 characters

        // Assert
        Assert.Equal(11, vm.CharacterCount);
    }

    /// <summary>
    /// Verifies EstimatedTokenCount returns approximate token count.
    /// </summary>
    [Fact]
    public async Task EstimatedTokenCount_ReturnsApproximateCount()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.EditorContent = "This is a test message"; // 22 characters / 4 = 5 tokens

        // Assert
        Assert.Equal(5, vm.EstimatedTokenCount);
    }

    /// <summary>
    /// Verifies CharacterCountText is formatted correctly.
    /// </summary>
    [Fact]
    public async Task CharacterCountText_FormatsCorrectly()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.EditorContent = "Hello";

        // Assert
        Assert.Equal("5 characters", vm.CharacterCountText);
    }

    /// <summary>
    /// Verifies TokenCountText is formatted correctly.
    /// </summary>
    [Fact]
    public async Task TokenCountText_FormatsCorrectly()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.EditorContent = "This is a test"; // 14 chars / 4 = 3 tokens

        // Assert
        Assert.Equal("~3 tokens", vm.TokenCountText);
    }

    /// <summary>
    /// Verifies HasContent returns true for non-empty content.
    /// </summary>
    [Fact]
    public async Task HasContent_WithContent_ReturnsTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.EditorContent = "Some content";

        // Assert
        Assert.True(vm.HasContent);
    }

    /// <summary>
    /// Verifies HasContent returns false for empty content.
    /// </summary>
    [Fact]
    public async Task HasContent_EmptyContent_ReturnsFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.EditorContent = "   ";

        // Assert
        Assert.False(vm.HasContent);
    }

    /// <summary>
    /// Verifies HasValidName returns true for non-empty name.
    /// </summary>
    [Fact]
    public async Task HasValidName_WithName_ReturnsTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.PromptName = "Valid Name";

        // Assert
        Assert.True(vm.HasValidName);
    }

    /// <summary>
    /// Verifies HasValidName returns false for empty name.
    /// </summary>
    [Fact]
    public async Task HasValidName_EmptyName_ReturnsFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.PromptName = "   ";

        // Assert
        Assert.False(vm.HasValidName);
    }

    #endregion

    #region CanSave Tests

    /// <summary>
    /// Verifies CanSave is true when all conditions are met.
    /// </summary>
    [Fact]
    public async Task CanSave_AllConditionsMet_ReturnsTrue()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.PromptName = "Valid Name";
        vm.EditorContent = "Valid content";

        // Assert
        Assert.True(vm.HasValidName);
        Assert.True(vm.HasContent);
        Assert.True(vm.IsDirty);
        Assert.True(vm.CanSave);
    }

    /// <summary>
    /// Verifies CanSave is false when name is empty.
    /// </summary>
    [Fact]
    public async Task CanSave_EmptyName_ReturnsFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.PromptName = "";
        vm.EditorContent = "Valid content";

        // Assert
        Assert.False(vm.CanSave);
    }

    /// <summary>
    /// Verifies CanSave is false when content is empty.
    /// </summary>
    [Fact]
    public async Task CanSave_EmptyContent_ReturnsFalse()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.PromptName = "Valid Name";
        vm.EditorContent = "";

        // Assert
        Assert.False(vm.CanSave);
    }

    /// <summary>
    /// Verifies CanSave is false when not dirty.
    /// </summary>
    [Fact]
    public async Task CanSave_NotDirty_ReturnsFalse()
    {
        // Arrange
        var prompt = CreateTestPrompt("Test", "Content");

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { prompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(prompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        // Assert - content matches original, so not dirty
        Assert.False(vm.IsDirty);
        Assert.False(vm.CanSave);
    }

    #endregion

    #region CanEdit Tests

    /// <summary>
    /// Verifies CanEdit is true for new prompt.
    /// </summary>
    [Fact]
    public async Task CanEdit_NewPrompt_ReturnsTrue()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Assert
        Assert.True(vm.CanEdit);
    }

    /// <summary>
    /// Verifies CanEdit is true for user prompt (not built-in).
    /// </summary>
    [Fact]
    public async Task CanEdit_UserPrompt_ReturnsTrue()
    {
        // Arrange
        var userPrompt = CreateTestPrompt("User Prompt", isBuiltIn: false);

        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { userPrompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(userPrompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPrompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.True(vm.CanEdit);
    }

    /// <summary>
    /// Verifies CanEdit is false for built-in prompt.
    /// </summary>
    [Fact]
    public async Task CanEdit_BuiltInPrompt_ReturnsFalse()
    {
        // Arrange
        var builtInPrompt = CreateTestPrompt("Built-in", isBuiltIn: true);

        _mockPromptService.Setup(s => s.GetTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPrompt> { builtInPrompt });
        _mockPromptService.Setup(s => s.GetByIdAsync(builtInPrompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(builtInPrompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        // Assert
        Assert.False(vm.CanEdit);
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Verifies ValidationError is set when name is empty.
    /// </summary>
    [Fact]
    public async Task Validation_EmptyName_SetsValidationError()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.PromptName = "";

        // Assert
        Assert.Equal("Name is required.", vm.ValidationError);
    }

    /// <summary>
    /// Verifies ValidationError is set when content is empty.
    /// </summary>
    [Fact]
    public async Task Validation_EmptyContent_SetsValidationError()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.PromptName = "Valid Name";
        vm.EditorContent = "";

        // Assert
        Assert.Equal("Content is required.", vm.ValidationError);
    }

    /// <summary>
    /// Verifies ValidationError is null when all valid.
    /// </summary>
    [Fact]
    public async Task Validation_AllValid_ClearsValidationError()
    {
        // Arrange
        var vm = CreateViewModel();
        await vm.CreateNewPromptCommand.ExecuteAsync(null);

        // Act
        vm.PromptName = "Valid Name";
        vm.EditorContent = "Valid content";

        // Assert
        Assert.Null(vm.ValidationError);
    }

    #endregion

    #region Event Handling Tests

    /// <summary>
    /// Verifies that PromptListChanged event triggers list reload.
    /// </summary>
    [Fact]
    public async Task PromptListChanged_ReloadsPrompts()
    {
        // Arrange
        var initialPrompt = CreateTestPrompt("Initial");
        var newPrompt = CreateTestPrompt("New");

        var callCount = 0;
        _mockPromptService.Setup(s => s.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new List<SystemPrompt> { initialPrompt }
                    : new List<SystemPrompt> { initialPrompt, newPrompt };
            });
        _mockPromptService.Setup(s => s.GetByIdAsync(initialPrompt.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialPrompt);

        var vm = CreateViewModel();
        await vm.InitializeCommand.ExecuteAsync(null);

        Assert.Single(vm.UserPrompts);

        // Act - Raise the event
        _mockPromptService.Raise(
            s => s.PromptListChanged += null,
            new PromptListChangedEventArgs
            {
                ChangeType = PromptListChangeType.PromptCreated,
                AffectedPromptId = newPrompt.Id,
                AffectedPromptName = newPrompt.Name
            });

        // Allow async event handler to complete
        await Task.Delay(100);

        // Assert - Should have reloaded
        Assert.Equal(2, vm.UserPrompts.Count);
    }

    #endregion

    #region Dispose Tests

    /// <summary>
    /// Verifies Dispose unsubscribes from PromptListChanged event.
    /// </summary>
    [Fact]
    public void Dispose_UnsubscribesFromPromptListChangedEvent()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.Dispose();

        // Assert
        _mockPromptService.VerifyRemove(
            s => s.PromptListChanged -= It.IsAny<EventHandler<PromptListChangedEventArgs>>(),
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
