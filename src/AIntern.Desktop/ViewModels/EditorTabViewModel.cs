using System;
using System.IO;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Utilities;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for a single editor tab representing an open file.
/// </summary>
public partial class EditorTabViewModel : ViewModelBase, IDisposable
{
    #region Observable Properties

    /// <summary>Unique identifier for this tab.</summary>
    [ObservableProperty]
    private Guid _id = Guid.NewGuid();

    /// <summary>Full path to the file on disk. Empty for new unsaved files.</summary>
    [ObservableProperty]
    private string _filePath = string.Empty;

    /// <summary>File name without path (e.g., "main.cs").</summary>
    [ObservableProperty]
    private string _fileName = string.Empty;

    /// <summary>The AvaloniaEdit document containing the text content.</summary>
    [ObservableProperty]
    private TextDocument _document = new();

    /// <summary>Detected language identifier (e.g., "csharp", "javascript").</summary>
    [ObservableProperty]
    private string? _language;

    /// <summary>Whether the document has unsaved changes.</summary>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>Whether this tab is currently active/focused.</summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>Whether the file is read-only.</summary>
    [ObservableProperty]
    private bool _isReadOnly;

    /// <summary>Current caret line number (1-based).</summary>
    [ObservableProperty]
    private int _caretLine = 1;

    /// <summary>Current caret column number (1-based).</summary>
    [ObservableProperty]
    private int _caretColumn = 1;

    /// <summary>Character count of current selection (0 if no selection).</summary>
    [ObservableProperty]
    private int _selectionLength;

    /// <summary>File encoding (e.g., "UTF-8", "UTF-16").</summary>
    [ObservableProperty]
    private string _encoding = "UTF-8";

    /// <summary>Line ending type (e.g., "LF", "CRLF").</summary>
    [ObservableProperty]
    private string _lineEnding = "LF";

    #endregion

    #region Internal State

    /// <summary>Hash of the content when last saved, used for dirty detection.</summary>
    private int _savedContentHash;

    /// <summary>Whether the tab has been disposed.</summary>
    private bool _disposed;

    #endregion

    #region Computed Properties

    /// <summary>Display title for the tab (includes dirty indicator).</summary>
    public string DisplayTitle => IsDirty ? $"{FileName} •" : FileName;

    /// <summary>Whether this is a new file that hasn't been saved yet.</summary>
    public bool IsNewFile => string.IsNullOrEmpty(FilePath);

    /// <summary>Total line count in the document.</summary>
    public int LineCount => Document.LineCount;

    /// <summary>File extension including the dot (e.g., ".cs").</summary>
    public string Extension => Path.GetExtension(FileName);

    /// <summary>Cursor position display string (e.g., "Ln 45, Col 12").</summary>
    public string CursorPositionDisplay => SelectionLength > 0
        ? $"Ln {CaretLine}, Col {CaretColumn} ({SelectionLength} selected)"
        : $"Ln {CaretLine}, Col {CaretColumn}";

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new EditorTabViewModel for an existing file.
    /// </summary>
    /// <param name="filePath">Full path to the file.</param>
    /// <param name="content">File content as string.</param>
    /// <returns>Configured EditorTabViewModel.</returns>
    public static EditorTabViewModel FromFile(string filePath, string content)
    {
        var tab = new EditorTabViewModel
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Language = LanguageDetector.DetectByFileName(Path.GetFileName(filePath)),
        };

        // Detect encoding and line endings from content
        tab.DetectEncodingAndLineEnding(content);

        // Set document content
        tab.Document.Text = content;
        tab._savedContentHash = content.GetHashCode();

        // Subscribe to document changes
        tab.Document.TextChanged += tab.OnDocumentTextChanged;

        return tab;
    }

    /// <summary>
    /// Creates a new EditorTabViewModel for a new unsaved file.
    /// </summary>
    /// <param name="suggestedName">Optional file name (default: "Untitled").</param>
    /// <param name="language">Optional language identifier.</param>
    /// <returns>Configured EditorTabViewModel.</returns>
    public static EditorTabViewModel CreateNew(string? suggestedName = null, string? language = null)
    {
        var fileName = suggestedName ?? "Untitled";

        var tab = new EditorTabViewModel
        {
            FileName = fileName,
            Language = language,
            IsDirty = false, // New empty files start as not dirty
        };

        tab._savedContentHash = string.Empty.GetHashCode();
        tab.Document.TextChanged += tab.OnDocumentTextChanged;

        return tab;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Marks the document as saved, resetting dirty state.
    /// </summary>
    /// <param name="newFilePath">Optional new file path (for Save As).</param>
    public void MarkAsSaved(string? newFilePath = null)
    {
        if (!string.IsNullOrEmpty(newFilePath))
        {
            FilePath = newFilePath;
            FileName = Path.GetFileName(newFilePath);
            Language = LanguageDetector.DetectByFileName(FileName);
        }

        _savedContentHash = Document.Text.GetHashCode();
        IsDirty = false;
        OnPropertyChanged(nameof(DisplayTitle));
        OnPropertyChanged(nameof(IsNewFile));
    }

    /// <summary>
    /// Gets the current document content as a string.
    /// </summary>
    public string GetContent() => Document.Text;

    /// <summary>
    /// Updates caret position from the editor.
    /// </summary>
    /// <param name="line">Line number (1-based).</param>
    /// <param name="column">Column number (1-based).</param>
    /// <param name="selectionLength">Number of selected characters.</param>
    public void UpdateCaretPosition(int line, int column, int selectionLength = 0)
    {
        CaretLine = line;
        CaretColumn = column;
        SelectionLength = selectionLength;
        OnPropertyChanged(nameof(CursorPositionDisplay));
    }

    /// <summary>
    /// Gets the document offset for a specific line number.
    /// </summary>
    /// <param name="lineNumber">Line number (1-based).</param>
    /// <returns>Character offset in document.</returns>
    public int GetOffsetForLine(int lineNumber)
    {
        if (lineNumber < 1) lineNumber = 1;
        if (lineNumber > Document.LineCount) lineNumber = Document.LineCount;

        return Document.GetLineByNumber(lineNumber).Offset;
    }

    #endregion

    #region Private Methods

    private void OnDocumentTextChanged(object? sender, EventArgs e)
    {
        var currentHash = Document.Text.GetHashCode();
        var wasDirty = IsDirty;
        IsDirty = currentHash != _savedContentHash;

        if (wasDirty != IsDirty)
        {
            OnPropertyChanged(nameof(DisplayTitle));
        }

        OnPropertyChanged(nameof(LineCount));
    }

    private void DetectEncodingAndLineEnding(string content)
    {
        // Detect line ending
        if (content.Contains("\r\n"))
        {
            LineEnding = "CRLF";
        }
        else if (content.Contains('\r'))
        {
            LineEnding = "CR";
        }
        else
        {
            LineEnding = "LF";
        }

        // Encoding is typically determined when reading the file
        // Default to UTF-8; actual detection happens in FileSystemService
        Encoding = "UTF-8";
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Document.TextChanged -= OnDocumentTextChanged;
    }

    #endregion
}
