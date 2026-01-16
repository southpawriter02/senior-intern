namespace AIntern.Services;

using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF SERVICE (v0.4.2b)                                                   │
// │ Computes diffs between text content using DiffPlex.                      │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Computes diffs between text content using DiffPlex.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2b.</para>
/// <para>
/// This service uses the DiffPlex library to compute line-level diffs between
/// original and proposed content. It organizes changes into hunks with configurable
/// context lines and supports both synchronous and async operations.
/// </para>
/// </remarks>
public sealed class DiffService : IDiffService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<DiffService>? _logger;
    private readonly DiffOptions _defaultOptions;

    private readonly Differ _differ;
    private readonly SideBySideDiffBuilder _sideBySideDiffBuilder;

    /// <summary>
    /// Initializes a new instance of the DiffService.
    /// </summary>
    /// <param name="fileSystemService">File system service for reading original files.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <param name="defaultOptions">Default diff options to use when not specified.</param>
    public DiffService(
        IFileSystemService fileSystemService,
        ILogger<DiffService>? logger = null,
        DiffOptions? defaultOptions = null)
    {
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _logger = logger;
        _defaultOptions = defaultOptions ?? DiffOptions.Default;

        _differ = new Differ();
        _sideBySideDiffBuilder = new SideBySideDiffBuilder(_differ);

        _logger?.LogDebug("DiffService initialized with ContextLines={ContextLines}, HunkThreshold={Threshold}",
            _defaultOptions.ContextLines, _defaultOptions.HunkSeparationThreshold);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public Interface Methods - Synchronous
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public DiffResult ComputeDiff(string originalContent, string proposedContent)
    {
        return ComputeDiff(originalContent, proposedContent, string.Empty, _defaultOptions);
    }

    /// <inheritdoc/>
    public DiffResult ComputeDiff(string originalContent, string proposedContent, string filePath)
    {
        return ComputeDiff(originalContent, proposedContent, filePath, _defaultOptions);
    }

    /// <inheritdoc/>
    public DiffResult ComputeDiff(
        string originalContent,
        string proposedContent,
        string filePath,
        DiffOptions options)
    {
        _logger?.LogDebug("Computing diff for {FilePath}", string.IsNullOrEmpty(filePath) ? "(unnamed)" : filePath);

        // Handle null inputs
        originalContent ??= string.Empty;
        proposedContent ??= string.Empty;

        // Normalize line endings
        originalContent = NormalizeLineEndings(originalContent);
        proposedContent = NormalizeLineEndings(proposedContent);

        // Apply preprocessing options
        if (options.TrimTrailingWhitespace)
        {
            originalContent = TrimTrailingWhitespaceFromLines(originalContent);
            proposedContent = TrimTrailingWhitespaceFromLines(proposedContent);
        }

        // Quick identity check
        if (originalContent == proposedContent)
        {
            _logger?.LogDebug("Content identical, returning no changes");
            return DiffResult.NoChanges(filePath, originalContent);
        }

        // Compute the diff using DiffPlex
        var diffModel = _sideBySideDiffBuilder.BuildDiffModel(originalContent, proposedContent);

        // Convert to our model with hunks
        var hunks = BuildHunks(diffModel, options);

        // Calculate statistics
        var stats = ComputeStats(hunks);

        _logger?.LogDebug("Diff computed: {Stats} ({HunkCount} hunks)", stats.Summary, hunks.Count);

        return new DiffResult
        {
            OriginalFilePath = filePath,
            OriginalContent = originalContent,
            ProposedContent = proposedContent,
            Hunks = hunks,
            Stats = stats,
            IsNewFile = string.IsNullOrEmpty(originalContent),
            IsDeleteFile = string.IsNullOrEmpty(proposedContent)
        };
    }

    /// <inheritdoc/>
    public DiffResult ComputeNewFileDiff(string proposedContent, string filePath)
    {
        _logger?.LogDebug("Computing new file diff for {FilePath}", filePath);

        proposedContent = NormalizeLineEndings(proposedContent ?? string.Empty);
        var lines = SplitLines(proposedContent);

        var diffLines = lines
            .Select((line, i) => DiffLine.Added(i + 1, line))
            .ToList();

        var hunk = new DiffHunk
        {
            OriginalStartLine = 0,
            OriginalLineCount = 0,
            ProposedStartLine = 1,
            ProposedLineCount = lines.Length,
            Lines = diffLines,
            Index = 0
        };

        _logger?.LogDebug("New file diff: +{LineCount} lines", lines.Length);

        return new DiffResult
        {
            OriginalFilePath = filePath,
            OriginalContent = string.Empty,
            ProposedContent = proposedContent,
            Hunks = [hunk],
            Stats = DiffStats.FromCounts(added: lines.Length, removed: 0, modified: 0, unchanged: 0),
            IsNewFile = true
        };
    }

    /// <inheritdoc/>
    public DiffResult ComputeDeleteFileDiff(string originalContent, string filePath)
    {
        _logger?.LogDebug("Computing delete file diff for {FilePath}", filePath);

        originalContent = NormalizeLineEndings(originalContent ?? string.Empty);
        var lines = SplitLines(originalContent);

        var diffLines = lines
            .Select((line, i) => DiffLine.Removed(i + 1, line))
            .ToList();

        var hunk = new DiffHunk
        {
            OriginalStartLine = 1,
            OriginalLineCount = lines.Length,
            ProposedStartLine = 0,
            ProposedLineCount = 0,
            Lines = diffLines,
            Index = 0
        };

        _logger?.LogDebug("Delete file diff: -{LineCount} lines", lines.Length);

        return new DiffResult
        {
            OriginalFilePath = filePath,
            OriginalContent = originalContent,
            ProposedContent = string.Empty,
            Hunks = [hunk],
            Stats = DiffStats.FromCounts(added: 0, removed: lines.Length, modified: 0, unchanged: 0),
            IsDeleteFile = true
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Public Interface Methods - Asynchronous
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<DiffResult> ComputeDiffForBlockAsync(
        CodeBlock block,
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(block.TargetFilePath))
        {
            _logger?.LogWarning("ComputeDiffForBlockAsync called with block missing TargetFilePath");
            throw new ArgumentException("Code block must have a target file path", nameof(block));
        }

        var fullPath = Path.Combine(workspacePath, block.TargetFilePath);
        _logger?.LogDebug("Computing diff for block {BlockId} targeting {FilePath}",
            block.Id, block.TargetFilePath);

        // Check if target file exists
        if (!await _fileSystemService.FileExistsAsync(fullPath))
        {
            _logger?.LogDebug("Target file does not exist, creating new file diff");
            var result = ComputeNewFileDiff(block.Content, block.TargetFilePath);
            return WithSourceBlockId(result, block.Id);
        }

        // Check for binary file
        if (!_fileSystemService.IsTextFile(fullPath))
        {
            _logger?.LogWarning("Cannot diff binary file: {FilePath}", block.TargetFilePath);
            return WithSourceBlockId(DiffResult.BinaryFile(block.TargetFilePath), block.Id);
        }

        // Read original content
        var originalContent = await _fileSystemService.ReadFileAsync(fullPath, cancellationToken);

        // Determine proposed content based on block type
        string proposedContent;

        if (block.BlockType == CodeBlockType.CompleteFile)
        {
            // Complete file replacement
            _logger?.LogDebug("Block type is CompleteFile, using full content replacement");
            proposedContent = block.Content;
        }
        else if (block.ReplacementRange is { } range && range.IsValid)
        {
            // Snippet with specific replacement range
            _logger?.LogDebug("Block type is Snippet with range {Start}-{End}",
                range.StartLine, range.EndLine);
            proposedContent = ReplaceLines(
                originalContent,
                range.StartLine,
                range.EndLine,
                block.Content);
        }
        else
        {
            // Snippet without range - use content as-is (full replacement fallback)
            _logger?.LogDebug("Snippet without replacement range, using full replacement fallback");
            proposedContent = block.Content;
        }

        var diffResult = ComputeDiff(originalContent, proposedContent, block.TargetFilePath);
        return WithSourceBlockId(diffResult, block.Id);
    }

    /// <inheritdoc/>
    public async Task<DiffResult> ComputeMergedDiffAsync(
        IReadOnlyList<CodeBlock> blocks,
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        if (blocks.Count == 0)
        {
            _logger?.LogWarning("ComputeMergedDiffAsync called with empty blocks list");
            throw new ArgumentException("At least one code block is required", nameof(blocks));
        }

        // Single block - just compute directly
        if (blocks.Count == 1)
        {
            _logger?.LogDebug("Single block provided, computing diff directly");
            return await ComputeDiffForBlockAsync(blocks[0], workspacePath, cancellationToken);
        }

        // Validate all blocks target the same file
        var targetPath = blocks[0].TargetFilePath;
        if (blocks.Any(b => b.TargetFilePath != targetPath))
        {
            _logger?.LogWarning("ComputeMergedDiffAsync called with blocks targeting different files");
            throw new ArgumentException("All blocks must target the same file", nameof(blocks));
        }

        _logger?.LogDebug("Computing merged diff for {Count} blocks targeting {FilePath}",
            blocks.Count, targetPath);

        // Strategy: Use the last CompleteFile block if present, otherwise use first block
        // TODO: Implement proper multi-block merging in future version
        var completeFileBlock = blocks.LastOrDefault(b => b.BlockType == CodeBlockType.CompleteFile);
        if (completeFileBlock != null)
        {
            _logger?.LogDebug("Found CompleteFile block, using it for merged diff");
            return await ComputeDiffForBlockAsync(completeFileBlock, workspacePath, cancellationToken);
        }

        // Fallback to first block
        _logger?.LogDebug("No CompleteFile block found, using first block as fallback");
        return await ComputeDiffForBlockAsync(blocks[0], workspacePath, cancellationToken);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Helper Methods - Hunk Building
    // ═══════════════════════════════════════════════════════════════════════

    private List<DiffHunk> BuildHunks(SideBySideDiffModel diffModel, DiffOptions options)
    {
        var hunks = new List<DiffHunk>();
        var currentHunkLines = new List<DiffLine>();

        int hunkOrigStart = 1;
        int hunkPropStart = 1;
        int unchangedRun = 0;
        bool inHunk = false;
        int hunkIndex = 0;

        var originalLines = diffModel.OldText.Lines;
        var proposedLines = diffModel.NewText.Lines;
        int maxLines = Math.Max(originalLines.Count, proposedLines.Count);

        for (int i = 0; i < maxLines; i++)
        {
            var origPiece = i < originalLines.Count ? originalLines[i] : null;
            var propPiece = i < proposedLines.Count ? proposedLines[i] : null;

            var lineType = DetermineLineType(origPiece, propPiece);
            var diffLine = CreateDiffLine(origPiece, propPiece, lineType);

            if (lineType != DiffLineType.Unchanged)
            {
                // Start or continue a hunk
                if (!inHunk)
                {
                    inHunk = true;

                    // Calculate hunk start positions
                    hunkOrigStart = Math.Max(1, (diffLine.OriginalLineNumber ?? diffLine.ProposedLineNumber ?? 1) - options.ContextLines);
                    hunkPropStart = Math.Max(1, (diffLine.ProposedLineNumber ?? diffLine.OriginalLineNumber ?? 1) - options.ContextLines);

                    // Add leading context lines
                    AddLeadingContext(currentHunkLines, originalLines, proposedLines, i, options.ContextLines);
                }

                currentHunkLines.Add(diffLine);
                unchangedRun = 0;
            }
            else if (inHunk)
            {
                currentHunkLines.Add(diffLine);
                unchangedRun++;

                // Check if we should close the hunk
                if (unchangedRun >= options.HunkSeparationThreshold)
                {
                    // Trim trailing context and close hunk
                    TrimTrailingContext(currentHunkLines, options.ContextLines);

                    if (currentHunkLines.Any(l => l.Type != DiffLineType.Unchanged))
                    {
                        hunks.Add(CreateHunk(hunkOrigStart, hunkPropStart, currentHunkLines, hunkIndex++));
                    }

                    currentHunkLines = [];
                    inHunk = false;
                    unchangedRun = 0;
                }
            }
        }

        // Close any remaining hunk
        if (currentHunkLines.Count > 0 && currentHunkLines.Any(l => l.Type != DiffLineType.Unchanged))
        {
            TrimTrailingContext(currentHunkLines, options.ContextLines);
            hunks.Add(CreateHunk(hunkOrigStart, hunkPropStart, currentHunkLines, hunkIndex));
        }

        return hunks;
    }

    private static DiffLineType DetermineLineType(DiffPiece? origPiece, DiffPiece? propPiece)
    {
        // Handle null cases (lines that only exist on one side)
        if (origPiece?.Type == ChangeType.Imaginary && propPiece?.Type == ChangeType.Inserted)
            return DiffLineType.Added;

        if (origPiece?.Type == ChangeType.Deleted && propPiece?.Type == ChangeType.Imaginary)
            return DiffLineType.Removed;

        // Handle explicit change types
        if (origPiece?.Type == ChangeType.Deleted)
            return DiffLineType.Removed;

        if (propPiece?.Type == ChangeType.Inserted)
            return DiffLineType.Added;

        if (origPiece?.Type == ChangeType.Modified || propPiece?.Type == ChangeType.Modified)
            return DiffLineType.Modified;

        return DiffLineType.Unchanged;
    }

    private static DiffLine CreateDiffLine(DiffPiece? origPiece, DiffPiece? propPiece, DiffLineType type)
    {
        var content = type switch
        {
            DiffLineType.Removed => origPiece?.Text ?? string.Empty,
            DiffLineType.Added => propPiece?.Text ?? string.Empty,
            _ => origPiece?.Text ?? propPiece?.Text ?? string.Empty
        };

        int? origLineNum = origPiece?.Type != ChangeType.Imaginary ? origPiece?.Position : null;
        int? propLineNum = propPiece?.Type != ChangeType.Imaginary ? propPiece?.Position : null;

        return new DiffLine
        {
            OriginalLineNumber = origLineNum,
            ProposedLineNumber = propLineNum,
            Content = content,
            Type = type
        };
    }

    private static void AddLeadingContext(
        List<DiffLine> hunkLines,
        IList<DiffPiece> originalLines,
        IList<DiffPiece> proposedLines,
        int currentIndex,
        int contextLines)
    {
        int startIndex = Math.Max(0, currentIndex - contextLines);

        for (int i = startIndex; i < currentIndex; i++)
        {
            var origPiece = i < originalLines.Count ? originalLines[i] : null;
            var propPiece = i < proposedLines.Count ? proposedLines[i] : null;

            // Only add if it's actually an unchanged line
            if (origPiece?.Type == ChangeType.Unchanged)
            {
                hunkLines.Add(DiffLine.Unchanged(
                    origPiece.Position ?? i + 1,
                    propPiece?.Position ?? i + 1,
                    origPiece.Text ?? string.Empty));
            }
        }
    }

    private static void TrimTrailingContext(List<DiffLine> hunkLines, int maxContext)
    {
        int trailingUnchanged = 0;
        for (int i = hunkLines.Count - 1; i >= 0; i--)
        {
            if (hunkLines[i].Type == DiffLineType.Unchanged)
                trailingUnchanged++;
            else
                break;
        }

        int toRemove = Math.Max(0, trailingUnchanged - maxContext);
        if (toRemove > 0)
        {
            hunkLines.RemoveRange(hunkLines.Count - toRemove, toRemove);
        }
    }

    private static DiffHunk CreateHunk(int origStart, int propStart, List<DiffLine> lines, int index)
    {
        // Count lines that appear on each side
        int origCount = lines.Count(l => l.Type is DiffLineType.Unchanged or DiffLineType.Removed or DiffLineType.Modified);
        int propCount = lines.Count(l => l.Type is DiffLineType.Unchanged or DiffLineType.Added or DiffLineType.Modified);

        return new DiffHunk
        {
            OriginalStartLine = origStart,
            OriginalLineCount = origCount,
            ProposedStartLine = propStart,
            ProposedLineCount = propCount,
            Lines = lines.ToList(),
            Index = index
        };
    }

    private static DiffStats ComputeStats(IReadOnlyList<DiffHunk> hunks)
    {
        int added = 0, removed = 0, modified = 0, unchanged = 0;

        foreach (var hunk in hunks)
        {
            foreach (var line in hunk.Lines)
            {
                switch (line.Type)
                {
                    case DiffLineType.Added: added++; break;
                    case DiffLineType.Removed: removed++; break;
                    case DiffLineType.Modified: modified++; break;
                    case DiffLineType.Unchanged: unchanged++; break;
                }
            }
        }

        return DiffStats.FromCounts(added, removed, modified, unchanged);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Helper Methods - Result Construction
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new DiffResult with the SourceBlockId set.
    /// </summary>
    private static DiffResult WithSourceBlockId(DiffResult result, Guid blockId)
    {
        return new DiffResult
        {
            Id = result.Id,
            OriginalFilePath = result.OriginalFilePath,
            OriginalContent = result.OriginalContent,
            ProposedContent = result.ProposedContent,
            Hunks = result.Hunks,
            Stats = result.Stats,
            IsNewFile = result.IsNewFile,
            IsDeleteFile = result.IsDeleteFile,
            IsBinaryFile = result.IsBinaryFile,
            SourceBlockId = blockId,
            ComputedAt = result.ComputedAt
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Helper Methods - Line Replacement
    // ═══════════════════════════════════════════════════════════════════════

    private static string ReplaceLines(string content, int startLine, int endLine, string replacement)
    {
        var lines = SplitLines(content).ToList();
        var replacementLines = SplitLines(replacement);

        // Validate range (1-based line numbers)
        if (startLine < 1)
            throw new ArgumentOutOfRangeException(nameof(startLine), "Start line must be >= 1");
        if (startLine > lines.Count + 1)
            throw new ArgumentOutOfRangeException(nameof(startLine), "Start line exceeds file length");
        if (endLine < startLine)
            throw new ArgumentOutOfRangeException(nameof(endLine), "End line must be >= start line");

        // Clamp end line to file length
        endLine = Math.Min(endLine, lines.Count);

        // Remove old lines (convert to 0-based index)
        int removeCount = endLine - startLine + 1;
        if (removeCount > 0 && startLine <= lines.Count)
        {
            lines.RemoveRange(startLine - 1, Math.Min(removeCount, lines.Count - startLine + 1));
        }

        // Insert new lines
        lines.InsertRange(startLine - 1, replacementLines);

        return string.Join("\n", lines);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private Helper Methods - Text Processing
    // ═══════════════════════════════════════════════════════════════════════

    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private static string TrimTrailingWhitespaceFromLines(string text)
    {
        var lines = SplitLines(text);
        return string.Join("\n", lines.Select(l => l.TrimEnd()));
    }

    private static string[] SplitLines(string text)
    {
        return text.Split('\n');
    }
}
