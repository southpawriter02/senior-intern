// ============================================================================
// InferenceSettingsServiceTests.cs
// AIntern.Services.Tests - Inference Settings Service Tests (v0.2.3b)
// ============================================================================
// Unit tests for the InferenceSettingsService, covering parameter updates with
// clamping and change detection, preset operations, event firing, HasUnsavedChanges
// logic, and initialization behavior.
// ============================================================================

using AIntern.Core;
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
/// Unit tests for <see cref="InferenceSettingsService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests cover the following v0.2.3b functionality:
/// </para>
/// <list type="bullet">
///   <item><description>Constructor null argument validation</description></item>
///   <item><description>Parameter updates with clamping to valid ranges</description></item>
///   <item><description>No event firing when value unchanged</description></item>
///   <item><description>Preset operations (apply, save, update, delete)</description></item>
///   <item><description>HasUnsavedChanges detection</description></item>
///   <item><description>Event firing with correct types and parameters</description></item>
///   <item><description>InitializeAsync loading from settings</description></item>
/// </list>
/// <para>Added in v0.2.5a (test coverage for v0.2.3b).</para>
/// </remarks>
public class InferenceSettingsServiceTests : IAsyncDisposable
{
    #region Test Infrastructure

    private readonly Mock<IInferencePresetRepository> _mockRepository;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<InferenceSettingsService>> _mockLogger;
    private readonly AppSettings _appSettings;

    private InferenceSettingsService? _service;

    public InferenceSettingsServiceTests()
    {
        _mockRepository = new Mock<IInferencePresetRepository>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<InferenceSettingsService>>();
        _appSettings = new AppSettings();

        // Default setup
        _mockSettingsService.Setup(s => s.CurrentSettings).Returns(_appSettings);
        _mockSettingsService.Setup(s => s.LoadSettingsAsync()).ReturnsAsync(_appSettings);
        _mockSettingsService.Setup(s => s.SaveSettingsAsync(It.IsAny<AppSettings>())).Returns(Task.CompletedTask);
    }

    private InferenceSettingsService CreateService()
    {
        _service = new InferenceSettingsService(
            _mockRepository.Object,
            _mockSettingsService.Object,
            _mockLogger.Object);
        return _service;
    }

    private static InferencePresetEntity CreateTestEntity(
        string name = "Test Preset",
        float temperature = 0.7f,
        float topP = 0.9f,
        int maxTokens = 2048,
        bool isBuiltIn = false,
        bool isDefault = false)
    {
        return new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test description",
            Category = "General",
            Temperature = temperature,
            TopP = topP,
            TopK = 40,
            RepeatPenalty = 1.1f,
            MaxTokens = maxTokens,
            ContextSize = 4096,
            Seed = -1,
            IsBuiltIn = isBuiltIn,
            IsDefault = isDefault,
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new InferenceSettingsService(null!, _mockSettingsService.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_NullSettingsService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new InferenceSettingsService(_mockRepository.Object, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new InferenceSettingsService(_mockRepository.Object, _mockSettingsService.Object, null!));
    }

    [Fact]
    public void Constructor_CurrentSettingsHasDefaultValues()
    {
        // Act
        var service = CreateService();

        // Assert
        Assert.Equal(ParameterConstants.Temperature.Default, service.CurrentSettings.Temperature);
        Assert.Equal(ParameterConstants.TopP.Default, service.CurrentSettings.TopP);
        Assert.Equal(ParameterConstants.MaxTokens.Default, service.CurrentSettings.MaxTokens);
    }

    [Fact]
    public void Constructor_ActivePresetIsNull()
    {
        // Act
        var service = CreateService();

        // Assert
        Assert.Null(service.ActivePreset);
    }

    [Fact]
    public void Constructor_HasUnsavedChangesIsFalse()
    {
        // Act
        var service = CreateService();

        // Assert
        Assert.False(service.HasUnsavedChanges);
    }

    #endregion

    #region UpdateTemperature Tests

    [Fact]
    public void UpdateTemperature_WithinRange_UpdatesValue()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateTemperature(1.5f);

        // Assert
        Assert.Equal(1.5f, service.CurrentSettings.Temperature);
    }

    [Fact]
    public void UpdateTemperature_AboveMax_ClampsToMax()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateTemperature(5.0f);

        // Assert
        Assert.Equal(ParameterConstants.Temperature.Max, service.CurrentSettings.Temperature);
    }

    [Fact]
    public void UpdateTemperature_BelowMin_ClampsToMin()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateTemperature(-1.0f);

        // Assert
        Assert.Equal(ParameterConstants.Temperature.Min, service.CurrentSettings.Temperature);
    }

    [Fact]
    public void UpdateTemperature_FiresSettingsChangedEvent()
    {
        // Arrange
        var service = CreateService();
        InferenceSettingsChangedEventArgs? eventArgs = null;
        service.SettingsChanged += (_, e) => eventArgs = e;

        // Act
        service.UpdateTemperature(1.2f);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(InferenceSettingsChangeType.ParameterChanged, eventArgs.ChangeType);
        Assert.Equal("Temperature", eventArgs.ChangedParameter);
        Assert.Equal(1.2f, eventArgs.NewSettings.Temperature);
    }

    [Fact]
    public void UpdateTemperature_SameValue_DoesNotFireEvent()
    {
        // Arrange
        var service = CreateService();
        service.UpdateTemperature(1.0f);

        var eventFired = false;
        service.SettingsChanged += (_, _) => eventFired = true;

        // Act - same value as current
        service.UpdateTemperature(1.0f);

        // Assert
        Assert.False(eventFired);
    }

    [Fact]
    public void UpdateTemperature_ValueWithinEpsilon_DoesNotFireEvent()
    {
        // Arrange
        var service = CreateService();
        service.UpdateTemperature(1.0f);

        var eventFired = false;
        service.SettingsChanged += (_, _) => eventFired = true;

        // Act - value within epsilon (0.001)
        service.UpdateTemperature(1.0005f);

        // Assert
        Assert.False(eventFired);
    }

    #endregion

    #region UpdateTopP Tests

    [Fact]
    public void UpdateTopP_WithinRange_UpdatesValue()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateTopP(0.8f);

        // Assert
        Assert.Equal(0.8f, service.CurrentSettings.TopP);
    }

    [Fact]
    public void UpdateTopP_AboveMax_ClampsToMax()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateTopP(1.5f);

        // Assert
        Assert.Equal(ParameterConstants.TopP.Max, service.CurrentSettings.TopP);
    }

    [Fact]
    public void UpdateTopP_FiresSettingsChangedEvent()
    {
        // Arrange
        var service = CreateService();
        InferenceSettingsChangedEventArgs? eventArgs = null;
        service.SettingsChanged += (_, e) => eventArgs = e;

        // Act
        service.UpdateTopP(0.95f);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("TopP", eventArgs.ChangedParameter);
    }

    #endregion

    #region UpdateTopK Tests

    [Fact]
    public void UpdateTopK_WithinRange_UpdatesValue()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateTopK(50);

        // Assert
        Assert.Equal(50, service.CurrentSettings.TopK);
    }

    [Fact]
    public void UpdateTopK_AboveMax_ClampsToMax()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateTopK(200);

        // Assert
        Assert.Equal(ParameterConstants.TopK.Max, service.CurrentSettings.TopK);
    }

    [Fact]
    public void UpdateTopK_SameValue_DoesNotFireEvent()
    {
        // Arrange
        var service = CreateService();
        service.UpdateTopK(40);

        var eventFired = false;
        service.SettingsChanged += (_, _) => eventFired = true;

        // Act
        service.UpdateTopK(40);

        // Assert
        Assert.False(eventFired);
    }

    #endregion

    #region UpdateMaxTokens Tests

    [Fact]
    public void UpdateMaxTokens_WithinRange_UpdatesValue()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateMaxTokens(4096);

        // Assert
        Assert.Equal(4096, service.CurrentSettings.MaxTokens);
    }

    [Fact]
    public void UpdateMaxTokens_AboveMax_ClampsToMax()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateMaxTokens(100000);

        // Assert
        Assert.Equal(ParameterConstants.MaxTokens.Max, service.CurrentSettings.MaxTokens);
    }

    [Fact]
    public void UpdateMaxTokens_BelowMin_ClampsToMin()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateMaxTokens(10);

        // Assert
        Assert.Equal(ParameterConstants.MaxTokens.Min, service.CurrentSettings.MaxTokens);
    }

    #endregion

    #region UpdateContextSize Tests

    [Fact]
    public void UpdateContextSize_WithinRange_UpdatesValue()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateContextSize(8192);

        // Assert
        Assert.Equal(8192u, service.CurrentSettings.ContextSize);
    }

    [Fact]
    public void UpdateContextSize_FiresSettingsChangedEvent()
    {
        // Arrange
        var service = CreateService();
        InferenceSettingsChangedEventArgs? eventArgs = null;
        service.SettingsChanged += (_, e) => eventArgs = e;

        // Act
        service.UpdateContextSize(16384);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("ContextSize", eventArgs.ChangedParameter);
    }

    #endregion

    #region UpdateRepetitionPenalty Tests

    [Fact]
    public void UpdateRepetitionPenalty_WithinRange_UpdatesValue()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateRepetitionPenalty(1.15f);

        // Assert
        Assert.Equal(1.15f, service.CurrentSettings.RepetitionPenalty);
    }

    [Fact]
    public void UpdateRepetitionPenalty_AboveMax_ClampsToMax()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateRepetitionPenalty(5.0f);

        // Assert
        Assert.Equal(ParameterConstants.RepetitionPenalty.Max, service.CurrentSettings.RepetitionPenalty);
    }

    #endregion

    #region UpdateSeed Tests

    [Fact]
    public void UpdateSeed_ValidValue_UpdatesValue()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateSeed(42);

        // Assert
        Assert.Equal(42, service.CurrentSettings.Seed);
    }

    [Fact]
    public void UpdateSeed_NegativeOne_IsValid()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateSeed(-1);

        // Assert
        Assert.Equal(-1, service.CurrentSettings.Seed);
    }

    [Fact]
    public void UpdateSeed_BelowNegativeOne_ClampsToNegativeOne()
    {
        // Arrange
        var service = CreateService();

        // Act
        service.UpdateSeed(-5);

        // Assert
        Assert.Equal(-1, service.CurrentSettings.Seed);
    }

    #endregion

    #region UpdateAll Tests

    [Fact]
    public void UpdateAll_NullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.UpdateAll(null!));
    }

    [Fact]
    public void UpdateAll_ValidSettings_UpdatesAllValues()
    {
        // Arrange
        var service = CreateService();
        var newSettings = new InferenceSettings
        {
            Temperature = 1.2f,
            TopP = 0.8f,
            TopK = 50,
            RepetitionPenalty = 1.15f,
            MaxTokens = 4096,
            ContextSize = 8192,
            Seed = 42
        };

        // Act
        service.UpdateAll(newSettings);

        // Assert
        Assert.Equal(1.2f, service.CurrentSettings.Temperature);
        Assert.Equal(0.8f, service.CurrentSettings.TopP);
        Assert.Equal(50, service.CurrentSettings.TopK);
        Assert.Equal(1.15f, service.CurrentSettings.RepetitionPenalty);
        Assert.Equal(4096, service.CurrentSettings.MaxTokens);
        Assert.Equal(8192u, service.CurrentSettings.ContextSize);
        Assert.Equal(42, service.CurrentSettings.Seed);
    }

    [Fact]
    public void UpdateAll_FiresSettingsChangedWithAllChangedType()
    {
        // Arrange
        var service = CreateService();
        InferenceSettingsChangedEventArgs? eventArgs = null;
        service.SettingsChanged += (_, e) => eventArgs = e;

        var newSettings = new InferenceSettings { Temperature = 1.5f };

        // Act
        service.UpdateAll(newSettings);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(InferenceSettingsChangeType.AllChanged, eventArgs.ChangeType);
        Assert.Null(eventArgs.ChangedParameter);
    }

    [Fact]
    public void UpdateAll_NoChanges_DoesNotFireEvent()
    {
        // Arrange
        var service = CreateService();
        // First set to specific values
        service.UpdateAll(new InferenceSettings
        {
            Temperature = 0.7f,
            TopP = 0.9f,
            TopK = 40,
            RepetitionPenalty = 1.1f,
            MaxTokens = 2048,
            ContextSize = 4096,
            Seed = -1
        });

        var eventFired = false;
        service.SettingsChanged += (_, _) => eventFired = true;

        // Act - same values
        service.UpdateAll(new InferenceSettings
        {
            Temperature = 0.7f,
            TopP = 0.9f,
            TopK = 40,
            RepetitionPenalty = 1.1f,
            MaxTokens = 2048,
            ContextSize = 4096,
            Seed = -1
        });

        // Assert
        Assert.False(eventFired);
    }

    [Fact]
    public void UpdateAll_ClampsOutOfRangeValues()
    {
        // Arrange
        var service = CreateService();
        var invalidSettings = new InferenceSettings
        {
            Temperature = 10.0f,  // Should clamp to max
            TopP = 2.0f,          // Should clamp to max
            MaxTokens = 100000    // Should clamp to max
        };

        // Act
        service.UpdateAll(invalidSettings);

        // Assert
        Assert.Equal(ParameterConstants.Temperature.Max, service.CurrentSettings.Temperature);
        Assert.Equal(ParameterConstants.TopP.Max, service.CurrentSettings.TopP);
        Assert.Equal(ParameterConstants.MaxTokens.Max, service.CurrentSettings.MaxTokens);
    }

    #endregion

    #region InitializeAsync Tests

    [Fact]
    public async Task InitializeAsync_WithSavedPresetId_LoadsPreset()
    {
        // Arrange
        var savedEntity = CreateTestEntity("Saved Preset", temperature: 1.2f);
        _appSettings.ActivePresetId = savedEntity.Id;
        _mockRepository.Setup(r => r.GetByIdAsync(savedEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);

        var service = CreateService();

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.NotNull(service.ActivePreset);
        Assert.Equal("Saved Preset", service.ActivePreset.Name);
        Assert.Equal(1.2f, service.CurrentSettings.Temperature);
    }

    [Fact]
    public async Task InitializeAsync_NoSavedPreset_FallsBackToDefault()
    {
        // Arrange
        var defaultEntity = CreateTestEntity("Balanced", temperature: 0.7f, isDefault: true);
        _appSettings.ActivePresetId = null;
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultEntity);

        var service = CreateService();

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.NotNull(service.ActivePreset);
        Assert.Equal("Balanced", service.ActivePreset.Name);
    }

    [Fact]
    public async Task InitializeAsync_SavedPresetNotFound_FallsBackToDefault()
    {
        // Arrange
        var defaultEntity = CreateTestEntity("Balanced", isDefault: true);
        _appSettings.ActivePresetId = Guid.NewGuid(); // Non-existent ID
        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity?)null);
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultEntity);

        var service = CreateService();

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.NotNull(service.ActivePreset);
        Assert.Equal("Balanced", service.ActivePreset.Name);
    }

    [Fact]
    public async Task InitializeAsync_CalledMultipleTimes_OnlyInitializesOnce()
    {
        // Arrange
        var defaultEntity = CreateTestEntity("Balanced", isDefault: true);
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

    [Fact]
    public async Task InitializeAsync_NoPresets_UsesDefaultSettings()
    {
        // Arrange
        _appSettings.ActivePresetId = null;
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity?)null);
        _mockRepository.Setup(r => r.GetByIdAsync(InferencePreset.BalancedPresetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity?)null);

        var service = CreateService();

        // Act
        await service.InitializeAsync();

        // Assert - should use default InferenceSettings values
        Assert.Null(service.ActivePreset);
        Assert.Equal(ParameterConstants.Temperature.Default, service.CurrentSettings.Temperature);
    }

    #endregion

    #region HasUnsavedChanges Tests

    [Fact]
    public async Task HasUnsavedChanges_AfterPresetApplied_IsFalse()
    {
        // Arrange
        var entity = CreateTestEntity("Test");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        await service.ApplyPresetAsync(entity.Id);

        // Assert
        Assert.False(service.HasUnsavedChanges);
    }

    [Fact]
    public async Task HasUnsavedChanges_AfterModifyingParameter_IsTrue()
    {
        // Arrange
        var entity = CreateTestEntity("Test", temperature: 0.7f);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        await service.ApplyPresetAsync(entity.Id);

        // Act
        service.UpdateTemperature(1.5f);

        // Assert
        Assert.True(service.HasUnsavedChanges);
    }

    [Fact]
    public void HasUnsavedChanges_NoActivePreset_IsFalse()
    {
        // Arrange
        var service = CreateService();
        service.UpdateTemperature(1.5f); // Modify without preset

        // Assert - no preset means no "unsaved changes" concept
        Assert.False(service.HasUnsavedChanges);
    }

    [Fact]
    public async Task HasUnsavedChanges_RevertToPresetValues_IsFalse()
    {
        // Arrange
        var entity = CreateTestEntity("Test", temperature: 0.7f);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        await service.ApplyPresetAsync(entity.Id);

        // Modify
        service.UpdateTemperature(1.5f);
        Assert.True(service.HasUnsavedChanges);

        // Revert
        service.UpdateTemperature(0.7f);

        // Assert
        Assert.False(service.HasUnsavedChanges);
    }

    #endregion

    #region ApplyPresetAsync Tests

    [Fact]
    public async Task ApplyPresetAsync_ValidPreset_UpdatesCurrentSettings()
    {
        // Arrange
        var entity = CreateTestEntity("Precise", temperature: 0.2f, topP: 0.85f, maxTokens: 1024);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act
        await service.ApplyPresetAsync(entity.Id);

        // Assert
        Assert.Equal(0.2f, service.CurrentSettings.Temperature);
        Assert.Equal(0.85f, service.CurrentSettings.TopP);
        Assert.Equal(1024, service.CurrentSettings.MaxTokens);
    }

    [Fact]
    public async Task ApplyPresetAsync_ValidPreset_SetsActivePreset()
    {
        // Arrange
        var entity = CreateTestEntity("Creative");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act
        await service.ApplyPresetAsync(entity.Id);

        // Assert
        Assert.NotNull(service.ActivePreset);
        Assert.Equal("Creative", service.ActivePreset.Name);
    }

    [Fact]
    public async Task ApplyPresetAsync_FiresSettingsChangedEvent()
    {
        // Arrange
        var entity = CreateTestEntity("Test");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        InferenceSettingsChangedEventArgs? eventArgs = null;
        service.SettingsChanged += (_, e) => eventArgs = e;

        // Act
        await service.ApplyPresetAsync(entity.Id);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(InferenceSettingsChangeType.PresetApplied, eventArgs.ChangeType);
    }

    [Fact]
    public async Task ApplyPresetAsync_FiresPresetChangedEvent()
    {
        // Arrange
        var entity = CreateTestEntity("Test");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        PresetChangedEventArgs? eventArgs = null;
        service.PresetChanged += (_, e) => eventArgs = e;

        // Act
        await service.ApplyPresetAsync(entity.Id);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(PresetChangeType.Applied, eventArgs.ChangeType);
        Assert.Equal("Test", eventArgs.NewPreset?.Name);
    }

    [Fact]
    public async Task ApplyPresetAsync_PersistsToSettings()
    {
        // Arrange
        var entity = CreateTestEntity("Test");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act
        await service.ApplyPresetAsync(entity.Id);

        // Assert
        _mockSettingsService.Verify(
            s => s.SaveSettingsAsync(It.Is<AppSettings>(a => a.ActivePresetId == entity.Id)),
            Times.Once);
    }

    [Fact]
    public async Task ApplyPresetAsync_IncrementsUsageCount()
    {
        // Arrange
        var entity = CreateTestEntity("Test");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act
        await service.ApplyPresetAsync(entity.Id);

        // Assert
        _mockRepository.Verify(r => r.IncrementUsageAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyPresetAsync_PresetNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ApplyPresetAsync(unknownId));
    }

    #endregion

    #region SaveAsPresetAsync Tests

    [Fact]
    public async Task SaveAsPresetAsync_ValidName_CreatesPreset()
    {
        // Arrange
        _mockRepository.Setup(r => r.NameExistsAsync("My Preset", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<InferencePresetEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity e, CancellationToken _) => e);

        var service = CreateService();
        service.UpdateTemperature(1.5f);

        // Act
        var result = await service.SaveAsPresetAsync("My Preset", "Description", "Code");

        // Assert
        Assert.Equal("My Preset", result.Name);
        Assert.Equal("Description", result.Description);
        Assert.Equal("Code", result.Category);
        Assert.Equal(1.5f, result.Options.Temperature);
        Assert.False(result.IsBuiltIn);
    }

    [Fact]
    public async Task SaveAsPresetAsync_FiresPresetChangedEvent()
    {
        // Arrange
        _mockRepository.Setup(r => r.NameExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<InferencePresetEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity e, CancellationToken _) => e);

        var service = CreateService();
        PresetChangedEventArgs? eventArgs = null;
        service.PresetChanged += (_, e) => eventArgs = e;

        // Act
        await service.SaveAsPresetAsync("New Preset");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(PresetChangeType.Created, eventArgs.ChangeType);
        Assert.Equal("New Preset", eventArgs.NewPreset?.Name);
        Assert.Null(eventArgs.PreviousPreset);
    }

    [Fact]
    public async Task SaveAsPresetAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockRepository.Setup(r => r.NameExistsAsync("Existing", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsPresetAsync("Existing"));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task SaveAsPresetAsync_NullName_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            service.SaveAsPresetAsync(null!));
    }

    [Fact]
    public async Task SaveAsPresetAsync_WhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveAsPresetAsync("   "));
    }

    #endregion

    #region UpdatePresetAsync Tests

    [Fact]
    public async Task UpdatePresetAsync_ValidPreset_UpdatesWithCurrentSettings()
    {
        // Arrange
        var entity = CreateTestEntity("Custom Preset", temperature: 0.7f, isBuiltIn: false);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        service.UpdateTemperature(1.8f);

        // Act
        await service.UpdatePresetAsync(entity.Id);

        // Assert - verify update was called with new temperature
        _mockRepository.Verify(
            r => r.UpdateAsync(It.Is<InferencePresetEntity>(e => e.Temperature == 1.8f), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePresetAsync_FiresPresetChangedEvent()
    {
        // Arrange
        var entity = CreateTestEntity("Custom", isBuiltIn: false);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        PresetChangedEventArgs? eventArgs = null;
        service.PresetChanged += (_, e) => eventArgs = e;

        // Act
        await service.UpdatePresetAsync(entity.Id);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(PresetChangeType.Updated, eventArgs.ChangeType);
    }

    [Fact]
    public async Task UpdatePresetAsync_BuiltInPreset_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = CreateTestEntity("Balanced", isBuiltIn: true);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdatePresetAsync(entity.Id));
        Assert.Contains("built-in", ex.Message);
    }

    [Fact]
    public async Task UpdatePresetAsync_PresetNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdatePresetAsync(unknownId));
    }

    [Fact]
    public async Task UpdatePresetAsync_ActivePreset_RefreshesSnapshot()
    {
        // Arrange
        var entity = CreateTestEntity("Custom", temperature: 0.7f, isBuiltIn: false);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        await service.ApplyPresetAsync(entity.Id);
        service.UpdateTemperature(1.5f);
        Assert.True(service.HasUnsavedChanges);

        // Act - update the preset with current settings
        await service.UpdatePresetAsync(entity.Id);

        // Assert - no longer has unsaved changes
        Assert.False(service.HasUnsavedChanges);
    }

    #endregion

    #region DeletePresetAsync Tests

    [Fact]
    public async Task DeletePresetAsync_ValidPreset_DeletesPreset()
    {
        // Arrange
        var entity = CreateTestEntity("Custom", isBuiltIn: false);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act
        await service.DeletePresetAsync(entity.Id);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePresetAsync_FiresPresetChangedEvent()
    {
        // Arrange
        var entity = CreateTestEntity("Custom", isBuiltIn: false);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        PresetChangedEventArgs? eventArgs = null;
        service.PresetChanged += (_, e) => eventArgs = e;

        // Act
        await service.DeletePresetAsync(entity.Id);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(PresetChangeType.Deleted, eventArgs.ChangeType);
        Assert.Null(eventArgs.NewPreset);
        Assert.Equal("Custom", eventArgs.PreviousPreset?.Name);
    }

    [Fact]
    public async Task DeletePresetAsync_BuiltInPreset_ThrowsInvalidOperationException()
    {
        // Arrange
        var entity = CreateTestEntity("Balanced", isBuiltIn: true);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeletePresetAsync(entity.Id));
        Assert.Contains("built-in", ex.Message);
    }

    [Fact]
    public async Task DeletePresetAsync_ActivePreset_ClearsActivePreset()
    {
        // Arrange
        var entity = CreateTestEntity("Custom", isBuiltIn: false);
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var service = CreateService();
        await service.ApplyPresetAsync(entity.Id);
        Assert.NotNull(service.ActivePreset);

        // Act
        await service.DeletePresetAsync(entity.Id);

        // Assert
        Assert.Null(service.ActivePreset);
    }

    [Fact]
    public async Task DeletePresetAsync_PresetNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeletePresetAsync(unknownId));
    }

    #endregion

    #region ResetToDefaultsAsync Tests

    [Fact]
    public async Task ResetToDefaultsAsync_AppliesDefaultPreset()
    {
        // Arrange
        var defaultEntity = CreateTestEntity("Balanced", temperature: 0.7f, isDefault: true);
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultEntity);
        _mockRepository.Setup(r => r.GetByIdAsync(defaultEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultEntity);

        var service = CreateService();
        service.UpdateTemperature(1.5f);

        // Act
        await service.ResetToDefaultsAsync();

        // Assert
        Assert.Equal(0.7f, service.CurrentSettings.Temperature);
        Assert.Equal("Balanced", service.ActivePreset?.Name);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_FiresSettingsChangedWithResetType()
    {
        // Arrange
        var defaultEntity = CreateTestEntity("Balanced", isDefault: true);
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultEntity);
        _mockRepository.Setup(r => r.GetByIdAsync(defaultEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultEntity);

        var service = CreateService();
        InferenceSettingsChangedEventArgs? eventArgs = null;
        service.SettingsChanged += (_, e) => eventArgs = e;

        // Act
        await service.ResetToDefaultsAsync();

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(InferenceSettingsChangeType.ResetToDefaults, eventArgs.ChangeType);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_NoDefaultPreset_FallsBackToBalanced()
    {
        // Arrange
        var balancedEntity = CreateTestEntity("Balanced", isDefault: true);
        balancedEntity.Id = InferencePreset.BalancedPresetId;

        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity?)null);
        _mockRepository.Setup(r => r.GetByIdAsync(InferencePreset.BalancedPresetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(balancedEntity);

        var service = CreateService();

        // Act
        await service.ResetToDefaultsAsync();

        // Assert
        Assert.Equal("Balanced", service.ActivePreset?.Name);
    }

    #endregion

    #region SetDefaultPresetAsync Tests

    [Fact]
    public async Task SetDefaultPresetAsync_ValidPreset_SetsAsDefault()
    {
        // Arrange
        var entity = CreateTestEntity("Custom");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity?)null);

        var service = CreateService();

        // Act
        await service.SetDefaultPresetAsync(entity.Id);

        // Assert
        _mockRepository.Verify(r => r.SetAsDefaultAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetDefaultPresetAsync_FiresPresetChangedEvent()
    {
        // Arrange
        var entity = CreateTestEntity("Custom");
        _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mockRepository.Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity?)null);

        var service = CreateService();
        PresetChangedEventArgs? eventArgs = null;
        service.PresetChanged += (_, e) => eventArgs = e;

        // Act
        await service.SetDefaultPresetAsync(entity.Id);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(PresetChangeType.DefaultChanged, eventArgs.ChangeType);
        Assert.Equal("Custom", eventArgs.NewPreset?.Name);
    }

    [Fact]
    public async Task SetDefaultPresetAsync_PresetNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InferencePresetEntity?)null);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetDefaultPresetAsync(unknownId));
    }

    #endregion

    #region GetPresetsAsync Tests

    [Fact]
    public async Task GetPresetsAsync_ReturnsAllPresets()
    {
        // Arrange
        var entities = new List<InferencePresetEntity>
        {
            CreateTestEntity("Balanced", isBuiltIn: true),
            CreateTestEntity("Precise", isBuiltIn: true),
            CreateTestEntity("Custom")
        };
        _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var service = CreateService();

        // Act
        var result = await service.GetPresetsAsync();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetPresetsAsync_MapsEntitiesToModels()
    {
        // Arrange
        var entity = CreateTestEntity("Test Preset", temperature: 1.5f);
        _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InferencePresetEntity> { entity });

        var service = CreateService();

        // Act
        var result = await service.GetPresetsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Test Preset", result[0].Name);
        Assert.Equal(1.5f, result[0].Options.Temperature);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - should not throw
        await service.DisposeAsync();
        await service.DisposeAsync();
        await service.DisposeAsync();
    }

    #endregion
}
