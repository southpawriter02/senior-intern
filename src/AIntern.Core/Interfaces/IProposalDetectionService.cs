using AIntern.Core.Models;

namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PROPOSAL DETECTION SERVICE INTERFACE (v0.4.4h)                          │
// │ Contract for detecting multi-file proposals in chat messages.           │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for detecting multi-file proposals in chat messages.
/// </summary>
/// <remarks>
/// <para>
/// Abstracts the detection logic for testability and extensibility.
/// Implementations should:
/// <list type="bullet">
/// <item>Filter code blocks by type and language</item>
/// <item>Apply minimum file count thresholds</item>
/// <item>Cache proposals by message ID for performance</item>
/// </list>
/// </para>
/// <para>Added in v0.4.4h.</para>
/// </remarks>
public interface IProposalDetectionService
{
    /// <summary>
    /// Detect a file tree proposal from message content and code blocks.
    /// </summary>
    /// <param name="messageContent">The full message text.</param>
    /// <param name="messageId">The message ID for linking and caching.</param>
    /// <param name="codeBlocks">The code blocks extracted from the message.</param>
    /// <returns>A FileTreeProposal if detected, null otherwise.</returns>
    FileTreeProposal? DetectProposal(
        string messageContent,
        Guid messageId,
        IEnumerable<CodeBlock> codeBlocks);

    /// <summary>
    /// Check if a proposal should show the panel UI.
    /// </summary>
    /// <param name="proposal">The proposal to check.</param>
    /// <returns>True if the panel should be shown.</returns>
    bool ShouldShowProposalPanel(FileTreeProposal? proposal);

    /// <summary>
    /// Get a cached proposal for a message.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    /// <returns>The cached proposal, or null if not cached.</returns>
    FileTreeProposal? GetCachedProposal(Guid messageId);

    /// <summary>
    /// Clear the proposal cache.
    /// </summary>
    void ClearCache();
}
