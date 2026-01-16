namespace AIntern.Services;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;

/// <summary>
/// Detects and normalizes programming language identifiers (v0.4.1c).
/// </summary>
public sealed partial class LanguageDetectionService : ILanguageDetectionService
{
    private readonly ILogger<LanguageDetectionService>? _logger;

    // Language definitions with aliases and metadata
    private static readonly Dictionary<string, LanguageInfo> Languages = new(
        StringComparer.OrdinalIgnoreCase)
    {
        // === C-Family ===
        ["csharp"] = new("csharp", "C#", ".cs", new[] { "cs", "c#" }),
        ["c"] = new("c", "C", ".c", new[] { "h" }),
        ["cpp"] = new("cpp", "C++", ".cpp", new[] { "c++", "cxx", "cc", "hpp", "hxx" }),
        ["objective-c"] = new("objective-c", "Objective-C", ".m", new[] { "objc", "obj-c" }),

        // === Web ===
        ["javascript"] = new("javascript", "JavaScript", ".js", new[] { "js", "mjs", "cjs", "jsx" }),
        ["typescript"] = new("typescript", "TypeScript", ".ts", new[] { "ts", "tsx" }),
        ["html"] = new("html", "HTML", ".html", new[] { "htm" }),
        ["css"] = new("css", "CSS", ".css", Array.Empty<string>()),
        ["scss"] = new("scss", "SCSS", ".scss", new[] { "sass" }),
        ["less"] = new("less", "Less", ".less", Array.Empty<string>()),
        ["vue"] = new("vue", "Vue", ".vue", Array.Empty<string>()),
        ["svelte"] = new("svelte", "Svelte", ".svelte", Array.Empty<string>()),

        // === Scripting ===
        ["python"] = new("python", "Python", ".py", new[] { "py", "py3" }),
        ["ruby"] = new("ruby", "Ruby", ".rb", new[] { "rb" }),
        ["perl"] = new("perl", "Perl", ".pl", new[] { "pm" }),
        ["php"] = new("php", "PHP", ".php", Array.Empty<string>()),
        ["lua"] = new("lua", "Lua", ".lua", Array.Empty<string>()),

        // === JVM ===
        ["java"] = new("java", "Java", ".java", Array.Empty<string>()),
        ["kotlin"] = new("kotlin", "Kotlin", ".kt", new[] { "kts" }),
        ["scala"] = new("scala", "Scala", ".scala", Array.Empty<string>()),
        ["groovy"] = new("groovy", "Groovy", ".groovy", Array.Empty<string>()),
        ["clojure"] = new("clojure", "Clojure", ".clj", new[] { "cljs", "cljc" }),

        // === Systems ===
        ["rust"] = new("rust", "Rust", ".rs", new[] { "rs" }),
        ["go"] = new("go", "Go", ".go", new[] { "golang" }),
        ["swift"] = new("swift", "Swift", ".swift", Array.Empty<string>()),
        ["zig"] = new("zig", "Zig", ".zig", Array.Empty<string>()),

        // === Shell (IsShell = true) ===
        ["bash"] = new("bash", "Bash", ".sh", new[] { "sh", "shell", "zsh" }, true, false),
        ["powershell"] = new("powershell", "PowerShell", ".ps1", new[] { "ps", "ps1", "pwsh" }, true, false),
        ["cmd"] = new("cmd", "Command Prompt", ".cmd", new[] { "bat", "batch" }, true, false),
        ["fish"] = new("fish", "Fish", ".fish", Array.Empty<string>(), true, false),

        // === Config/Data (IsConfig = true) ===
        ["json"] = new("json", "JSON", ".json", new[] { "jsonc" }, false, true),
        ["yaml"] = new("yaml", "YAML", ".yaml", new[] { "yml" }, false, true),
        ["xml"] = new("xml", "XML", ".xml", new[] { "xsl", "xslt" }, false, true),
        ["toml"] = new("toml", "TOML", ".toml", Array.Empty<string>(), false, true),
        ["ini"] = new("ini", "INI", ".ini", new[] { "cfg", "conf" }, false, true),
        ["properties"] = new("properties", "Properties", ".properties", Array.Empty<string>(), false, true),
        ["env"] = new("env", "Environment", ".env", Array.Empty<string>(), false, true),

        // === Markup ===
        ["markdown"] = new("markdown", "Markdown", ".md", new[] { "md", "mdx" }),
        ["latex"] = new("latex", "LaTeX", ".tex", new[] { "tex" }),
        ["rst"] = new("rst", "reStructuredText", ".rst", Array.Empty<string>()),

        // === .NET Specific ===
        ["fsharp"] = new("fsharp", "F#", ".fs", new[] { "fs", "f#", "fsx", "fsi" }),
        ["vb"] = new("vb", "Visual Basic", ".vb", new[] { "vbnet", "visualbasic", "vb.net" }),
        ["razor"] = new("razor", "Razor", ".razor", new[] { "cshtml" }),
        ["axaml"] = new("axaml", "Avalonia XAML", ".axaml", Array.Empty<string>()),
        ["xaml"] = new("xaml", "XAML", ".xaml", Array.Empty<string>()),

        // === Database ===
        ["sql"] = new("sql", "SQL", ".sql", new[] { "mysql", "postgresql", "postgres", "sqlite", "tsql", "plsql" }),
        ["graphql"] = new("graphql", "GraphQL", ".graphql", new[] { "gql" }),

        // === Build/DevOps ===
        ["dockerfile"] = new("dockerfile", "Dockerfile", "", new[] { "docker" }),
        ["makefile"] = new("makefile", "Makefile", "", new[] { "make" }),
        ["terraform"] = new("terraform", "Terraform", ".tf", new[] { "tf", "hcl" }, false, true),

        // === Other ===
        ["regex"] = new("regex", "Regex", "", new[] { "regexp" }),
        ["diff"] = new("diff", "Diff", ".diff", new[] { "patch" }),
        ["plaintext"] = new("plaintext", "Plain Text", ".txt", new[] { "text", "txt" }),
        ["output"] = new("output", "Output", "", new[] { "log", "console", "terminal", "stdout", "stderr" }),
    };

    // Extension to language mapping (built from Languages)
    private static readonly Dictionary<string, string> ExtensionMap;

    // Alias to canonical ID mapping (built from Languages)
    private static readonly Dictionary<string, string> AliasMap;

    static LanguageDetectionService()
    {
        ExtensionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (id, info) in Languages)
        {
            // Map extension to language
            if (!string.IsNullOrEmpty(info.Extension))
            {
                ExtensionMap[info.Extension] = id;
            }

            // Map aliases to canonical ID
            foreach (var alias in info.Aliases)
            {
                AliasMap[alias] = id;
            }
            AliasMap[id] = id; // Map ID to itself
        }
    }

    public LanguageDetectionService(ILogger<LanguageDetectionService>? logger = null)
    {
        _logger = logger;
    }

    public (string? Language, string? DisplayLanguage) DetectLanguage(
        string? fenceLanguage,
        string content,
        string? filePath = null)
    {
        // Priority 1: Explicit fence language
        if (!string.IsNullOrWhiteSpace(fenceLanguage))
        {
            var normalized = NormalizeLanguageId(fenceLanguage.Trim());
            if (normalized != null)
            {
                _logger?.LogDebug("[INFO] Detected language '{Language}' from fence spec", normalized);
                return (normalized, GetDisplayName(normalized));
            }
        }

        // Priority 2: File extension
        if (!string.IsNullOrEmpty(filePath))
        {
            var ext = Path.GetExtension(filePath);
            if (!string.IsNullOrEmpty(ext))
            {
                var langFromExt = GetLanguageForExtension(ext);
                if (langFromExt != null)
                {
                    _logger?.LogDebug("[INFO] Detected language '{Language}' from extension '{Ext}'",
                        langFromExt, ext);
                    return (langFromExt, GetDisplayName(langFromExt));
                }
            }
        }

        // Priority 3: Content-based detection (heuristics)
        var detected = DetectFromContent(content);
        if (detected != null)
        {
            _logger?.LogDebug("[INFO] Detected language '{Language}' from content heuristics", detected);
            return (detected, GetDisplayName(detected));
        }

        return (null, null);
    }

    public string? NormalizeLanguageId(string languageAlias)
    {
        if (string.IsNullOrWhiteSpace(languageAlias))
            return null;

        return AliasMap.TryGetValue(languageAlias.Trim(), out var id) ? id : null;
    }

    public string? GetDisplayName(string languageId)
    {
        return Languages.TryGetValue(languageId, out var info) ? info.DisplayName : null;
    }

    public string? GetFileExtension(string languageId)
    {
        if (Languages.TryGetValue(languageId, out var info) &&
            !string.IsNullOrEmpty(info.Extension))
        {
            return info.Extension;
        }
        return null;
    }

    public string? GetLanguageForExtension(string extension)
    {
        var ext = extension.StartsWith('.') ? extension : $".{extension}";
        return ExtensionMap.TryGetValue(ext, out var lang) ? lang : null;
    }

    public bool IsShellLanguage(string? languageId)
    {
        if (string.IsNullOrEmpty(languageId))
            return false;
        return Languages.TryGetValue(languageId, out var info) && info.IsShell;
    }

    public bool IsConfigLanguage(string? languageId)
    {
        if (string.IsNullOrEmpty(languageId))
            return false;
        return Languages.TryGetValue(languageId, out var info) && info.IsConfig;
    }

    public IReadOnlyCollection<string> GetSupportedLanguages()
    {
        return Languages.Keys.ToList();
    }

    // === Content-Based Detection Heuristics ===

    [GeneratedRegex(@"^\s*(public|private|internal|protected)\s+(class|interface|record|struct)",
        RegexOptions.Multiline)]
    private static partial Regex CSharpClassPattern();

    [GeneratedRegex(@"^(def |class |import |from .+ import)", RegexOptions.Multiline)]
    private static partial Regex PythonPattern();

    [GeneratedRegex(@"^(fn |pub fn |use |mod |impl |struct |enum )", RegexOptions.Multiline)]
    private static partial Regex RustPattern();

    [GeneratedRegex(@"^(public |private |protected )?(class|interface|enum)\s+\w+")]
    private static partial Regex JavaPattern();

    [GeneratedRegex(@"^\w+:\s*$", RegexOptions.Multiline)]
    private static partial Regex YamlKeyPattern();

    [GeneratedRegex(@"^(SELECT|INSERT|UPDATE|DELETE|CREATE|ALTER|DROP)\s",
        RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex SqlPattern();

    [GeneratedRegex(@"^(const|let|var)\s+\w+\s*=")]
    private static partial Regex JsVarPattern();

    private static string? DetectFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var trimmed = content.TrimStart();

        // C# indicators
        if (trimmed.Contains("namespace ") && trimmed.Contains("using "))
            return "csharp";
        if (CSharpClassPattern().IsMatch(trimmed))
            return "csharp";

        // TypeScript/JavaScript
        if (trimmed.Contains("import ") && trimmed.Contains("from "))
            return trimmed.Contains(": ") || trimmed.Contains("<") ? "typescript" : "javascript";
        if (trimmed.Contains("export default") || trimmed.Contains("export class"))
            return "javascript";
        if (JsVarPattern().IsMatch(trimmed))
            return "javascript";

        // Python
        if (PythonPattern().IsMatch(trimmed))
            return "python";
        if (trimmed.StartsWith("#!/usr/bin/env python") || trimmed.StartsWith("#!/usr/bin/python"))
            return "python";

        // Go
        if (trimmed.StartsWith("package ") && trimmed.Contains("func "))
            return "go";

        // Rust
        if (RustPattern().IsMatch(trimmed))
            return "rust";

        // Java (must distinguish from C#)
        if (JavaPattern().IsMatch(trimmed) && !trimmed.Contains("namespace "))
            return "java";

        // JSON
        if ((trimmed.StartsWith('{') && trimmed.TrimEnd().EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.TrimEnd().EndsWith(']')))
            return "json";

        // YAML
        if (YamlKeyPattern().IsMatch(trimmed) && !trimmed.Contains('{'))
            return "yaml";

        // XML/HTML
        if (trimmed.StartsWith("<?xml") || trimmed.StartsWith("<Project") || trimmed.StartsWith("<Configuration"))
            return "xml";
        if (trimmed.StartsWith("<!DOCTYPE html") || trimmed.StartsWith("<html"))
            return "html";

        // Shell commands (single line with $ or #)
        if (trimmed.StartsWith("$ ") || (trimmed.StartsWith("# ") && !trimmed.Contains('\n')))
            return "bash";

        // SQL
        if (SqlPattern().IsMatch(trimmed))
            return "sql";

        return null;
    }

    // === Internal Types ===

    private sealed record LanguageInfo(
        string Id,
        string DisplayName,
        string Extension,
        string[] Aliases,
        bool IsShell = false,
        bool IsConfig = false);
}
