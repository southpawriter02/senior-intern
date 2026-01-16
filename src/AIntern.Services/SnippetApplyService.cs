using System.Text.RegularExpressions;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET APPLY SERVICE (v0.4.5d)                                         │
// │ Service for applying code snippets to specific file locations.          │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for applying code snippets to specific file locations.
/// Coordinates with diff, backup, and settings services for safe operations.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5d.</para>
/// </remarks>
public sealed partial class SnippetApplyService : ISnippetApplyService
{
    private readonly IDiffService _diffService;
    private readonly IBackupService _backupService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SnippetApplyService>? _logger;

    // Regex patterns for code structure detection
    [GeneratedRegex(@"^\s*(public|private|protected|internal|static|async|override|virtual|sealed|abstract)*\s*[\w<>\[\],\s]+\s+\w+\s*\(", RegexOptions.Compiled)]
    private static partial Regex MethodPattern();

    [GeneratedRegex(@"^\s*(public|private|protected|internal|static|sealed|abstract|partial)*\s*(class|interface|struct|record|enum)\s+\w+", RegexOptions.Compiled)]
    private static partial Regex ClassPattern();

    [GeneratedRegex(@"^\s*(public|private|protected|internal|static|virtual|override|abstract)*\s*[\w<>\[\],\?\s]+\s+\w+\s*\{", RegexOptions.Compiled)]
    private static partial Regex PropertyPattern();

    public SnippetApplyService(
        IDiffService diffService,
        IBackupService backupService,
        ISettingsService settingsService,
        ILogger<SnippetApplyService>? logger = null)
    {
        _diffService = diffService ?? throw new ArgumentNullException(nameof(diffService));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════════

    public async Task<SnippetApplyResult> ApplySnippetAsync(
        string filePath,
        string snippetContent,
        SnippetApplyOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(snippetContent);
        ArgumentNullException.ThrowIfNull(options);

        _logger?.LogDebug("ApplySnippetAsync starting for {FilePath}, mode: {Mode}", filePath, options.InsertMode);

        var (isValid, error) = options.Validate();
        if (!isValid)
        {
            _logger?.LogWarning("Invalid snippet apply options for {FilePath}: {Error}", filePath, error);
            return SnippetApplyResult.Failed(filePath, error ?? "Invalid options");
        }

        try
        {
            // Validate options against actual file
            var validation = await ValidateOptionsAsync(filePath, options, cancellationToken);
            if (!validation.IsValid)
            {
                var errors = string.Join("; ", validation.Errors);
                _logger?.LogWarning("Validation failed for {FilePath}: {Errors}", filePath, errors);
                return SnippetApplyResult.Failed(filePath, $"Validation failed: {errors}");
            }

            // Generate preview to compute result
            var preview = await PreviewSnippetAsync(filePath, snippetContent, options, cancellationToken);

            // Create backup if enabled
            var settings = _settingsService.CurrentSettings;
            string? backupPath = null;

            if (settings.CreateBackupBeforeApply && File.Exists(filePath))
            {
                try
                {
                    backupPath = await _backupService.CreateBackupAsync(filePath, cancellationToken);
                    _logger?.LogDebug("Created backup at {BackupPath}", backupPath);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to create backup for {FilePath}", filePath);
                    // Continue without backup - log warning only
                }
            }

            // Ensure directory exists for new files
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger?.LogDebug("Created directory {Directory}", directory);
            }

            // Write the result
            await File.WriteAllTextAsync(filePath, preview.ResultContent, cancellationToken);
            _logger?.LogInformation(
                "Applied snippet to {FilePath}: {LinesAdded} added, {LinesRemoved} removed",
                filePath, preview.LinesAdded, preview.LinesRemoved);

            return SnippetApplyResult.Succeeded(
                filePath,
                options,
                backupPath,
                linesModified: preview.AffectedRange.LineCount,
                linesAdded: preview.LinesAdded,
                linesRemoved: preview.LinesRemoved,
                diff: preview.Diff);
        }
        catch (SnippetApplyException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to apply snippet to {FilePath}", filePath);
            return SnippetApplyResult.Failed(filePath, ex.Message);
        }
    }

    public async Task<SnippetApplyPreview> PreviewSnippetAsync(
        string filePath,
        string snippetContent,
        SnippetApplyOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(snippetContent);
        ArgumentNullException.ThrowIfNull(options);

        _logger?.LogDebug("PreviewSnippetAsync for {FilePath}, mode: {Mode}", filePath, options.InsertMode);

        var originalContent = File.Exists(filePath)
            ? await File.ReadAllTextAsync(filePath, cancellationToken)
            : string.Empty;

        var originalLines = SplitLines(originalContent);
        var snippetLines = SplitLines(snippetContent);

        // Normalize and trim if requested
        if (options.NormalizeLineEndings)
            snippetLines = NormalizeLineEndings(snippetLines);
        if (options.TrimTrailingWhitespace)
            snippetLines = TrimTrailingWhitespace(snippetLines);

        // Adjust indentation if needed
        var resolvedOptions = options;
        if (resolvedOptions.PreserveIndentation && resolvedOptions.TargetLine.HasValue && 
            resolvedOptions.TargetLine.Value <= originalLines.Count)
        {
            var targetIndent = GetLineIndentation(originalLines, resolvedOptions.TargetLine.Value);
            snippetLines = AdjustIndentation(snippetLines, targetIndent, resolvedOptions.IndentationOverride);
        }

        // Apply the operation
        var (resultLines, affectedRange) = ApplyOperation(originalLines, snippetLines, resolvedOptions);

        // Handle blank lines
        resultLines = HandleBlankLines(resultLines, affectedRange, resolvedOptions, out affectedRange);

        var resultContent = JoinLines(resultLines);

        // Compute diff
        var diff = _diffService.ComputeDiff(originalContent, resultContent, filePath);

        // Collect warnings
        var warnings = CollectWarnings(resolvedOptions, originalLines, snippetLines);

        _logger?.LogDebug("Preview generated: {LinesAdded} added, {LinesRemoved} removed, {WarningCount} warnings",
            snippetLines.Count, GetLinesRemoved(resolvedOptions, originalLines.Count), warnings.Count);

        return new SnippetApplyPreview
        {
            ResultContent = resultContent,
            Diff = diff,
            AffectedRange = affectedRange,
            LinesAdded = snippetLines.Count,
            LinesRemoved = GetLinesRemoved(resolvedOptions, originalLines.Count),
            Warnings = warnings
        };
    }

    public async Task<IndentationStyle> DetectIndentationAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            _logger?.LogDebug("File does not exist, returning default indentation: {FilePath}", filePath);
            return IndentationStyle.Default;
        }

        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        return DetectIndentationFromContent(content);
    }

    public async Task<SnippetLocationSuggestion?> SuggestLocationAsync(
        string filePath,
        string snippetContent,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(snippetContent);

        // New file - suggest creating it
        if (!File.Exists(filePath))
        {
            _logger?.LogDebug("File does not exist, suggesting ReplaceFile: {FilePath}", filePath);
            return new SnippetLocationSuggestion
            {
                SuggestedMode = SnippetInsertMode.ReplaceFile,
                Confidence = 1.0,
                Reason = "File does not exist - will create new file"
            };
        }

        var fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
        var fileLines = SplitLines(fileContent);
        var snippetLines = SplitLines(snippetContent);

        var firstMeaningfulLine = snippetLines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
        if (firstMeaningfulLine is null)
        {
            return null;
        }

        var suggestion = TryMatchMethod(fileLines, firstMeaningfulLine)
                      ?? TryMatchClass(fileLines, firstMeaningfulLine)
                      ?? TryMatchProperty(fileLines, firstMeaningfulLine)
                      ?? SuggestDefaultLocation(fileLines);

        _logger?.LogDebug("Suggested location for {FilePath}: {Mode}, confidence: {Confidence}",
            filePath, suggestion.SuggestedMode, suggestion.Confidence);

        return suggestion;
    }

    public async Task<SnippetOptionsValidationResult> ValidateOptionsAsync(
        string filePath,
        SnippetApplyOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(options);

        var issues = new List<SnippetOptionsValidationIssue>();

        // Basic validation
        var (isValid, error) = options.Validate();
        if (!isValid)
        {
            issues.Add(new SnippetOptionsValidationIssue(
                SnippetOptionsValidationSeverity.Error, error ?? "Options are not valid"));
        }

        // File-specific validation
        if (!File.Exists(filePath))
        {
            if (options.InsertMode != SnippetInsertMode.ReplaceFile &&
                options.InsertMode != SnippetInsertMode.Append)
            {
                issues.Add(new SnippetOptionsValidationIssue(
                    SnippetOptionsValidationSeverity.Warning,
                    $"File does not exist. InsertMode '{options.InsertMode}' will be treated as ReplaceFile."));
            }
        }
        else
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var lineCount = SplitLines(content).Count;

            // Validate target line
            if (options.TargetLine.HasValue)
            {
                if (options.TargetLine.Value < 1)
                    issues.Add(new SnippetOptionsValidationIssue(
                        SnippetOptionsValidationSeverity.Error, "Target line must be >= 1"));
                else if (options.TargetLine.Value > lineCount + 1)
                    issues.Add(new SnippetOptionsValidationIssue(
                        SnippetOptionsValidationSeverity.Warning,
                        $"Target line {options.TargetLine.Value} exceeds file length ({lineCount} lines)"));
            }

            // Validate replace range
            if (options.ReplaceRange.HasValue)
            {
                var range = options.ReplaceRange.Value;
                if (!range.IsValid)
                    issues.Add(new SnippetOptionsValidationIssue(
                        SnippetOptionsValidationSeverity.Error, "Replace range is invalid"));
                else if (range.StartLine > lineCount)
                    issues.Add(new SnippetOptionsValidationIssue(
                        SnippetOptionsValidationSeverity.Error,
                        $"Replace range start ({range.StartLine}) exceeds file length ({lineCount} lines)"));
                else if (range.EndLine > lineCount)
                    issues.Add(new SnippetOptionsValidationIssue(
                        SnippetOptionsValidationSeverity.Warning,
                        $"Replace range end ({range.EndLine}) exceeds file length ({lineCount} lines) - will be clamped"));
            }
        }

        return new SnippetOptionsValidationResult(issues);
    }

    // ═══════════════════════════════════════════════════════════════
    // Private Helper Methods
    // ═══════════════════════════════════════════════════════════════

    private static List<string> SplitLines(string content)
    {
        if (string.IsNullOrEmpty(content))
            return [];
        return content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').ToList();
    }

    private static string JoinLines(List<string> lines) => string.Join('\n', lines);

    private static List<string> NormalizeLineEndings(List<string> lines) =>
        lines.Select(l => l.TrimEnd('\r')).ToList();

    private static List<string> TrimTrailingWhitespace(List<string> lines) =>
        lines.Select(l => l.TrimEnd()).ToList();

    private static string GetLineIndentation(List<string> lines, int lineNumber)
    {
        if (lineNumber <= 0 || lineNumber > lines.Count)
            return string.Empty;
        var line = lines[lineNumber - 1];
        return new string(line.TakeWhile(char.IsWhiteSpace).ToArray());
    }

    private static List<string> AdjustIndentation(List<string> lines, string targetIndent, string? indentOverride)
    {
        if (lines.Count == 0)
            return lines;

        var minIndent = lines
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => new string(l.TakeWhile(char.IsWhiteSpace).ToArray()))
            .OrderBy(i => i.Length)
            .FirstOrDefault() ?? string.Empty;

        var effectiveIndent = indentOverride ?? targetIndent;

        return lines.Select(line =>
        {
            if (string.IsNullOrWhiteSpace(line))
                return line;
            if (line.StartsWith(minIndent))
                return effectiveIndent + line[minIndent.Length..];
            return effectiveIndent + line;
        }).ToList();
    }

    private static (List<string> ResultLines, LineRange AffectedRange) ApplyOperation(
        List<string> originalLines,
        List<string> snippetLines,
        SnippetApplyOptions options)
    {
        var result = new List<string>(originalLines);
        LineRange affectedRange;

        switch (options.InsertMode)
        {
            case SnippetInsertMode.ReplaceFile:
                result = new List<string>(snippetLines);
                affectedRange = snippetLines.Count > 0 ? new LineRange(1, snippetLines.Count) : LineRange.Empty;
                break;

            case SnippetInsertMode.Replace:
                var range = options.ReplaceRange!.Value;
                var startIdx = Math.Max(0, range.StartLine - 1);
                var endIdx = Math.Min(result.Count, range.EndLine);
                var removeCount = Math.Max(0, endIdx - startIdx);

                if (removeCount > 0)
                    result.RemoveRange(startIdx, removeCount);
                result.InsertRange(startIdx, snippetLines);

                affectedRange = snippetLines.Count > 0
                    ? new LineRange(range.StartLine, range.StartLine + snippetLines.Count - 1)
                    : LineRange.Empty;
                break;

            case SnippetInsertMode.InsertBefore:
                var beforeIdx = Math.Max(0, options.TargetLine!.Value - 1);
                result.InsertRange(beforeIdx, snippetLines);
                affectedRange = snippetLines.Count > 0
                    ? new LineRange(options.TargetLine.Value, options.TargetLine.Value + snippetLines.Count - 1)
                    : LineRange.Empty;
                break;

            case SnippetInsertMode.InsertAfter:
                var afterIdx = Math.Min(result.Count, options.TargetLine!.Value);
                result.InsertRange(afterIdx, snippetLines);
                affectedRange = snippetLines.Count > 0
                    ? new LineRange(options.TargetLine.Value + 1, options.TargetLine.Value + snippetLines.Count)
                    : LineRange.Empty;
                break;

            case SnippetInsertMode.Append:
                var appendStart = result.Count + 1;
                result.AddRange(snippetLines);
                affectedRange = snippetLines.Count > 0
                    ? new LineRange(appendStart, appendStart + snippetLines.Count - 1)
                    : LineRange.Empty;
                break;

            case SnippetInsertMode.Prepend:
                result.InsertRange(0, snippetLines);
                affectedRange = snippetLines.Count > 0 ? new LineRange(1, snippetLines.Count) : LineRange.Empty;
                break;

            default:
                throw new ArgumentException($"Unknown insert mode: {options.InsertMode}");
        }

        return (result, affectedRange);
    }

    private static List<string> HandleBlankLines(
        List<string> resultLines,
        LineRange affectedRange,
        SnippetApplyOptions options,
        out LineRange newAffectedRange)
    {
        var result = new List<string>(resultLines);
        newAffectedRange = affectedRange;

        if (options.AddBlankLineBefore && affectedRange.StartLine > 1)
        {
            var insertIndex = affectedRange.StartLine - 1;
            if (insertIndex >= 0 && insertIndex < result.Count &&
                insertIndex > 0 && !string.IsNullOrWhiteSpace(result[insertIndex - 1]))
            {
                result.Insert(insertIndex, string.Empty);
                newAffectedRange = new LineRange(affectedRange.StartLine + 1, affectedRange.EndLine + 1);
            }
        }

        if (options.AddBlankLineAfter && newAffectedRange.EndLine <= result.Count)
        {
            var insertIndex = newAffectedRange.EndLine;
            if (insertIndex < result.Count && !string.IsNullOrWhiteSpace(result[insertIndex]))
            {
                result.Insert(insertIndex, string.Empty);
            }
        }

        return result;
    }

    private static List<string> CollectWarnings(
        SnippetApplyOptions options,
        List<string> originalLines,
        List<string> snippetLines)
    {
        var warnings = new List<string>();

        if (options.ReplaceRange.HasValue && options.ReplaceRange.Value.EndLine > originalLines.Count)
            warnings.Add($"Replace range extends beyond end of file (file has {originalLines.Count} lines)");

        if (snippetLines.Count == 0)
            warnings.Add("Snippet content is empty");

        return warnings;
    }

    private static int GetLinesRemoved(SnippetApplyOptions options, int originalLineCount) =>
        options.InsertMode switch
        {
            SnippetInsertMode.Replace => options.ReplaceRange?.LineCount ?? 0,
            SnippetInsertMode.ReplaceFile => originalLineCount,
            _ => 0
        };

    private IndentationStyle DetectIndentationFromContent(string content)
    {
        var lines = SplitLines(content);
        int tabCount = 0, spaceCount = 0;
        var spaceIndents = new Dictionary<int, int>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var leading = line.TakeWhile(char.IsWhiteSpace).ToArray();
            if (leading.Length == 0) continue;

            if (leading[0] == '\t')
                tabCount++;
            else if (leading[0] == ' ')
            {
                spaceCount++;
                var len = leading.TakeWhile(c => c == ' ').Count();
                spaceIndents[len] = spaceIndents.GetValueOrDefault(len) + 1;
            }
        }

        var total = tabCount + spaceCount;
        if (total == 0) return IndentationStyle.Default;

        if (tabCount > spaceCount)
            return new IndentationStyle { UseTabs = true, Confidence = tabCount / (double)total };

        var spacesPerIndent = 4;
        if (spaceIndents.Count > 0)
        {
            var indentLevels = spaceIndents.Keys.Where(k => k > 0).ToList();
            if (indentLevels.Count > 0)
            {
                spacesPerIndent = indentLevels.Aggregate(GCD);
                if (spacesPerIndent <= 0 || spacesPerIndent > 8)
                    spacesPerIndent = 4;
            }
        }

        return new IndentationStyle
        {
            UseTabs = false,
            SpacesPerIndent = spacesPerIndent,
            Confidence = spaceCount / (double)(total + 1)
        };
    }

    private SnippetLocationSuggestion? TryMatchMethod(List<string> fileLines, string snippetFirstLine)
    {
        if (!MethodPattern().IsMatch(snippetFirstLine))
            return null;

        for (int i = 0; i < fileLines.Count; i++)
        {
            if (MethodPattern().IsMatch(fileLines[i]))
            {
                var snippetMethodName = ExtractMethodName(snippetFirstLine);
                var fileMethodName = ExtractMethodName(fileLines[i]);

                if (snippetMethodName is not null &&
                    snippetMethodName.Equals(fileMethodName, StringComparison.Ordinal))
                {
                    var methodEnd = FindMethodEnd(fileLines, i);
                    return new SnippetLocationSuggestion
                    {
                        SuggestedMode = SnippetInsertMode.Replace,
                        SuggestedRange = new LineRange(i + 1, methodEnd + 1),
                        Confidence = 0.85,
                        Reason = $"Found existing method '{fileMethodName}' to replace",
                        MatchedContext = fileLines[i].Trim()
                    };
                }
            }
        }

        var lastBraceIndex = FindLastClosingBrace(fileLines);
        if (lastBraceIndex > 0)
        {
            return new SnippetLocationSuggestion
            {
                SuggestedMode = SnippetInsertMode.InsertBefore,
                SuggestedLine = lastBraceIndex + 1,
                Confidence = 0.5,
                Reason = "Insert new method before closing brace"
            };
        }

        return null;
    }

    private SnippetLocationSuggestion? TryMatchClass(List<string> fileLines, string snippetFirstLine)
    {
        if (!ClassPattern().IsMatch(snippetFirstLine))
            return null;

        return new SnippetLocationSuggestion
        {
            SuggestedMode = SnippetInsertMode.Append,
            Confidence = 0.6,
            Reason = "New class/interface - append to end of file"
        };
    }

    private SnippetLocationSuggestion? TryMatchProperty(List<string> fileLines, string snippetFirstLine)
    {
        if (!PropertyPattern().IsMatch(snippetFirstLine))
            return null;

        for (int i = 0; i < fileLines.Count; i++)
        {
            if (PropertyPattern().IsMatch(fileLines[i]))
            {
                var lastPropertyLine = i;
                for (int j = i + 1; j < fileLines.Count; j++)
                {
                    if (PropertyPattern().IsMatch(fileLines[j]))
                        lastPropertyLine = j;
                    else if (MethodPattern().IsMatch(fileLines[j]))
                        break;
                }

                var propertyEnd = FindPropertyEnd(fileLines, lastPropertyLine);
                return new SnippetLocationSuggestion
                {
                    SuggestedMode = SnippetInsertMode.InsertAfter,
                    SuggestedLine = propertyEnd + 1,
                    Confidence = 0.65,
                    Reason = "Insert after existing properties"
                };
            }
        }

        return null;
    }

    private static SnippetLocationSuggestion SuggestDefaultLocation(List<string> fileLines) =>
        new()
        {
            SuggestedMode = SnippetInsertMode.Append,
            Confidence = 0.3,
            Reason = "No specific location detected - appending to end"
        };

    private static string? ExtractMethodName(string line)
    {
        var parenIndex = line.IndexOf('(');
        if (parenIndex <= 0) return null;
        var beforeParen = line[..parenIndex].TrimEnd();
        var lastSpace = beforeParen.LastIndexOf(' ');
        return lastSpace < 0 ? beforeParen : beforeParen[(lastSpace + 1)..];
    }

    private static int FindMethodEnd(List<string> lines, int startLine)
    {
        int braceCount = 0;
        bool foundOpenBrace = false;

        for (int i = startLine; i < lines.Count; i++)
        {
            foreach (var c in lines[i])
            {
                if (c == '{') { braceCount++; foundOpenBrace = true; }
                else if (c == '}')
                {
                    braceCount--;
                    if (foundOpenBrace && braceCount == 0)
                        return i;
                }
            }
        }
        return lines.Count - 1;
    }

    private static int FindPropertyEnd(List<string> lines, int startLine)
    {
        int braceCount = 0;
        bool foundOpenBrace = false;

        for (int i = startLine; i < lines.Count; i++)
        {
            foreach (var c in lines[i])
            {
                if (c == '{') { braceCount++; foundOpenBrace = true; }
                else if (c == '}')
                {
                    braceCount--;
                    if (foundOpenBrace && braceCount == 0)
                        return i;
                }
            }
        }
        return startLine;
    }

    private static int FindLastClosingBrace(List<string> lines)
    {
        for (int i = lines.Count - 1; i >= 0; i--)
            if (lines[i].Trim() == "}") return i;
        return -1;
    }

    private static int GCD(int a, int b)
    {
        while (b != 0) { var temp = b; b = a % b; a = temp; }
        return Math.Abs(a);
    }
}
