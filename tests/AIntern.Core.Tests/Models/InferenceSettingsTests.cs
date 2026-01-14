using Xunit;
using AIntern.Core;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="InferenceSettings"/> class.
/// Verifies default values, validation, and cloning functionality.
/// </summary>
/// <remarks>
/// <para>
/// These tests ensure that InferenceSettings correctly:
/// </para>
/// <list type="bullet">
///   <item><description>Initializes with default values from ParameterConstants</description></item>
///   <item><description>Validates all parameter ranges correctly</description></item>
///   <item><description>Creates independent clones</description></item>
/// </list>
/// </remarks>
public class InferenceSettingsTests
{
    #region Default Value Tests

    /// <summary>
    /// Verifies that Temperature defaults to the value specified in ParameterConstants.
    /// </summary>
    [Fact]
    public void Constructor_Temperature_DefaultsToParameterConstant()
    {
        // Arrange & Act
        var settings = new InferenceSettings();

        // Assert
        Assert.Equal(ParameterConstants.Temperature.Default, settings.Temperature);
    }

    /// <summary>
    /// Verifies that TopP defaults to the value specified in ParameterConstants.
    /// </summary>
    [Fact]
    public void Constructor_TopP_DefaultsToParameterConstant()
    {
        // Arrange & Act
        var settings = new InferenceSettings();

        // Assert
        Assert.Equal(ParameterConstants.TopP.Default, settings.TopP);
    }

    /// <summary>
    /// Verifies that TopK defaults to the value specified in ParameterConstants.
    /// </summary>
    [Fact]
    public void Constructor_TopK_DefaultsToParameterConstant()
    {
        // Arrange & Act
        var settings = new InferenceSettings();

        // Assert
        Assert.Equal(ParameterConstants.TopK.Default, settings.TopK);
    }

    /// <summary>
    /// Verifies that RepetitionPenalty defaults to the value specified in ParameterConstants.
    /// </summary>
    [Fact]
    public void Constructor_RepetitionPenalty_DefaultsToParameterConstant()
    {
        // Arrange & Act
        var settings = new InferenceSettings();

        // Assert
        Assert.Equal(ParameterConstants.RepetitionPenalty.Default, settings.RepetitionPenalty);
    }

    /// <summary>
    /// Verifies that MaxTokens defaults to the value specified in ParameterConstants.
    /// </summary>
    [Fact]
    public void Constructor_MaxTokens_DefaultsToParameterConstant()
    {
        // Arrange & Act
        var settings = new InferenceSettings();

        // Assert
        Assert.Equal(ParameterConstants.MaxTokens.Default, settings.MaxTokens);
    }

    /// <summary>
    /// Verifies that ContextSize defaults to the value specified in ParameterConstants.
    /// </summary>
    [Fact]
    public void Constructor_ContextSize_DefaultsToParameterConstant()
    {
        // Arrange & Act
        var settings = new InferenceSettings();

        // Assert
        Assert.Equal(ParameterConstants.ContextSize.Default, settings.ContextSize);
    }

    /// <summary>
    /// Verifies that Seed defaults to the value specified in ParameterConstants.
    /// </summary>
    [Fact]
    public void Constructor_Seed_DefaultsToParameterConstant()
    {
        // Arrange & Act
        var settings = new InferenceSettings();

        // Assert
        Assert.Equal(ParameterConstants.Seed.Default, settings.Seed);
    }

    #endregion

    #region Clone Tests

    /// <summary>
    /// Verifies that Clone creates an independent copy of the settings.
    /// </summary>
    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new InferenceSettings
        {
            Temperature = 1.5f,
            TopP = 0.8f,
            TopK = 50,
            RepetitionPenalty = 1.2f,
            MaxTokens = 4096,
            ContextSize = 8192,
            Seed = 42
        };

        // Act
        var clone = original.Clone();

        // Assert - Clone has same values
        Assert.Equal(original.Temperature, clone.Temperature);
        Assert.Equal(original.TopP, clone.TopP);
        Assert.Equal(original.TopK, clone.TopK);
        Assert.Equal(original.RepetitionPenalty, clone.RepetitionPenalty);
        Assert.Equal(original.MaxTokens, clone.MaxTokens);
        Assert.Equal(original.ContextSize, clone.ContextSize);
        Assert.Equal(original.Seed, clone.Seed);
    }

    /// <summary>
    /// Verifies that modifying the clone does not affect the original.
    /// </summary>
    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new InferenceSettings { Temperature = 1.5f };
        var clone = original.Clone();

        // Act
        clone.Temperature = 0.5f;

        // Assert
        Assert.Equal(1.5f, original.Temperature);
        Assert.Equal(0.5f, clone.Temperature);
    }

    /// <summary>
    /// Verifies that Clone returns a new instance, not the same reference.
    /// </summary>
    [Fact]
    public void Clone_ReturnsNewInstance()
    {
        // Arrange
        var original = new InferenceSettings();

        // Act
        var clone = original.Clone();

        // Assert
        Assert.NotSame(original, clone);
    }

    #endregion

    #region Validate - Success Tests

    /// <summary>
    /// Verifies that default settings pass validation.
    /// </summary>
    [Fact]
    public void Validate_DefaultSettings_ReturnsSuccess()
    {
        // Arrange
        var settings = new InferenceSettings();

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// Verifies that minimum boundary values pass validation.
    /// </summary>
    [Fact]
    public void Validate_MinBoundaryValues_ReturnsSuccess()
    {
        // Arrange
        var settings = new InferenceSettings
        {
            Temperature = ParameterConstants.Temperature.Min,
            TopP = ParameterConstants.TopP.Min,
            TopK = ParameterConstants.TopK.Min,
            RepetitionPenalty = ParameterConstants.RepetitionPenalty.Min,
            MaxTokens = ParameterConstants.MaxTokens.Min,
            ContextSize = ParameterConstants.ContextSize.Min,
            Seed = ParameterConstants.Seed.Min
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    /// <summary>
    /// Verifies that maximum boundary values pass validation.
    /// </summary>
    [Fact]
    public void Validate_MaxBoundaryValues_ReturnsSuccess()
    {
        // Arrange
        var settings = new InferenceSettings
        {
            Temperature = ParameterConstants.Temperature.Max,
            TopP = ParameterConstants.TopP.Max,
            TopK = ParameterConstants.TopK.Max,
            RepetitionPenalty = ParameterConstants.RepetitionPenalty.Max,
            MaxTokens = ParameterConstants.MaxTokens.Max,
            ContextSize = ParameterConstants.ContextSize.Max,
            Seed = 12345
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Validate - Temperature Tests

    /// <summary>
    /// Verifies that Temperature below minimum fails validation.
    /// </summary>
    [Fact]
    public void Validate_TemperatureBelowMin_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { Temperature = -0.1f };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Temperature", result.Errors[0]);
    }

    /// <summary>
    /// Verifies that Temperature above maximum fails validation.
    /// </summary>
    [Fact]
    public void Validate_TemperatureAboveMax_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { Temperature = 2.1f };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Temperature", result.Errors[0]);
    }

    #endregion

    #region Validate - TopP Tests

    /// <summary>
    /// Verifies that TopP below minimum fails validation.
    /// </summary>
    [Fact]
    public void Validate_TopPBelowMin_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { TopP = -0.1f };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Top-P", result.Errors[0]);
    }

    /// <summary>
    /// Verifies that TopP above maximum fails validation.
    /// </summary>
    [Fact]
    public void Validate_TopPAboveMax_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { TopP = 1.1f };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Top-P", result.Errors[0]);
    }

    #endregion

    #region Validate - TopK Tests

    /// <summary>
    /// Verifies that TopK below minimum fails validation.
    /// </summary>
    [Fact]
    public void Validate_TopKBelowMin_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { TopK = -1 };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Top-K", result.Errors[0]);
    }

    /// <summary>
    /// Verifies that TopK above maximum fails validation.
    /// </summary>
    [Fact]
    public void Validate_TopKAboveMax_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { TopK = 101 };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Top-K", result.Errors[0]);
    }

    #endregion

    #region Validate - RepetitionPenalty Tests

    /// <summary>
    /// Verifies that RepetitionPenalty below minimum fails validation.
    /// </summary>
    [Fact]
    public void Validate_RepetitionPenaltyBelowMin_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { RepetitionPenalty = 0.9f };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Repetition Penalty", result.Errors[0]);
    }

    /// <summary>
    /// Verifies that RepetitionPenalty above maximum fails validation.
    /// </summary>
    [Fact]
    public void Validate_RepetitionPenaltyAboveMax_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { RepetitionPenalty = 2.1f };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Repetition Penalty", result.Errors[0]);
    }

    #endregion

    #region Validate - MaxTokens Tests

    /// <summary>
    /// Verifies that MaxTokens below minimum fails validation.
    /// </summary>
    [Fact]
    public void Validate_MaxTokensBelowMin_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { MaxTokens = 63 };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Max Tokens", result.Errors[0]);
    }

    /// <summary>
    /// Verifies that MaxTokens above maximum fails validation.
    /// </summary>
    [Fact]
    public void Validate_MaxTokensAboveMax_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { MaxTokens = 8193 };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Max Tokens", result.Errors[0]);
    }

    #endregion

    #region Validate - ContextSize Tests

    /// <summary>
    /// Verifies that ContextSize below minimum fails validation.
    /// </summary>
    [Fact]
    public void Validate_ContextSizeBelowMin_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { ContextSize = 511 };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Context Size", result.Errors[0]);
    }

    /// <summary>
    /// Verifies that ContextSize above maximum fails validation.
    /// </summary>
    [Fact]
    public void Validate_ContextSizeAboveMax_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { ContextSize = 32769 };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Context Size", result.Errors[0]);
    }

    #endregion

    #region Validate - Seed Tests

    /// <summary>
    /// Verifies that Seed below minimum fails validation.
    /// </summary>
    [Fact]
    public void Validate_SeedBelowMin_ReturnsError()
    {
        // Arrange
        var settings = new InferenceSettings { Seed = -2 };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Seed", result.Errors[0]);
    }

    /// <summary>
    /// Verifies that Seed of -1 (random) passes validation.
    /// </summary>
    [Fact]
    public void Validate_SeedNegativeOne_ReturnsSuccess()
    {
        // Arrange
        var settings = new InferenceSettings { Seed = -1 };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Verifies that Seed of 0 passes validation.
    /// </summary>
    [Fact]
    public void Validate_SeedZero_ReturnsSuccess()
    {
        // Arrange
        var settings = new InferenceSettings { Seed = 0 };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Validate - Multiple Errors Tests

    /// <summary>
    /// Verifies that multiple validation errors are all reported.
    /// </summary>
    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var settings = new InferenceSettings
        {
            Temperature = 3.0f,  // Invalid
            TopP = 2.0f,         // Invalid
            TopK = 200           // Invalid
        };

        // Act
        var result = settings.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.Contains("Temperature"));
        Assert.Contains(result.Errors, e => e.Contains("Top-P"));
        Assert.Contains(result.Errors, e => e.Contains("Top-K"));
    }

    /// <summary>
    /// Verifies that GetAllErrors returns properly formatted error messages.
    /// </summary>
    [Fact]
    public void Validate_MultipleErrors_GetAllErrors_ReturnsFormattedString()
    {
        // Arrange
        var settings = new InferenceSettings
        {
            Temperature = 3.0f,
            TopP = 2.0f
        };

        // Act
        var result = settings.Validate();
        var allErrors = result.GetAllErrors();

        // Assert
        Assert.Contains("Temperature", allErrors);
        Assert.Contains("Top-P", allErrors);
        Assert.Contains("\n", allErrors);
    }

    #endregion
}
