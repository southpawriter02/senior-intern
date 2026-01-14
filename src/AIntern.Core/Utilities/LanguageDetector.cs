namespace AIntern.Core.Utilities;

/// <summary>
/// Detects programming language from file extensions and names.
/// Used for syntax highlighting hints and token estimation adjustments.
/// </summary>
public static class LanguageDetector
{
    /// <summary>
    /// Maps file extensions to their programming language identifiers.
    /// Keys are lowercase with leading dot (e.g., ".cs").
    /// </summary>
    private static readonly Dictionary<string, string> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // .NET / C#
        [".cs"] = "csharp",
        [".csx"] = "csharp",
        [".vb"] = "vb",
        [".fs"] = "fsharp",
        [".fsx"] = "fsharp",

        // JavaScript / TypeScript
        [".js"] = "javascript",
        [".mjs"] = "javascript",
        [".cjs"] = "javascript",
        [".jsx"] = "javascriptreact",
        [".ts"] = "typescript",
        [".tsx"] = "typescriptreact",

        // Python
        [".py"] = "python",
        [".pyw"] = "python",
        [".pyi"] = "python",

        // JVM
        [".java"] = "java",
        [".kt"] = "kotlin",
        [".kts"] = "kotlin",
        [".scala"] = "scala",
        [".groovy"] = "groovy",

        // Systems
        [".c"] = "c",
        [".h"] = "c",
        [".cpp"] = "cpp",
        [".hpp"] = "cpp",
        [".cc"] = "cpp",
        [".cxx"] = "cpp",
        [".rs"] = "rust",
        [".go"] = "go",
        [".swift"] = "swift",

        // Shell / Scripts
        [".sh"] = "shellscript",
        [".bash"] = "shellscript",
        [".zsh"] = "shellscript",
        [".fish"] = "shellscript",
        [".ps1"] = "powershell",
        [".psm1"] = "powershell",
        [".bat"] = "bat",
        [".cmd"] = "bat",

        // Web
        [".html"] = "html",
        [".htm"] = "html",
        [".css"] = "css",
        [".scss"] = "scss",
        [".sass"] = "sass",
        [".less"] = "less",
        [".vue"] = "vue",
        [".svelte"] = "svelte",

        // Data / Config
        [".json"] = "json",
        [".jsonc"] = "jsonc",
        [".xml"] = "xml",
        [".yaml"] = "yaml",
        [".yml"] = "yaml",
        [".toml"] = "toml",
        [".ini"] = "ini",
        [".env"] = "properties",
        [".properties"] = "properties",

        // Markup / Documentation
        [".md"] = "markdown",
        [".mdx"] = "mdx",
        [".rst"] = "restructuredtext",
        [".tex"] = "latex",
        [".adoc"] = "asciidoc",

        // .NET Project Files
        [".csproj"] = "xml",
        [".fsproj"] = "xml",
        [".vbproj"] = "xml",
        [".props"] = "xml",
        [".targets"] = "xml",
        [".axaml"] = "xml",
        [".xaml"] = "xml",
        [".sln"] = "text",

        // Database
        [".sql"] = "sql",

        // Ruby
        [".rb"] = "ruby",
        [".erb"] = "erb",
        [".rake"] = "ruby",

        // PHP
        [".php"] = "php",

        // Other
        [".dockerfile"] = "dockerfile",
        [".dockerignore"] = "ignore",
        [".gitignore"] = "ignore",
        [".gitattributes"] = "properties",
        [".editorconfig"] = "editorconfig",
        [".lua"] = "lua",
        [".r"] = "r",
        [".R"] = "r",
        [".pl"] = "perl",
        [".pm"] = "perl",
    };

    /// <summary>
    /// Maps special file names (without extensions) to their languages.
    /// </summary>
    private static readonly Dictionary<string, string> SpecialFileMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dockerfile"] = "dockerfile",
        ["makefile"] = "makefile",
        ["gnumakefile"] = "makefile",
        ["rakefile"] = "ruby",
        ["gemfile"] = "ruby",
        ["cmakelists.txt"] = "cmake",
        ["vagrantfile"] = "ruby",
        ["jenkinsfile"] = "groovy",
    };

    /// <summary>
    /// Detects programming language from a file extension.
    /// </summary>
    /// <param name="extension">File extension including the dot (e.g., ".cs").</param>
    /// <returns>Language identifier, or null if unknown.</returns>
    public static string? DetectByExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        return ExtensionMap.TryGetValue(extension, out var language)
            ? language
            : null;
    }

    /// <summary>
    /// Detects programming language from a file name.
    /// Checks special file names first, then falls back to extension detection.
    /// </summary>
    /// <param name="fileName">File name (with or without path).</param>
    /// <returns>Language identifier, or null if unknown.</returns>
    public static string? DetectByFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        // Extract just the file name if a path was provided
        var name = Path.GetFileName(fileName);

        // Check special file names first
        if (SpecialFileMap.TryGetValue(name, out var language))
            return language;

        // Fall back to extension detection
        var extension = Path.GetExtension(name);
        return DetectByExtension(extension);
    }

    /// <summary>
    /// Gets all file extensions supported by the detector.
    /// </summary>
    /// <returns>Collection of supported extensions (with leading dots).</returns>
    public static IReadOnlyCollection<string> GetAllSupportedExtensions()
        => ExtensionMap.Keys;

    /// <summary>
    /// Gets all special file names supported by the detector.
    /// </summary>
    /// <returns>Collection of special file names.</returns>
    public static IReadOnlyCollection<string> GetAllSpecialFileNames()
        => SpecialFileMap.Keys;

    /// <summary>
    /// Checks if a file extension is supported.
    /// </summary>
    /// <param name="extension">File extension to check.</param>
    /// <returns>True if the extension is recognized.</returns>
    public static bool IsSupported(string? extension)
        => !string.IsNullOrWhiteSpace(extension) && ExtensionMap.ContainsKey(extension);
}
