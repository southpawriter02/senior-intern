namespace AIntern.Core.Enums;

/// <summary>
/// Date-based grouping categories for conversation lists.
/// </summary>
/// <remarks>
/// Used by the conversation sidebar to group conversations by recency.
/// Groups are determined by comparing UpdatedAt to the current date.
/// </remarks>
public enum DateGroup
{
    /// <summary>
    /// Conversations updated today (same calendar day).
    /// </summary>
    Today,

    /// <summary>
    /// Conversations updated yesterday.
    /// </summary>
    Yesterday,

    /// <summary>
    /// Conversations updated within the previous 7 days (excluding today and yesterday).
    /// </summary>
    Previous7Days,

    /// <summary>
    /// Conversations updated within the previous 30 days (excluding the last 7 days).
    /// </summary>
    Previous30Days,

    /// <summary>
    /// Conversations older than 30 days.
    /// </summary>
    Older
}
