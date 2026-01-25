using System.Diagnostics;
using AIntern.Core.Entities;
using AIntern.Core.Enums;
using AIntern.Core.Models;
using AIntern.Data.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIntern.Data;

/// <summary>
/// Entity Framework Core DbContext for the AIntern application.
/// Provides access to conversations, messages, system prompts, and inference presets.
/// </summary>
/// <remarks>
/// <para>
/// This DbContext implements the following key features:
/// </para>
/// <list type="bullet">
///   <item><description>Four DbSet properties for entity access</description></item>
///   <item><description>Automatic timestamp management (CreatedAt/UpdatedAt)</description></item>
///   <item><description>Configuration auto-discovery via ApplyConfigurationsFromAssembly</description></item>
///   <item><description>Comprehensive logging at all appropriate levels</description></item>
///   <item><description>Dual constructors for DI and design-time scenarios</description></item>
/// </list>
/// <para>
/// <b>Logging Behavior:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Debug:</b> Entry/exit for SaveChanges with timing and entity counts</description></item>
///   <item><description><b>Information:</b> Configuration loading, table mappings</description></item>
///   <item><description><b>Warning:</b> Design-time constructor usage</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is not thread-safe. Each request should use its own
/// instance via dependency injection with scoped lifetime.
/// </para>
/// <para>
/// The context automatically sets CreatedAt when entities are added and UpdatedAt when
/// entities are modified, ensuring consistent timestamp handling across the application.
/// </para>
/// </remarks>
/// <example>
/// Using with dependency injection:
/// <code>
/// services.AddDbContext&lt;AInternDbContext&gt;(options =>
///     options.UseSqlite(connectionString));
/// </code>
/// </example>
/// <example>
/// Using for testing with in-memory database:
/// <code>
/// var options = new DbContextOptionsBuilder&lt;AInternDbContext&gt;()
///     .UseSqlite("DataSource=:memory:")
///     .Options;
/// using var context = new AInternDbContext(options);
/// </code>
/// </example>
public class AInternDbContext : DbContext
{
    #region Fields

    /// <summary>
    /// Logger instance for diagnostic output.
    /// </summary>
    private readonly ILogger<AInternDbContext> _logger;

    #endregion

    #region FTS5 SQL Constants

    // ============================================================================
    // FTS5 Full-Text Search Infrastructure (v0.2.5a)
    // ============================================================================
    //
    // These SQL constants define the FTS5 virtual tables and triggers for full-text
    // search across conversations and messages.
    //
    // FTS5 (Full-Text Search 5) is SQLite's advanced full-text search engine that
    // provides:
    //   - Efficient tokenized indexing of text content
    //   - BM25 relevance ranking algorithm
    //   - Boolean query operators (AND, OR, NOT)
    //   - Prefix matching and phrase search
    //
    // External Content Tables:
    // We use the "external content" pattern where FTS5 indexes point to existing
    // tables rather than storing their own copy of the data. This:
    //   - Eliminates data duplication
    //   - Keeps source tables as the single source of truth
    //   - Requires triggers to maintain synchronization
    //
    // The content_rowid option maps FTS rowids to SQLite's internal rowid, which
    // is the most efficient way to join FTS results with source data.
    // ============================================================================

    /// <summary>
    /// SQL command to create the ConversationsFts virtual table.
    /// Indexes the Title column from the Conversations table.
    /// </summary>
    private const string CreateConversationsFtsSql = @"
        CREATE VIRTUAL TABLE IF NOT EXISTS ConversationsFts USING fts5(
            Title,
            content='Conversations',
            content_rowid='rowid'
        );";

    /// <summary>
    /// SQL command to create the MessagesFts virtual table.
    /// Indexes the Content column from the Messages table.
    /// </summary>
    private const string CreateMessagesFtsSql = @"
        CREATE VIRTUAL TABLE IF NOT EXISTS MessagesFts USING fts5(
            Content,
            content='Messages',
            content_rowid='rowid'
        );";

    // ============================================================================
    // Synchronization Triggers
    // ============================================================================
    //
    // External content FTS5 tables require triggers to stay synchronized with
    // their source tables. We need 3 triggers per table:
    //   - INSERT: Add new rows to the FTS index
    //   - UPDATE: Update changed content in the FTS index
    //   - DELETE: Remove deleted rows from the FTS index
    //
    // The UPDATE triggers only fire when the indexed column changes (Title for
    // conversations, Content for messages) to avoid unnecessary FTS updates.
    // ============================================================================

    /// <summary>
    /// SQL command to create the INSERT trigger for ConversationsFts.
    /// Automatically indexes new conversations when they are inserted.
    /// </summary>
    private const string CreateTrgConversationsInsertSql = @"
        CREATE TRIGGER IF NOT EXISTS trg_conversations_fts_insert
        AFTER INSERT ON Conversations
        BEGIN
            INSERT INTO ConversationsFts(rowid, Title)
            VALUES (NEW.rowid, NEW.Title);
        END;";

    /// <summary>
    /// SQL command to create the UPDATE trigger for ConversationsFts.
    /// Updates the FTS index when a conversation's Title changes.
    /// </summary>
    /// <remarks>
    /// FTS5 external content tables require special delete/insert syntax.
    /// The 'delete' command removes the old entry from the index,
    /// then we insert the new values.
    /// </remarks>
    private const string CreateTrgConversationsUpdateSql = @"
        CREATE TRIGGER IF NOT EXISTS trg_conversations_fts_update
        AFTER UPDATE OF Title ON Conversations
        BEGIN
            INSERT INTO ConversationsFts(ConversationsFts, rowid, Title) VALUES('delete', OLD.rowid, OLD.Title);
            INSERT INTO ConversationsFts(rowid, Title) VALUES (NEW.rowid, NEW.Title);
        END;";

    /// <summary>
    /// SQL command to create the DELETE trigger for ConversationsFts.
    /// Removes conversations from the FTS index when they are deleted.
    /// </summary>
    /// <remarks>
    /// FTS5 external content tables require the special 'delete' command
    /// that includes the old values being removed.
    /// </remarks>
    private const string CreateTrgConversationsDeleteSql = @"
        CREATE TRIGGER IF NOT EXISTS trg_conversations_fts_delete
        AFTER DELETE ON Conversations
        BEGIN
            INSERT INTO ConversationsFts(ConversationsFts, rowid, Title) VALUES('delete', OLD.rowid, OLD.Title);
        END;";

    /// <summary>
    /// SQL command to create the INSERT trigger for MessagesFts.
    /// Automatically indexes new messages when they are inserted.
    /// </summary>
    private const string CreateTrgMessagesInsertSql = @"
        CREATE TRIGGER IF NOT EXISTS trg_messages_fts_insert
        AFTER INSERT ON Messages
        BEGIN
            INSERT INTO MessagesFts(rowid, Content)
            VALUES (NEW.rowid, NEW.Content);
        END;";

    /// <summary>
    /// SQL command to create the UPDATE trigger for MessagesFts.
    /// Updates the FTS index when a message's Content changes.
    /// </summary>
    /// <remarks>
    /// FTS5 external content tables require special delete/insert syntax.
    /// The 'delete' command removes the old entry from the index,
    /// then we insert the new values.
    /// </remarks>
    private const string CreateTrgMessagesUpdateSql = @"
        CREATE TRIGGER IF NOT EXISTS trg_messages_fts_update
        AFTER UPDATE OF Content ON Messages
        BEGIN
            INSERT INTO MessagesFts(MessagesFts, rowid, Content) VALUES('delete', OLD.rowid, OLD.Content);
            INSERT INTO MessagesFts(rowid, Content) VALUES (NEW.rowid, NEW.Content);
        END;";

    /// <summary>
    /// SQL command to create the DELETE trigger for MessagesFts.
    /// Removes messages from the FTS index when they are deleted.
    /// </summary>
    /// <remarks>
    /// FTS5 external content tables require the special 'delete' command
    /// that includes the old values being removed.
    /// </remarks>
    private const string CreateTrgMessagesDeleteSql = @"
        CREATE TRIGGER IF NOT EXISTS trg_messages_fts_delete
        AFTER DELETE ON Messages
        BEGIN
            INSERT INTO MessagesFts(MessagesFts, rowid, Content) VALUES('delete', OLD.rowid, OLD.Content);
        END;";

    /// <summary>
    /// SQL command to rebuild the ConversationsFts index from source data.
    /// </summary>
    private const string RebuildConversationsFtsSql = @"
        INSERT INTO ConversationsFts(ConversationsFts) VALUES('rebuild');";

    /// <summary>
    /// SQL command to rebuild the MessagesFts index from source data.
    /// </summary>
    private const string RebuildMessagesFtsSql = @"
        INSERT INTO MessagesFts(MessagesFts) VALUES('rebuild');";

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of <see cref="AInternDbContext"/> with the specified options.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    /// <remarks>
    /// This constructor is used when the context is resolved via dependency injection.
    /// A <see cref="NullLogger{T}"/> is used when no logger is provided.
    /// </remarks>
    public AInternDbContext(DbContextOptions<AInternDbContext> options)
        : this(options, NullLogger<AInternDbContext>.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AInternDbContext"/> with options and logging.
    /// </summary>
    /// <param name="options">The options to configure the context.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <remarks>
    /// This constructor enables full logging support and is the primary constructor
    /// for production use with dependency injection.
    /// </remarks>
    public AInternDbContext(DbContextOptions<AInternDbContext> options, ILogger<AInternDbContext> logger)
        : base(options)
    {
        _logger = logger ?? NullLogger<AInternDbContext>.Instance;
        _logger.LogDebug("AInternDbContext instance created with options");
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AInternDbContext"/> for design-time scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This parameterless constructor is required for:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>EF Core design-time tools (migrations, scaffolding)</description></item>
    ///   <item><description>Unit testing without full DI setup</description></item>
    /// </list>
    /// <para>
    /// A warning is logged when this constructor is used in non-design-time scenarios.
    /// </para>
    /// </remarks>
    public AInternDbContext()
    {
        _logger = NullLogger<AInternDbContext>.Instance;
        _logger.LogWarning("AInternDbContext created with parameterless constructor (design-time fallback)");
    }

    #endregion

    #region DbSet Properties

    /// <summary>
    /// Gets the set of conversations in the database.
    /// </summary>
    /// <remarks>
    /// Conversations are the primary organizational unit for chat sessions.
    /// Each conversation contains an ordered collection of messages.
    /// </remarks>
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();

    /// <summary>
    /// Gets the set of messages in the database.
    /// </summary>
    /// <remarks>
    /// Messages belong to conversations and are ordered by SequenceNumber.
    /// Deleting a conversation cascades to delete all its messages.
    /// </remarks>
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();

    /// <summary>
    /// Gets the set of system prompts in the database.
    /// </summary>
    /// <remarks>
    /// System prompts are reusable templates that can be associated with conversations.
    /// When a system prompt is deleted, associated conversations have their FK set to null.
    /// </remarks>
    public DbSet<SystemPromptEntity> SystemPrompts => Set<SystemPromptEntity>();

    /// <summary>
    /// Gets the set of inference presets in the database.
    /// </summary>
    /// <remarks>
    /// Inference presets store saved configurations for model generation parameters
    /// such as temperature, top-p, and context size.
    /// </remarks>
    public DbSet<InferencePresetEntity> InferencePresets => Set<InferencePresetEntity>();

    /// <summary>
    /// Gets the set of application version records for migration tracking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Application versions track migration history. Each record represents a version
    /// the database was migrated to. The most recent record indicates the current version.
    /// </para>
    /// <para>Added in v0.2.5d.</para>
    /// </remarks>
    public DbSet<AppVersionEntity> AppVersions => Set<AppVersionEntity>();

    /// <summary>
    /// Gets the set of recent workspaces in the database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Workspaces store project folder state including open files and UI expansion.
    /// </para>
    /// <para>Added in v0.3.1b.</para>
    /// </remarks>
    public DbSet<RecentWorkspaceEntity> RecentWorkspaces => Set<RecentWorkspaceEntity>();

    /// <summary>
    /// Gets the set of file context history in the database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// File contexts track files attached to chat messages.
    /// Cascade deletes with conversation/message.
    /// </para>
    /// <para>Added in v0.3.1b.</para>
    /// </remarks>
    public DbSet<FileContextEntity> FileContextHistory => Set<FileContextEntity>();

    /// <summary>
    /// Gets the set of terminal command history entries in the database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Terminal history stores executed commands with metadata including
    /// exit code, duration, working directory, and session association.
    /// </para>
    /// <para>Added in v0.5.5i.</para>
    /// </remarks>
    public DbSet<TerminalHistoryEntity> TerminalHistory => Set<TerminalHistoryEntity>();

    #endregion

    #region DbContext Overrides

    /// <summary>
    /// Configures the model using Fluent API configurations.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure entities.</param>
    /// <remarks>
    /// <para>
    /// Entity configurations are automatically discovered from the assembly containing
    /// <see cref="ConversationConfiguration"/> using <c>ApplyConfigurationsFromAssembly</c>.
    /// </para>
    /// <para>
    /// This approach ensures all configurations in the <c>Configurations</c> namespace
    /// are applied without explicit registration.
    /// </para>
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _logger.LogDebug("[ENTER] OnModelCreating - Applying entity configurations");

        base.OnModelCreating(modelBuilder);

        // Use ApplyConfigurationsFromAssembly for automatic discovery of IEntityTypeConfiguration<T>.
        // This is preferred over explicit registration because:
        // 1. Adding new configurations doesn't require changes here
        // 2. Configurations are co-located with their responsibilities
        // 3. Reduces risk of forgetting to register new configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ConversationConfiguration).Assembly);

        _logger.LogInformation(
            "[EXIT] OnModelCreating - Configured tables: {Tables}",
            string.Join(", ", new[]
            {
                ConversationConfiguration.TableName,
                MessageConfiguration.TableName,
                SystemPromptConfiguration.TableName,
                InferencePresetConfiguration.TableName,
                AppVersionConfiguration.TableName,
                RecentWorkspaceConfiguration.TableName,
                FileContextConfiguration.TableName,
                TerminalHistoryConfiguration.TableName
            }));
    }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    /// <remarks>
    /// This override automatically manages CreatedAt and UpdatedAt timestamps
    /// for all entities that have these properties.
    /// </remarks>
    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    /// Indicates whether <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges"/>
    /// is called after the changes have been sent successfully to the database.
    /// </param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <remarks>
    /// This override automatically manages CreatedAt and UpdatedAt timestamps
    /// for all entities that have these properties.
    /// </remarks>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Update timestamps before saving to ensure consistent audit trail.
        // This is done synchronously for the sync SaveChanges path.
        UpdateTimestamps();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        
        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] SaveChanges - Saved {Count} changes in {DurationMs}ms",
            result, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    /// <summary>
    /// Asynchronously saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous save operation.
    /// The task result contains the number of state entries written to the database.
    /// </returns>
    /// <remarks>
    /// This override automatically manages CreatedAt and UpdatedAt timestamps
    /// for all entities that have these properties.
    /// </remarks>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
    }

    /// <summary>
    /// Asynchronously saves all changes made in this context to the database.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    /// Indicates whether <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges"/>
    /// is called after the changes have been sent successfully to the database.
    /// </param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous save operation.
    /// The task result contains the number of state entries written to the database.
    /// </returns>
    /// <remarks>
    /// This override automatically manages CreatedAt and UpdatedAt timestamps
    /// for all entities that have these properties.
    /// </remarks>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Update timestamps before saving to ensure consistent audit trail.
        // This ensures CreatedAt is set on new entities and UpdatedAt on modified ones.
        UpdateTimestamps();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        
        stopwatch.Stop();
        _logger.LogDebug(
            "[EXIT] SaveChangesAsync - Saved {Count} changes in {DurationMs}ms",
            result, stopwatch.ElapsedMilliseconds);
        
        return result;
    }

    #endregion

    #region FTS5 Search Infrastructure

    // ============================================================================
    // FTS5 Full-Text Search Public Methods (v0.2.5a)
    // ============================================================================
    //
    // These methods provide the public API for the FTS5 search infrastructure:
    //
    //   - EnsureFts5TablesAsync: Creates FTS5 tables and triggers (idempotent)
    //   - RebuildFts5IndexesAsync: Rebuilds indexes from source data
    //   - SearchAsync: Executes full-text search with BM25 ranking
    //
    // Usage Pattern:
    //   1. Call EnsureFts5TablesAsync() during application startup
    //   2. Triggers automatically maintain index synchronization
    //   3. Call SearchAsync() to execute searches
    //   4. Call RebuildFts5IndexesAsync() only if indexes become corrupted
    // ============================================================================

    /// <summary>
    /// Ensures FTS5 virtual tables and synchronization triggers exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// This method creates the FTS5 infrastructure if it doesn't exist:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>ConversationsFts virtual table (indexes Title)</description></item>
    ///   <item><description>MessagesFts virtual table (indexes Content)</description></item>
    ///   <item><description>6 synchronization triggers for automatic index maintenance</description></item>
    /// </list>
    /// <para>
    /// <b>Idempotency:</b> This method is safe to call multiple times. All CREATE
    /// statements use IF NOT EXISTS clauses.
    /// </para>
    /// <para>
    /// <b>External Content Tables:</b> FTS5 is configured with external content
    /// (content='TableName') to avoid data duplication. The triggers ensure the
    /// FTS indexes stay synchronized with the source tables.
    /// </para>
    /// <para>
    /// <b>Performance:</b> Creating the infrastructure is fast (typically &lt;10ms).
    /// The first call after creating new tables should be followed by
    /// <see cref="RebuildFts5IndexesAsync"/> if existing data needs indexing.
    /// </para>
    /// <para>Added in v0.2.5a.</para>
    /// </remarks>
    /// <example>
    /// Calling during application startup:
    /// <code>
    /// await dbContext.Database.EnsureCreatedAsync();
    /// await dbContext.EnsureFts5TablesAsync();
    /// </code>
    /// </example>
    public async Task EnsureFts5TablesAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] EnsureFts5TablesAsync - Creating FTS5 infrastructure");

        try
        {
            // Create FTS5 virtual tables
            // These use external content pointing to our source tables
            _logger.LogDebug("[INFO] Creating ConversationsFts virtual table");
            await Database.ExecuteSqlRawAsync(CreateConversationsFtsSql, cancellationToken);

            _logger.LogDebug("[INFO] Creating MessagesFts virtual table");
            await Database.ExecuteSqlRawAsync(CreateMessagesFtsSql, cancellationToken);

            // Create synchronization triggers (6 total: INSERT/UPDATE/DELETE for each table)
            // These ensure the FTS indexes stay in sync with source data
            _logger.LogDebug("[INFO] Creating Conversations synchronization triggers");
            await Database.ExecuteSqlRawAsync(CreateTrgConversationsInsertSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateTrgConversationsUpdateSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateTrgConversationsDeleteSql, cancellationToken);

            _logger.LogDebug("[INFO] Creating Messages synchronization triggers");
            await Database.ExecuteSqlRawAsync(CreateTrgMessagesInsertSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateTrgMessagesUpdateSql, cancellationToken);
            await Database.ExecuteSqlRawAsync(CreateTrgMessagesDeleteSql, cancellationToken);

            sw.Stop();
            _logger.LogInformation(
                "[EXIT] EnsureFts5TablesAsync - FTS5 infrastructure ready in {DurationMs}ms",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[ERROR] EnsureFts5TablesAsync - Failed to create FTS5 infrastructure after {DurationMs}ms",
                sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Rebuilds the FTS5 indexes from source table data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// Use this method when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>FTS tables were just created on existing data</description></item>
    ///   <item><description>FTS indexes become corrupted or out of sync</description></item>
    ///   <item><description>After bulk data imports that bypassed triggers</description></item>
    /// </list>
    /// <para>
    /// <b>Performance:</b> This operation scans all rows in Conversations and Messages.
    /// For large databases, this may take several seconds. Progress is not reported.
    /// </para>
    /// <para>
    /// <b>Prerequisites:</b> <see cref="EnsureFts5TablesAsync"/> must be called first
    /// to create the FTS5 tables.
    /// </para>
    /// <para>Added in v0.2.5a.</para>
    /// </remarks>
    /// <example>
    /// Rebuilding indexes after initial setup:
    /// <code>
    /// await dbContext.EnsureFts5TablesAsync();
    /// await dbContext.RebuildFts5IndexesAsync(); // Index existing data
    /// </code>
    /// </example>
    public async Task RebuildFts5IndexesAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] RebuildFts5IndexesAsync - Rebuilding FTS5 indexes from source data");

        try
        {
            // Rebuild ConversationsFts from Conversations table
            // The 'rebuild' command re-reads all content from the source table
            _logger.LogDebug("[INFO] Rebuilding ConversationsFts index");
            await Database.ExecuteSqlRawAsync(RebuildConversationsFtsSql, cancellationToken);

            // Rebuild MessagesFts from Messages table
            _logger.LogDebug("[INFO] Rebuilding MessagesFts index");
            await Database.ExecuteSqlRawAsync(RebuildMessagesFtsSql, cancellationToken);

            sw.Stop();
            _logger.LogInformation(
                "[EXIT] RebuildFts5IndexesAsync - FTS5 indexes rebuilt in {DurationMs}ms",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[ERROR] RebuildFts5IndexesAsync - Failed to rebuild FTS5 indexes after {DurationMs}ms",
                sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Executes a full-text search across conversations and messages.
    /// </summary>
    /// <param name="query">The search query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results with relevance ranking.</returns>
    /// <remarks>
    /// <para>
    /// This method performs a full-text search using SQLite FTS5's MATCH operator
    /// and BM25 ranking algorithm. Results are ordered by relevance.
    /// </para>
    /// <para>
    /// <b>Query Processing:</b>
    /// </para>
    /// <list type="number">
    ///   <item><description>Validates query parameters</description></item>
    ///   <item><description>Escapes special FTS5 characters in query text</description></item>
    ///   <item><description>Executes separate queries against ConversationsFts and MessagesFts</description></item>
    ///   <item><description>Joins with source tables for full entity data</description></item>
    ///   <item><description>Merges and orders by BM25 rank (ascending, lower = better match)</description></item>
    /// </list>
    /// <para>
    /// <b>FTS5 Query Syntax:</b> The query text supports FTS5 syntax:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Simple terms: <c>hello world</c> (implicit AND)</description></item>
    ///   <item><description>Phrases: <c>"hello world"</c> (exact phrase)</description></item>
    ///   <item><description>Prefix: <c>hel*</c> (prefix matching)</description></item>
    ///   <item><description>Boolean: <c>hello OR world</c></description></item>
    /// </list>
    /// <para>
    /// <b>Performance:</b> FTS5 queries are optimized for speed. Typical searches
    /// complete in under 10ms even with thousands of messages.
    /// </para>
    /// <para>Added in v0.2.5a.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">When <paramref name="query"/> is null.</exception>
    /// <example>
    /// Searching for content:
    /// <code>
    /// var results = await dbContext.SearchAsync(SearchQuery.Simple("machine learning"));
    ///
    /// foreach (var result in results.Results)
    /// {
    ///     Console.WriteLine($"{result.TypeLabel}: {result.Title} (rank: {result.Rank})");
    /// }
    /// </code>
    /// </example>
    public async Task<SearchResults> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sw = Stopwatch.StartNew();
        _logger.LogDebug("[ENTER] SearchAsync - {LogSummary}", query.LogSummary);

        // Early exit for invalid queries
        if (!query.IsValid)
        {
            _logger.LogDebug("[SKIP] SearchAsync - Empty query, returning empty results");
            return SearchResults.Empty(query);
        }

        if (!query.HasContentTypeFilter)
        {
            _logger.LogDebug("[SKIP] SearchAsync - No content types selected, returning empty results");
            return SearchResults.Empty(query);
        }

        try
        {
            var results = new List<SearchResult>();

            // Escape the query for FTS5 to prevent syntax errors
            var escapedQuery = EscapeFts5Query(query.NormalizedQueryText);
            _logger.LogDebug("[INFO] Escaped query: '{EscapedQuery}'", escapedQuery);

            // Search conversations if requested
            if (query.IncludeConversations)
            {
                _logger.LogDebug("[INFO] Searching ConversationsFts");
                var conversationResults = await SearchConversationsAsync(
                    escapedQuery, query.MaxResults, query.MinRank, cancellationToken);
                results.AddRange(conversationResults);
                _logger.LogDebug("[INFO] Found {Count} conversation matches", conversationResults.Count);
            }

            // Search messages if requested
            if (query.IncludeMessages)
            {
                _logger.LogDebug("[INFO] Searching MessagesFts");
                var messageResults = await SearchMessagesAsync(
                    escapedQuery, query.MaxResults, query.MinRank, cancellationToken);
                results.AddRange(messageResults);
                _logger.LogDebug("[INFO] Found {Count} message matches", messageResults.Count);
            }

            // Sort all results by relevance (lower rank = better match in BM25)
            // Then take only MaxResults from the combined set
            var totalCount = results.Count;
            var sortedResults = results
                .OrderBy(r => r.Rank)
                .Take(query.MaxResults)
                .ToList();

            sw.Stop();
            var searchResults = new SearchResults(
                sortedResults,
                TotalCount: totalCount,
                Query: query,
                SearchDuration: sw.Elapsed);

            _logger.LogInformation(
                "[EXIT] SearchAsync - {Summary}",
                searchResults.Summary);

            return searchResults;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[ERROR] SearchAsync - Search failed for query '{Query}' after {DurationMs}ms",
                query.NormalizedQueryText, sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Searches the ConversationsFts index for matching conversation titles.
    /// </summary>
    /// <param name="escapedQuery">The FTS5-escaped search query.</param>
    /// <param name="maxResults">Maximum results to return.</param>
    /// <param name="minRank">Minimum BM25 rank threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of search results for matching conversations.</returns>
    private async Task<List<SearchResult>> SearchConversationsAsync(
        string escapedQuery,
        int maxResults,
        double minRank,
        CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();

        // Use raw SQL to execute FTS5 MATCH query with BM25 ranking
        // The bm25() function returns negative values where more negative = better match
        var sql = @"
            SELECT c.Id, c.Title, c.UpdatedAt, bm25(ConversationsFts) as Rank
            FROM ConversationsFts
            INNER JOIN Conversations c ON ConversationsFts.rowid = c.rowid
            WHERE ConversationsFts MATCH @query
              AND bm25(ConversationsFts) >= @minRank
            ORDER BY Rank ASC
            LIMIT @maxResults";

        var connection = Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            // Add parameters to prevent SQL injection
            var queryParam = command.CreateParameter();
            queryParam.ParameterName = "@query";
            queryParam.Value = escapedQuery;
            command.Parameters.Add(queryParam);

            var minRankParam = command.CreateParameter();
            minRankParam.ParameterName = "@minRank";
            minRankParam.Value = minRank;
            command.Parameters.Add(minRankParam);

            var maxResultsParam = command.CreateParameter();
            maxResultsParam.ParameterName = "@maxResults";
            maxResultsParam.Value = maxResults;
            command.Parameters.Add(maxResultsParam);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var title = reader.GetString(1);
                var updatedAt = reader.GetDateTime(2);
                var rank = reader.GetDouble(3);

                results.Add(new SearchResult(
                    Id: id,
                    ResultType: SearchResultType.Conversation,
                    Title: title,
                    Preview: title, // For conversations, preview is the title itself
                    Rank: rank,
                    Timestamp: updatedAt,
                    ConversationId: id,
                    MessageId: null));
            }
        }
        finally
        {
            // Don't close the connection as EF Core manages it
        }

        return results;
    }

    /// <summary>
    /// Searches the MessagesFts index for matching message content.
    /// </summary>
    /// <param name="escapedQuery">The FTS5-escaped search query.</param>
    /// <param name="maxResults">Maximum results to return.</param>
    /// <param name="minRank">Minimum BM25 rank threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of search results for matching messages.</returns>
    private async Task<List<SearchResult>> SearchMessagesAsync(
        string escapedQuery,
        int maxResults,
        double minRank,
        CancellationToken cancellationToken)
    {
        var results = new List<SearchResult>();

        // Use raw SQL to execute FTS5 MATCH query with BM25 ranking
        // Use snippet() to generate a preview with matched terms highlighted
        // snippet(fts_table, column_index, start_marker, end_marker, ellipsis, max_tokens)
        var sql = @"
            SELECT m.Id, c.Title, m.Timestamp, m.ConversationId, bm25(MessagesFts) as Rank,
                   snippet(MessagesFts, 0, '<mark>', '</mark>', '...', 32) as Preview
            FROM MessagesFts
            INNER JOIN Messages m ON MessagesFts.rowid = m.rowid
            INNER JOIN Conversations c ON m.ConversationId = c.Id
            WHERE MessagesFts MATCH @query
              AND bm25(MessagesFts) >= @minRank
            ORDER BY Rank ASC
            LIMIT @maxResults";

        var connection = Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            // Add parameters to prevent SQL injection
            var queryParam = command.CreateParameter();
            queryParam.ParameterName = "@query";
            queryParam.Value = escapedQuery;
            command.Parameters.Add(queryParam);

            var minRankParam = command.CreateParameter();
            minRankParam.ParameterName = "@minRank";
            minRankParam.Value = minRank;
            command.Parameters.Add(minRankParam);

            var maxResultsParam = command.CreateParameter();
            maxResultsParam.ParameterName = "@maxResults";
            maxResultsParam.Value = maxResults;
            command.Parameters.Add(maxResultsParam);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var title = reader.GetString(1);
                var timestamp = reader.GetDateTime(2);
                var conversationId = reader.GetGuid(3);
                var rank = reader.GetDouble(4);
                var preview = reader.GetString(5);

                results.Add(new SearchResult(
                    Id: id,
                    ResultType: SearchResultType.Message,
                    Title: title, // Parent conversation title
                    Preview: preview, // Snippet with highlighted matches
                    Rank: rank,
                    Timestamp: timestamp,
                    ConversationId: conversationId,
                    MessageId: id));
            }
        }
        finally
        {
            // Don't close the connection as EF Core manages it
        }

        return results;
    }

    /// <summary>
    /// Escapes special FTS5 query characters to prevent syntax errors.
    /// </summary>
    /// <param name="query">The raw query text from the user.</param>
    /// <returns>The escaped query safe for FTS5 MATCH.</returns>
    /// <remarks>
    /// <para>
    /// FTS5 query syntax has special characters that need escaping:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>"</c> - Quote for phrases</description></item>
    ///   <item><description><c>*</c> - Prefix operator</description></item>
    ///   <item><description><c>^</c> - Column filter</description></item>
    ///   <item><description><c>:</c> - Column filter separator</description></item>
    ///   <item><description><c>()</c> - Grouping</description></item>
    ///   <item><description><c>-</c> - NOT operator (at word start)</description></item>
    /// </list>
    /// <para>
    /// This method wraps the query in quotes to treat it as a literal phrase,
    /// with any internal quotes escaped by doubling them.
    /// </para>
    /// </remarks>
    private static string EscapeFts5Query(string query)
    {
        // Simple approach: escape double quotes by doubling them
        // and wrap in quotes for phrase matching
        // This handles most user input safely while preserving basic search
        var escaped = query.Replace("\"", "\"\"");

        // For simple single-word queries, don't wrap in quotes to allow prefix matching
        // For multi-word queries or queries with special chars, wrap in quotes
        if (!query.Contains(' ') && !query.Contains('"') && !query.Contains('*'))
        {
            // Add wildcard suffix for prefix matching on single words
            return escaped + "*";
        }

        // Wrap multi-word queries in quotes for phrase matching
        return $"\"{escaped}\"";
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Updates CreatedAt and UpdatedAt timestamps for tracked entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For added entities (EntityState.Added):
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Sets CreatedAt to current UTC time if it's default</description></item>
    ///   <item><description>Sets UpdatedAt to match CreatedAt</description></item>
    /// </list>
    /// <para>
    /// For modified entities (EntityState.Modified):
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Sets UpdatedAt to current UTC time</description></item>
    /// </list>
    /// <para>
    /// This method handles ConversationEntity, SystemPromptEntity, and InferencePresetEntity.
    /// MessageEntity uses Timestamp instead of CreatedAt/UpdatedAt pattern.
    /// </para>
    /// </remarks>
    private void UpdateTimestamps()
    {
        var now = DateTime.UtcNow;
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var addedCount = 0;
        var modifiedCount = 0;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                addedCount++;
                SetCreatedTimestamp(entry.Entity, now);
            }

            if (entry.State == EntityState.Modified)
            {
                modifiedCount++;
            }

            SetUpdatedTimestamp(entry.Entity, now);
        }

        if (addedCount > 0 || modifiedCount > 0)
        {
            _logger.LogDebug(
                "Updated timestamps for {AddedCount} added and {ModifiedCount} modified entities",
                addedCount,
                modifiedCount);
        }
    }

    /// <summary>
    /// Sets the CreatedAt timestamp for an entity if applicable.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="timestamp">The timestamp to set.</param>
    private static void SetCreatedTimestamp(object entity, DateTime timestamp)
    {
        switch (entity)
        {
            case ConversationEntity conversation when conversation.CreatedAt == default:
                conversation.CreatedAt = timestamp;
                break;

            case SystemPromptEntity prompt when prompt.CreatedAt == default:
                prompt.CreatedAt = timestamp;
                break;

            case InferencePresetEntity preset when preset.CreatedAt == default:
                preset.CreatedAt = timestamp;
                break;

            case MessageEntity message when message.Timestamp == default:
                message.Timestamp = timestamp;
                break;
        }
    }

    /// <summary>
    /// Sets the UpdatedAt timestamp for an entity if applicable.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="timestamp">The timestamp to set.</param>
    private static void SetUpdatedTimestamp(object entity, DateTime timestamp)
    {
        switch (entity)
        {
            case ConversationEntity conversation:
                conversation.UpdatedAt = timestamp;
                break;

            case SystemPromptEntity prompt:
                prompt.UpdatedAt = timestamp;
                break;

            case InferencePresetEntity preset:
                preset.UpdatedAt = timestamp;
                break;

            // MessageEntity doesn't have UpdatedAt - it uses EditedAt which is set explicitly
        }
    }

    #endregion
}
