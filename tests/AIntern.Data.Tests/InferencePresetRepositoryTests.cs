using AIntern.Core.Entities;
using AIntern.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIntern.Data.Tests;

/// <summary>
/// Unit tests for the <see cref="InferencePresetRepository"/> class.
/// Verifies CRUD operations, default handling, and duplication functionality.
/// </summary>
/// <remarks>
/// <para>
/// These tests use SQLite in-memory databases to verify:
/// </para>
/// <list type="bullet">
///   <item><description>Repository instantiation and constructor validation</description></item>
///   <item><description>Inference preset CRUD operations</description></item>
///   <item><description>Built-in preset protection</description></item>
///   <item><description>Default preset management</description></item>
///   <item><description>Preset duplication</description></item>
/// </list>
/// </remarks>
public class InferencePresetRepositoryTests : IDisposable
{
    #region Test Infrastructure

    /// <summary>
    /// SQLite connection kept open for in-memory database lifetime.
    /// </summary>
    private readonly SqliteConnection _connection;

    /// <summary>
    /// Database context for repository operations.
    /// </summary>
    private readonly AInternDbContext _context;

    /// <summary>
    /// Repository under test.
    /// </summary>
    private readonly InferencePresetRepository _repository;

    /// <summary>
    /// Initializes test infrastructure with an in-memory SQLite database.
    /// </summary>
    public InferencePresetRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);
        _context.Database.EnsureCreated();

        _repository = new InferencePresetRepository(_context);
    }

    /// <summary>
    /// Disposes of the test infrastructure.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region CRUD Tests

    /// <summary>
    /// Verifies that CreateAsync generates a new ID when the entity has an empty GUID.
    /// </summary>
    [Fact]
    public async Task CreateAsync_GeneratesId_WhenIdIsEmpty()
    {
        // Arrange
        var preset = new InferencePresetEntity
        {
            Name = "Test Preset"
        };

        // Act
        var result = await _repository.CreateAsync(preset);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    /// <summary>
    /// Verifies that DeleteAsync does not delete built-in presets.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ProtectsBuiltInPresets()
    {
        // Arrange
        var builtIn = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "Built-In Preset",
            IsBuiltIn = true
        };

        _context.InferencePresets.Add(builtIn);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(builtIn.Id);

        // Assert - preset should still exist
        var stillExists = await _context.InferencePresets
            .AnyAsync(p => p.Id == builtIn.Id);

        Assert.True(stillExists);
    }

    #endregion

    #region Default Handling Tests

    /// <summary>
    /// Verifies that SetAsDefaultAsync clears the previous default.
    /// </summary>
    [Fact]
    public async Task SetAsDefaultAsync_ClearsPreviousDefault()
    {
        // Arrange
        var preset1 = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Preset 1",
            IsDefault = true
        });

        var preset2 = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Preset 2"
        });

        // Act
        await _repository.SetAsDefaultAsync(preset2.Id);

        // Assert
        var updatedPreset1 = await _context.InferencePresets
            .AsNoTracking()
            .FirstAsync(p => p.Id == preset1.Id);

        var updatedPreset2 = await _context.InferencePresets
            .AsNoTracking()
            .FirstAsync(p => p.Id == preset2.Id);

        Assert.False(updatedPreset1.IsDefault);
        Assert.True(updatedPreset2.IsDefault);
    }

    /// <summary>
    /// Verifies that DeleteAsync reassigns the default when deleting the default preset.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ReassignsDefault_WhenDeletingDefault()
    {
        // Arrange
        var preset1 = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Preset 1",
            IsDefault = true,
            IsBuiltIn = false
        });

        var preset2 = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Preset 2",
            IsBuiltIn = false
        });

        // Act
        await _repository.DeleteAsync(preset1.Id);

        // Assert
        var remainingDefault = await _repository.GetDefaultAsync();

        Assert.NotNull(remainingDefault);
        Assert.Equal(preset2.Id, remainingDefault.Id);
    }

    #endregion

    #region Duplication Tests

    /// <summary>
    /// Verifies that DuplicateAsync copies all parameter values.
    /// </summary>
    [Fact]
    public async Task DuplicateAsync_CopiesAllParameters()
    {
        // Arrange
        var source = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Source Preset",
            Description = "A test preset",
            Temperature = 0.8f,
            TopP = 0.95f,
            TopK = 50,
            RepeatPenalty = 1.2f,
            MaxTokens = 4096,
            ContextSize = 8192
        });

        // Act
        var duplicate = await _repository.DuplicateAsync(source.Id, "Duplicated Preset");

        // Assert
        Assert.NotNull(duplicate);
        Assert.NotEqual(source.Id, duplicate.Id);
        Assert.Equal("Duplicated Preset", duplicate.Name);
        Assert.Equal(source.Description, duplicate.Description);
        Assert.Equal(source.Temperature, duplicate.Temperature);
        Assert.Equal(source.TopP, duplicate.TopP);
        Assert.Equal(source.TopK, duplicate.TopK);
        Assert.Equal(source.RepeatPenalty, duplicate.RepeatPenalty);
        Assert.Equal(source.MaxTokens, duplicate.MaxTokens);
        Assert.Equal(source.ContextSize, duplicate.ContextSize);
    }

    /// <summary>
    /// Verifies that DuplicateAsync sets IsBuiltIn to false on the duplicate.
    /// </summary>
    [Fact]
    public async Task DuplicateAsync_SetsIsBuiltInFalse()
    {
        // Arrange
        var builtIn = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "Built-In Preset",
            IsBuiltIn = true,
            IsDefault = true
        };

        _context.InferencePresets.Add(builtIn);
        await _context.SaveChangesAsync();

        // Act
        var duplicate = await _repository.DuplicateAsync(builtIn.Id, "Custom Preset");

        // Assert
        Assert.NotNull(duplicate);
        Assert.False(duplicate.IsBuiltIn);
        Assert.False(duplicate.IsDefault);
    }

    /// <summary>
    /// Verifies that DuplicateAsync returns null when source preset is not found.
    /// </summary>
    [Fact]
    public async Task DuplicateAsync_ReturnsNull_WhenSourceNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var duplicate = await _repository.DuplicateAsync(nonExistentId, "New Preset");

        // Assert
        Assert.Null(duplicate);
    }

    /// <summary>
    /// Verifies that DuplicateAsync resets UsageCount to zero.
    /// </summary>
    [Fact]
    public async Task DuplicateAsync_ResetsUsageCount()
    {
        // Arrange
        var source = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "Source Preset",
            UsageCount = 42
        };

        _context.InferencePresets.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var duplicate = await _repository.DuplicateAsync(source.Id, "Duplicated");

        // Assert
        Assert.NotNull(duplicate);
        Assert.Equal(0, duplicate.UsageCount);
    }

    #endregion

    #region Seeding Tests

    /// <summary>
    /// Verifies that SeedBuiltInPresetsAsync creates all 5 built-in presets.
    /// </summary>
    [Fact]
    public async Task SeedBuiltInPresetsAsync_CreatesAllFivePresets()
    {
        // Act
        await _repository.SeedBuiltInPresetsAsync();

        // Assert
        var builtInPresets = await _context.InferencePresets
            .Where(p => p.IsBuiltIn)
            .ToListAsync();

        Assert.Equal(5, builtInPresets.Count);
    }

    /// <summary>
    /// Verifies that SeedBuiltInPresetsAsync creates presets with correct well-known IDs.
    /// </summary>
    [Fact]
    public async Task SeedBuiltInPresetsAsync_CreatesPresetsWithWellKnownIds()
    {
        // Act
        await _repository.SeedBuiltInPresetsAsync();

        // Assert
        var preciseId = new Guid("00000001-0000-0000-0000-000000000001");
        var balancedId = new Guid("00000001-0000-0000-0000-000000000002");
        var creativeId = new Guid("00000001-0000-0000-0000-000000000003");
        var longFormId = new Guid("00000001-0000-0000-0000-000000000004");
        var codeReviewId = new Guid("00000001-0000-0000-0000-000000000005");

        Assert.NotNull(await _context.InferencePresets.FindAsync(preciseId));
        Assert.NotNull(await _context.InferencePresets.FindAsync(balancedId));
        Assert.NotNull(await _context.InferencePresets.FindAsync(creativeId));
        Assert.NotNull(await _context.InferencePresets.FindAsync(longFormId));
        Assert.NotNull(await _context.InferencePresets.FindAsync(codeReviewId));
    }

    /// <summary>
    /// Verifies that SeedBuiltInPresetsAsync sets Balanced as the default preset.
    /// </summary>
    [Fact]
    public async Task SeedBuiltInPresetsAsync_SetsBalancedAsDefault()
    {
        // Act
        await _repository.SeedBuiltInPresetsAsync();

        // Assert
        var defaultPreset = await _repository.GetDefaultAsync();

        Assert.NotNull(defaultPreset);
        Assert.Equal("Balanced", defaultPreset.Name);
        Assert.True(defaultPreset.IsDefault);
    }

    /// <summary>
    /// Verifies that SeedBuiltInPresetsAsync is idempotent (safe to call multiple times).
    /// </summary>
    [Fact]
    public async Task SeedBuiltInPresetsAsync_IsIdempotent()
    {
        // Act - Call seeding twice
        await _repository.SeedBuiltInPresetsAsync();
        await _repository.SeedBuiltInPresetsAsync();

        // Assert - Should still have exactly 5 built-in presets
        var builtInPresets = await _context.InferencePresets
            .Where(p => p.IsBuiltIn)
            .ToListAsync();

        Assert.Equal(5, builtInPresets.Count);
    }

    /// <summary>
    /// Verifies that SeedBuiltInPresetsAsync creates presets with correct parameter values.
    /// </summary>
    [Fact]
    public async Task SeedBuiltInPresetsAsync_SetsCorrectParameterValues()
    {
        // Act
        await _repository.SeedBuiltInPresetsAsync();

        // Assert - Check Precise preset has low temperature
        var precise = await _context.InferencePresets.FindAsync(
            new Guid("00000001-0000-0000-0000-000000000001"));
        Assert.NotNull(precise);
        Assert.Equal(0.2f, precise.Temperature);
        Assert.Equal("Code", precise.Category);

        // Assert - Check Creative preset has high temperature
        var creative = await _context.InferencePresets.FindAsync(
            new Guid("00000001-0000-0000-0000-000000000003"));
        Assert.NotNull(creative);
        Assert.Equal(1.2f, creative.Temperature);
        Assert.Equal("Creative", creative.Category);

        // Assert - Check Long-form has extended context
        var longForm = await _context.InferencePresets.FindAsync(
            new Guid("00000001-0000-0000-0000-000000000004"));
        Assert.NotNull(longForm);
        Assert.Equal(16384, longForm.ContextSize);
    }

    #endregion

    #region Read Operation Tests

    /// <summary>
    /// Verifies that GetByIdAsync returns null for non-existent preset.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenPresetNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetByIdAsync returns the preset when it exists.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_ReturnsPreset_WhenExists()
    {
        // Arrange
        var preset = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Test Preset"
        });

        // Act
        var result = await _repository.GetByIdAsync(preset.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Preset", result.Name);
    }

    /// <summary>
    /// Verifies that GetAllAsync returns presets ordered correctly.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ReturnsPresetsOrderedByBuiltInThenName()
    {
        // Arrange
        await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Zebra Preset",
            IsBuiltIn = false
        });

        await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Alpha Preset",
            IsBuiltIn = true
        });

        await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Beta Preset",
            IsBuiltIn = false
        });

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("Alpha Preset", results[0].Name); // Built-in first
        Assert.Equal("Beta Preset", results[1].Name);  // Then alphabetically
        Assert.Equal("Zebra Preset", results[2].Name);
    }

    /// <summary>
    /// Verifies that GetBuiltInAsync returns only built-in presets.
    /// </summary>
    [Fact]
    public async Task GetBuiltInAsync_ReturnsOnlyBuiltInPresets()
    {
        // Arrange
        await _repository.SeedBuiltInPresetsAsync();
        await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Custom Preset",
            IsBuiltIn = false
        });

        // Act
        var results = await _repository.GetBuiltInAsync();

        // Assert
        Assert.Equal(5, results.Count);
        Assert.All(results, p => Assert.True(p.IsBuiltIn));
    }

    /// <summary>
    /// Verifies that GetUserCreatedAsync returns only user-created presets.
    /// </summary>
    [Fact]
    public async Task GetUserCreatedAsync_ReturnsOnlyUserPresets()
    {
        // Arrange
        await _repository.SeedBuiltInPresetsAsync();
        await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Custom Preset 1",
            IsBuiltIn = false
        });
        await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Custom Preset 2",
            IsBuiltIn = false
        });

        // Act
        var results = await _repository.GetUserCreatedAsync();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, p => Assert.False(p.IsBuiltIn));
    }

    /// <summary>
    /// Verifies that GetByCategoryAsync returns presets in the specified category.
    /// </summary>
    [Fact]
    public async Task GetByCategoryAsync_ReturnsPresetsInCategory()
    {
        // Arrange
        await _repository.SeedBuiltInPresetsAsync();

        // Act
        var codePresets = await _repository.GetByCategoryAsync("Code");

        // Assert
        Assert.Equal(2, codePresets.Count); // Precise and Code Review
        Assert.All(codePresets, p => Assert.Equal("Code", p.Category));
    }

    /// <summary>
    /// Verifies that NameExistsAsync returns true when name exists.
    /// </summary>
    [Fact]
    public async Task NameExistsAsync_ReturnsTrue_WhenNameExists()
    {
        // Arrange
        await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Existing Preset"
        });

        // Act
        var exists = await _repository.NameExistsAsync("Existing Preset");

        // Assert
        Assert.True(exists);
    }

    /// <summary>
    /// Verifies that NameExistsAsync returns false when name does not exist.
    /// </summary>
    [Fact]
    public async Task NameExistsAsync_ReturnsFalse_WhenNameNotExists()
    {
        // Act
        var exists = await _repository.NameExistsAsync("Nonexistent Preset");

        // Assert
        Assert.False(exists);
    }

    /// <summary>
    /// Verifies that NameExistsAsync excludes the specified ID from the check.
    /// </summary>
    [Fact]
    public async Task NameExistsAsync_ExcludesSpecifiedId()
    {
        // Arrange
        var preset = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "My Preset"
        });

        // Act
        var existsExcludingSelf = await _repository.NameExistsAsync("My Preset", preset.Id);
        var existsWithoutExclusion = await _repository.NameExistsAsync("My Preset");

        // Assert
        Assert.False(existsExcludingSelf);
        Assert.True(existsWithoutExclusion);
    }

    #endregion

    #region Write Operation Tests

    /// <summary>
    /// Verifies that CreateAsync preserves provided ID when not empty.
    /// </summary>
    [Fact]
    public async Task CreateAsync_PreservesProvidedId_WhenNotEmpty()
    {
        // Arrange
        var providedId = Guid.NewGuid();
        var preset = new InferencePresetEntity
        {
            Id = providedId,
            Name = "Test Preset"
        };

        // Act
        var result = await _repository.CreateAsync(preset);

        // Assert
        Assert.Equal(providedId, result.Id);
    }

    /// <summary>
    /// Verifies that UpdateAsync modifies the preset.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ModifiesPreset()
    {
        // Arrange
        var preset = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Original Name"
        });

        // Detach entity to simulate a real-world scenario
        _context.Entry(preset).State = EntityState.Detached;

        preset.Name = "Updated Name";

        // Act
        await _repository.UpdateAsync(preset);

        // Assert
        var updated = await _context.InferencePresets
            .AsNoTracking()
            .FirstAsync(p => p.Id == preset.Id);

        Assert.Equal("Updated Name", updated.Name);
    }

    /// <summary>
    /// Verifies that IncrementUsageAsync increases UsageCount by 1.
    /// </summary>
    [Fact]
    public async Task IncrementUsageAsync_IncreasesUsageCount()
    {
        // Arrange
        var preset = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Test Preset",
            UsageCount = 5
        });

        // Act
        await _repository.IncrementUsageAsync(preset.Id);

        // Assert
        var updated = await _context.InferencePresets
            .AsNoTracking()
            .FirstAsync(p => p.Id == preset.Id);

        Assert.Equal(6, updated.UsageCount);
    }

    /// <summary>
    /// Verifies that IncrementUsageAsync does nothing for non-existent preset.
    /// </summary>
    [Fact]
    public async Task IncrementUsageAsync_DoesNothing_WhenPresetNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act - Should not throw
        await _repository.IncrementUsageAsync(nonExistentId);

        // Assert - No exception thrown
    }

    /// <summary>
    /// Verifies that DeleteAsync removes user-created presets.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_RemovesUserCreatedPreset()
    {
        // Arrange
        var preset = await _repository.CreateAsync(new InferencePresetEntity
        {
            Name = "Test Preset",
            IsBuiltIn = false
        });

        // Act
        await _repository.DeleteAsync(preset.Id);

        // Assert
        var exists = await _context.InferencePresets
            .AnyAsync(p => p.Id == preset.Id);

        Assert.False(exists);
    }

    /// <summary>
    /// Verifies that DeleteAsync does nothing for non-existent preset.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_DoesNothing_WhenPresetNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act - Should not throw
        await _repository.DeleteAsync(nonExistentId);

        // Assert - No exception thrown
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that constructor throws when context is null.
    /// </summary>
    [Fact]
    public void Constructor_NullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new InferencePresetRepository(null!));
    }

    /// <summary>
    /// Verifies that constructor accepts null logger without throwing.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_DoesNotThrow()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);

        // Act & Assert - Should not throw
        var repository = new InferencePresetRepository(context, null);
        Assert.NotNull(repository);
    }

    #endregion
}
