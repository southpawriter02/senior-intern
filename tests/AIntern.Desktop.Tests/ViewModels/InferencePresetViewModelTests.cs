// ============================================================================
// InferencePresetViewModelTests.cs
// AIntern.Desktop.Tests - Inference Preset ViewModel Tests (v0.2.3c)
// ============================================================================
// Unit tests for the InferencePresetViewModel, covering constructor mapping,
// property bindings, and computed properties (ParameterSummary, TypeIndicator).
// ============================================================================

using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="InferencePresetViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests cover the following v0.2.3c functionality:
/// </para>
/// <list type="bullet">
///   <item><description>Constructor mapping from InferencePreset domain model</description></item>
///   <item><description>ParameterSummary computed property formatting</description></item>
///   <item><description>TypeIndicator computed property ("Built-in" vs "Custom")</description></item>
///   <item><description>Observable property setters</description></item>
/// </list>
/// </remarks>
public class InferencePresetViewModelTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithPreset_MapsIdCorrectly()
    {
        // Arrange
        var presetId = Guid.NewGuid();
        var preset = CreateTestPreset(id: presetId);

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.Equal(presetId, vm.Id);
    }

    [Fact]
    public void Constructor_WithPreset_MapsNameCorrectly()
    {
        // Arrange
        var preset = CreateTestPreset(name: "Test Preset");

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.Equal("Test Preset", vm.Name);
    }

    [Fact]
    public void Constructor_WithPreset_MapsDescriptionCorrectly()
    {
        // Arrange
        var preset = CreateTestPreset(description: "Test description");

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.Equal("Test description", vm.Description);
    }

    [Fact]
    public void Constructor_WithPreset_MapsNullDescriptionCorrectly()
    {
        // Arrange
        var preset = CreateTestPreset(description: null);

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.Null(vm.Description);
    }

    [Fact]
    public void Constructor_WithPreset_MapsCategoryCorrectly()
    {
        // Arrange
        var preset = CreateTestPreset(category: "Code");

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.Equal("Code", vm.Category);
    }

    [Fact]
    public void Constructor_WithPreset_MapsIsBuiltInCorrectly()
    {
        // Arrange
        var preset = CreateTestPreset(isBuiltIn: true);

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.True(vm.IsBuiltIn);
    }

    [Fact]
    public void Constructor_WithPreset_MapsIsDefaultCorrectly()
    {
        // Arrange
        var preset = CreateTestPreset(isDefault: true);

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.True(vm.IsDefault);
    }

    [Fact]
    public void Constructor_WithNullPreset_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InferencePresetViewModel(null!));
    }

    #endregion

    #region ParameterSummary Tests

    [Fact]
    public void ParameterSummary_ContainsTemperature()
    {
        // Arrange
        var preset = CreateTestPreset(temperature: 1.5f);

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.Contains("Temp: 1.5", vm.ParameterSummary);
    }

    [Fact]
    public void ParameterSummary_ContainsTopP()
    {
        // Arrange
        var preset = CreateTestPreset(topP: 0.95f);

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.Contains("TopP: 0.95", vm.ParameterSummary);
    }

    [Fact]
    public void ParameterSummary_ContainsMaxTokens()
    {
        // Arrange
        var preset = CreateTestPreset(maxTokens: 4096);

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.Contains("Max: 4096", vm.ParameterSummary);
    }

    [Fact]
    public void ParameterSummary_FormatsCorrectly()
    {
        // Arrange
        var preset = CreateTestPreset(
            temperature: 0.7f,
            topP: 0.90f,
            maxTokens: 2048);

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.Equal("Temp: 0.7, TopP: 0.90, Max: 2048", vm.ParameterSummary);
    }

    #endregion

    #region TypeIndicator Tests

    [Fact]
    public void TypeIndicator_WhenIsBuiltInTrue_ReturnsBuiltIn()
    {
        // Arrange
        var preset = CreateTestPreset(isBuiltIn: true);

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.Equal("Built-in", vm.TypeIndicator);
    }

    [Fact]
    public void TypeIndicator_WhenIsBuiltInFalse_ReturnsCustom()
    {
        // Arrange
        var preset = CreateTestPreset(isBuiltIn: false);

        // Act
        var vm = new InferencePresetViewModel(preset);

        // Assert
        Assert.Equal("Custom", vm.TypeIndicator);
    }

    [Fact]
    public void TypeIndicator_UpdatesWhenIsBuiltInChanges()
    {
        // Arrange
        var preset = CreateTestPreset(isBuiltIn: false);
        var vm = new InferencePresetViewModel(preset);

        // Act
        vm.IsBuiltIn = true;

        // Assert
        Assert.Equal("Built-in", vm.TypeIndicator);
    }

    #endregion

    #region Default Constructor Tests

    [Fact]
    public void DefaultConstructor_SetsEmptyName()
    {
        // Act
        var vm = new InferencePresetViewModel();

        // Assert
        Assert.Equal(string.Empty, vm.Name);
    }

    [Fact]
    public void DefaultConstructor_SetsDefaultId()
    {
        // Act
        var vm = new InferencePresetViewModel();

        // Assert - Id should be default Guid (not set via init)
        Assert.Equal(Guid.Empty, vm.Id);
    }

    [Fact]
    public void DefaultConstructor_SetsEmptyParameterSummary()
    {
        // Act
        var vm = new InferencePresetViewModel();

        // Assert
        Assert.Equal(string.Empty, vm.ParameterSummary);
    }

    #endregion

    #region Observable Property Tests

    [Fact]
    public void Name_CanBeSet()
    {
        // Arrange
        var vm = new InferencePresetViewModel();

        // Act
        vm.Name = "Updated Name";

        // Assert
        Assert.Equal("Updated Name", vm.Name);
    }

    [Fact]
    public void IsSelected_CanBeSet()
    {
        // Arrange
        var vm = new InferencePresetViewModel();

        // Act
        vm.IsSelected = true;

        // Assert
        Assert.True(vm.IsSelected);
    }

    [Fact]
    public void IsSelected_DefaultIsFalse()
    {
        // Act
        var vm = new InferencePresetViewModel();

        // Assert
        Assert.False(vm.IsSelected);
    }

    #endregion

    #region Helper Methods

    private static InferencePreset CreateTestPreset(
        Guid? id = null,
        string name = "Test Preset",
        string? description = null,
        string? category = null,
        bool isBuiltIn = false,
        bool isDefault = false,
        float temperature = 0.7f,
        float topP = 0.9f,
        int maxTokens = 2048)
    {
        return new InferencePreset
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Description = description,
            Category = category,
            IsBuiltIn = isBuiltIn,
            IsDefault = isDefault,
            Options = new InferenceSettings
            {
                Temperature = temperature,
                TopP = topP,
                MaxTokens = maxTokens
            }
        };
    }

    #endregion
}
