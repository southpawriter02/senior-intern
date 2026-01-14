using Xunit;
using AIntern.Core;
using AIntern.Core.Entities;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="InferencePreset"/> class.
/// Verifies factory methods, entity mapping, and well-known preset IDs.
/// </summary>
/// <remarks>
/// <para>
/// These tests ensure that InferencePreset correctly:
/// </para>
/// <list type="bullet">
///   <item><description>Creates presets from options using FromOptions</description></item>
///   <item><description>Converts to/from entity representations</description></item>
///   <item><description>Exposes correct well-known preset IDs</description></item>
/// </list>
/// </remarks>
public class InferencePresetTests
{
    #region Default Value Tests

    /// <summary>
    /// Verifies that a new InferencePreset has a generated Id.
    /// </summary>
    [Fact]
    public void Constructor_Id_IsGenerated()
    {
        // Arrange & Act
        var preset = new InferencePreset();

        // Assert
        Assert.NotEqual(Guid.Empty, preset.Id);
    }

    /// <summary>
    /// Verifies that a new InferencePreset has an empty Name.
    /// </summary>
    [Fact]
    public void Constructor_Name_IsEmptyString()
    {
        // Arrange & Act
        var preset = new InferencePreset();

        // Assert
        Assert.Equal(string.Empty, preset.Name);
    }

    /// <summary>
    /// Verifies that a new InferencePreset has null Description.
    /// </summary>
    [Fact]
    public void Constructor_Description_IsNull()
    {
        // Arrange & Act
        var preset = new InferencePreset();

        // Assert
        Assert.Null(preset.Description);
    }

    /// <summary>
    /// Verifies that a new InferencePreset has null Category.
    /// </summary>
    [Fact]
    public void Constructor_Category_IsNull()
    {
        // Arrange & Act
        var preset = new InferencePreset();

        // Assert
        Assert.Null(preset.Category);
    }

    /// <summary>
    /// Verifies that a new InferencePreset is not built-in by default.
    /// </summary>
    [Fact]
    public void Constructor_IsBuiltIn_IsFalse()
    {
        // Arrange & Act
        var preset = new InferencePreset();

        // Assert
        Assert.False(preset.IsBuiltIn);
    }

    /// <summary>
    /// Verifies that a new InferencePreset is not default by default.
    /// </summary>
    [Fact]
    public void Constructor_IsDefault_IsFalse()
    {
        // Arrange & Act
        var preset = new InferencePreset();

        // Assert
        Assert.False(preset.IsDefault);
    }

    /// <summary>
    /// Verifies that a new InferencePreset has zero UsageCount.
    /// </summary>
    [Fact]
    public void Constructor_UsageCount_IsZero()
    {
        // Arrange & Act
        var preset = new InferencePreset();

        // Assert
        Assert.Equal(0, preset.UsageCount);
    }

    /// <summary>
    /// Verifies that a new InferencePreset has default Options.
    /// </summary>
    [Fact]
    public void Constructor_Options_IsNotNull()
    {
        // Arrange & Act
        var preset = new InferencePreset();

        // Assert
        Assert.NotNull(preset.Options);
        Assert.Equal(ParameterConstants.Temperature.Default, preset.Options.Temperature);
    }

    #endregion

    #region Well-Known Preset ID Tests

    /// <summary>
    /// Verifies that PrecisePresetId has the correct well-known GUID.
    /// </summary>
    [Fact]
    public void PrecisePresetId_HasCorrectValue()
    {
        // Assert
        Assert.Equal(new Guid("00000001-0000-0000-0000-000000000001"), InferencePreset.PrecisePresetId);
    }

    /// <summary>
    /// Verifies that BalancedPresetId has the correct well-known GUID.
    /// </summary>
    [Fact]
    public void BalancedPresetId_HasCorrectValue()
    {
        // Assert
        Assert.Equal(new Guid("00000001-0000-0000-0000-000000000002"), InferencePreset.BalancedPresetId);
    }

    /// <summary>
    /// Verifies that CreativePresetId has the correct well-known GUID.
    /// </summary>
    [Fact]
    public void CreativePresetId_HasCorrectValue()
    {
        // Assert
        Assert.Equal(new Guid("00000001-0000-0000-0000-000000000003"), InferencePreset.CreativePresetId);
    }

    /// <summary>
    /// Verifies that LongFormPresetId has the correct well-known GUID.
    /// </summary>
    [Fact]
    public void LongFormPresetId_HasCorrectValue()
    {
        // Assert
        Assert.Equal(new Guid("00000001-0000-0000-0000-000000000004"), InferencePreset.LongFormPresetId);
    }

    /// <summary>
    /// Verifies that CodeReviewPresetId has the correct well-known GUID.
    /// </summary>
    [Fact]
    public void CodeReviewPresetId_HasCorrectValue()
    {
        // Assert
        Assert.Equal(new Guid("00000001-0000-0000-0000-000000000005"), InferencePreset.CodeReviewPresetId);
    }

    /// <summary>
    /// Verifies that all well-known preset IDs are unique.
    /// </summary>
    [Fact]
    public void WellKnownPresetIds_AreUnique()
    {
        // Arrange
        var ids = new[]
        {
            InferencePreset.PrecisePresetId,
            InferencePreset.BalancedPresetId,
            InferencePreset.CreativePresetId,
            InferencePreset.LongFormPresetId,
            InferencePreset.CodeReviewPresetId
        };

        // Act
        var uniqueIds = ids.Distinct().ToList();

        // Assert
        Assert.Equal(5, uniqueIds.Count);
    }

    #endregion

    #region FromOptions Tests

    /// <summary>
    /// Verifies that FromOptions creates a preset with the specified name.
    /// </summary>
    [Fact]
    public void FromOptions_SetsName()
    {
        // Arrange
        var options = new InferenceSettings();

        // Act
        var preset = InferencePreset.FromOptions("Test Preset", options);

        // Assert
        Assert.Equal("Test Preset", preset.Name);
    }

    /// <summary>
    /// Verifies that FromOptions generates a new Id.
    /// </summary>
    [Fact]
    public void FromOptions_GeneratesNewId()
    {
        // Arrange
        var options = new InferenceSettings();

        // Act
        var preset = InferencePreset.FromOptions("Test", options);

        // Assert
        Assert.NotEqual(Guid.Empty, preset.Id);
    }

    /// <summary>
    /// Verifies that FromOptions clones the options (not same reference).
    /// </summary>
    [Fact]
    public void FromOptions_ClonesOptions()
    {
        // Arrange
        var options = new InferenceSettings { Temperature = 1.5f };

        // Act
        var preset = InferencePreset.FromOptions("Test", options);

        // Assert
        Assert.NotSame(options, preset.Options);
        Assert.Equal(1.5f, preset.Options.Temperature);
    }

    /// <summary>
    /// Verifies that modifying original options does not affect the preset.
    /// </summary>
    [Fact]
    public void FromOptions_ModifyingOriginalOptions_DoesNotAffectPreset()
    {
        // Arrange
        var options = new InferenceSettings { Temperature = 1.5f };
        var preset = InferencePreset.FromOptions("Test", options);

        // Act
        options.Temperature = 0.5f;

        // Assert
        Assert.Equal(1.5f, preset.Options.Temperature);
    }

    /// <summary>
    /// Verifies that FromOptions sets IsBuiltIn to false.
    /// </summary>
    [Fact]
    public void FromOptions_IsBuiltIn_IsFalse()
    {
        // Arrange
        var options = new InferenceSettings();

        // Act
        var preset = InferencePreset.FromOptions("Test", options);

        // Assert
        Assert.False(preset.IsBuiltIn);
    }

    /// <summary>
    /// Verifies that FromOptions sets IsDefault to false.
    /// </summary>
    [Fact]
    public void FromOptions_IsDefault_IsFalse()
    {
        // Arrange
        var options = new InferenceSettings();

        // Act
        var preset = InferencePreset.FromOptions("Test", options);

        // Assert
        Assert.False(preset.IsDefault);
    }

    /// <summary>
    /// Verifies that FromOptions sets UsageCount to zero.
    /// </summary>
    [Fact]
    public void FromOptions_UsageCount_IsZero()
    {
        // Arrange
        var options = new InferenceSettings();

        // Act
        var preset = InferencePreset.FromOptions("Test", options);

        // Assert
        Assert.Equal(0, preset.UsageCount);
    }

    /// <summary>
    /// Verifies that FromOptions sets CreatedAt to current time.
    /// </summary>
    [Fact]
    public void FromOptions_CreatedAt_IsRecent()
    {
        // Arrange
        var options = new InferenceSettings();
        var before = DateTime.UtcNow;

        // Act
        var preset = InferencePreset.FromOptions("Test", options);

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(preset.CreatedAt, before, after);
    }

    /// <summary>
    /// Verifies that FromOptions sets UpdatedAt to current time.
    /// </summary>
    [Fact]
    public void FromOptions_UpdatedAt_IsRecent()
    {
        // Arrange
        var options = new InferenceSettings();
        var before = DateTime.UtcNow;

        // Act
        var preset = InferencePreset.FromOptions("Test", options);

        // Assert
        var after = DateTime.UtcNow;
        Assert.InRange(preset.UpdatedAt, before, after);
    }

    /// <summary>
    /// Verifies that calling FromOptions twice creates presets with different IDs.
    /// </summary>
    [Fact]
    public void FromOptions_CalledTwice_GeneratesDifferentIds()
    {
        // Arrange
        var options = new InferenceSettings();

        // Act
        var preset1 = InferencePreset.FromOptions("Test 1", options);
        var preset2 = InferencePreset.FromOptions("Test 2", options);

        // Assert
        Assert.NotEqual(preset1.Id, preset2.Id);
    }

    #endregion

    #region ToEntity Tests

    /// <summary>
    /// Verifies that ToEntity copies the Id.
    /// </summary>
    [Fact]
    public void ToEntity_CopiesId()
    {
        // Arrange
        var preset = new InferencePreset { Id = Guid.NewGuid() };

        // Act
        var entity = preset.ToEntity();

        // Assert
        Assert.Equal(preset.Id, entity.Id);
    }

    /// <summary>
    /// Verifies that ToEntity copies the Name.
    /// </summary>
    [Fact]
    public void ToEntity_CopiesName()
    {
        // Arrange
        var preset = new InferencePreset { Name = "Test Preset" };

        // Act
        var entity = preset.ToEntity();

        // Assert
        Assert.Equal("Test Preset", entity.Name);
    }

    /// <summary>
    /// Verifies that ToEntity copies the Description.
    /// </summary>
    [Fact]
    public void ToEntity_CopiesDescription()
    {
        // Arrange
        var preset = new InferencePreset { Description = "A test description" };

        // Act
        var entity = preset.ToEntity();

        // Assert
        Assert.Equal("A test description", entity.Description);
    }

    /// <summary>
    /// Verifies that ToEntity copies the Category.
    /// </summary>
    [Fact]
    public void ToEntity_CopiesCategory()
    {
        // Arrange
        var preset = new InferencePreset { Category = "Code" };

        // Act
        var entity = preset.ToEntity();

        // Assert
        Assert.Equal("Code", entity.Category);
    }

    /// <summary>
    /// Verifies that ToEntity flattens the Options into entity properties.
    /// </summary>
    [Fact]
    public void ToEntity_FlattensOptions()
    {
        // Arrange
        var preset = new InferencePreset
        {
            Options = new InferenceSettings
            {
                Temperature = 1.2f,
                TopP = 0.8f,
                TopK = 50,
                RepetitionPenalty = 1.15f,
                MaxTokens = 4096,
                ContextSize = 8192,
                Seed = 42
            }
        };

        // Act
        var entity = preset.ToEntity();

        // Assert
        Assert.Equal(1.2f, entity.Temperature);
        Assert.Equal(0.8f, entity.TopP);
        Assert.Equal(50, entity.TopK);
        Assert.Equal(1.15f, entity.RepeatPenalty);
        Assert.Equal(4096, entity.MaxTokens);
        Assert.Equal(8192, entity.ContextSize);
        Assert.Equal(42, entity.Seed);
    }

    /// <summary>
    /// Verifies that ToEntity copies the IsBuiltIn flag.
    /// </summary>
    [Fact]
    public void ToEntity_CopiesIsBuiltIn()
    {
        // Arrange
        var preset = new InferencePreset { IsBuiltIn = true };

        // Act
        var entity = preset.ToEntity();

        // Assert
        Assert.True(entity.IsBuiltIn);
    }

    /// <summary>
    /// Verifies that ToEntity copies the IsDefault flag.
    /// </summary>
    [Fact]
    public void ToEntity_CopiesIsDefault()
    {
        // Arrange
        var preset = new InferencePreset { IsDefault = true };

        // Act
        var entity = preset.ToEntity();

        // Assert
        Assert.True(entity.IsDefault);
    }

    /// <summary>
    /// Verifies that ToEntity copies the UsageCount.
    /// </summary>
    [Fact]
    public void ToEntity_CopiesUsageCount()
    {
        // Arrange
        var preset = new InferencePreset { UsageCount = 42 };

        // Act
        var entity = preset.ToEntity();

        // Assert
        Assert.Equal(42, entity.UsageCount);
    }

    /// <summary>
    /// Verifies that ToEntity copies the timestamps.
    /// </summary>
    [Fact]
    public void ToEntity_CopiesTimestamps()
    {
        // Arrange
        var created = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var updated = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var preset = new InferencePreset
        {
            CreatedAt = created,
            UpdatedAt = updated
        };

        // Act
        var entity = preset.ToEntity();

        // Assert
        Assert.Equal(created, entity.CreatedAt);
        Assert.Equal(updated, entity.UpdatedAt);
    }

    #endregion

    #region FromEntity Tests

    /// <summary>
    /// Verifies that FromEntity throws when entity is null.
    /// </summary>
    [Fact]
    public void FromEntity_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => InferencePreset.FromEntity(null!));
    }

    /// <summary>
    /// Verifies that FromEntity copies the Id.
    /// </summary>
    [Fact]
    public void FromEntity_CopiesId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new InferencePresetEntity { Id = id };

        // Act
        var preset = InferencePreset.FromEntity(entity);

        // Assert
        Assert.Equal(id, preset.Id);
    }

    /// <summary>
    /// Verifies that FromEntity copies the Name.
    /// </summary>
    [Fact]
    public void FromEntity_CopiesName()
    {
        // Arrange
        var entity = new InferencePresetEntity { Name = "Test Preset" };

        // Act
        var preset = InferencePreset.FromEntity(entity);

        // Assert
        Assert.Equal("Test Preset", preset.Name);
    }

    /// <summary>
    /// Verifies that FromEntity copies the Description.
    /// </summary>
    [Fact]
    public void FromEntity_CopiesDescription()
    {
        // Arrange
        var entity = new InferencePresetEntity { Description = "A description" };

        // Act
        var preset = InferencePreset.FromEntity(entity);

        // Assert
        Assert.Equal("A description", preset.Description);
    }

    /// <summary>
    /// Verifies that FromEntity copies the Category.
    /// </summary>
    [Fact]
    public void FromEntity_CopiesCategory()
    {
        // Arrange
        var entity = new InferencePresetEntity { Category = "General" };

        // Act
        var preset = InferencePreset.FromEntity(entity);

        // Assert
        Assert.Equal("General", preset.Category);
    }

    /// <summary>
    /// Verifies that FromEntity reconstructs the Options from flattened entity properties.
    /// </summary>
    [Fact]
    public void FromEntity_ReconstructsOptions()
    {
        // Arrange
        var entity = new InferencePresetEntity
        {
            Temperature = 1.2f,
            TopP = 0.8f,
            TopK = 50,
            RepeatPenalty = 1.15f,
            MaxTokens = 4096,
            ContextSize = 8192,
            Seed = 42
        };

        // Act
        var preset = InferencePreset.FromEntity(entity);

        // Assert
        Assert.Equal(1.2f, preset.Options.Temperature);
        Assert.Equal(0.8f, preset.Options.TopP);
        Assert.Equal(50, preset.Options.TopK);
        Assert.Equal(1.15f, preset.Options.RepetitionPenalty);
        Assert.Equal(4096, preset.Options.MaxTokens);
        Assert.Equal(8192u, preset.Options.ContextSize);
        Assert.Equal(42, preset.Options.Seed);
    }

    /// <summary>
    /// Verifies that FromEntity copies the IsBuiltIn flag.
    /// </summary>
    [Fact]
    public void FromEntity_CopiesIsBuiltIn()
    {
        // Arrange
        var entity = new InferencePresetEntity { IsBuiltIn = true };

        // Act
        var preset = InferencePreset.FromEntity(entity);

        // Assert
        Assert.True(preset.IsBuiltIn);
    }

    /// <summary>
    /// Verifies that FromEntity copies the IsDefault flag.
    /// </summary>
    [Fact]
    public void FromEntity_CopiesIsDefault()
    {
        // Arrange
        var entity = new InferencePresetEntity { IsDefault = true };

        // Act
        var preset = InferencePreset.FromEntity(entity);

        // Assert
        Assert.True(preset.IsDefault);
    }

    /// <summary>
    /// Verifies that FromEntity copies the UsageCount.
    /// </summary>
    [Fact]
    public void FromEntity_CopiesUsageCount()
    {
        // Arrange
        var entity = new InferencePresetEntity { UsageCount = 42 };

        // Act
        var preset = InferencePreset.FromEntity(entity);

        // Assert
        Assert.Equal(42, preset.UsageCount);
    }

    /// <summary>
    /// Verifies that FromEntity copies the timestamps.
    /// </summary>
    [Fact]
    public void FromEntity_CopiesTimestamps()
    {
        // Arrange
        var created = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var updated = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var entity = new InferencePresetEntity
        {
            CreatedAt = created,
            UpdatedAt = updated
        };

        // Act
        var preset = InferencePreset.FromEntity(entity);

        // Assert
        Assert.Equal(created, preset.CreatedAt);
        Assert.Equal(updated, preset.UpdatedAt);
    }

    #endregion

    #region Round-Trip Tests

    /// <summary>
    /// Verifies that ToEntity and FromEntity round-trip preserves all properties.
    /// </summary>
    [Fact]
    public void ToEntity_FromEntity_RoundTrip_PreservesAllProperties()
    {
        // Arrange
        var original = new InferencePreset
        {
            Id = Guid.NewGuid(),
            Name = "Test Preset",
            Description = "A test preset",
            Category = "Code",
            IsBuiltIn = true,
            IsDefault = true,
            UsageCount = 42,
            CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            Options = new InferenceSettings
            {
                Temperature = 0.3f,
                TopP = 0.85f,
                TopK = 30,
                RepetitionPenalty = 1.2f,
                MaxTokens = 4096,
                ContextSize = 8192,
                Seed = 12345
            }
        };

        // Act
        var entity = original.ToEntity();
        var restored = InferencePreset.FromEntity(entity);

        // Assert
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Description, restored.Description);
        Assert.Equal(original.Category, restored.Category);
        Assert.Equal(original.IsBuiltIn, restored.IsBuiltIn);
        Assert.Equal(original.IsDefault, restored.IsDefault);
        Assert.Equal(original.UsageCount, restored.UsageCount);
        Assert.Equal(original.CreatedAt, restored.CreatedAt);
        Assert.Equal(original.UpdatedAt, restored.UpdatedAt);
        Assert.Equal(original.Options.Temperature, restored.Options.Temperature);
        Assert.Equal(original.Options.TopP, restored.Options.TopP);
        Assert.Equal(original.Options.TopK, restored.Options.TopK);
        Assert.Equal(original.Options.RepetitionPenalty, restored.Options.RepetitionPenalty);
        Assert.Equal(original.Options.MaxTokens, restored.Options.MaxTokens);
        Assert.Equal(original.Options.ContextSize, restored.Options.ContextSize);
        Assert.Equal(original.Options.Seed, restored.Options.Seed);
    }

    #endregion
}
