using AIntern.Core.Entities;
using AIntern.Core.Templates;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIntern.Data.Tests;

/// <summary>
/// Unit tests for the <see cref="DatabaseInitializer"/> class.
/// Verifies database initialization, seeding, and idempotency.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify that the DatabaseInitializer correctly handles:
/// </para>
/// <list type="bullet">
///   <item><description>Database creation and schema setup</description></item>
///   <item><description>Seeding of default system prompts (8 templates)</description></item>
///   <item><description>Seeding of default inference presets (5 presets)</description></item>
///   <item><description>Idempotent seeding (safe to call multiple times)</description></item>
///   <item><description>Correct default values for seeded data</description></item>
/// </list>
/// <para>
/// Tests use SQLite in-memory databases for isolation.
/// </para>
/// </remarks>
public class DatabaseInitializerTests : IDisposable
{
    #region Test Infrastructure

    private SqliteConnection? _connection;
    private AInternDbContext? _context;
    private DatabaseInitializer? _initializer;

    /// <summary>
    /// Creates test infrastructure with in-memory SQLite.
    /// </summary>
    private void SetupTestEnvironment()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);

        // Create a mock path resolver for testing
        var pathResolver = new DatabasePathResolver();

        _initializer = new DatabaseInitializer(
            _context,
            pathResolver,
            NullLogger<DatabaseInitializer>.Instance);
    }

    /// <summary>
    /// Disposes of the test infrastructure.
    /// </summary>
    public void Dispose()
    {
        _context?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies that constructor throws when context is null.
    /// </summary>
    [Fact]
    public void Constructor_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var pathResolver = new DatabasePathResolver();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DatabaseInitializer(
                null!,
                pathResolver,
                NullLogger<DatabaseInitializer>.Instance));
    }

    /// <summary>
    /// Verifies that constructor throws when path resolver is null.
    /// </summary>
    [Fact]
    public void Constructor_NullPathResolver_ThrowsArgumentNullException()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DatabaseInitializer(
                context,
                null!,
                NullLogger<DatabaseInitializer>.Instance));
    }

    /// <summary>
    /// Verifies that constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);
        var pathResolver = new DatabasePathResolver();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DatabaseInitializer(context, pathResolver, null!));
    }

    #endregion

    #region InitializeAsync Tests

    /// <summary>
    /// Verifies that InitializeAsync creates the database schema.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_CreatesDatabase()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert - Can add entities to all tables
        _context!.Conversations.Add(new ConversationEntity { Id = Guid.NewGuid(), Title = "Test" });
        await _context.SaveChangesAsync();

        Assert.Single(_context.Conversations);
    }

    /// <summary>
    /// Verifies that InitializeAsync seeds system prompts.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SeedsSystemPrompts()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var prompts = await _context!.SystemPrompts.ToListAsync();
        Assert.Equal(8, prompts.Count);
    }

    /// <summary>
    /// Verifies that InitializeAsync seeds inference presets.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SeedsInferencePresets()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var presets = await _context!.InferencePresets.ToListAsync();
        Assert.Equal(5, presets.Count);
    }

    /// <summary>
    /// Verifies that InitializeAsync is idempotent (safe to call multiple times).
    /// </summary>
    [Fact]
    public async Task InitializeAsync_IsIdempotent()
    {
        // Arrange
        SetupTestEnvironment();

        // Act - Initialize twice
        await _initializer!.InitializeAsync();
        await _initializer.InitializeAsync();

        // Assert - Should still have same counts
        Assert.Equal(8, await _context!.SystemPrompts.CountAsync());
        Assert.Equal(5, await _context.InferencePresets.CountAsync());
    }

    /// <summary>
    /// Verifies that seeded system prompts have IsBuiltIn set to true.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SystemPromptsAreBuiltIn()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var prompts = await _context!.SystemPrompts.ToListAsync();
        Assert.All(prompts, p => Assert.True(p.IsBuiltIn));
    }

    /// <summary>
    /// Verifies that seeded inference presets have IsBuiltIn set to true.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_InferencePresetsAreBuiltIn()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var presets = await _context!.InferencePresets.ToListAsync();
        Assert.All(presets, p => Assert.True(p.IsBuiltIn));
    }

    #endregion

    #region Default Entity Tests

    /// <summary>
    /// Verifies that Default Assistant is the default system prompt.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_DefaultPromptIsDefaultAssistant()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var defaultPrompt = await _context!.SystemPrompts
            .FirstOrDefaultAsync(p => p.IsDefault);

        Assert.NotNull(defaultPrompt);
        Assert.Equal("Default Assistant", defaultPrompt.Name);
    }

    /// <summary>
    /// Verifies that only one system prompt is marked as default.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_OnlyOneDefaultPrompt()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var defaultPrompts = await _context!.SystemPrompts
            .Where(p => p.IsDefault)
            .ToListAsync();

        Assert.Single(defaultPrompts);
    }

    /// <summary>
    /// Verifies that Balanced is the default inference preset.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_DefaultPresetIsBalanced()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var defaultPreset = await _context!.InferencePresets
            .FirstOrDefaultAsync(p => p.IsDefault);

        Assert.NotNull(defaultPreset);
        Assert.Equal("Balanced", defaultPreset.Name);
    }

    /// <summary>
    /// Verifies that only one inference preset is marked as default.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_OnlyOneDefaultPreset()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var defaultPresets = await _context!.InferencePresets
            .Where(p => p.IsDefault)
            .ToListAsync();

        Assert.Single(defaultPresets);
    }

    #endregion

    #region System Prompt Well-Known ID Tests

    /// <summary>
    /// Verifies that system prompts are seeded with well-known IDs.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SystemPromptsHaveWellKnownIds()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert - Check well-known IDs from SystemPromptTemplates
        Assert.NotNull(await _context!.SystemPrompts.FindAsync(SystemPromptTemplates.DefaultAssistantId));
        Assert.NotNull(await _context.SystemPrompts.FindAsync(SystemPromptTemplates.SeniorInternId));
        Assert.NotNull(await _context.SystemPrompts.FindAsync(SystemPromptTemplates.CodeExpertId));
        Assert.NotNull(await _context.SystemPrompts.FindAsync(SystemPromptTemplates.TechnicalWriterId));
        Assert.NotNull(await _context.SystemPrompts.FindAsync(SystemPromptTemplates.RubberDuckId));
        Assert.NotNull(await _context.SystemPrompts.FindAsync(SystemPromptTemplates.SocraticTutorId));
        Assert.NotNull(await _context.SystemPrompts.FindAsync(SystemPromptTemplates.CodeReviewerId));
        Assert.NotNull(await _context.SystemPrompts.FindAsync(SystemPromptTemplates.DebuggerId));
    }

    #endregion

    #region Inference Preset Well-Known ID Tests

    /// <summary>
    /// Verifies that inference presets are seeded with well-known IDs.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_InferencePresetsHaveWellKnownIds()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert - Check well-known IDs
        var preciseId = new Guid("00000001-0000-0000-0000-000000000001");
        var balancedId = new Guid("00000001-0000-0000-0000-000000000002");
        var creativeId = new Guid("00000001-0000-0000-0000-000000000003");
        var longFormId = new Guid("00000001-0000-0000-0000-000000000004");
        var codeReviewId = new Guid("00000001-0000-0000-0000-000000000005");

        Assert.NotNull(await _context!.InferencePresets.FindAsync(preciseId));
        Assert.NotNull(await _context.InferencePresets.FindAsync(balancedId));
        Assert.NotNull(await _context.InferencePresets.FindAsync(creativeId));
        Assert.NotNull(await _context.InferencePresets.FindAsync(longFormId));
        Assert.NotNull(await _context.InferencePresets.FindAsync(codeReviewId));
    }

    #endregion

    #region Preset Parameter Tests

    /// <summary>
    /// Verifies that Precise preset has low temperature.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_PrecisePresetHasLowTemperature()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var precise = await _context!.InferencePresets
            .FirstOrDefaultAsync(p => p.Name == "Precise");

        Assert.NotNull(precise);
        Assert.Equal(0.2f, precise.Temperature);
        Assert.Equal("Code", precise.Category);
    }

    /// <summary>
    /// Verifies that Creative preset has high temperature.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_CreativePresetHasHighTemperature()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var creative = await _context!.InferencePresets
            .FirstOrDefaultAsync(p => p.Name == "Creative");

        Assert.NotNull(creative);
        Assert.Equal(1.2f, creative.Temperature);
        Assert.Equal("Creative", creative.Category);
    }

    /// <summary>
    /// Verifies that Long-form preset has extended context size.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LongFormPresetHasExtendedContext()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var longForm = await _context!.InferencePresets
            .FirstOrDefaultAsync(p => p.Name == "Long-form");

        Assert.NotNull(longForm);
        Assert.Equal(16384, longForm.ContextSize);
        Assert.Equal(8192, longForm.MaxTokens);
    }

    #endregion

    #region Category Tests

    /// <summary>
    /// Verifies that system prompts have correct categories.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SystemPromptsHaveCorrectCategories()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var prompts = await _context!.SystemPrompts.ToListAsync();

        Assert.Contains(prompts, p => p.Category == "General");
        Assert.Contains(prompts, p => p.Category == "Creative");
        Assert.Contains(prompts, p => p.Category == "Code");
        Assert.Contains(prompts, p => p.Category == "Technical");
    }

    /// <summary>
    /// Verifies that inference presets have correct categories.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_InferencePresetsHaveCorrectCategories()
    {
        // Arrange
        SetupTestEnvironment();

        // Act
        await _initializer!.InitializeAsync();

        // Assert
        var presets = await _context!.InferencePresets.ToListAsync();

        Assert.Contains(presets, p => p.Category == "General");
        Assert.Contains(presets, p => p.Category == "Creative");
        Assert.Contains(presets, p => p.Category == "Code");
        Assert.Contains(presets, p => p.Category == "Technical");
    }

    #endregion

    #region Skip Seeding Tests

    /// <summary>
    /// Verifies that seeding is skipped when system prompts already exist.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SkipsSeedingWhenPromptsExist()
    {
        // Arrange
        SetupTestEnvironment();
        await _context!.Database.EnsureCreatedAsync();

        // Add a custom prompt before initialization
        var customPrompt = new SystemPromptEntity
        {
            Id = Guid.NewGuid(),
            Name = "Custom",
            Content = "Test",
            IsBuiltIn = false
        };
        _context.SystemPrompts.Add(customPrompt);
        await _context.SaveChangesAsync();

        // Act
        await _initializer!.InitializeAsync();

        // Assert - Should only have the custom prompt, not the seeded ones
        Assert.Single(_context.SystemPrompts);
        Assert.Equal("Custom", (await _context.SystemPrompts.FirstAsync()).Name);
    }

    /// <summary>
    /// Verifies that seeding is skipped when inference presets already exist.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_SkipsSeedingWhenPresetsExist()
    {
        // Arrange
        SetupTestEnvironment();
        await _context!.Database.EnsureCreatedAsync();

        // Add a custom preset before initialization
        var customPreset = new InferencePresetEntity
        {
            Id = Guid.NewGuid(),
            Name = "Custom",
            IsBuiltIn = false
        };
        _context.InferencePresets.Add(customPreset);
        await _context.SaveChangesAsync();

        // Act
        await _initializer!.InitializeAsync();

        // Assert - Should only have the custom preset, not the seeded ones
        Assert.Single(_context.InferencePresets);
        Assert.Equal("Custom", (await _context.InferencePresets.FirstAsync()).Name);
    }

    #endregion
}
