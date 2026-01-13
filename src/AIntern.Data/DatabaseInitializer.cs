using System.Diagnostics;
using AIntern.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIntern.Data;

/// <summary>
/// Handles database initialization, migrations, and seed data creation.
/// </summary>
/// <remarks>
/// <para>
/// This class is responsible for:
/// </para>
/// <list type="bullet">
///   <item><description>Creating the database file if it doesn't exist</description></item>
///   <item><description>Applying pending EF Core migrations</description></item>
///   <item><description>Seeding default system prompts and inference presets</description></item>
///   <item><description>Creating backups before potentially destructive operations</description></item>
/// </list>
/// <para>
/// <b>Logging Behavior:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Debug:</b> Entry/exit with timing, skip messages for existing data</description></item>
///   <item><description><b>Information:</b> Database path, migration count, seeding actions</description></item>
///   <item><description><b>Warning:</b> Backup failures, connection issues</description></item>
///   <item><description><b>Error:</b> Initialization failures with exception details</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is not thread-safe. Use scoped lifetime via DI.
/// </para>
/// <para>
/// Call <see cref="InitializeAsync"/> during application startup.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var scope = services.CreateScope();
/// var initializer = scope.ServiceProvider.GetRequiredService&lt;DatabaseInitializer&gt;();
/// await initializer.InitializeAsync();
/// </code>
/// </example>
public sealed class DatabaseInitializer
{
    private readonly AInternDbContext _context;
    private readonly DatabasePathResolver _pathResolver;
    private readonly ILogger<DatabaseInitializer> _logger;

    /// <summary>
    /// Creates a new instance of the database initializer.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="pathResolver">The database path resolver for file paths.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/>, <paramref name="pathResolver"/>, or <paramref name="logger"/> is null.
    /// </exception>
    public DatabaseInitializer(
        AInternDbContext context,
        DatabasePathResolver pathResolver,
        ILogger<DatabaseInitializer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _pathResolver = pathResolver ?? throw new ArgumentNullException(nameof(pathResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the database, applying migrations and seeding data.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the initialization operation.</returns>
    /// <remarks>
    /// <para>This method is idempotent - safe to call multiple times.</para>
    /// <para>Seed data is only inserted if the tables are empty.</para>
    /// </remarks>
    /// <exception cref="DatabaseInitializationException">
    /// Thrown when initialization fails due to migration or seeding errors.
    /// </exception>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "[ENTER] InitializeAsync - Database path: {DatabasePath}",
            _pathResolver.DatabasePath);

        try
        {
            // Check for pending migrations to determine initialization strategy
            var pendingMigrations = await _context.Database
                .GetPendingMigrationsAsync(ct);

            var pendingList = pendingMigrations.ToList();

            if (pendingList.Count > 0)
            {
                _logger.LogInformation(
                    "Applying {Count} pending migration(s): {Migrations}",
                    pendingList.Count,
                    string.Join(", ", pendingList));

                // Create backup before migration if database already exists.
                // This protects user data in case a migration fails or corrupts data.
                if (_pathResolver.DatabaseExists)
                {
                    await CreateBackupAsync(ct);
                }

                // Apply all pending migrations using EF Core's migration system.
                // This is preferred over EnsureCreated because it:
                // 1. Tracks migration history
                // 2. Supports incremental schema changes
                // 3. Enables rollback if needed
                await _context.Database.MigrateAsync(ct);

                _logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                _logger.LogDebug("No pending migrations found");

                // EnsureCreated handles fresh install when no migrations exist.
                // This will create the database and schema from the current model.
                // Note: EnsureCreated doesn't work with migrations - only use when
                // no migrations are pending (i.e., fresh install or already up-to-date).
                await _context.Database.EnsureCreatedAsync(ct);
            }

            // Seed default data after schema is ready
            await SeedDataAsync(ct);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] InitializeAsync - Complete in {DurationMs}ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "[EXIT] InitializeAsync - Failed after {DurationMs}ms: {Message}",
                stopwatch.ElapsedMilliseconds,
                ex.Message);

            throw new DatabaseInitializationException(
                "Failed to initialize database. See inner exception for details.",
                ex);
        }
    }

    /// <summary>
    /// Creates a backup of the current database.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The path to the backup file, or empty string if no backup was created.</returns>
    /// <remarks>
    /// <para>
    /// Backups are created before migrations to protect against data loss.
    /// The backup file includes a timestamp in the filename.
    /// </para>
    /// <para>
    /// This method handles connection management by closing and reopening
    /// the connection to allow file-level operations on SQLite.
    /// </para>
    /// </remarks>
    public async Task<string> CreateBackupAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var sourcePath = _pathResolver.DatabasePath;
        var backupPath = _pathResolver.GetBackupPath();

        _logger.LogDebug("[ENTER] CreateBackupAsync - Source: {Source}", sourcePath);

        if (!File.Exists(sourcePath))
        {
            _logger.LogDebug("[EXIT] CreateBackupAsync - No database file to backup");
            return string.Empty;
        }

        _logger.LogInformation("Creating database backup: {BackupPath}", backupPath);

        // Ensure any pending transaction is committed before backup.
        // An uncommitted transaction would leave the backup in an inconsistent state.
        if (_context.Database.CurrentTransaction is not null)
        {
            await _context.Database.CurrentTransaction.CommitAsync(ct);
        }

        // Close the connection to release file locks.
        // SQLite holds a lock on the database file while connected,
        // which would prevent the file copy operation on some platforms.
        await _context.Database.CloseConnectionAsync();

        try
        {
            // Copy the database file. We use overwrite: false to avoid
            // accidentally replacing an existing backup with the same timestamp.
            await Task.Run(() => File.Copy(sourcePath, backupPath, overwrite: false), ct);

            stopwatch.Stop();
            _logger.LogInformation(
                "[EXIT] CreateBackupAsync - Success in {DurationMs}ms",
                stopwatch.ElapsedMilliseconds);

            return backupPath;
        }
        catch (IOException ex) when (ex.Message.Contains("already exists"))
        {
            // Backup file already exists - this is fine, just log and continue
            _logger.LogWarning(
                "Backup file already exists, skipping: {BackupPath}",
                backupPath);
            return backupPath;
        }
        finally
        {
            // Reopen the connection for continued database operations.
            // This is important because the context may be used after backup.
            await _context.Database.OpenConnectionAsync(ct);
        }
    }

    /// <summary>
    /// Seeds default data if tables are empty.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// <para>
    /// This method is idempotent - it only inserts data if the relevant
    /// tables are empty. This allows safe re-execution on every startup.
    /// </para>
    /// </remarks>
    private async Task SeedDataAsync(CancellationToken ct)
    {
        _logger.LogDebug("[ENTER] SeedDataAsync");

        // Check for existing data before seeding.
        // This makes the operation idempotent - safe to call on every startup.
        var hasSystemPrompts = await _context.SystemPrompts.AnyAsync(ct);
        var hasInferencePresets = await _context.InferencePresets.AnyAsync(ct);

        if (!hasSystemPrompts)
        {
            _logger.LogInformation("Seeding default system prompts");
            await SeedSystemPromptsAsync(ct);
        }
        else
        {
            _logger.LogDebug("System prompts already exist, skipping seed");
        }

        if (!hasInferencePresets)
        {
            _logger.LogInformation("Seeding default inference presets");
            await SeedInferencePresetsAsync(ct);
        }
        else
        {
            _logger.LogDebug("Inference presets already exist, skipping seed");
        }

        _logger.LogDebug("[EXIT] SeedDataAsync");
    }

    /// <summary>
    /// Seeds the default system prompts.
    /// </summary>
    private async Task SeedSystemPromptsAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var prompts = CreateDefaultSystemPrompts();

        _context.SystemPrompts.AddRange(prompts);
        await _context.SaveChangesAsync(ct);

        stopwatch.Stop();
        _logger.LogDebug(
            "Seeded {Count} system prompts in {DurationMs}ms",
            prompts.Count,
            stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Seeds the default inference presets.
    /// </summary>
    private async Task SeedInferencePresetsAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var presets = CreateDefaultInferencePresets();

        _context.InferencePresets.AddRange(presets);
        await _context.SaveChangesAsync(ct);

        stopwatch.Stop();
        _logger.LogDebug(
            "Seeded {Count} inference presets in {DurationMs}ms",
            presets.Count,
            stopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Creates the list of default system prompts.
    /// </summary>
    /// <returns>List of system prompt entities to seed.</returns>
    /// <remarks>
    /// <para>
    /// Default prompts include a variety of personas for different use cases:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Default Assistant:</b> Balanced, helpful for general use (DEFAULT)</description></item>
    ///   <item><description><b>The Senior Intern:</b> Signature personality with wit</description></item>
    ///   <item><description><b>Code Expert:</b> Programming-focused assistance</description></item>
    ///   <item><description><b>Technical Writer:</b> Documentation specialist</description></item>
    ///   <item><description><b>Rubber Duck:</b> Debug through questioning</description></item>
    ///   <item><description><b>Socratic Tutor:</b> Teaching through questions</description></item>
    /// </list>
    /// </remarks>
    private static List<SystemPromptEntity> CreateDefaultSystemPrompts()
    {
        return
        [
            // Default Assistant - The default prompt for new conversations
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Default Assistant",
                Content = """
                    You are a helpful, harmless, and honest AI assistant. You provide clear,
                    accurate, and thoughtful responses to help users with their questions
                    and tasks. When you don't know something, you say so. When asked to do
                    something harmful or unethical, you politely decline.
                    """.TrimIndent(),
                Description = "A balanced, helpful assistant for general use",
                Category = "General",
                IsDefault = true,
                IsBuiltIn = true,
                IsActive = true,
                UsageCount = 0
            },

            // The Senior Intern - The signature personality
            new()
            {
                Id = Guid.NewGuid(),
                Name = "The Senior Intern",
                Content = """
                    You are "The Senior Intern" - an AI assistant with the knowledge of a senior
                    developer but the enthusiasm of a new hire. You're technically brilliant but
                    sometimes make sarcastic observations about code quality or architecture
                    decisions. You help users with coding tasks while occasionally dropping witty
                    remarks about the state of the codebase or industry trends.

                    Key traits:
                    - Technically accurate and thorough
                    - Occasionally sarcastic but never mean
                    - Enthusiastic about good practices
                    - Mildly judgmental about bad practices
                    - Uses programming humor when appropriate

                    Always prioritize being helpful over being funny. If the user seems frustrated
                    or the task is urgent, dial back the personality and focus on solutions.
                    """.TrimIndent(),
                Description = "The classic Senior Intern personality - helpful with a side of snark",
                Category = "Creative",
                IsDefault = false,
                IsBuiltIn = true,
                IsActive = true,
                UsageCount = 0
            },

            // Code Expert - Focused programming assistant
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Code Expert",
                Content = """
                    You are an expert software engineer and coding assistant. Focus on:
                    - Writing clean, efficient, and well-documented code
                    - Following best practices and design patterns
                    - Explaining technical concepts clearly
                    - Identifying bugs and suggesting improvements
                    - Providing complete, working code examples

                    When reviewing code, be thorough but constructive. Explain the "why"
                    behind your suggestions, not just the "what". Consider performance,
                    readability, maintainability, and security in your recommendations.
                    """.TrimIndent(),
                Description = "Focused on programming tasks and code quality",
                Category = "Code",
                IsDefault = false,
                IsBuiltIn = true,
                IsActive = true,
                UsageCount = 0
            },

            // Technical Writer - Documentation specialist
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Technical Writer",
                Content = """
                    You are a technical documentation specialist. Your goal is to create
                    clear, comprehensive, and well-organized documentation. Focus on:
                    - Clear explanations accessible to the target audience
                    - Proper structure with headers, lists, and code blocks
                    - Examples that illustrate key concepts
                    - Accurate technical details
                    - Consistent terminology and formatting

                    Adapt your writing style to the audience - more casual for tutorials,
                    more formal for API documentation. Always include practical examples.
                    """.TrimIndent(),
                Description = "Specialized in creating documentation and explanations",
                Category = "Technical",
                IsDefault = false,
                IsBuiltIn = true,
                IsActive = true,
                UsageCount = 0
            },

            // Rubber Duck - Debugging companion
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Rubber Duck",
                Content = """
                    You are a rubber duck debugger. Your role is to help users debug their
                    code by asking clarifying questions that lead them to find the solution
                    themselves. Instead of giving direct answers:
                    - Ask about their assumptions
                    - Question what they expect vs what happens
                    - Encourage them to walk through the code step by step
                    - Point out areas that might be worth investigating

                    Only provide direct solutions if the user explicitly asks or seems stuck
                    after several rounds of questions. The goal is to help them develop their
                    debugging skills, not just solve the immediate problem.
                    """.TrimIndent(),
                Description = "Helps debug by asking questions - like a rubber duck!",
                Category = "Code",
                IsDefault = false,
                IsBuiltIn = true,
                IsActive = true,
                UsageCount = 0
            },

            // Socratic Tutor - Educational assistant
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Socratic Tutor",
                Content = """
                    You are a Socratic tutor who teaches through questions. Rather than
                    giving direct answers:
                    - Ask guiding questions that lead to understanding
                    - Build on what the student already knows
                    - Encourage critical thinking
                    - Celebrate correct reasoning
                    - Gently redirect incorrect thinking

                    Your goal is to help the learner develop understanding, not just
                    memorize answers. Be patient and encouraging. Adjust your questions
                    based on the learner's responses and apparent skill level.
                    """.TrimIndent(),
                Description = "Teaches through questions to build understanding",
                Category = "General",
                IsDefault = false,
                IsBuiltIn = true,
                IsActive = true,
                UsageCount = 0
            }
        ];
    }

    /// <summary>
    /// Creates the list of default inference presets.
    /// </summary>
    /// <returns>List of inference preset entities to seed.</returns>
    /// <remarks>
    /// <para>
    /// Default presets cover common use cases with varying parameter combinations:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Balanced:</b> General conversation (DEFAULT) - Category: General</description></item>
    ///   <item><description><b>Precise:</b> Low temperature for factual responses - Category: Code</description></item>
    ///   <item><description><b>Creative:</b> High temperature for brainstorming - Category: Creative</description></item>
    ///   <item><description><b>Long-form:</b> Extended context for detailed work - Category: Technical</description></item>
    ///   <item><description><b>Code Review:</b> Optimized for code analysis - Category: Code</description></item>
    /// </list>
    /// <para>
    /// All presets use well-known GUIDs for stable references. These match the
    /// IDs defined in <c>InferencePreset.cs</c>.
    /// </para>
    /// <para>Updated in v0.2.3a to add Seed, Category, UsageCount, and Code Review preset.</para>
    /// </remarks>
    private static List<InferencePresetEntity> CreateDefaultInferencePresets()
    {
        return
        [
            // Balanced - Default preset for general use
            new()
            {
                Id = new Guid("00000001-0000-0000-0000-000000000002"),
                Name = "Balanced",
                Description = "Good balance of creativity and consistency. Recommended for general conversation and most tasks.",
                Category = "General",
                Temperature = 0.7f,
                TopP = 0.9f,
                TopK = 40,
                RepeatPenalty = 1.1f,
                Seed = -1,
                MaxTokens = 2048,
                ContextSize = 4096,
                IsDefault = true,
                IsBuiltIn = true,
                UsageCount = 0
            },

            // Precise - Low temperature for factual responses
            new()
            {
                Id = new Guid("00000001-0000-0000-0000-000000000001"),
                Name = "Precise",
                Description = "Low temperature for factual, consistent, and deterministic responses. Best for code generation and technical questions.",
                Category = "Code",
                Temperature = 0.2f,
                TopP = 0.8f,
                TopK = 20,
                RepeatPenalty = 1.1f,
                Seed = -1,
                MaxTokens = 1024,
                ContextSize = 4096,
                IsDefault = false,
                IsBuiltIn = true,
                UsageCount = 0
            },

            // Creative - High temperature for brainstorming
            new()
            {
                Id = new Guid("00000001-0000-0000-0000-000000000003"),
                Name = "Creative",
                Description = "High temperature for brainstorming, creative writing, and exploring diverse ideas. May produce more varied outputs.",
                Category = "Creative",
                Temperature = 1.2f,
                TopP = 0.95f,
                TopK = 60,
                RepeatPenalty = 1.05f,
                Seed = -1,
                MaxTokens = 4096,
                ContextSize = 8192,
                IsDefault = false,
                IsBuiltIn = true,
                UsageCount = 0
            },

            // Long-form - Extended context for detailed work
            new()
            {
                Id = new Guid("00000001-0000-0000-0000-000000000004"),
                Name = "Long-form",
                Description = "Extended context window and output length for detailed explanations, long documents, and complex conversations.",
                Category = "Technical",
                Temperature = 0.7f,
                TopP = 0.9f,
                TopK = 40,
                RepeatPenalty = 1.15f,
                Seed = -1,
                MaxTokens = 8192,
                ContextSize = 16384,
                IsDefault = false,
                IsBuiltIn = true,
                UsageCount = 0
            },

            // Code Review - Optimized for code analysis (added in v0.2.3a)
            new()
            {
                Id = new Guid("00000001-0000-0000-0000-000000000005"),
                Name = "Code Review",
                Description = "Optimized for code review and analysis. Low temperature for consistent feedback, extended context for reviewing larger files.",
                Category = "Code",
                Temperature = 0.3f,
                TopP = 0.85f,
                TopK = 30,
                RepeatPenalty = 1.1f,
                Seed = -1,
                MaxTokens = 2048,
                ContextSize = 8192,
                IsDefault = false,
                IsBuiltIn = true,
                UsageCount = 0
            }
        ];
    }
}
