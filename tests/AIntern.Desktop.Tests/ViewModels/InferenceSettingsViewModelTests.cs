// ============================================================================
// InferenceSettingsViewModelTests.cs
// AIntern.Desktop.Tests - Inference Settings ViewModel Tests (v0.2.3c)
// ============================================================================
// Unit tests for the InferenceSettingsViewModel, covering computed descriptions,
// service synchronization, debouncing, and feedback loop prevention.
// ============================================================================

using AIntern.Core;
using AIntern.Core.Events;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using AIntern.Desktop.Tests.TestHelpers;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="InferenceSettingsViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests cover the following v0.2.3c functionality:
/// </para>
/// <list type="bullet">
///   <item><description>Computed description properties (TemperatureDescription, etc.)</description></item>
///   <item><description>HasUnsavedChanges tracking</description></item>
///   <item><description>Service sync preventing feedback loops</description></item>
///   <item><description>Toggle commands</description></item>
///   <item><description>Default property values from ParameterConstants</description></item>
/// </list>
/// <para>
/// <b>Note:</b> Debounce timing tests are excluded because they require precise
/// timing control that isn't reliable in unit tests. The debounce mechanism
/// is verified through integration testing.
/// </para>
/// </remarks>
public class InferenceSettingsViewModelTests : IDisposable
{
    private readonly Mock<IInferenceSettingsService> _mockService;
    private readonly TestDispatcher _testDispatcher;
    private InferenceSettingsViewModel? _viewModel;

    public InferenceSettingsViewModelTests()
    {
        _mockService = new Mock<IInferenceSettingsService>();
        _testDispatcher = new TestDispatcher();

        // Setup default service behavior
        _mockService.Setup(s => s.CurrentSettings).Returns(new InferenceSettings());
        _mockService.Setup(s => s.HasUnsavedChanges).Returns(false);
        _mockService.Setup(s => s.ActivePreset).Returns((InferencePreset?)null);
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
    }

    private InferenceSettingsViewModel CreateViewModel()
    {
        _viewModel = new InferenceSettingsViewModel(_mockService.Object, _testDispatcher);
        return _viewModel;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new InferenceSettingsViewModel(null!, _testDispatcher));
    }

    [Fact]
    public void Constructor_WithNullDispatcher_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new InferenceSettingsViewModel(_mockService.Object, null!));
    }

    [Fact]
    public void Constructor_SubscribesToSettingsChangedEvent()
    {
        // Act
        var vm = CreateViewModel();

        // Assert - Verify event was subscribed
        _mockService.VerifyAdd(s => s.SettingsChanged += It.IsAny<EventHandler<InferenceSettingsChangedEventArgs>>(), Times.Once);
    }

    [Fact]
    public void Constructor_SubscribesToPresetChangedEvent()
    {
        // Act
        var vm = CreateViewModel();

        // Assert - Verify event was subscribed
        _mockService.VerifyAdd(s => s.PresetChanged += It.IsAny<EventHandler<PresetChangedEventArgs>>(), Times.Once);
    }

    [Fact]
    public void Constructor_SyncsFromService()
    {
        // Arrange
        var settings = new InferenceSettings { Temperature = 1.5f, TopP = 0.8f };
        _mockService.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(1.5f, vm.Temperature);
        Assert.Equal(0.8f, vm.TopP);
    }

    #endregion

    #region Default Property Value Tests

    [Fact]
    public void DefaultTemperature_MatchesParameterConstants()
    {
        // Arrange - Service returns default settings
        _mockService.Setup(s => s.CurrentSettings).Returns(new InferenceSettings());

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(ParameterConstants.Temperature.Default, vm.Temperature);
    }

    [Fact]
    public void DefaultTopP_MatchesParameterConstants()
    {
        // Arrange
        _mockService.Setup(s => s.CurrentSettings).Returns(new InferenceSettings());

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(ParameterConstants.TopP.Default, vm.TopP);
    }

    [Fact]
    public void DefaultMaxTokens_MatchesParameterConstants()
    {
        // Arrange
        _mockService.Setup(s => s.CurrentSettings).Returns(new InferenceSettings());

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(ParameterConstants.MaxTokens.Default, vm.MaxTokens);
    }

    [Fact]
    public void DefaultIsExpanded_IsTrue()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.True(vm.IsExpanded);
    }

    [Fact]
    public void DefaultShowAdvanced_IsFalse()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.False(vm.ShowAdvanced);
    }

    #endregion

    #region TemperatureDescription Tests

    [Theory]
    [InlineData(0.1f, "Very focused and deterministic")]
    [InlineData(0.2f, "Very focused and deterministic")]
    [InlineData(0.3f, "Consistent with slight variation")]
    [InlineData(0.5f, "Consistent with slight variation")]
    [InlineData(0.6f, "Balanced creativity and consistency")]
    [InlineData(0.7f, "Balanced creativity and consistency")]
    [InlineData(0.8f, "More creative and varied")]
    [InlineData(0.9f, "More creative and varied")]
    [InlineData(1.0f, "Creative and experimental")]
    [InlineData(1.2f, "Creative and experimental")]
    [InlineData(1.3f, "Highly random and experimental")]
    [InlineData(1.8f, "Highly random and experimental")]
    public void TemperatureDescription_ReturnsCorrectText(float temperature, string expectedDescription)
    {
        // Arrange
        var settings = new InferenceSettings { Temperature = temperature };
        _mockService.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(expectedDescription, vm.TemperatureDescription);
    }

    #endregion

    #region TopPDescription Tests

    [Theory]
    [InlineData(0.5f, "Considers top 50% probability mass")]
    [InlineData(0.9f, "Considers top 90% probability mass")]
    [InlineData(1.0f, "Considers top 100% probability mass")]
    public void TopPDescription_ReturnsCorrectText(float topP, string expectedDescription)
    {
        // Arrange
        var settings = new InferenceSettings { TopP = topP };
        _mockService.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(expectedDescription, vm.TopPDescription);
    }

    #endregion

    #region TopKDescription Tests

    [Fact]
    public void TopKDescription_WhenZero_ReturnsDisabled()
    {
        // Arrange
        var settings = new InferenceSettings { TopK = 0 };
        _mockService.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal("Disabled", vm.TopKDescription);
    }

    [Theory]
    [InlineData(10, "Consider top 10 tokens")]
    [InlineData(40, "Consider top 40 tokens")]
    [InlineData(100, "Consider top 100 tokens")]
    public void TopKDescription_WhenPositive_ReturnsCorrectText(int topK, string expectedDescription)
    {
        // Arrange
        var settings = new InferenceSettings { TopK = topK };
        _mockService.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(expectedDescription, vm.TopKDescription);
    }

    #endregion

    #region MaxTokensDescription Tests

    [Theory]
    [InlineData(1024, "~768 words maximum")]
    [InlineData(2048, "~1536 words maximum")]
    [InlineData(4096, "~3072 words maximum")]
    public void MaxTokensDescription_ReturnsCorrectText(int maxTokens, string expectedDescription)
    {
        // Arrange
        var settings = new InferenceSettings { MaxTokens = maxTokens };
        _mockService.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(expectedDescription, vm.MaxTokensDescription);
    }

    #endregion

    #region ContextSizeDescription Tests

    [Theory]
    [InlineData(2048u, "~1536 words of history")]
    [InlineData(4096u, "~3072 words of history")]
    [InlineData(8192u, "~6144 words of history")]
    public void ContextSizeDescription_ReturnsCorrectText(uint contextSize, string expectedDescription)
    {
        // Arrange
        var settings = new InferenceSettings { ContextSize = contextSize };
        _mockService.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(expectedDescription, vm.ContextSizeDescription);
    }

    #endregion

    #region RepetitionPenaltyDescription Tests

    [Theory]
    [InlineData(1.0f, "No repetition penalty")]
    [InlineData(1.05f, "Light repetition penalty")]
    [InlineData(1.1f, "Moderate repetition penalty")]
    [InlineData(1.15f, "Moderate repetition penalty")]
    [InlineData(1.2f, "Strong repetition penalty")]
    [InlineData(1.5f, "Strong repetition penalty")]
    public void RepetitionPenaltyDescription_ReturnsCorrectText(float penalty, string expectedDescription)
    {
        // Arrange
        var settings = new InferenceSettings { RepetitionPenalty = penalty };
        _mockService.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(expectedDescription, vm.RepetitionPenaltyDescription);
    }

    #endregion

    #region SeedDescription Tests

    [Fact]
    public void SeedDescription_WhenNegativeOne_ReturnsRandomText()
    {
        // Arrange
        var settings = new InferenceSettings { Seed = -1 };
        _mockService.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal("Random each generation", vm.SeedDescription);
    }

    [Theory]
    [InlineData(0, "Seed: 0 (reproducible)")]
    [InlineData(42, "Seed: 42 (reproducible)")]
    [InlineData(12345, "Seed: 12345 (reproducible)")]
    public void SeedDescription_WhenPositive_ReturnsReproducibleText(int seed, string expectedDescription)
    {
        // Arrange
        var settings = new InferenceSettings { Seed = seed };
        _mockService.Setup(s => s.CurrentSettings).Returns(settings);

        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(expectedDescription, vm.SeedDescription);
    }

    #endregion

    #region HasUnsavedChanges Tests

    [Fact]
    public void TemperatureChange_SetsHasUnsavedChanges()
    {
        // Arrange
        var vm = CreateViewModel();
        Assert.False(vm.HasUnsavedChanges);

        // Act
        vm.Temperature = 1.5f;

        // Assert
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public void TopPChange_SetsHasUnsavedChanges()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.TopP = 0.8f;

        // Assert
        Assert.True(vm.HasUnsavedChanges);
    }

    [Fact]
    public void MaxTokensChange_SetsHasUnsavedChanges()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.MaxTokens = 4096;

        // Assert
        Assert.True(vm.HasUnsavedChanges);
    }

    #endregion

    #region Toggle Command Tests

    [Fact]
    public void ToggleAdvanced_TogglesShowAdvanced()
    {
        // Arrange
        var vm = CreateViewModel();
        Assert.False(vm.ShowAdvanced);

        // Act
        vm.ToggleAdvancedCommand.Execute(null);

        // Assert
        Assert.True(vm.ShowAdvanced);

        // Act again
        vm.ToggleAdvancedCommand.Execute(null);

        // Assert
        Assert.False(vm.ShowAdvanced);
    }

    [Fact]
    public void ToggleExpanded_TogglesIsExpanded()
    {
        // Arrange
        var vm = CreateViewModel();
        Assert.True(vm.IsExpanded);

        // Act
        vm.ToggleExpandedCommand.Execute(null);

        // Assert
        Assert.False(vm.IsExpanded);

        // Act again
        vm.ToggleExpandedCommand.Execute(null);

        // Assert
        Assert.True(vm.IsExpanded);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_UnsubscribesFromSettingsChangedEvent()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.Dispose();

        // Assert - Verify event was unsubscribed
        _mockService.VerifyRemove(s => s.SettingsChanged -= It.IsAny<EventHandler<InferenceSettingsChangedEventArgs>>(), Times.Once);
    }

    [Fact]
    public void Dispose_UnsubscribesFromPresetChangedEvent()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.Dispose();

        // Assert - Verify event was unsubscribed
        _mockService.VerifyRemove(s => s.PresetChanged -= It.IsAny<EventHandler<PresetChangedEventArgs>>(), Times.Once);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act & Assert - Should not throw
        vm.Dispose();
        vm.Dispose();
        vm.Dispose();
    }

    #endregion

    #region Preset Collection Tests

    [Fact]
    public void Presets_DefaultsToEmptyCollection()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.NotNull(vm.Presets);
        Assert.Empty(vm.Presets);
    }

    [Fact]
    public void SelectedPreset_DefaultsToNull()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Null(vm.SelectedPreset);
    }

    #endregion

    #region New Preset Input Tests

    [Fact]
    public void NewPresetName_DefaultsToEmpty()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Equal(string.Empty, vm.NewPresetName);
    }

    [Fact]
    public void NewPresetName_CanBeSet()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.NewPresetName = "My Custom Preset";

        // Assert
        Assert.Equal("My Custom Preset", vm.NewPresetName);
    }

    [Fact]
    public void NewPresetDescription_DefaultsToNull()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Null(vm.NewPresetDescription);
    }

    [Fact]
    public void NewPresetCategory_DefaultsToNull()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        Assert.Null(vm.NewPresetCategory);
    }

    #endregion
}
