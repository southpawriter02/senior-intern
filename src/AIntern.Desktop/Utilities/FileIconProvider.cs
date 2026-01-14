namespace AIntern.Desktop.Utilities;

/// <summary>
/// Provides icon keys and SVG path data for file types.
/// All icons designed for 24x24 viewBox.
/// </summary>
/// <remarks>
/// Added in v0.3.2a, enhanced with SVG paths in v0.3.2c.
/// </remarks>
public static class FileIconProvider
{
    #region SVG Icon Paths (24x24 viewBox)

    /// <summary>
    /// SVG path data for each icon key.
    /// </summary>
    private static readonly Dictionary<string, string> IconPaths = new()
    {
        // Folders
        ["folder"] = "M10 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z",
        ["folder-open"] = "M20 6h-8l-2-2H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm0 12H4V8h16v10z",
        ["folder-src"] = "M10 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2zm-1 14l-4-4 1.4-1.4L9 15.2l4.6-4.6L15 12l-6 6z",
        ["folder-src-open"] = "M20 6h-8l-2-2H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm-11 12l-4-4 1.4-1.4L9 15.2l4.6-4.6L15 12l-6 6z",

        // Generic files
        ["file"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm4 18H6V4h7v5h5v11z",
        ["file-code"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM9.5 13.5L7 16l2.5 2.5 1-1L8.5 16l2-1.5-1-1zm5.5 5L17.5 16 15 13.5l-1 1 2 1.5-2 1.5 1 1z",
        ["file-text"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM8 12h8v2H8v-2zm0 4h8v2H8v-2z",

        // Programming languages
        ["file-csharp"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM11.5 13h-1v-1h-1v1h-1v1h1v1h1v-1h1v2h-3v-1h-1v-3h1v-1h3v1zm4 0h-1v-1h-1v1h-1v1h1v1h1v-1h1v2h-3v-1h-1v-3h1v-1h3v1z",
        ["file-javascript"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM9 18v-1.5c0-.8-.7-1.5-1.5-1.5s-1.5.7-1.5 1.5V18h-1v-1.5C5 15.1 6.1 14 7.5 14s2.5 1.1 2.5 2.5V18H9zm5-3.5c0 .8-.7 1.5-1.5 1.5H11v1.5h2V19h-3v-3h1.5c.8 0 1.5-.7 1.5-1.5V14h1v.5z",
        ["file-typescript"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM8 14h4v1H10.5v4h-1v-4H8v-1zm6.5 0H18v1h-1.5v4h-1v-4h-1v-1z",
        ["file-python"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM12.5 12c-.83 0-1.5.67-1.5 1.5v1c0 .83.67 1.5 1.5 1.5h1c.28 0 .5.22.5.5v.5h-2.5v1h2.5c.83 0 1.5-.67 1.5-1.5v-1c0-.83-.67-1.5-1.5-1.5h-1c-.28 0-.5-.22-.5-.5v-.5h2.5v-1h-2.5z",
        ["file-rust"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM12 12c-1.93 0-3.5 1.57-3.5 3.5S10.07 19 12 19c.17 0 .34-.01.5-.04V17.4c-.16.04-.33.06-.5.06-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2v.5h1.5v-.5c0-1.93-1.57-3.5-3.5-3.5z",
        ["file-go"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM12 12c-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4-1.79-4-4-4zm1 5.5h-2v-1h2v1zm0-2h-2v-2h2v2z",
        ["file-java"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM10 18v-1c0-.83.67-1.5 1.5-1.5h1c.28 0 .5-.22.5-.5V14h1v1c0 .83-.67 1.5-1.5 1.5h-1c-.28 0-.5.22-.5.5V18h-1z",

        // Web files
        ["file-html"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM8 12l-2 4 2 4h1.5l-2-4 2-4H8zm6.5 0l2 4-2 4H16l2-4-2-4h-1.5z",
        ["file-css"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM10 14v4c0 .55-.45 1-1 1H7v-1h2v-1H7v-1h2v-1H7v-1h2c.55 0 1 .45 1 1zm6-1h-3v1h2v1h-2v1h2v1h-3v-4c0-.55.45-1 1-1h2c.55 0 1 .45 1 1v1z",

        // Data files
        ["file-json"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM8 12v1.5c0 .28-.22.5-.5.5H7v1h.5c.28 0 .5.22.5.5V17c0 .55.45 1 1 1h1v-1H9v-1.5c0-.55-.45-1-1-1 .55 0 1-.45 1-1V12c0-.55-.45-1-1-1H7v1h1zm8 5c0 .55-.45 1-1 1h-1v-1h1v-1.5c0-.55.45-1 1-1-.55 0-1-.45-1-1V12h1v1h1c.55 0 1 .45 1 1v1.5c0 .28-.22.5-.5.5h-.5v1h.5c.28 0 .5.22.5.5V17z",
        ["file-xml"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM8 12l-1.5 3 1.5 3H9l-1.5-3L9 12H8zm5 0l1.5 3-1.5 3h1l1.5-3-1.5-3h-1z",
        ["file-yaml"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM8 13l1.5 2.5V18h1v-2.5L12 13h-1l-1 1.5-1-1.5H8z",
        ["file-markdown"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM6.5 17.5v-5h1.5l1.5 2 1.5-2h1.5v5h-1.5v-2.5l-1.5 2-1.5-2v2.5h-1.5zm9-5v3.5l1.5-1.5v1l-2.5 2-2.5-2v-1l1.5 1.5v-3.5h2z",

        // Other
        ["file-image"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zm-5-7c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm-4.5 5.5l1.5-2 1.5 2.5 2-3 2.5 3H7.5z",
        ["file-git"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zm-6-6c-1.1 0-2 .9-2 2 0 .74.4 1.38 1 1.73v1.27h2v-1.27c.6-.35 1-.99 1-1.73 0-1.1-.9-2-2-2z",
        ["file-config"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM12 12c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3zm0 4.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z",
        ["file-database"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM12 11c-2.21 0-4 .9-4 2v4c0 1.1 1.79 2 4 2s4-.9 4-2v-4c0-1.1-1.79-2-4-2zm0 6c-1.66 0-3-.45-3-1v-1.2c.66.45 1.78.7 3 .7s2.34-.25 3-.7V16c0 .55-1.34 1-3 1z",
        ["file-shell"] = "M14 2H6c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h12c1.1 0 2-.89 2-2V8l-6-6zm4 18H6V4h7v5h5v11zM8 14l3 2-3 2v-1l1.5-1L8 15v-1zm4 3h4v1h-4v-1z",
    };

    #endregion

    #region Extension Mapping

    /// <summary>
    /// Maps file extensions (case-insensitive) to icon keys.
    /// </summary>
    private static readonly Dictionary<string, string> ExtensionIconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // C# / .NET
        [".cs"] = "file-csharp", [".csx"] = "file-csharp",
        [".csproj"] = "file-xml", [".sln"] = "file-config",
        [".axaml"] = "file-xml", [".xaml"] = "file-xml",
        [".props"] = "file-xml", [".targets"] = "file-xml",

        // JavaScript / TypeScript
        [".js"] = "file-javascript", [".mjs"] = "file-javascript", [".jsx"] = "file-javascript",
        [".ts"] = "file-typescript", [".mts"] = "file-typescript", [".tsx"] = "file-typescript",

        // Python
        [".py"] = "file-python", [".pyi"] = "file-python", [".pyw"] = "file-python",

        // Web
        [".html"] = "file-html", [".htm"] = "file-html",
        [".css"] = "file-css", [".scss"] = "file-css", [".sass"] = "file-css",

        // Data / Config
        [".json"] = "file-json", [".jsonc"] = "file-json",
        [".xml"] = "file-xml",
        [".yaml"] = "file-yaml", [".yml"] = "file-yaml",
        [".toml"] = "file-config", [".ini"] = "file-config", [".env"] = "file-config",

        // Markdown / Text
        [".md"] = "file-markdown", [".mdx"] = "file-markdown",
        [".txt"] = "file-text",

        // Images
        [".png"] = "file-image", [".jpg"] = "file-image", [".jpeg"] = "file-image",
        [".gif"] = "file-image", [".svg"] = "file-image", [".ico"] = "file-image",
        [".webp"] = "file-image", [".bmp"] = "file-image",

        // Git
        [".gitignore"] = "file-git", [".gitattributes"] = "file-git",

        // Shell
        [".sh"] = "file-shell", [".bash"] = "file-shell", [".ps1"] = "file-shell",
        [".bat"] = "file-shell", [".cmd"] = "file-shell",

        // Database
        [".sql"] = "file-database", [".db"] = "file-database",

        // Other languages
        [".rs"] = "file-rust", [".go"] = "file-go", [".java"] = "file-java",
        [".c"] = "file-code", [".cpp"] = "file-code", [".h"] = "file-code",
    };

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the icon key for a file extension.
    /// </summary>
    /// <param name="extension">File extension including dot (e.g., ".cs").</param>
    /// <returns>Icon key string for asset lookup.</returns>
    public static string GetIconKeyForExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return "file";

        return ExtensionIconMap.TryGetValue(extension, out var key) ? key : "file-code";
    }

    /// <summary>
    /// Gets the icon key for a special file by name.
    /// </summary>
    /// <param name="fileName">Full file name.</param>
    /// <returns>Icon key if special file, null otherwise.</returns>
    public static string? GetIconKeyForSpecialFile(string fileName)
    {
        return fileName.ToLowerInvariant() switch
        {
            "dockerfile" => "file-config",
            "makefile" => "file-shell",
            "package.json" or "tsconfig.json" => "file-json",
            ".editorconfig" or ".prettierrc" => "file-config",
            "readme.md" or "readme.txt" or "readme" => "file-markdown",
            "license" or "license.md" => "file-text",
            "changelog.md" or "changelog" => "file-markdown",
            ".gitignore" or ".gitattributes" => "file-git",
            "appsettings.json" or "appsettings.development.json" => "file-json",
            _ => null
        };
    }

    /// <summary>
    /// Gets the SVG path data for an icon key.
    /// </summary>
    /// <param name="iconKey">Icon key (e.g., "file-csharp").</param>
    /// <returns>SVG path data string for 24x24 viewBox.</returns>
    public static string GetIconPath(string iconKey)
    {
        return IconPaths.TryGetValue(iconKey, out var path) ? path : IconPaths["file"];
    }

    /// <summary>
    /// Gets all available icon keys.
    /// </summary>
    public static IReadOnlyCollection<string> GetAllIconKeys() => IconPaths.Keys;

    #endregion
}

