namespace AIntern.Desktop.ViewModels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;
using AIntern.Core.Utilities;

/// <summary>
/// ViewModel for an attached file context.
/// </summary>
/// <remarks>
/// <para>
/// Bridges the core <see cref="FileContext"/> model with the UI layer,
/// providing computed properties for display labels, icons, badges, and previews.
/// </para>
/// <para>Added in v0.3.4c.</para>
/// </remarks>
public partial class FileContextViewModel : ViewModelBase
{
    #region Constants

    /// <summary>
    /// Maximum characters in preview content.
    /// </summary>
    private const int MaxPreviewLength = 500;

    /// <summary>
    /// Maximum lines in preview content.
    /// </summary>
    private const int MaxPreviewLines = 15;

    /// <summary>
    /// Maximum characters for short label.
    /// </summary>
    private const int MaxShortLabelLength = 20;

    #endregion

    #region Observable Properties

    /// <summary>
    /// Unique identifier for this context.
    /// </summary>
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();

    /// <summary>
    /// Full path to the file (empty for clipboard content).
    /// </summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>
    /// File name for display.
    /// </summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>
    /// Detected language identifier.
    /// </summary>
    [ObservableProperty]
    private string? _language;

    /// <summary>
    /// Full content of the context.
    /// </summary>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// Estimated token count.
    /// </summary>
    [ObservableProperty]
    private int _estimatedTokens;

    /// <summary>
    /// Total line count.
    /// </summary>
    [ObservableProperty]
    private int _lineCount;

    /// <summary>
    /// Starting line number (for selections).
    /// </summary>
    [ObservableProperty]
    private int? _startLine;

    /// <summary>
    /// Ending line number (for selections).
    /// </summary>
    [ObservableProperty]
    private int? _endLine;

    /// <summary>
    /// Type of attachment.
    /// </summary>
    [ObservableProperty]
    private ContextAttachmentType _attachmentType = ContextAttachmentType.File;

    /// <summary>
    /// Whether the preview is expanded.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// When the context was attached (UTC).
    /// </summary>
    [ObservableProperty]
    private DateTime _attachedAt = DateTime.UtcNow;

    /// <summary>
    /// Whether this context is currently being hovered.
    /// </summary>
    [ObservableProperty]
    private bool _isHovered;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Label for display (includes line range for selections).
    /// </summary>
    public string DisplayLabel
    {
        get
        {
            if (AttachmentType == ContextAttachmentType.Clipboard)
            {
                return "Clipboard";
            }

            if (StartLine.HasValue && EndLine.HasValue)
            {
                return $"{FileName}:{StartLine}-{EndLine}";
            }

            return FileName;
        }
    }

    /// <summary>
    /// Short label for compact display (max 20 chars).
    /// </summary>
    public string ShortLabel
    {
        get
        {
            var name = FileName;
            if (name.Length > MaxShortLabelLength)
            {
                name = name[..(MaxShortLabelLength - 3)] + "...";
            }

            if (StartLine.HasValue && EndLine.HasValue)
            {
                return $"{name}:{StartLine}-{EndLine}";
            }

            return name;
        }
    }

    /// <summary>
    /// Preview content (truncated to MaxPreviewLines and MaxPreviewLength).
    /// </summary>
    public string PreviewContent
    {
        get
        {
            if (string.IsNullOrEmpty(Content))
            {
                return "(empty)";
            }

            var lines = Content.Split('\n');
            if (lines.Length <= MaxPreviewLines && Content.Length <= MaxPreviewLength)
            {
                return Content;
            }

            var preview = string.Join('\n', lines.Take(MaxPreviewLines));
            if (preview.Length > MaxPreviewLength)
            {
                preview = preview[..MaxPreviewLength];
            }

            var remainingLines = lines.Length - MaxPreviewLines;
            if (remainingLines > 0)
            {
                preview += $"\n// ... ({remainingLines} more lines)";
            }
            else if (Content.Length > preview.Length)
            {
                preview += "...";
            }

            return preview;
        }
    }

    /// <summary>
    /// Whether this is a partial selection (not full file).
    /// </summary>
    public bool IsPartialContent => StartLine.HasValue || EndLine.HasValue;

    /// <summary>
    /// Tooltip text for the context pill.
    /// </summary>
    public string Tooltip
    {
        get
        {
            var parts = new List<string> { DisplayLabel };

            if (!string.IsNullOrEmpty(Language))
            {
                parts.Add(Language);
            }

            parts.Add($"{LineCount} lines");
            parts.Add($"~{EstimatedTokens} tokens");

            return string.Join(" • ", parts);
        }
    }

    /// <summary>
    /// Icon key based on language/type.
    /// </summary>
    public string IconKey
    {
        get
        {
            if (AttachmentType == ContextAttachmentType.Clipboard)
            {
                return "ClipboardIcon";
            }

            if (AttachmentType == ContextAttachmentType.Selection)
            {
                return "SelectionIcon";
            }

            return Language switch
            {
                "csharp" => "CSharpIcon",
                "javascript" or "typescript" => "JavaScriptIcon",
                "python" => "PythonIcon",
                "json" => "JsonIcon",
                "xml" or "html" => "XmlIcon",
                "markdown" => "MarkdownIcon",
                _ => "FileCodeIcon"
            };
        }
    }

    /// <summary>
    /// Badge text (e.g., "C#", "JS", "SEL", "CLIP").
    /// </summary>
    public string Badge
    {
        get
        {
            if (AttachmentType == ContextAttachmentType.Selection)
            {
                return "SEL";
            }

            if (AttachmentType == ContextAttachmentType.Clipboard)
            {
                return "CLIP";
            }

            return Language?.ToUpperInvariant() switch
            {
                "CSHARP" => "C#",
                "JAVASCRIPT" => "JS",
                "TYPESCRIPT" => "TS",
                "PYTHON" => "PY",
                _ => Language?.ToUpperInvariant()?[..Math.Min(4, Language.Length)] ?? "TXT"
            };
        }
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a FileContextViewModel from a file path and content.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="content">File content.</param>
    /// <param name="estimatedTokens">Estimated token count.</param>
    /// <returns>A new FileContextViewModel for the file.</returns>
    public static FileContextViewModel FromFile(
        string filePath,
        string content,
        int estimatedTokens)
    {
        var fileName = Path.GetFileName(filePath);
        var language = LanguageDetector.DetectByFileName(fileName);

        return new FileContextViewModel
        {
            FilePath = filePath,
            FileName = fileName,
            Language = language,
            Content = content,
            EstimatedTokens = estimatedTokens,
            LineCount = CountLines(content),
            AttachmentType = ContextAttachmentType.File
        };
    }

    /// <summary>
    /// Creates a FileContextViewModel from a code selection.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="content">Selected content.</param>
    /// <param name="startLine">Starting line number (1-indexed).</param>
    /// <param name="endLine">Ending line number (1-indexed, inclusive).</param>
    /// <param name="estimatedTokens">Estimated token count.</param>
    /// <returns>A new FileContextViewModel for the selection.</returns>
    public static FileContextViewModel FromSelection(
        string filePath,
        string content,
        int startLine,
        int endLine,
        int estimatedTokens)
    {
        var fileName = Path.GetFileName(filePath);
        var language = LanguageDetector.DetectByFileName(fileName);

        return new FileContextViewModel
        {
            FilePath = filePath,
            FileName = fileName,
            Language = language,
            Content = content,
            EstimatedTokens = estimatedTokens,
            LineCount = endLine - startLine + 1,
            StartLine = startLine,
            EndLine = endLine,
            AttachmentType = ContextAttachmentType.Selection
        };
    }

    /// <summary>
    /// Creates a FileContextViewModel from clipboard content.
    /// </summary>
    /// <param name="content">Clipboard content.</param>
    /// <param name="language">Optional language identifier.</param>
    /// <param name="estimatedTokens">Estimated token count.</param>
    /// <returns>A new FileContextViewModel for clipboard content.</returns>
    public static FileContextViewModel FromClipboard(
        string content,
        string? language,
        int estimatedTokens)
    {
        return new FileContextViewModel
        {
            FileName = "Clipboard",
            Language = language,
            Content = content,
            EstimatedTokens = estimatedTokens,
            LineCount = CountLines(content),
            AttachmentType = ContextAttachmentType.Clipboard
        };
    }

    #endregion

    #region Conversion

    /// <summary>
    /// Converts to core FileContext model.
    /// </summary>
    /// <returns>A new FileContext with the ViewModel's data.</returns>
    public FileContext ToFileContext()
    {
        return new FileContext
        {
            Id = Id,
            FilePath = FilePath,
            Content = Content,
            Language = Language,
            LineCount = LineCount,
            EstimatedTokens = EstimatedTokens,
            AttachedAt = AttachedAt,
            StartLine = StartLine,
            EndLine = EndLine
        };
    }

    /// <summary>
    /// Creates a FileContextViewModel from a core FileContext model.
    /// </summary>
    /// <param name="context">The core model.</param>
    /// <returns>A new FileContextViewModel.</returns>
    public static FileContextViewModel FromFileContext(FileContext context)
    {
        return new FileContextViewModel
        {
            Id = context.Id,
            FilePath = context.FilePath,
            FileName = Path.GetFileName(context.FilePath),
            Language = context.Language,
            Content = context.Content,
            EstimatedTokens = context.EstimatedTokens,
            LineCount = context.LineCount,
            StartLine = context.StartLine,
            EndLine = context.EndLine,
            AttachmentType = context.IsPartialContent
                ? ContextAttachmentType.Selection
                : ContextAttachmentType.File,
            AttachedAt = context.AttachedAt
        };
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Counts lines in content (handles various line endings).
    /// </summary>
    private static int CountLines(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        return content.Count(c => c == '\n') + 1;
    }

    #endregion
}
