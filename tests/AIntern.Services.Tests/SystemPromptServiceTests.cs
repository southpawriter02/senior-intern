using AIntern.Core.Entities;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Data.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIntern.Services.Tests;

/// <summary>
/// Unit tests for SystemPromptService (v0.2.4b).
/// Tests CRUD operations, event firing, current prompt management, and initialization.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify:
/// </para>
/// <list type="bullet">
///   <item><description>Query operations delegate to repository correctly</description></item>
///   <item><description>Create operations validate input and fire events</description></item>
///   <item><description>Update operations handle changes and built-in protection</description></item>
///   <item><description>Delete operations handle soft delete and current prompt reset</description></item>
///   <item><description>SetCurrentPromptAsync persists selection and fires events</description></item>
///   <item><description>InitializeAsync loads from settings or falls back to default</description></item>
/// </list>
/// <para>Added in v0.2.5a (test coverage for v0.2.4b).</para>
/// </remarks>
public class SystemPromptServiceTests : IAsyncDisposable
{
    #region Test Infrastructure

    private readonly Mock<ISystemPromptRepository> _mockRepository;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<SystemPromptService>> _mockLogger;
    private readonly AppSettings _appSettings;

    private SystemPromptService? _service;

    public SystemPromptServiceTests()
    {
        _mockRepository = new Mock<ISystemPromptRepository>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<SystemPromptService>>();
        _appSettings = new AppSettings();

        // Default setup
        _mockSettingsService.Setup(s => s.CurrentSettings).Returns(_appSettings);
        _mockSettingsService.Setup(s => s.LoadSettingsAsync()).ReturnsAsync(_appSettings);
        _mockSettingsService.Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
    }

    private SystemPromptService CreateService()
    {
        _service = new SystemPromptService(
            _mockRepository.Object,
            _mockSettingsService.Object,
            _mockLogger.Object);
        return _service;
    }

    private static SystemPromptEntity CreateTestEntity(
        string name = "Test Prompt",
        string content = "You are a helpful assistant.",
        bool isBuiltIn = false,
        bool isDefault = false,
        bool isActive = true)
    {
        return new SystemPromptEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Content = content,
            Description = "Test description",
            Category = "General",
            IsBuiltIn = isBuiltIn,
            IsDefault = isDefault,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UsageCount = 0
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_service != null)
        {
            await _service.DisposeAsync();
        }
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies constructor throws when repository is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SystemPromptService(null!, _mockSettingsService.Object, _mockLogger.Object));
    }

    /// <summary>
    /// Verifies constructor throws when settings service is null.
    /// </summary>
    [Fact]
    public void Constructor_NullSettingsService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SystemPromptService(_mockRepository.Object, null!, _mockLogger.Object));
    }

    /// <summary>
    /// Verifies constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SystemPromptService(_mockRepository.Object, _mockSettingsService.Object, null!));
    }

    /// <summary>
    /// Verifies CurrentPrompt is null before initialization.
    /// </summary>
    [Fact]
    public void Constructor_CurrentPromptIsNullBeforeInitialization()
    {
        // Act
        var service = CreateService();

        // Assert
        Assert.Null(service.CurrentPrompt);
    }

    #endregion

    #region InitializeAsync Tests

    /// <summary>
    /// Verifies InitializeAsync loads saved prompt from settings.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_WithSavedPromptId_LoadsSavedPrompt()
    {
        // Arrange
        var savedEntity = CreateTestEntity("Saved Prompt");
        _appSettings.CurrentSystemPromptId = savedEntity.Id;
        _mockRepository.Setup(r => r.GetByIdAsync(savedEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);

        var service = CreateService();

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.NotNull(service.CurrentPrompt);
        Assert.Equal(savedEntity.Id, service.CurrentPrompt.Id);
        Assert.Equal("Saved Prompt", service.CurrentPrompt.Name);
    }

    /// <summary>
    /// Verifies InitializeAsync falls back to default when no saved prompt.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_NoSavedPrompt_FallsBackToDefault()
    {
        // Arrange
        var defaultEntity = CreateTestEntity("Default Prompt", isDefault: true);
        _appSettings.CurrentSystemPromptId = null;
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultEntity);

        var service = CreateService();

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.NotNull(service.CurrentPrompt);
        Assert.Equal("Default Prompt", service.CurrentPrompt.Name);
    }

    /// <summary>
    /// Verifies InitializeAsync falls back to first prompt when no default exists.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_NoDefault_FallsBackToFirstPrompt()
    {
        // Arrange
        var firstEntity = CreateTestEntity("First Prompt");
        _appSettings.CurrentSystemPromptId = null;
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity?)null);
        _mockRepository.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPromptEntity> { firstEntity });

        var service = CreateService();

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.NotNull(service.CurrentPrompt);
        Assert.Equal("First Prompt", service.CurrentPrompt.Name);
    }

    /// <summary>
    /// Verifies InitializeAsync is idempotent.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_CalledMultipleTimes_OnlyInitializesOnce()
    {
        // Arrange
        var defaultEntity = CreateTestEntity("Default");
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultEntity);

        var service = CreateService();

        // Act
        await service.InitializeAsync();
        await service.InitializeAsync();
        await service.InitializeAsync();

        // Assert - LoadSettingsAsync should only be called once
        _mockSettingsService.Verify(s => s.LoadSettingsAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies InitializeAsync handles inactive saved prompt.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SavedPromptInactive_FallsBackToDefault()
    {
        // Arrange
        var inactiveEntity = CreateTestEntity("Inactive", isActive: false);
        var defaultEntity = CreateTestEntity("Default", isDefault: true);
        _appSettings.CurrentSystemPromptId = inactiveEntity.Id;

        _mockRepository.Setup(r => r.GetByIdAsync(inactiveEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveEntity);
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultEntity);

        var service = CreateService();

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.NotNull(service.CurrentPrompt);
        Assert.Equal("Default", service.CurrentPrompt.Name);
    }

    #endregion

    #region GetAllPromptsAsync Tests

    /// <summary>
    /// Verifies GetAllPromptsAsync returns all active prompts.
    /// </summary>
    [Fact]
    public async Task GetAllPromptsAsync_ReturnsAllActivePrompts()
    {
        // Arrange
        var entities = new List<SystemPromptEntity>
        {
            CreateTestEntity("Prompt 1"),
            CreateTestEntity("Prompt 2")
        };
        _mockRepository.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var service = CreateService();

        // Act
        var result = await service.GetAllPromptsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Prompt 1", result[0].Name);
        Assert.Equal("Prompt 2", result[1].Name);
    }

    /// <summary>
    /// Verifies GetAllPromptsAsync returns empty list when no prompts.
    /// </summary>
    [Fact]
    public async Task GetAllPromptsAsync_NoPrompts_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SystemPromptEntity>());

        var service = CreateService();

        // Act
        var result = await service.GetAllPromptsAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetUserPromptsAsync Tests

    /// <summary>
    /// Verifies GetUserPromptsAsync returns only user prompts.
    /// </summary>
    [Fact]
    public async Task GetUserPromptsAsync_ReturnsOnlyUserPrompts()
    {
        // Arrange
        var entities = new List<SystemPromptEntity>
        {
            CreateTestEntity("User Prompt 1", isBuiltIn: false),
            CreateTestEntity("User Prompt 2", isBuiltIn: false)
        };
        _mockRepository.Setup(r => r.GetUserPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var service = CreateService();

        // Act
        var result = await service.GetUserPromptsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.False(p.IsBuiltIn));
    }

    #endregion

    #region GetTemplatesAsync Tests

    /// <summary>
    /// Verifies GetTemplatesAsync returns only built-in prompts.
    /// </summary>
    [Fact]
    public async Task GetTemplatesAsync_ReturnsOnlyBuiltInPrompts()
    {
        // Arrange
        var entities = new List<SystemPromptEntity>
        {
            CreateTestEntity("Template 1", isBuiltIn: true),
            CreateTestEntity("Template 2", isBuiltIn: true)
        };
        _mockRepository.Setup(r => r.GetBuiltInPromptsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var service = CreateService();

        // Act
        var result = await service.GetTemplatesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.IsBuiltIn));
    }

    #endregion

    #region GetByIdAsync Tests

    /// <summary>
    /// Verifies GetByIdAsync returns prompt when found.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsPrompt()
    {
        // Arrange
        var entity = CreateTestEntity("Found Prompt");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Found Prompt", result.Name);
    }

    /// <summary>
    /// Verifies GetByIdAsync returns null when not found.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity?)null);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(unknownId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region SearchPromptsAsync Tests

    /// <summary>
    /// Verifies SearchPromptsAsync returns matching prompts.
    /// </summary>
    [Fact]
    public async Task SearchPromptsAsync_ReturnsMatchingPrompts()
    {
        // Arrange
        var entities = new List<SystemPromptEntity>
        {
            CreateTestEntity("Code Review"),
            CreateTestEntity("Code Assistant")
        };
        _mockRepository.Setup(r => r.SearchAsync("Code", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var service = CreateService();

        // Act
        var result = await service.SearchPromptsAsync("Code");

        // Assert
        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// Verifies SearchPromptsAsync returns empty for whitespace search term.
    /// </summary>
    [Fact]
    public async Task SearchPromptsAsync_WhitespaceSearchTerm_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SearchPromptsAsync("   ");

        // Assert
        Assert.Empty(result);
        _mockRepository.Verify(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region CreatePromptAsync Tests

    /// <summary>
    /// Verifies CreatePromptAsync creates prompt and fires event.
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_ValidInput_CreatesPromptAndFiresEvent()
    {
        // Arrange
        _mockRepository.Setup(r => r.NameExistsAsync("New Prompt", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SystemPromptEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity e, CancellationToken _) => e);

        var service = CreateService();
        PromptListChangedEventArgs? eventArgs = null;
        service.PromptListChanged += (_, e) => eventArgs = e;

        // Act
        var result = await service.CreatePromptAsync("New Prompt", "Content");

        // Assert
        Assert.Equal("New Prompt", result.Name);
        Assert.Equal("Content", result.Content);
        Assert.False(result.IsBuiltIn);

        Assert.NotNull(eventArgs);
        Assert.Equal(PromptListChangeType.PromptCreated, eventArgs.ChangeType);
        Assert.Equal("New Prompt", eventArgs.AffectedPromptName);
    }

    /// <summary>
    /// Verifies CreatePromptAsync throws for null name.
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_NullName_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        // ThrowIfNullOrWhiteSpace throws ArgumentNullException (subclass of ArgumentException)
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            service.CreatePromptAsync(null!, "Content"));
    }

    /// <summary>
    /// Verifies CreatePromptAsync throws for empty content.
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_EmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreatePromptAsync("Name", ""));
    }

    /// <summary>
    /// Verifies CreatePromptAsync throws for duplicate name.
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockRepository.Setup(r => r.NameExistsAsync("Existing", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePromptAsync("Existing", "Content"));
        Assert.Contains("already exists", ex.Message);
    }

    /// <summary>
    /// Verifies CreatePromptAsync uses default category when not specified.
    /// </summary>
    [Fact]
    public async Task CreatePromptAsync_NoCategory_UsesDefaultGeneral()
    {
        // Arrange
        _mockRepository.Setup(r => r.NameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SystemPromptEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity e, CancellationToken _) => e);

        var service = CreateService();

        // Act
        var result = await service.CreatePromptAsync("Test", "Content");

        // Assert
        Assert.Equal("General", result.Category);
    }

    #endregion

    #region UpdatePromptAsync Tests

    /// <summary>
    /// Verifies UpdatePromptAsync updates prompt and fires event.
    /// </summary>
    [Fact]
    public async Task UpdatePromptAsync_ValidChanges_UpdatesAndFiresEvent()
    {
        // Arrange
        var entity = CreateTestEntity("Original");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mockRepository.Setup(r => r.NameExistsAsync("Updated", entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();
        PromptListChangedEventArgs? eventArgs = null;
        service.PromptListChanged += (_, e) => eventArgs = e;

        // Act
        var result = await service.UpdatePromptAsync(entity.Id, name: "Updated", content: "New content");

        // Assert
        Assert.Equal("Updated", result.Name);
        Assert.Equal("New content", result.Content);

        Assert.NotNull(eventArgs);
        Assert.Equal(PromptListChangeType.PromptUpdated, eventArgs.ChangeType);
    }

    /// <summary>
    /// Verifies UpdatePromptAsync skips when no changes.
    /// </summary>
    [Fact]
    public async Task UpdatePromptAsync_NoChanges_SkipsUpdate()
    {
        // Arrange
        var entity = CreateTestEntity("Unchanged");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        var eventFired = false;
        service.PromptListChanged += (_, _) => eventFired = true;

        // Act - pass same name, no other changes
        var result = await service.UpdatePromptAsync(entity.Id, name: "Unchanged");

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<SystemPromptEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(eventFired);
    }

    /// <summary>
    /// Verifies UpdatePromptAsync throws for non-existent prompt.
    /// </summary>
    [Fact]
    public async Task UpdatePromptAsync_NonExistent_ThrowsInvalidOperationException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdatePromptAsync(unknownId, name: "New Name"));
    }

    /// <summary>
    /// Verifies UpdatePromptAsync refreshes current prompt reference.
    /// </summary>
    [Fact]
    public async Task UpdatePromptAsync_CurrentPrompt_RefreshesReference()
    {
        // Arrange
        var entity = CreateTestEntity("Current");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        await service.InitializeAsync();

        // Act
        await service.UpdatePromptAsync(entity.Id, content: "Updated content");

        // Assert - current prompt should be updated
        Assert.Equal("Updated content", service.CurrentPrompt?.Content);
    }

    #endregion

    #region DeletePromptAsync Tests

    /// <summary>
    /// Verifies DeletePromptAsync soft-deletes and fires event.
    /// </summary>
    [Fact]
    public async Task DeletePromptAsync_ValidPrompt_DeletesAndFiresEvent()
    {
        // Arrange
        var entity = CreateTestEntity("ToDelete", isBuiltIn: false);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        PromptListChangedEventArgs? eventArgs = null;
        service.PromptListChanged += (_, e) => eventArgs = e;

        // Act
        await service.DeletePromptAsync(entity.Id);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(eventArgs);
        Assert.Equal(PromptListChangeType.PromptDeleted, eventArgs.ChangeType);
        Assert.Equal("ToDelete", eventArgs.AffectedPromptName);
    }

    /// <summary>
    /// Verifies DeletePromptAsync throws for built-in prompt.
    /// </summary>
    [Fact]
    public async Task DeletePromptAsync_BuiltInPrompt_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = CreateTestEntity("BuiltIn", isBuiltIn: true);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeletePromptAsync(entity.Id));
        Assert.Contains("built-in", ex.Message);
    }

    /// <summary>
    /// Verifies DeletePromptAsync resets current prompt when deleted.
    /// </summary>
    [Fact]
    public async Task DeletePromptAsync_CurrentPrompt_ResetsToDefault()
    {
        // Arrange
        var currentEntity = CreateTestEntity("Current", isBuiltIn: false);
        var defaultEntity = CreateTestEntity("Default", isDefault: true);

        _mockRepository.Setup(r => r.GetByIdAsync(currentEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentEntity);
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultEntity);

        // Setup for initialize - load current
        _appSettings.CurrentSystemPromptId = currentEntity.Id;

        var service = CreateService();
        await service.InitializeAsync();

        Assert.Equal(currentEntity.Id, service.CurrentPrompt?.Id);

        CurrentPromptChangedEventArgs? changedArgs = null;
        service.CurrentPromptChanged += (_, e) => changedArgs = e;

        // Act
        await service.DeletePromptAsync(currentEntity.Id);

        // Assert - should have reset to default
        Assert.NotNull(changedArgs);
        Assert.Equal("Default", changedArgs.NewPrompt?.Name);
        Assert.Equal("Current", changedArgs.PreviousPrompt?.Name);
    }

    /// <summary>
    /// Verifies DeletePromptAsync throws for non-existent prompt.
    /// </summary>
    [Fact]
    public async Task DeletePromptAsync_NonExistent_ThrowsInvalidOperationException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeletePromptAsync(unknownId));
    }

    #endregion

    #region DuplicatePromptAsync Tests

    /// <summary>
    /// Verifies DuplicatePromptAsync creates copy with new name.
    /// </summary>
    [Fact]
    public async Task DuplicatePromptAsync_ValidPrompt_CreatesCopy()
    {
        // Arrange
        var sourceEntity = CreateTestEntity("Original");
        _mockRepository.Setup(r => r.GetByIdAsync(sourceEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceEntity);
        _mockRepository.Setup(r => r.NameExistsAsync("Original (Copy)", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SystemPromptEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity e, CancellationToken _) => e);

        var service = CreateService();

        // Act
        var result = await service.DuplicatePromptAsync(sourceEntity.Id);

        // Assert
        Assert.Equal("Original (Copy)", result.Name);
        Assert.Equal(sourceEntity.Content, result.Content);
        Assert.False(result.IsBuiltIn);
        Assert.NotEqual(sourceEntity.Id, result.Id);
    }

    /// <summary>
    /// Verifies DuplicatePromptAsync uses provided name.
    /// </summary>
    [Fact]
    public async Task DuplicatePromptAsync_WithCustomName_UsesCustomName()
    {
        // Arrange
        var sourceEntity = CreateTestEntity("Original");
        _mockRepository.Setup(r => r.GetByIdAsync(sourceEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceEntity);
        _mockRepository.Setup(r => r.NameExistsAsync("My Copy", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SystemPromptEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity e, CancellationToken _) => e);

        var service = CreateService();

        // Act
        var result = await service.DuplicatePromptAsync(sourceEntity.Id, "My Copy");

        // Assert
        Assert.Equal("My Copy", result.Name);
    }

    /// <summary>
    /// Verifies DuplicatePromptAsync generates unique name on conflict.
    /// </summary>
    [Fact]
    public async Task DuplicatePromptAsync_NameConflict_GeneratesUniqueName()
    {
        // Arrange
        var sourceEntity = CreateTestEntity("Original");
        _mockRepository.Setup(r => r.GetByIdAsync(sourceEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceEntity);
        _mockRepository.Setup(r => r.NameExistsAsync("Original (Copy)", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockRepository.Setup(r => r.NameExistsAsync("Original (Copy) (1)", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SystemPromptEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity e, CancellationToken _) => e);

        var service = CreateService();

        // Act
        var result = await service.DuplicatePromptAsync(sourceEntity.Id);

        // Assert
        Assert.Equal("Original (Copy) (1)", result.Name);
    }

    #endregion

    #region SetAsDefaultAsync Tests

    /// <summary>
    /// Verifies SetAsDefaultAsync sets default and fires event.
    /// </summary>
    [Fact]
    public async Task SetAsDefaultAsync_ValidPrompt_SetsDefaultAndFiresEvent()
    {
        // Arrange
        var entity = CreateTestEntity("NewDefault");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        PromptListChangedEventArgs? eventArgs = null;
        service.PromptListChanged += (_, e) => eventArgs = e;

        // Act
        await service.SetAsDefaultAsync(entity.Id);

        // Assert
        _mockRepository.Verify(r => r.SetAsDefaultAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(eventArgs);
        Assert.Equal(PromptListChangeType.DefaultChanged, eventArgs.ChangeType);
    }

    /// <summary>
    /// Verifies SetAsDefaultAsync skips when already default.
    /// </summary>
    [Fact]
    public async Task SetAsDefaultAsync_AlreadyDefault_Skips()
    {
        // Arrange
        var entity = CreateTestEntity("AlreadyDefault", isDefault: true);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        var eventFired = false;
        service.PromptListChanged += (_, _) => eventFired = true;

        // Act
        await service.SetAsDefaultAsync(entity.Id);

        // Assert
        _mockRepository.Verify(r => r.SetAsDefaultAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(eventFired);
    }

    #endregion

    #region SetCurrentPromptAsync Tests

    /// <summary>
    /// Verifies SetCurrentPromptAsync sets current prompt and persists.
    /// </summary>
    [Fact]
    public async Task SetCurrentPromptAsync_ValidId_SetsCurrentAndPersists()
    {
        // Arrange
        var entity = CreateTestEntity("Selected");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        CurrentPromptChangedEventArgs? eventArgs = null;
        service.CurrentPromptChanged += (_, e) => eventArgs = e;

        // Act
        await service.SetCurrentPromptAsync(entity.Id);

        // Assert
        Assert.NotNull(service.CurrentPrompt);
        Assert.Equal("Selected", service.CurrentPrompt.Name);

        _mockSettingsService.Verify(s => s.SaveSettingsAsync(It.Is<AppSettings>(a => a.CurrentSystemPromptId == entity.Id)), Times.Once);
        _mockRepository.Verify(r => r.IncrementUsageCountAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(eventArgs);
        Assert.Equal("Selected", eventArgs.NewPrompt?.Name);
    }

    /// <summary>
    /// Verifies SetCurrentPromptAsync with null resets to default.
    /// </summary>
    [Fact]
    public async Task SetCurrentPromptAsync_NullId_ResetsToDefault()
    {
        // Arrange
        var currentEntity = CreateTestEntity("Current");
        var defaultEntity = CreateTestEntity("Default", isDefault: true);

        _appSettings.CurrentSystemPromptId = currentEntity.Id;
        _mockRepository.Setup(r => r.GetByIdAsync(currentEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentEntity);
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultEntity);

        var service = CreateService();
        await service.InitializeAsync();

        // Act
        await service.SetCurrentPromptAsync(null);

        // Assert
        Assert.Equal("Default", service.CurrentPrompt?.Name);
    }

    /// <summary>
    /// Verifies SetCurrentPromptAsync skips when already selected.
    /// </summary>
    [Fact]
    public async Task SetCurrentPromptAsync_AlreadySelected_Skips()
    {
        // Arrange
        var entity = CreateTestEntity("Current");
        _appSettings.CurrentSystemPromptId = entity.Id;
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        await service.InitializeAsync();

        var eventFired = false;
        service.CurrentPromptChanged += (_, _) => eventFired = true;

        // Act - try to select the same prompt
        await service.SetCurrentPromptAsync(entity.Id);

        // Assert - should skip without firing event or incrementing usage
        Assert.False(eventFired);
        // InitializeAsync with a valid saved prompt does NOT call SaveSettingsAsync,
        // and SetCurrentPromptAsync should skip because prompt is already current.
        // So SaveSettingsAsync should never be called.
        _mockSettingsService.Verify(s => s.SaveSettingsAsync(It.IsAny<AppSettings>()), Times.Never);
        _mockRepository.Verify(r => r.IncrementUsageCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies SetCurrentPromptAsync throws for inactive prompt.
    /// </summary>
    [Fact]
    public async Task SetCurrentPromptAsync_InactivePrompt_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = CreateTestEntity("Inactive", isActive: false);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetCurrentPromptAsync(entity.Id));
    }

    #endregion

    #region CreateFromTemplateAsync Tests

    /// <summary>
    /// Verifies CreateFromTemplateAsync creates copy from template.
    /// </summary>
    [Fact]
    public async Task CreateFromTemplateAsync_ValidTemplate_CreatesCopy()
    {
        // Arrange
        var templateEntity = CreateTestEntity("Template", isBuiltIn: true);
        templateEntity.Content = "Template content";

        _mockRepository.Setup(r => r.GetByIdAsync(templateEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateEntity);
        _mockRepository.Setup(r => r.NameExistsAsync("My Custom", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<SystemPromptEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity e, CancellationToken _) => e);

        var service = CreateService();

        // Act
        var result = await service.CreateFromTemplateAsync(templateEntity.Id, "My Custom");

        // Assert
        Assert.Equal("My Custom", result.Name);
        Assert.Equal("Template content", result.Content);
        Assert.False(result.IsBuiltIn);
    }

    /// <summary>
    /// Verifies CreateFromTemplateAsync throws for non-existent template.
    /// </summary>
    [Fact]
    public async Task CreateFromTemplateAsync_NonExistent_ThrowsInvalidOperationException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateFromTemplateAsync(unknownId));
    }

    #endregion

    #region FormatPromptForContext Tests

    /// <summary>
    /// Verifies FormatPromptForContext returns content for valid prompt.
    /// </summary>
    [Fact]
    public void FormatPromptForContext_ValidPrompt_ReturnsContent()
    {
        // Arrange
        var service = CreateService();
        var prompt = new SystemPrompt
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Content = "You are a helpful assistant.",
            Category = "General",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = service.FormatPromptForContext(prompt);

        // Assert
        Assert.Equal("You are a helpful assistant.", result);
    }

    /// <summary>
    /// Verifies FormatPromptForContext returns empty for null prompt.
    /// </summary>
    [Fact]
    public void FormatPromptForContext_NullPrompt_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.FormatPromptForContext(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region GetDefaultPromptAsync Tests

    /// <summary>
    /// Verifies GetDefaultPromptAsync returns default prompt.
    /// </summary>
    [Fact]
    public async Task GetDefaultPromptAsync_ReturnsDefaultPrompt()
    {
        // Arrange
        var entity = CreateTestEntity("Default", isDefault: true);
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act
        var result = await service.GetDefaultPromptAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Default", result.Name);
    }

    /// <summary>
    /// Verifies GetDefaultPromptAsync returns null when no default.
    /// </summary>
    [Fact]
    public async Task GetDefaultPromptAsync_NoDefault_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemPromptEntity?)null);

        var service = CreateService();

        // Act
        var result = await service.GetDefaultPromptAsync();

        // Assert
        Assert.Null(result);
    }

    #endregion
}
