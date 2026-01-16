namespace AIntern.Core.Models;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET CONTEXT (v0.4.5c)                                               │
// │ Context information about the file where a snippet will be applied.    │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Context information about the file where a snippet will be applied.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5c.</para>
/// </remarks>
public sealed class SnippetContext
{
    private readonly string[] _lines;

    /// <summary>
    /// Creates a new snippet context from file content.
    /// </summary>
    public SnippetContext(string filePath, string content)
    {
        FilePath = filePath;
        OriginalContent = content;
        
        // Split preserving empty lines
        _lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        
        // Detect line ending
        LineEnding = content.Contains("\r\n") ? "\r\n" : "\n";
        
        // Check for trailing newline
        HasTrailingNewline = content.EndsWith("\n");
        
        // Detect indentation
        DetectedIndentation = DetectIndentationStyle(_lines);
    }

    /// <summary>
    /// Path to the file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Original file content.
    /// </summary>
    public string OriginalContent { get; }

    /// <summary>
    /// File lines as a readonly list.
    /// </summary>
    public IReadOnlyList<string> OriginalLines => _lines;

    /// <summary>
    /// Total number of lines in the file.
    /// </summary>
    public int LineCount => _lines.Length;

    /// <summary>
    /// Detected indentation style.
    /// </summary>
    public IndentationStyle DetectedIndentation { get; }

    /// <summary>
    /// Line ending used in the file (\n or \r\n).
    /// </summary>
    public string LineEnding { get; }

    /// <summary>
    /// Whether the file ends with a newline.
    /// </summary>
    public bool HasTrailingNewline { get; }

    // ═══════════════════════════════════════════════════════════════
    // Query Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets a specific line (1-indexed).
    /// </summary>
    public string GetLine(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > LineCount)
            throw new ArgumentOutOfRangeException(nameof(lineNumber));
        return _lines[lineNumber - 1];
    }

    /// <summary>
    /// Gets lines in a range.
    /// </summary>
    public IEnumerable<string> GetLines(LineRange range)
    {
        if (!range.IsValid) yield break;
        
        var start = Math.Max(1, range.StartLine);
        var end = Math.Min(LineCount, range.EndLine);
        
        for (int i = start; i <= end; i++)
            yield return GetLine(i);
    }

    /// <summary>
    /// Gets the indentation of a specific line.
    /// </summary>
    public string GetLineIndentation(int lineNumber)
    {
        var line = GetLine(lineNumber);
        var indent = new System.Text.StringBuilder();
        
        foreach (var c in line)
        {
            if (c == ' ' || c == '\t')
                indent.Append(c);
            else
                break;
        }
        
        return indent.ToString();
    }

    /// <summary>
    /// Gets the indentation level of a specific line.
    /// </summary>
    public int GetIndentLevel(int lineNumber)
    {
        var indent = GetLineIndentation(lineNumber);
        if (DetectedIndentation.UseTabs)
            return indent.Count(c => c == '\t');
        return indent.Length / DetectedIndentation.SpacesPerIndent;
    }

    /// <summary>
    /// Finds the first occurrence of text and returns its line number.
    /// </summary>
    public int? FindText(string searchText, bool caseSensitive = true)
    {
        var comparison = caseSensitive 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;
            
        for (int i = 0; i < _lines.Length; i++)
        {
            if (_lines[i].Contains(searchText, comparison))
                return i + 1; // 1-indexed
        }
        return null;
    }

    /// <summary>
    /// Finds all occurrences of text and returns their line numbers.
    /// </summary>
    public IReadOnlyList<int> FindAllText(string searchText, bool caseSensitive = true)
    {
        var comparison = caseSensitive 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;
            
        var results = new List<int>();
        for (int i = 0; i < _lines.Length; i++)
        {
            if (_lines[i].Contains(searchText, comparison))
                results.Add(i + 1); // 1-indexed
        }
        return results;
    }

    // ═══════════════════════════════════════════════════════════════
    // Static Factory
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a context for an empty/new file.
    /// </summary>
    public static SnippetContext Empty(string filePath) => new(filePath, string.Empty);

    /// <summary>
    /// Creates a context from file path by reading the file.
    /// </summary>
    public static async Task<SnippetContext> FromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var content = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken);
        return new SnippetContext(filePath, content);
    }

    // ═══════════════════════════════════════════════════════════════
    // Private Helpers
    // ═══════════════════════════════════════════════════════════════

    private static IndentationStyle DetectIndentationStyle(string[] lines)
    {
        int tabCount = 0;
        int spaceCount = 0;
        int twoSpaceCount = 0;
        int fourSpaceCount = 0;
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            var indent = GetLeadingWhitespace(line);
            if (indent.Length == 0) continue;
            
            if (indent[0] == '\t')
            {
                tabCount++;
            }
            else if (indent[0] == ' ')
            {
                spaceCount++;
                if (indent.Length >= 2 && indent.Length % 2 == 0)
                    twoSpaceCount++;
                if (indent.Length >= 4 && indent.Length % 4 == 0)
                    fourSpaceCount++;
            }
        }
        
        if (tabCount > spaceCount)
            return IndentationStyle.Tabs with { Confidence = tabCount / (double)(tabCount + spaceCount) };
        
        if (spaceCount == 0)
            return IndentationStyle.Unknown;
        
        // Prefer 4 spaces unless 2 spaces is clearly more common
        if (twoSpaceCount > fourSpaceCount * 1.5)
            return IndentationStyle.TwoSpaces with { Confidence = 0.8 };
        
        return IndentationStyle.Default with { Confidence = 0.8 };
    }

    private static string GetLeadingWhitespace(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
            i++;
        return line[..i];
    }
}
