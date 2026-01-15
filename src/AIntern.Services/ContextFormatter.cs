namespace AIntern.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Formats file contexts for LLM prompts and UI display.
/// </summary>
/// <remarks>
/// <para>
/// Provides multiple formatting options for attached file contexts:
/// </para>
/// <list type="bullet">
///   <item><description>FormatForPrompt: Creates structured prompts for LLM consumption</description></item>
///   <item><description>FormatForDisplay: Creates markdown for UI display with previews</description></item>
///   <item><description>FormatForStorage: Creates JSON metadata for persistence</description></item>
/// </list>
/// <para>Added in v0.3.4b.</para>
/// </remarks>
public sealed class ContextFormatter : IContextFormatter
{
    #region Constants

    /// <summary>
    /// Maximum lines to show in display preview.
    /// </summary>
    private const int MaxDisplayLines = 10;

    #endregion

    #region Fields

    private readonly ILogger<ContextFormatter>? _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="ContextFormatter"/>.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public ContextFormatter(ILogger<ContextFormatter>? logger = null)
    {
        _logger = logger;
    }

    #endregion

    #region IContextFormatter Implementation

    /// <inheritdoc />
    public string FormatForPrompt(IEnumerable<FileContext> contexts)
    {
        var contextList = contexts.ToList();
        if (contextList.Count == 0)
        {
            _logger?.LogDebug("[CONTEXT] FormatForPrompt called with empty context list");
            return string.Empty;
        }

        var sb = new StringBuilder();

        // Introduction
        sb.AppendLine(ContextPromptTemplates.PromptIntroduction);

        // Format each context
        foreach (var context in contextList)
        {
            sb.Append(FormatSingleContext(context));
            sb.AppendLine();
        }

        // Conclusion
        sb.Append(ContextPromptTemplates.PromptConclusion);

        _logger?.LogDebug(
            "[CONTEXT] Formatted {Count} contexts for prompt ({Length} chars)",
            contextList.Count, sb.Length);

        return sb.ToString();
    }

    /// <inheritdoc />
    public string FormatSingleContext(FileContext context)
    {
        var sb = new StringBuilder();

        // Header with file info
        sb.AppendLine(FormatContextHeader(context));

        // Code block with syntax highlighting
        sb.Append(FormatCodeBlock(context.Content, context.Language));

        return sb.ToString();
    }

    /// <inheritdoc />
    public string FormatForDisplay(IEnumerable<FileContext> contexts, bool expanded = false)
    {
        var contextList = contexts.ToList();
        if (contextList.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var context in contextList)
        {
            // File name in bold
            sb.AppendLine($"**{context.FileName}**");

            // Line range if partial content
            if (context.IsPartialContent)
            {
                sb.AppendLine($"_Lines {context.StartLine}-{context.EndLine}_");
            }

            // Content - full or preview
            if (expanded)
            {
                sb.Append(FormatCodeBlock(context.Content, context.Language));
            }
            else
            {
                var preview = GetPreview(context.Content, MaxDisplayLines);
                sb.Append(FormatCodeBlock(preview, context.Language));
            }

            sb.AppendLine();
        }

        _logger?.LogDebug(
            "[CONTEXT] Formatted {Count} contexts for display (expanded={Expanded})",
            contextList.Count, expanded);

        return sb.ToString();
    }

    /// <inheritdoc />
    public string FormatCodeBlock(string content, string? language)
    {
        var sb = new StringBuilder();

        sb.Append("```");
        sb.AppendLine(language ?? string.Empty);
        sb.AppendLine(content.TrimEnd());
        sb.AppendLine("```");

        return sb.ToString();
    }

    /// <inheritdoc />
    public string FormatContextHeader(FileContext context)
    {
        var sb = new StringBuilder();

        // File header with or without selection info
        if (context.IsPartialContent)
        {
            sb.Append($"### Selected Code from `{context.FileName}` (lines {context.StartLine}-{context.EndLine})");
        }
        else
        {
            sb.Append($"### File: `{context.FileName}`");
            if (!string.IsNullOrEmpty(context.Language))
            {
                sb.Append($" ({context.Language})");
            }
        }

        sb.AppendLine();

        // Add relative path if available and different from filename
        if (!string.IsNullOrEmpty(context.FilePath))
        {
            var relativePath = GetDisplayPath(context.FilePath);
            if (relativePath != context.FileName)
            {
                sb.AppendLine($"_Path: {relativePath}_");
            }
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public string FormatForStorage(IEnumerable<FileContext> contexts)
    {
        var storageItems = contexts.Select(c => new ContextStorageItem
        {
            Id = c.Id,
            FilePath = c.FilePath,
            FileName = c.FileName,
            Language = c.Language,
            StartLine = c.StartLine,
            EndLine = c.EndLine,
            EstimatedTokens = c.EstimatedTokens,
            AttachedAt = c.AttachedAt,
            ContentHash = c.ContentHash,
            ContentLength = c.Content.Length
        });

        var json = JsonSerializer.Serialize(storageItems, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        _logger?.LogDebug("[CONTEXT] Serialized {Count} contexts for storage", contexts.Count());

        return json;
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Creates a truncated preview of content.
    /// </summary>
    /// <param name="content">The content to preview.</param>
    /// <param name="maxLines">Maximum lines to include.</param>
    /// <returns>Truncated preview with line count indicator.</returns>
    private static string GetPreview(string content, int maxLines)
    {
        var lines = content.Split('\n');
        if (lines.Length <= maxLines)
        {
            return content;
        }

        var preview = string.Join('\n', lines.Take(maxLines));
        var remaining = lines.Length - maxLines;
        return $"{preview}\n// ... ({remaining} more lines)";
    }

    /// <summary>
    /// Extracts a meaningful display path from a full path.
    /// Takes the last 3 path segments for brevity.
    /// </summary>
    /// <param name="fullPath">The full file path.</param>
    /// <returns>Shortened display path.</returns>
    private static string GetDisplayPath(string fullPath)
    {
        // Normalize path separators
        var normalized = fullPath.Replace('\\', '/');
        var parts = normalized.Split('/');

        // Take last 3 parts at most
        var meaningfulParts = parts.TakeLast(3);
        return string.Join('/', meaningfulParts);
    }

    #endregion

    #region Storage Model

    /// <summary>
    /// Lightweight storage model for context metadata.
    /// Excludes full content to reduce storage size.
    /// </summary>
    private sealed class ContextStorageItem
    {
        public Guid Id { get; init; }
        public string FilePath { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public string? Language { get; init; }
        public int? StartLine { get; init; }
        public int? EndLine { get; init; }
        public int EstimatedTokens { get; init; }
        public DateTime AttachedAt { get; init; }
        public string ContentHash { get; init; } = string.Empty;
        public int ContentLength { get; init; }
    }

    #endregion
}
