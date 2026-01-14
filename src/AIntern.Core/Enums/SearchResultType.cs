namespace AIntern.Core.Enums;

/// <summary>
/// Specifies the type of entity matched in a full-text search result.
/// </summary>
/// <remarks>
/// <para>
/// Used by <see cref="Models.SearchResult"/> to indicate whether the search
/// matched a conversation title or a message content.
/// </para>
/// <para>
/// The search infrastructure indexes two content types:
/// </para>
/// <list type="bullet">
///   <item><description><b>Conversation:</b> Title field from the Conversations table</description></item>
///   <item><description><b>Message:</b> Content field from the Messages table</description></item>
/// </list>
/// <para>
/// <b>FTS5 Infrastructure:</b> This enum works with the FTS5 virtual tables
/// (ConversationsFts, MessagesFts) and their synchronization triggers to provide
/// efficient full-text search with BM25 relevance ranking.
/// </para>
/// <para>Added in v0.2.5a.</para>
/// </remarks>
public enum SearchResultType
{
    /// <summary>
    /// The search result matches a conversation title.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this type is set, <see cref="Models.SearchResult.ConversationId"/>
    /// contains the matched conversation's ID, and <see cref="Models.SearchResult.MessageId"/>
    /// is null.
    /// </para>
    /// <para>
    /// Conversation matches are found via the ConversationsFts virtual table which
    /// indexes the Title column of the Conversations table.
    /// </para>
    /// </remarks>
    Conversation,

    /// <summary>
    /// The search result matches message content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this type is set, both <see cref="Models.SearchResult.ConversationId"/>
    /// and <see cref="Models.SearchResult.MessageId"/> contain values identifying
    /// the specific message and its parent conversation.
    /// </para>
    /// <para>
    /// Message matches are found via the MessagesFts virtual table which indexes
    /// the Content column of the Messages table.
    /// </para>
    /// </remarks>
    Message
}
