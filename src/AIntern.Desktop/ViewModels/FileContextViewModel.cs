using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;
using AIntern.Core.Utilities;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// Type of context attachment.
/// </summary>
public enum ContextAttachmentType
{
    /// <summary>Full file content.</summary>
    File,

    /// <summary>Code selection from a file.</summary>
    Selection,

    /// <summary>Content pasted from clipboard.</summary>
    Clipboard
}

/// <summary>
/// ViewModel for attached file contexts in the chat interface.
/// Manages display state, preview content, and user interactions.
/// </summary>
public sealed partial class FileContextViewModel : ViewModelBase
{
    private const int MaxPreviewLength = 500;
    private const int MaxPreviewLines = 15;
    private const int MaxShortLabelLength = 20;

    #region Observable Properties

    /// <summary>Unique identifier for the context.</summary>
    [ObservableProperty]
    private Guid _id;

    /// <summary>Absolute path to the file (empty for clipboard content).</summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>File name or "Clipboard" for clipboard content.</summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>Detected programming language.</summary>
    [ObservableProperty]
    private string? _language;

    /// <summary>The actual content.</summary>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>Estimated token count for LLM context.</summary>
    [ObservableProperty]
    private int _estimatedTokens;

    /// <summary>Total line count of the content.</summary>
    [ObservableProperty]
    private int _lineCount;

    /// <summary>Starting line number for selections (1-indexed).</summary>
    [ObservableProperty]
    private int? _startLine;

    /// <summary>Ending line number for selections (1-indexed, inclusive).</summary>
    [ObservableProperty]
    private int? _endLine;

    /// <summary>Type of attachment (File, Selection, or Clipboard).</summary>
    [ObservableProperty]
    private ContextAttachmentType _attachmentType;

    /// <summary>Whether the context preview is expanded in UI.</summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>When the context was attached.</summary>
    [ObservableProperty]
    private DateTime _attachedAt;

    /// <summary>Whether the user is hovering over this context in UI.</summary>
    [ObservableProperty]
    private bool _isHovered;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Display label showing filename with optional line range.
    /// Examples: "file.cs", "file.cs:10-25", "Clipboard"
    /// </summary>
    public string DisplayLabel => AttachmentType switch
    {
        ContextAttachmentType.Selection => $"{FileName}:{StartLine}-{EndLine}",
        ContextAttachmentType.Clipboard => "Clipboard",
        _ => FileName
    };

    /// <summary>
    /// Shortened label for compact UI (max 20 chars).
    /// </summary>
    public string ShortLabel
    {
        get
        {
            var label = DisplayLabel;
            if (label.Length <= MaxShortLabelLength)
                return label;

            return label[..(MaxShortLabelLength - 3)] + "...";
        }
    }

    /// <summary>
    /// Preview content (first 15 lines, max 500 chars).
    /// Includes truncation indicator if content is longer.
    /// </summary>
    public string PreviewContent
    {
        get
        {
            if (string.IsNullOrEmpty(Content))
                return string.Empty;

            var lines = Content.Split('\n');
            var previewLines = lines.Take(MaxPreviewLines).ToArray();
            var preview = string.Join('\n', previewLines);

            // Truncate to max length
            if (preview.Length > MaxPreviewLength)
            {
                preview = preview[..MaxPreviewLength];
            }

            // Add truncation indicator if needed
            var remainingLines = lines.Length - previewLines.Length;
            var wasTruncatedByLength = Content.Length > preview.Length && remainingLines == 0;

            if (remainingLines > 0)
            {
                preview += $"\n// ... ({remainingLines} more lines)";
            }
            else if (wasTruncatedByLength)
            {
                preview += "\n// ... (content truncated)";
            }

            return preview;
        }
    }

    /// <summary>
    /// Whether this is partial content (a selection rather than full file).
    /// </summary>
    public bool IsPartialContent => StartLine.HasValue || EndLine.HasValue;

    /// <summary>
    /// Tooltip text with detailed information.
    /// Format: "file.cs • csharp • 50 lines • ~200 tokens"
    /// </summary>
    public string Tooltip
    {
        get
        {
            var parts = new List<string> { FileName };

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
    /// Icon key for the context based on attachment type and language.
    /// </summary>
    public string IconKey => AttachmentType switch
    {
        ContextAttachmentType.Clipboard => "clipboard",
        ContextAttachmentType.Selection => "selection",
        _ => LanguageDetector.GetIconName(Language)
    };

    /// <summary>
    /// Short badge text for the context (max 4 chars).
    /// </summary>
    public string Badge => AttachmentType switch
    {
        ContextAttachmentType.Selection => "SEL",
        ContextAttachmentType.Clipboard => "CLIP",
        _ => GetLanguageBadge(Language)
    };

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a FileContextViewModel from a full file.
    /// </summary>
    public static FileContextViewModel FromFile(string filePath, string content, int estimatedTokens)
    {
        var fileName = Path.GetFileName(filePath);
        var language = LanguageDetector.DetectByFileName(fileName);
        var lineCount = CountLines(content);

        return new FileContextViewModel
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            FileName = fileName,
            Language = language,
            Content = content,
            EstimatedTokens = estimatedTokens,
            LineCount = lineCount,
            AttachmentType = ContextAttachmentType.File,
            AttachedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a FileContextViewModel from a code selection.
    /// </summary>
    public static FileContextViewModel FromSelection(
        string filePath,
        string content,
        int startLine,
        int endLine,
        int estimatedTokens)
    {
        var fileName = Path.GetFileName(filePath);
        var language = LanguageDetector.DetectByFileName(fileName);
        var lineCount = endLine - startLine + 1;

        return new FileContextViewModel
        {
            Id = Guid.NewGuid(),
            FilePath = filePath,
            FileName = fileName,
            Language = language,
            Content = content,
            EstimatedTokens = estimatedTokens,
            LineCount = lineCount,
            StartLine = startLine,
            EndLine = endLine,
            AttachmentType = ContextAttachmentType.Selection,
            AttachedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a FileContextViewModel from clipboard content.
    /// </summary>
    public static FileContextViewModel FromClipboard(string content, string? language, int estimatedTokens)
    {
        var lineCount = CountLines(content);

        return new FileContextViewModel
        {
            Id = Guid.NewGuid(),
            FilePath = string.Empty,
            FileName = "Clipboard",
            Language = language,
            Content = content,
            EstimatedTokens = estimatedTokens,
            LineCount = lineCount,
            AttachmentType = ContextAttachmentType.Clipboard,
            AttachedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Conversion

    /// <summary>
    /// Converts this ViewModel to a core FileContext model.
    /// </summary>
    public FileContext ToFileContext()
    {
        if (AttachmentType == ContextAttachmentType.Selection)
        {
            return FileContext.FromSelection(
                FilePath,
                Content,
                StartLine ?? 1,
                EndLine ?? LineCount);
        }

        // For File and Clipboard types
        var effectivePath = AttachmentType == ContextAttachmentType.Clipboard
            ? "clipboard://content"
            : FilePath;

        return FileContext.FromFile(effectivePath, Content);
    }

    #endregion

    #region Private Helpers

    private static int CountLines(string content)
        => string.IsNullOrEmpty(content) ? 0 : content.Count(c => c == '\n') + 1;

    private static string GetLanguageBadge(string? language)
    {
        if (string.IsNullOrEmpty(language))
            return "TXT";

        return language switch
        {
            "csharp" => "C#",
            "fsharp" => "F#",
            "javascript" or "javascriptreact" => "JS",
            "typescript" or "typescriptreact" => "TS",
            "python" => "PY",
            "ruby" => "RB",
            "go" => "GO",
            "rust" => "RS",
            "java" => "JAVA",
            "kotlin" => "KT",
            "swift" => "SW",
            "cpp" or "c" => "C++",
            "html" => "HTML",
            "css" or "scss" or "sass" or "less" => "CSS",
            "json" or "jsonc" => "JSON",
            "yaml" or "yml" => "YAML",
            "xml" => "XML",
            "markdown" or "mdx" => "MD",
            "sql" => "SQL",
            "shellscript" or "bash" => "SH",
            "powershell" => "PS",
            "dockerfile" => "DOCK",
            _ => language.Length <= 4
                ? language.ToUpperInvariant()
                : language[..4].ToUpperInvariant()
        };
    }

    #endregion
}
