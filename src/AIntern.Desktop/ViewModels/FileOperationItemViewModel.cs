namespace AIntern.Desktop.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;
using AIntern.Core.Interfaces;
using Microsoft.Extensions.Logging;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ FILE OPERATION ITEM VIEWMODEL (v0.4.4d)                                  │
// │ ViewModel for a single item (file or directory) in a proposal tree.     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// ViewModel representing a single item (file or directory) in a file operation tree.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel is specifically for displaying file operations from a <see cref="FileTreeProposal"/>.
/// It differs from <see cref="FileTreeItemViewModel"/> which is used for the file explorer with
/// lazy loading and rename support.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item>Selection state for batch operations</item>
/// <item>Validation issue display</item>
/// <item>Operation status tracking (Pending, Applied, Failed)</item>
/// <item>File type icons based on extension</item>
/// </list>
/// </para>
/// </remarks>
public partial class FileOperationItemViewModel : ViewModelBase
{
    private readonly ILogger<FileOperationItemViewModel>? _logger;

    #region Identity Properties

    /// <summary>
    /// Display name of the item (file or directory name without path).
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Relative path from the workspace root.
    /// </summary>
    [ObservableProperty]
    private string _path = string.Empty;

    /// <summary>
    /// Whether this item represents a directory.
    /// </summary>
    [ObservableProperty]
    private bool _isDirectory;

    /// <summary>
    /// The underlying FileOperation for file items. Null for directory items.
    /// </summary>
    [ObservableProperty]
    private FileOperation? _operation;

    #endregion

    #region State Properties

    /// <summary>
    /// Whether a directory node is expanded in the tree view.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Icon))]
    private bool _isExpanded = true;

    /// <summary>
    /// Whether a file is selected for creation/modification.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected = true;

    /// <summary>
    /// Whether the item is enabled for interaction.
    /// </summary>
    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>
    /// Whether the item should be visually highlighted.
    /// </summary>
    [ObservableProperty]
    private bool _isHighlighted;

    /// <summary>
    /// Selection state for directory items (None, Some, All).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Icon))]
    private SelectionState _selectionState = SelectionState.All;

    #endregion

    #region Display Properties

    /// <summary>
    /// Programming language identifier for syntax highlighting indication.
    /// </summary>
    [ObservableProperty]
    private string? _language;

    /// <summary>
    /// Whether a file with this path already exists in the workspace.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _fileExists;

    /// <summary>
    /// Validation issue associated with this item.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWarning))]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(HasValidationIssue))]
    [NotifyPropertyChangedFor(nameof(Tooltip))]
    private ValidationIssue? _validationIssue;

    /// <summary>
    /// Current status of the file operation (during/after apply).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusIcon))]
    private FileOperationStatus? _operationStatus;

    /// <summary>
    /// Child items (files and subdirectories).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<FileOperationItemViewModel> _children = [];

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this item represents a file (not a directory).
    /// </summary>
    public bool IsFile => !IsDirectory;

    /// <summary>
    /// Whether this item has child items.
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Whether the validation issue is a warning.
    /// </summary>
    public bool HasWarning => ValidationIssue?.Severity == ValidationSeverity.Warning;

    /// <summary>
    /// Whether the validation issue is an error.
    /// </summary>
    public bool HasError => ValidationIssue?.Severity == ValidationSeverity.Error;

    /// <summary>
    /// Whether there is any validation issue.
    /// </summary>
    public bool HasValidationIssue => ValidationIssue != null;

    /// <summary>
    /// Icon resource name based on item type and state.
    /// </summary>
    public string Icon => IsDirectory
        ? (IsExpanded ? "FolderOpen" : "Folder")
        : GetFileIcon();

    /// <summary>
    /// Status text displayed next to the item.
    /// </summary>
    public string StatusText => OperationStatus switch
    {
        FileOperationStatus.Applied => "Created",
        FileOperationStatus.Failed => "Failed",
        FileOperationStatus.Skipped => "Skipped",
        FileOperationStatus.InProgress => "Creating...",
        _ => FileExists ? "Exists" : ""
    };

    /// <summary>
    /// Status icon resource name based on operation status.
    /// </summary>
    public string? StatusIcon => OperationStatus switch
    {
        FileOperationStatus.Applied => "Check",
        FileOperationStatus.Failed => "Error",
        FileOperationStatus.Skipped => "Skip",
        FileOperationStatus.InProgress => "Spinner",
        _ => null
    };

    /// <summary>
    /// Tooltip text with path and any validation messages.
    /// </summary>
    public string Tooltip
    {
        get
        {
            var lines = new List<string> { Path };

            if (ValidationIssue != null)
            {
                lines.Add(string.Empty);
                lines.Add(ValidationIssue.Message);
                if (!string.IsNullOrEmpty(ValidationIssue.SuggestedFix))
                {
                    lines.Add($"Suggestion: {ValidationIssue.SuggestedFix}");
                }
            }

            if (Operation != null)
            {
                lines.Add(string.Empty);
                lines.Add($"Lines: {Operation.LineCount}");
                lines.Add($"Size: {FormatSize(Operation.ContentSizeBytes)}");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>
    /// File extension without the dot.
    /// </summary>
    public string Extension => System.IO.Path.GetExtension(Path).TrimStart('.');

    /// <summary>
    /// Whether this is a new file (Create operation) vs modification.
    /// </summary>
    public bool IsNewFile => Operation?.Type == FileOperationType.Create;

    /// <summary>
    /// Whether this is a modification of an existing file.
    /// </summary>
    public bool IsModification => Operation?.Type == FileOperationType.Modify;

    /// <summary>
    /// Depth level in the tree (0 = root).
    /// </summary>
    public int Depth => Path.Count(c => c == '/' || c == '\\');

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new FileOperationItemViewModel.
    /// </summary>
    public FileOperationItemViewModel()
    {
    }

    /// <summary>
    /// Creates a new FileOperationItemViewModel with logging.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public FileOperationItemViewModel(ILogger<FileOperationItemViewModel>? logger)
    {
        _logger = logger;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a FileOperationItemViewModel from a TreeNode.
    /// </summary>
    /// <param name="node">The tree node to convert.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>A new ViewModel instance.</returns>
    public static FileOperationItemViewModel FromTreeNode(
        TreeNode node,
        ILogger<FileOperationItemViewModel>? logger = null)
    {
        var vm = new FileOperationItemViewModel(logger)
        {
            Name = node.Name,
            Path = node.Path,
            IsDirectory = node.IsDirectory,
            Operation = node.Operation,
            Language = node.Operation?.DisplayLanguage,
            IsSelected = node.Operation?.IsSelected ?? true
        };

        // Recursively create children
        foreach (var child in node.Children)
        {
            vm.Children.Add(FromTreeNode(child, logger));
        }

        return vm;
    }

    /// <summary>
    /// Creates a directory node.
    /// </summary>
    /// <param name="name">Directory name.</param>
    /// <param name="path">Directory path.</param>
    /// <param name="expanded">Whether to expand by default.</param>
    /// <returns>A new directory ViewModel.</returns>
    public static FileOperationItemViewModel CreateDirectory(
        string name,
        string path,
        bool expanded = true)
    {
        return new FileOperationItemViewModel
        {
            Name = name,
            Path = path,
            IsDirectory = true,
            IsExpanded = expanded
        };
    }

    /// <summary>
    /// Creates a file node from a FileOperation.
    /// </summary>
    /// <param name="operation">The file operation.</param>
    /// <returns>A new file ViewModel.</returns>
    public static FileOperationItemViewModel CreateFile(FileOperation operation)
    {
        return new FileOperationItemViewModel
        {
            Name = operation.FileName,
            Path = operation.Path,
            IsDirectory = false,
            Operation = operation,
            Language = operation.DisplayLanguage,
            IsSelected = operation.IsSelected
        };
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the appropriate icon resource name based on file extension.
    /// </summary>
    private string GetFileIcon()
    {
        return System.IO.Path.GetExtension(Path).ToLowerInvariant() switch
        {
            ".cs" => "CSharp",
            ".ts" or ".tsx" => "TypeScript",
            ".js" or ".jsx" or ".mjs" => "JavaScript",
            ".py" => "Python",
            ".json" => "Json",
            ".xml" or ".axaml" or ".xaml" => "Xml",
            ".md" or ".markdown" => "Markdown",
            ".html" or ".htm" => "Html",
            ".css" or ".scss" or ".sass" or ".less" => "Css",
            ".yaml" or ".yml" => "Yaml",
            ".sql" => "Sql",
            ".sh" or ".bash" => "Bash",
            ".ps1" => "PowerShell",
            ".java" => "Java",
            ".rb" => "Ruby",
            ".go" => "Go",
            ".rs" => "Rust",
            ".cpp" or ".cc" or ".cxx" or ".c" or ".h" or ".hpp" => "Cpp",
            ".swift" => "Swift",
            ".kt" or ".kts" => "Kotlin",
            ".php" => "Php",
            ".vue" => "Vue",
            ".svelte" => "Svelte",
            ".razor" => "Razor",
            ".sln" or ".csproj" or ".fsproj" or ".vbproj" => "Project",
            ".gitignore" or ".gitattributes" => "Git",
            ".dockerfile" or ".dockerignore" => "Docker",
            ".env" or ".env.example" => "Env",
            _ => "File"
        };
    }

    /// <summary>
    /// Formats a byte size as a human-readable string.
    /// </summary>
    private static string FormatSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB"];
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:0.##} {suffixes[suffixIndex]}";
    }

    /// <summary>
    /// Updates the ViewModel state from the associated FileOperation.
    /// </summary>
    public void UpdateFromOperation()
    {
        if (Operation == null)
            return;

        _logger?.LogDebug("Updating item {Path} from operation", Path);

        IsSelected = Operation.IsSelected;
        OperationStatus = Operation.Status;
        Language = Operation.DisplayLanguage;
    }

    /// <summary>
    /// Clears validation state from this item.
    /// </summary>
    public void ClearValidation()
    {
        _logger?.LogDebug("Clearing validation for {Path}", Path);

        ValidationIssue = null;
        FileExists = false;
    }

    /// <summary>
    /// Resets the operation status to pending.
    /// </summary>
    public void ResetStatus()
    {
        _logger?.LogDebug("Resetting status for {Path}", Path);

        OperationStatus = null;
    }

    #endregion

    #region Partial Methods

    partial void OnIsExpandedChanged(bool value)
    {
        _logger?.LogTrace("IsExpanded changed to {Value} for {Path}", value, Path);
        OnPropertyChanged(nameof(Icon));
    }

    partial void OnIsSelectedChanged(bool value)
    {
        _logger?.LogTrace("IsSelected changed to {Value} for {Path}", value, Path);

        // Sync selection state to the underlying operation
        if (Operation != null)
        {
            Operation.IsSelected = value;
        }
    }

    #endregion
}
