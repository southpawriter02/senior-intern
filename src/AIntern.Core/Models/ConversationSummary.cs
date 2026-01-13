namespace AIntern.Core.Models;

using AIntern.Core.Enums;

/// <summary>
/// Lightweight summary of a conversation for list display.
/// Does not include full message content to optimize performance.
/// </summary>
/// <remarks>
/// <para>
/// Used by the conversation sidebar to display a compact list of conversations.
/// Contains only the essential properties needed for display and sorting.
/// </para>
/// <para>
/// The <see cref="Preview"/> property contains a truncated snippet of the
/// first user message for quick identification.
/// </para>
/// </remarks>
public sealed record ConversationSummary
{
    /// <summary>
    /// Gets the unique identifier for this conversation.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the display title of the conversation.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets the UTC timestamp when this conversation was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this conversation was last modified.
    /// Used for sorting and date grouping.
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Gets the total number of messages in the conversation.
    /// Displayed as a badge in the UI.
    /// </summary>
    public int MessageCount { get; init; }

    /// <summary>
    /// Gets a preview snippet of the first user message (truncated).
    /// Used for quick identification of conversation content.
    /// </summary>
    public string? Preview { get; init; }

    /// <summary>
    /// Gets whether the conversation is archived (soft deleted).
    /// Archived conversations are hidden from the main list by default.
    /// </summary>
    public bool IsArchived { get; init; }

    /// <summary>
    /// Gets whether the conversation is pinned to the top of the list.
    /// </summary>
    public bool IsPinned { get; init; }

    /// <summary>
    /// Gets the name of the model used in this conversation.
    /// Displayed as additional context in the UI.
    /// </summary>
    public string? ModelName { get; init; }

    /// <summary>
    /// Gets the date group for this conversation based on UpdatedAt.
    /// </summary>
    /// <returns>
    /// The appropriate <see cref="DateGroup"/> for UI grouping.
    /// </returns>
    /// <remarks>
    /// Compares the conversation's UpdatedAt date against the current UTC date
    /// to determine which group it belongs to (Today, Yesterday, etc.).
    /// </remarks>
    public DateGroup GetDateGroup()
    {
        // Use UTC dates for consistent grouping across time zones.
        var now = DateTime.UtcNow.Date;
        var date = UpdatedAt.Date;

        // Today: same calendar day
        if (date == now)
        {
            return DateGroup.Today;
        }

        // Yesterday: exactly one day ago
        if (date == now.AddDays(-1))
        {
            return DateGroup.Yesterday;
        }

        // Previous 7 days: 2-7 days ago
        if (date >= now.AddDays(-7))
        {
            return DateGroup.Previous7Days;
        }

        // Previous 30 days: 8-30 days ago
        if (date >= now.AddDays(-30))
        {
            return DateGroup.Previous30Days;
        }

        // Older: more than 30 days ago
        return DateGroup.Older;
    }
}
