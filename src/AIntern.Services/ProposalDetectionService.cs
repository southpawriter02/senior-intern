using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PROPOSAL DETECTION SERVICE (v0.4.4h)                                    │
// │ Detects multi-file proposals in chat messages.                          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for detecting multi-file proposals in chat messages.
/// </summary>
/// <remarks>
/// <para>
/// Uses the FileTreeParser and applies detection thresholds.
/// Caches proposals by message ID for performance.
/// </para>
/// <para>Added in v0.4.4h.</para>
/// </remarks>
public class ProposalDetectionService : IProposalDetectionService
{
    private readonly IFileTreeParser _parser;
    private readonly ILogger<ProposalDetectionService> _logger;
    private readonly ProposalDetectionOptions _options;
    private readonly ConcurrentDictionary<Guid, FileTreeProposal?> _cache = new();

    /// <summary>
    /// Create a new proposal detection service.
    /// </summary>
    /// <param name="parser">The file tree parser.</param>
    /// <param name="options">Detection options.</param>
    /// <param name="logger">Logger instance.</param>
    public ProposalDetectionService(
        IFileTreeParser parser,
        IOptions<ProposalDetectionOptions> options,
        ILogger<ProposalDetectionService> logger)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _options = options?.Value ?? new ProposalDetectionOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("ProposalDetectionService initialized with MinimumFilesForPanel={Min}",
            _options.MinimumFilesForPanel);
    }

    /// <inheritdoc/>
    public FileTreeProposal? DetectProposal(
        string messageContent,
        Guid messageId,
        IEnumerable<CodeBlock> codeBlocks)
    {
        // Check cache first
        if (_cache.TryGetValue(messageId, out var cached))
        {
            _logger.LogDebug("Returning cached proposal for message {MessageId}", messageId);
            return cached;
        }

        // Filter to applicable blocks
        var applicableBlocks = codeBlocks
            .Where(IsApplicableBlock)
            .ToList();

        // Check minimum threshold
        if (applicableBlocks.Count < _options.MinimumFilesForPanel)
        {
            _logger.LogDebug(
                "Message {MessageId} has {Count} applicable blocks, below threshold of {Threshold}",
                messageId, applicableBlocks.Count, _options.MinimumFilesForPanel);

            TryAddToCache(messageId, null);
            return null;
        }

        try
        {
            // Parse the proposal
            var proposal = _parser.ParseProposal(
                messageContent,
                messageId,
                applicableBlocks);

            if (proposal == null)
            {
                _logger.LogDebug("No proposal detected for message {MessageId}", messageId);
                TryAddToCache(messageId, null);
                return null;
            }

            _logger.LogInformation(
                "Detected multi-file proposal with {Count} files for message {MessageId}",
                proposal.FileCount, messageId);

            TryAddToCache(messageId, proposal);
            return proposal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting proposal for message {MessageId}", messageId);
            TryAddToCache(messageId, null);
            return null;
        }
    }

    /// <inheritdoc/>
    public bool ShouldShowProposalPanel(FileTreeProposal? proposal)
    {
        if (proposal == null)
        {
            return false;
        }

        // Must meet minimum file count
        if (proposal.FileCount < _options.MinimumFilesForPanel)
        {
            return false;
        }

        // Don't show for fully applied or rejected
        if (proposal.Status == FileTreeProposalStatus.FullyApplied ||
            proposal.Status == FileTreeProposalStatus.Rejected)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public FileTreeProposal? GetCachedProposal(Guid messageId)
    {
        _cache.TryGetValue(messageId, out var proposal);
        return proposal;
    }

    /// <inheritdoc/>
    public void ClearCache()
    {
        _cache.Clear();
        _logger.LogDebug("Proposal cache cleared");
    }

    /// <summary>
    /// Check if a code block is applicable for multi-file proposals.
    /// </summary>
    private bool IsApplicableBlock(CodeBlock block)
    {
        // Must be file-related type (not command, output, or example)
        if (block.BlockType != CodeBlockType.CompleteFile &&
            block.BlockType != CodeBlockType.Snippet &&
            block.BlockType != CodeBlockType.Config)
        {
            return false;
        }

        // Must have a file path
        if (string.IsNullOrEmpty(block.TargetFilePath))
        {
            return false;
        }

        // Check ignored languages
        if (_options.IgnoredLanguages.Contains(
            block.Language ?? string.Empty,
            StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Add to cache with size limit enforcement.
    /// </summary>
    private void TryAddToCache(Guid messageId, FileTreeProposal? proposal)
    {
        // Simple size limit - remove oldest if at capacity
        if (_cache.Count >= _options.MaxCacheSize)
        {
            var oldest = _cache.Keys.FirstOrDefault();
            if (oldest != default)
            {
                _cache.TryRemove(oldest, out _);
            }
        }

        _cache.TryAdd(messageId, proposal);
    }
}
