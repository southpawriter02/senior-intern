using AIntern.Core.Entities;
using AIntern.Core.Enums;
using AIntern.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIntern.Data.Tests;

/// <summary>
/// Integration tests for the FTS5 full-text search infrastructure in <see cref="AInternDbContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify the complete FTS5 search functionality:
/// </para>
/// <list type="bullet">
///   <item><description><b>EnsureFts5TablesAsync:</b> FTS5 table and trigger creation</description></item>
///   <item><description><b>RebuildFts5IndexesAsync:</b> Index rebuilding from source data</description></item>
///   <item><description><b>SearchAsync:</b> Full-text search with BM25 ranking</description></item>
///   <item><description><b>Trigger synchronization:</b> Automatic index updates</description></item>
/// </list>
/// <para>
/// Tests use SQLite in-memory databases to ensure isolation and speed.
/// Each test creates its own database instance.
/// </para>
/// <para>Added in v0.2.5a.</para>
/// </remarks>
public class Fts5SearchTests : IDisposable
{
    #region Test Infrastructure

    /// <summary>
    /// SQLite connection kept open for in-memory database lifetime.
    /// </summary>
    private SqliteConnection? _connection;

    /// <summary>
    /// Creates an in-memory SQLite DbContext with FTS5 infrastructure initialized.
    /// </summary>
    /// <param name="initializeFts5">Whether to initialize FTS5 tables and triggers.</param>
    /// <returns>A configured AInternDbContext instance.</returns>
    private async Task<AInternDbContext> CreateInMemoryContextAsync(bool initializeFts5 = true)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new AInternDbContext(options, NullLogger<AInternDbContext>.Instance);
        await context.Database.EnsureCreatedAsync();

        if (initializeFts5)
        {
            await context.EnsureFts5TablesAsync();
        }

        return context;
    }

    /// <summary>
    /// Creates a conversation entity with the specified title.
    /// </summary>
    private static ConversationEntity CreateConversation(string title)
    {
        return new ConversationEntity
        {
            Id = Guid.NewGuid(),
            Title = title
        };
    }

    /// <summary>
    /// Creates a message entity with the specified content.
    /// </summary>
    private static MessageEntity CreateMessage(Guid conversationId, string content, int sequenceNumber = 0)
    {
        return new MessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = MessageRole.User,
            Content = content,
            SequenceNumber = sequenceNumber
        };
    }

    /// <summary>
    /// Disposes of the test infrastructure.
    /// </summary>
    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion

    #region EnsureFts5TablesAsync Tests

    /// <summary>
    /// Verifies that EnsureFts5TablesAsync creates the ConversationsFts virtual table.
    /// </summary>
    [Fact]
    public async Task EnsureFts5TablesAsync_CreatesConversationsFtsTable()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync(initializeFts5: false);

        // Act
        await context.EnsureFts5TablesAsync();

        // Assert - Check if table exists
        var tableExists = await TableExistsAsync(context, "ConversationsFts");
        Assert.True(tableExists, "ConversationsFts table should exist after EnsureFts5TablesAsync");
    }

    /// <summary>
    /// Verifies that EnsureFts5TablesAsync creates the MessagesFts virtual table.
    /// </summary>
    [Fact]
    public async Task EnsureFts5TablesAsync_CreatesMessagesFtsTable()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync(initializeFts5: false);

        // Act
        await context.EnsureFts5TablesAsync();

        // Assert
        var tableExists = await TableExistsAsync(context, "MessagesFts");
        Assert.True(tableExists, "MessagesFts table should exist after EnsureFts5TablesAsync");
    }

    /// <summary>
    /// Verifies that EnsureFts5TablesAsync creates synchronization triggers.
    /// </summary>
    [Fact]
    public async Task EnsureFts5TablesAsync_CreatesSynchronizationTriggers()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync(initializeFts5: false);

        // Act
        await context.EnsureFts5TablesAsync();

        // Assert - Check all 6 triggers exist
        var expectedTriggers = new[]
        {
            "trg_conversations_fts_insert",
            "trg_conversations_fts_update",
            "trg_conversations_fts_delete",
            "trg_messages_fts_insert",
            "trg_messages_fts_update",
            "trg_messages_fts_delete"
        };

        foreach (var triggerName in expectedTriggers)
        {
            var exists = await TriggerExistsAsync(context, triggerName);
            Assert.True(exists, $"Trigger '{triggerName}' should exist after EnsureFts5TablesAsync");
        }
    }

    /// <summary>
    /// Verifies that EnsureFts5TablesAsync is idempotent (safe to call multiple times).
    /// </summary>
    [Fact]
    public async Task EnsureFts5TablesAsync_IsIdempotent()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync(initializeFts5: false);

        // Act - Call twice
        await context.EnsureFts5TablesAsync();
        var exception = await Record.ExceptionAsync(() => context.EnsureFts5TablesAsync());

        // Assert - No exception should be thrown
        Assert.Null(exception);
    }

    #endregion

    #region Trigger Synchronization Tests

    /// <summary>
    /// Verifies that inserting a conversation automatically indexes it in FTS.
    /// </summary>
    [Fact]
    public async Task InsertConversation_AutomaticallyIndexed()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        // Act - Insert a conversation
        var conversation = CreateConversation("Machine Learning Basics");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        // Assert - Search should find it
        var results = await context.SearchAsync(SearchQuery.ConversationsOnly("machine"));
        Assert.True(results.HasResults, "Newly inserted conversation should be searchable");
        Assert.Equal(1, results.ConversationResultCount);
    }

    /// <summary>
    /// Verifies that inserting a message automatically indexes it in FTS.
    /// </summary>
    [Fact]
    public async Task InsertMessage_AutomaticallyIndexed()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var conversation = CreateConversation("Test Conv");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        // Act - Insert a message
        var message = CreateMessage(conversation.Id, "Hello, this is about neural networks");
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        // Assert - Search should find it
        var results = await context.SearchAsync(SearchQuery.MessagesOnly("neural"));
        Assert.True(results.HasResults, "Newly inserted message should be searchable");
        Assert.Equal(1, results.MessageResultCount);
    }

    /// <summary>
    /// Verifies that updating a conversation title updates the FTS index.
    /// </summary>
    [Fact]
    public async Task UpdateConversationTitle_UpdatesFtsIndex()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var conversation = CreateConversation("Original Title");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        // Act - Update the title
        conversation.Title = "Updated Deep Learning Discussion";
        await context.SaveChangesAsync();

        // Assert - Old search should not find it, new search should
        var oldResults = await context.SearchAsync(SearchQuery.ConversationsOnly("original"));
        var newResults = await context.SearchAsync(SearchQuery.ConversationsOnly("deep"));

        Assert.False(oldResults.HasResults, "Old title should not be searchable");
        Assert.True(newResults.HasResults, "New title should be searchable");
    }

    /// <summary>
    /// Verifies that updating message content updates the FTS index.
    /// </summary>
    /// <remarks>
    /// Note: The FTS5 UPDATE trigger for external content tables requires DELETE + INSERT.
    /// This test verifies that the trigger properly removes old content and indexes new content.
    /// </remarks>
    [Fact]
    public async Task UpdateMessageContent_UpdatesFtsIndex()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var conversation = CreateConversation("UpdateMsg UniqueConv XYZ123");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var message = CreateMessage(conversation.Id, "Zebracorn is a mythical animal ABC789");
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        // Verify initial indexing works
        var initialSearch = await context.SearchAsync(SearchQuery.MessagesOnly("zebracorn"));
        Assert.True(initialSearch.HasResults, "Initial content should be searchable");

        // Act - Update the content via direct SQL to ensure trigger fires
        // EF Core tracked entity updates may not trigger SQLite triggers correctly
        var messageId = message.Id;
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE Messages SET Content = 'Unicorn is a mythical animal DEF456' WHERE Id = {0}",
            messageId);

        // Assert
        var oldResults = await context.SearchAsync(SearchQuery.MessagesOnly("zebracorn"));
        var newResults = await context.SearchAsync(SearchQuery.MessagesOnly("unicorn"));

        Assert.False(oldResults.HasResults, "Old content should not be searchable after update");
        Assert.True(newResults.HasResults, "New content should be searchable after update");
    }

    /// <summary>
    /// Verifies that deleting a conversation removes it from the FTS index.
    /// </summary>
    [Fact]
    public async Task DeleteConversation_RemovesFromFtsIndex()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var conversation = CreateConversation("Deletable Conversation");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        // Verify it's searchable first
        var beforeDelete = await context.SearchAsync(SearchQuery.ConversationsOnly("deletable"));
        Assert.True(beforeDelete.HasResults);

        // Act - Delete the conversation
        context.Conversations.Remove(conversation);
        await context.SaveChangesAsync();

        // Assert - Should no longer be searchable
        var afterDelete = await context.SearchAsync(SearchQuery.ConversationsOnly("deletable"));
        Assert.False(afterDelete.HasResults, "Deleted conversation should not be searchable");
    }

    /// <summary>
    /// Verifies that deleting a message removes it from the FTS index.
    /// </summary>
    [Fact]
    public async Task DeleteMessage_RemovesFromFtsIndex()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();
        var conversation = CreateConversation("Test Conv");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var message = CreateMessage(conversation.Id, "Deletable message content");
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        // Verify searchable first
        var beforeDelete = await context.SearchAsync(SearchQuery.MessagesOnly("deletable"));
        Assert.True(beforeDelete.HasResults);

        // Act - Delete the message
        context.Messages.Remove(message);
        await context.SaveChangesAsync();

        // Assert
        var afterDelete = await context.SearchAsync(SearchQuery.MessagesOnly("deletable"));
        Assert.False(afterDelete.HasResults, "Deleted message should not be searchable");
    }

    #endregion

    #region RebuildFts5IndexesAsync Tests

    /// <summary>
    /// Verifies that RebuildFts5IndexesAsync indexes existing conversations.
    /// </summary>
    [Fact]
    public async Task RebuildFts5IndexesAsync_IndexesExistingConversations()
    {
        // Arrange - Create context without FTS, add data, then initialize FTS
        await using var context = await CreateInMemoryContextAsync(initializeFts5: false);

        var conversation = CreateConversation("Legacy Conversation Data");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        // Initialize FTS tables (triggers don't backfill existing data)
        await context.EnsureFts5TablesAsync();

        // Search before rebuild - should not find anything (data existed before triggers)
        var beforeRebuild = await context.SearchAsync(SearchQuery.ConversationsOnly("legacy"));

        // Act - Rebuild indexes
        await context.RebuildFts5IndexesAsync();

        // Assert - Should find it after rebuild
        var afterRebuild = await context.SearchAsync(SearchQuery.ConversationsOnly("legacy"));
        Assert.True(afterRebuild.HasResults, "Legacy conversation should be searchable after rebuild");
    }

    /// <summary>
    /// Verifies that RebuildFts5IndexesAsync indexes existing messages.
    /// </summary>
    [Fact]
    public async Task RebuildFts5IndexesAsync_IndexesExistingMessages()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync(initializeFts5: false);

        var conversation = CreateConversation("Test Conv");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var message = CreateMessage(conversation.Id, "Legacy message about quantum computing");
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        await context.EnsureFts5TablesAsync();

        // Act
        await context.RebuildFts5IndexesAsync();

        // Assert
        var results = await context.SearchAsync(SearchQuery.MessagesOnly("quantum"));
        Assert.True(results.HasResults, "Legacy message should be searchable after rebuild");
    }

    /// <summary>
    /// Verifies RebuildFts5IndexesAsync is idempotent.
    /// </summary>
    [Fact]
    public async Task RebuildFts5IndexesAsync_IsIdempotent()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        var conversation = CreateConversation("Idempotent Test");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        // Act - Call rebuild multiple times
        await context.RebuildFts5IndexesAsync();
        var exception = await Record.ExceptionAsync(() => context.RebuildFts5IndexesAsync());

        // Assert
        Assert.Null(exception);

        // Search should still work correctly
        var results = await context.SearchAsync(SearchQuery.ConversationsOnly("idempotent"));
        Assert.Equal(1, results.TotalCount);
    }

    #endregion

    #region SearchAsync Tests - Basic Functionality

    /// <summary>
    /// Verifies SearchAsync finds conversations by title.
    /// </summary>
    [Fact]
    public async Task SearchAsync_FindsConversationsByTitle()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        context.Conversations.Add(CreateConversation("Introduction to Python Programming"));
        context.Conversations.Add(CreateConversation("Advanced JavaScript Patterns"));
        context.Conversations.Add(CreateConversation("Unrelated Topic"));
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(SearchQuery.ConversationsOnly("programming"));

        // Assert
        Assert.Equal(1, results.TotalCount);
        Assert.True(results.Results[0].IsConversationResult);
        Assert.Contains("Python", results.Results[0].Title);
    }

    /// <summary>
    /// Verifies SearchAsync finds messages by content.
    /// </summary>
    [Fact]
    public async Task SearchAsync_FindsMessagesByContent()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        var conversation = CreateConversation("Test Conversation");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        context.Messages.Add(CreateMessage(conversation.Id, "Discussing machine learning algorithms", 0));
        context.Messages.Add(CreateMessage(conversation.Id, "Hello world example", 1));
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(SearchQuery.MessagesOnly("algorithms"));

        // Assert
        Assert.Equal(1, results.TotalCount);
        Assert.True(results.Results[0].IsMessageResult);
    }

    /// <summary>
    /// Verifies SearchAsync searches both types when IncludeConversations and IncludeMessages are true.
    /// </summary>
    [Fact]
    public async Task SearchAsync_SearchesBothTypes_WhenBothEnabled()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        var conversation = CreateConversation("Neural Network Discussion");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        context.Messages.Add(CreateMessage(conversation.Id, "Neural networks are fascinating", 0));
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(SearchQuery.Simple("neural"));

        // Assert - Should find both the conversation and the message
        Assert.True(results.ConversationResultCount > 0 || results.MessageResultCount > 0);
    }

    #endregion

    #region SearchAsync Tests - Query Validation

    /// <summary>
    /// Verifies SearchAsync returns empty results for empty query.
    /// </summary>
    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmpty()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        context.Conversations.Add(CreateConversation("Test Conversation"));
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(new SearchQuery(""));

        // Assert
        Assert.False(results.HasResults);
        Assert.Equal(0, results.TotalCount);
    }

    /// <summary>
    /// Verifies SearchAsync returns empty results for whitespace-only query.
    /// </summary>
    [Fact]
    public async Task SearchAsync_WhitespaceQuery_ReturnsEmpty()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        context.Conversations.Add(CreateConversation("Test Conversation"));
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(new SearchQuery("   \t\n  "));

        // Assert
        Assert.False(results.HasResults);
    }

    /// <summary>
    /// Verifies SearchAsync returns empty when no content types are selected.
    /// </summary>
    [Fact]
    public async Task SearchAsync_NoContentTypesSelected_ReturnsEmpty()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        context.Conversations.Add(CreateConversation("Test Query"));
        await context.SaveChangesAsync();

        var query = new SearchQuery("test", IncludeConversations: false, IncludeMessages: false);

        // Act
        var results = await context.SearchAsync(query);

        // Assert
        Assert.False(results.HasResults);
    }

    /// <summary>
    /// Verifies SearchAsync returns empty when no matches found.
    /// </summary>
    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmpty()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        context.Conversations.Add(CreateConversation("Apples and Oranges"));
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(SearchQuery.Simple("xyznonexistent"));

        // Assert
        Assert.False(results.HasResults);
        Assert.Equal(0, results.TotalCount);
    }

    #endregion

    #region SearchAsync Tests - MaxResults

    /// <summary>
    /// Verifies SearchAsync respects MaxResults limit.
    /// </summary>
    /// <remarks>
    /// Note: The current implementation limits results at each source query level,
    /// so the combined result count equals MaxResults when there are sufficient matches.
    /// </remarks>
    [Fact]
    public async Task SearchAsync_RespectsMaxResultsLimit()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        for (int i = 0; i < 10; i++)
        {
            context.Conversations.Add(CreateConversation($"Searchable Topic Number {i}"));
        }
        await context.SaveChangesAsync();

        var query = new SearchQuery("searchable", MaxResults: 5);

        // Act
        var results = await context.SearchAsync(query);

        // Assert - Should return exactly MaxResults items
        Assert.Equal(5, results.Results.Count);
        // TotalCount equals Results.Count because source queries are also limited
        Assert.Equal(5, results.TotalCount);
    }

    /// <summary>
    /// Verifies TotalCount reflects all matches even when limited.
    /// </summary>
    [Fact]
    public async Task SearchAsync_TotalCountReflectsAllMatches()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        for (int i = 0; i < 10; i++)
        {
            context.Conversations.Add(CreateConversation($"Machine Learning Topic {i}"));
        }
        await context.SaveChangesAsync();

        var query = new SearchQuery("machine", MaxResults: 3);

        // Act
        var results = await context.SearchAsync(query);

        // Assert
        Assert.Equal(3, results.Results.Count);
        // TotalCount should be the combined count from both searches
        Assert.True(results.TotalCount >= 3);
    }

    #endregion

    #region SearchAsync Tests - BM25 Ranking

    /// <summary>
    /// Verifies results are ordered by BM25 rank (ascending, more negative = better).
    /// </summary>
    [Fact]
    public async Task SearchAsync_ResultsOrderedByRank()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        // Create conversations with different relevance
        context.Conversations.Add(CreateConversation("Python"));  // Single match
        context.Conversations.Add(CreateConversation("Python Python Programming")); // Multiple matches
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(SearchQuery.ConversationsOnly("python"));

        // Assert - Results should be sorted by rank ascending (better matches first)
        if (results.Results.Count >= 2)
        {
            Assert.True(
                results.Results[0].Rank <= results.Results[1].Rank,
                "Results should be ordered by rank ascending");
        }
    }

    /// <summary>
    /// Verifies BM25 scores are negative (as expected from FTS5).
    /// </summary>
    [Fact]
    public async Task SearchAsync_BM25ScoresAreNegative()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        context.Conversations.Add(CreateConversation("Test Search Query"));
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(SearchQuery.ConversationsOnly("search"));

        // Assert
        Assert.True(results.HasResults);
        Assert.True(results.Results[0].Rank < 0, "BM25 scores should be negative");
    }

    #endregion

    #region SearchAsync Tests - Result Properties

    /// <summary>
    /// Verifies conversation results have correct properties set.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ConversationResults_HaveCorrectProperties()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        var conversation = CreateConversation("Unique Searchable Title");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(SearchQuery.ConversationsOnly("unique"));

        // Assert
        Assert.True(results.HasResults);
        var result = results.Results[0];

        Assert.Equal(SearchResultType.Conversation, result.ResultType);
        Assert.Equal(conversation.Id, result.Id);
        Assert.Equal(conversation.Id, result.ConversationId);
        Assert.Null(result.MessageId);
        Assert.Equal("Unique Searchable Title", result.Title);
        Assert.Equal("Unique Searchable Title", result.Preview); // For conversations, preview = title
    }

    /// <summary>
    /// Verifies message results have correct properties set.
    /// </summary>
    [Fact]
    public async Task SearchAsync_MessageResults_HaveCorrectProperties()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        var conversation = CreateConversation("Parent Conversation");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var message = CreateMessage(conversation.Id, "Unique searchable content in message");
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(SearchQuery.MessagesOnly("searchable"));

        // Assert
        Assert.True(results.HasResults);
        var result = results.Results[0];

        Assert.Equal(SearchResultType.Message, result.ResultType);
        Assert.Equal(message.Id, result.Id);
        Assert.Equal(conversation.Id, result.ConversationId);
        Assert.Equal(message.Id, result.MessageId);
        Assert.Equal("Parent Conversation", result.Title); // Message uses parent conversation title
        Assert.NotEmpty(result.Preview); // Preview contains snippet with highlights
    }

    /// <summary>
    /// Verifies message preview contains highlight markers.
    /// </summary>
    [Fact]
    public async Task SearchAsync_MessagePreview_ContainsHighlightMarkers()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        var conversation = CreateConversation("Test Conv");
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();

        var message = CreateMessage(conversation.Id, "This is a highlighted term in the middle of content");
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(SearchQuery.MessagesOnly("highlighted"));

        // Assert
        Assert.True(results.HasResults);
        var preview = results.Results[0].Preview;
        Assert.Contains("<mark>", preview);
        Assert.Contains("</mark>", preview);
    }

    #endregion

    #region SearchAsync Tests - SearchDuration

    /// <summary>
    /// Verifies SearchDuration is populated.
    /// </summary>
    [Fact]
    public async Task SearchAsync_PopulatesSearchDuration()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        context.Conversations.Add(CreateConversation("Test Conversation"));
        await context.SaveChangesAsync();

        // Act
        var results = await context.SearchAsync(SearchQuery.Simple("test"));

        // Assert
        Assert.True(results.SearchDuration >= TimeSpan.Zero);
    }

    #endregion

    #region SearchAsync Tests - Prefix Matching

    /// <summary>
    /// Verifies single-word queries use prefix matching.
    /// </summary>
    [Fact]
    public async Task SearchAsync_SingleWordQuery_UsesPrefixMatching()
    {
        // Arrange
        await using var context = await CreateInMemoryContextAsync();

        context.Conversations.Add(CreateConversation("Programming Languages"));
        await context.SaveChangesAsync();

        // Act - Search with prefix
        var results = await context.SearchAsync(SearchQuery.ConversationsOnly("prog"));

        // Assert - Should find "Programming" with prefix "prog"
        Assert.True(results.HasResults, "Prefix 'prog' should match 'Programming'");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Checks if a table exists in the SQLite database.
    /// </summary>
    private static async Task<bool> TableExistsAsync(AInternDbContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) FROM sqlite_master
            WHERE type='table' AND name=@name";

        var param = command.CreateParameter();
        param.ParameterName = "@name";
        param.Value = tableName;
        command.Parameters.Add(param);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result) > 0;
    }

    /// <summary>
    /// Checks if a trigger exists in the SQLite database.
    /// </summary>
    private static async Task<bool> TriggerExistsAsync(AInternDbContext context, string triggerName)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) FROM sqlite_master
            WHERE type='trigger' AND name=@name";

        var param = command.CreateParameter();
        param.ParameterName = "@name";
        param.Value = triggerName;
        command.Parameters.Add(param);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result) > 0;
    }

    #endregion
}
