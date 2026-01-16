namespace AIntern.Desktop.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using AIntern.Core.Models;

/// <summary>
/// ViewModel for a single Quick Open result item.
/// </summary>
/// <remarks>Added in v0.3.5c.</remarks>
public partial class QuickOpenItemViewModel : ViewModelBase
{
    /// <summary>
    /// Full path to the file.
    /// </summary>
    [ObservableProperty]
    private string _filePath;

    /// <summary>
    /// File name without path.
    /// </summary>
    [ObservableProperty]
    private string _fileName;

    /// <summary>
    /// Path relative to workspace.
    /// </summary>
    [ObservableProperty]
    private string _relativePath;

    /// <summary>
    /// Detected language.
    /// </summary>
    [ObservableProperty]
    private string? _language;

    /// <summary>
    /// Whether this is a recently opened file.
    /// </summary>
    [ObservableProperty]
    private bool _isRecent;

    /// <summary>
    /// Indices of matched characters in file name.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<int> _matchedIndices;

    /// <summary>
    /// Gets the icon key based on language.
    /// </summary>
    public string IconKey => Language switch
    {
        "csharp" => "CSharpIcon",
        "javascript" or "typescript" => "JavaScriptIcon",
        "python" => "PythonIcon",
        "json" => "JsonIcon",
        "xml" or "xaml" => "XmlIcon",
        "markdown" => "MarkdownIcon",
        _ => "FileCodeIcon"
    };

    /// <summary>
    /// Creates a new item from a search result.
    /// </summary>
    /// <param name="result">Search result.</param>
    /// <param name="isRecent">Whether this is a recent file.</param>
    public QuickOpenItemViewModel(FileSearchResult result, bool isRecent = false)
    {
        _filePath = result.FilePath;
        _fileName = result.FileName;
        _relativePath = result.RelativePath;
        _language = result.Language;
        _matchedIndices = result.MatchedIndices;
        _isRecent = isRecent;
    }
}
