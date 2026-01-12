using System.Collections.Generic;
using System.Linq;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace AIntern.Desktop.Services;

/// <summary>
/// Provides syntax highlighting using TextMate grammars.
/// </summary>
public sealed class SyntaxHighlightingService : IDisposable
{
    private RegistryOptions _registryOptions;
    private readonly Dictionary<string, string> _languageToScope;
    private readonly Dictionary<TextEditor, (TextMate.Installation Installation, string? Scope)> _installations = new();
    private ThemeName _currentTheme;
    private bool _disposed;

    /// <summary>
    /// Initializes a new SyntaxHighlightingService with the specified theme.
    /// </summary>
    /// <param name="useDarkTheme">Whether to use dark theme (default: true).</param>
    public SyntaxHighlightingService(bool useDarkTheme = true)
    {
        _currentTheme = useDarkTheme ? ThemeName.DarkPlus : ThemeName.LightPlus;
        _registryOptions = new RegistryOptions(_currentTheme);
        _languageToScope = BuildLanguageScopeMap();
    }

    #region Properties

    /// <summary>
    /// Available themes for syntax highlighting.
    /// </summary>
    public static IReadOnlyList<ThemeName> AvailableThemes { get; } = new[]
    {
        ThemeName.DarkPlus,
        ThemeName.LightPlus,
        ThemeName.Monokai,
        ThemeName.SolarizedDark,
        ThemeName.SolarizedLight,
        ThemeName.HighContrastDark,
        ThemeName.HighContrastLight,
    };

    /// <summary>
    /// Currently active theme.
    /// </summary>
    public ThemeName CurrentTheme => _currentTheme;

    /// <summary>
    /// Gets the TextMate registry options.
    /// </summary>
    public RegistryOptions RegistryOptions => _registryOptions;

    /// <summary>
    /// Gets all supported languages.
    /// </summary>
    public IReadOnlyList<string> SupportedLanguages => _languageToScope.Keys.ToList();

    /// <summary>
    /// Number of editors currently registered.
    /// </summary>
    public int RegisteredEditorCount => _installations.Count;

    #endregion

    #region Core Methods

    /// <summary>
    /// Applies syntax highlighting to an editor for the specified language.
    /// </summary>
    /// <param name="editor">The TextEditor to apply highlighting to.</param>
    /// <param name="language">Language identifier (e.g., "csharp", "javascript").</param>
    /// <returns>The TextMate installation for further configuration.</returns>
    public TextMate.Installation ApplyHighlighting(TextEditor editor, string? language)
    {
        ArgumentNullException.ThrowIfNull(editor);

        // Remove existing installation if any
        if (_installations.TryGetValue(editor, out var existing))
        {
            existing.Installation.Dispose();
            _installations.Remove(editor);
        }

        // Create new installation
        var installation = editor.InstallTextMate(_registryOptions);
        string? scope = null;

        // Set grammar if language is specified
        if (!string.IsNullOrEmpty(language) && _languageToScope.TryGetValue(language, out scope))
        {
            try
            {
                installation.SetGrammar(scope);
            }
            catch
            {
                // Grammar not found, continue without highlighting
                scope = null;
            }
        }

        _installations[editor] = (installation, scope);
        return installation;
    }

    /// <summary>
    /// Updates the grammar for an existing editor installation.
    /// </summary>
    /// <param name="editor">The TextEditor to update.</param>
    /// <param name="language">Language identifier (null to clear).</param>
    public void SetLanguage(TextEditor editor, string? language)
    {
        if (!_installations.TryGetValue(editor, out var entry)) return;

        string? newScope = null;

        if (string.IsNullOrEmpty(language))
        {
            entry.Installation.SetGrammar(null);
        }
        else if (_languageToScope.TryGetValue(language, out newScope))
        {
            try
            {
                entry.Installation.SetGrammar(newScope);
            }
            catch
            {
                // Grammar not found
                newScope = null;
            }
        }

        _installations[editor] = (entry.Installation, newScope);
    }

    /// <summary>
    /// Changes the theme for all registered editors.
    /// </summary>
    /// <param name="theme">The new theme to apply.</param>
    public void ChangeTheme(ThemeName theme)
    {
        if (_currentTheme == theme) return;

        _currentTheme = theme;
        _registryOptions = new RegistryOptions(theme);

        // Re-apply to all editors
        foreach (var (editor, entry) in _installations.ToArray())
        {
            var currentScope = entry.Scope;

            entry.Installation.Dispose();
            _installations.Remove(editor);

            var newInstallation = editor.InstallTextMate(_registryOptions);
            if (!string.IsNullOrEmpty(currentScope))
            {
                try
                {
                    newInstallation.SetGrammar(currentScope);
                }
                catch { /* Grammar not found */ }
            }

            _installations[editor] = (newInstallation, currentScope);
        }
    }

    /// <summary>
    /// Removes syntax highlighting from an editor.
    /// </summary>
    /// <param name="editor">The TextEditor to remove highlighting from.</param>
    public void RemoveHighlighting(TextEditor editor)
    {
        if (_installations.TryGetValue(editor, out var entry))
        {
            entry.Installation.Dispose();
            _installations.Remove(editor);
        }
    }

    /// <summary>
    /// Gets the scope name for a language identifier.
    /// </summary>
    /// <param name="language">Language identifier.</param>
    /// <returns>TextMate scope name, or null if not found.</returns>
    public string? GetScopeForLanguage(string language)
    {
        return _languageToScope.TryGetValue(language, out var scope) ? scope : null;
    }

    /// <summary>
    /// Checks if a language is supported.
    /// </summary>
    public bool IsLanguageSupported(string language)
    {
        return _languageToScope.ContainsKey(language);
    }

    #endregion

    #region Language Scope Mapping

    private static Dictionary<string, string> BuildLanguageScopeMap()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // .NET Languages
            ["csharp"] = "source.cs",
            ["fsharp"] = "source.fsharp",
            ["vb"] = "source.asp.vb.net",

            // Web Languages
            ["javascript"] = "source.js",
            ["javascriptreact"] = "source.js.jsx",
            ["typescript"] = "source.ts",
            ["typescriptreact"] = "source.tsx",
            ["html"] = "text.html.basic",
            ["css"] = "source.css",
            ["scss"] = "source.css.scss",
            ["less"] = "source.css.less",
            ["vue"] = "source.vue",

            // Systems Languages
            ["c"] = "source.c",
            ["cpp"] = "source.cpp",
            ["rust"] = "source.rust",
            ["go"] = "source.go",
            ["swift"] = "source.swift",
            ["objective-c"] = "source.objc",

            // Scripting Languages
            ["python"] = "source.python",
            ["ruby"] = "source.ruby",
            ["php"] = "source.php",
            ["perl"] = "source.perl",
            ["lua"] = "source.lua",
            ["r"] = "source.r",

            // JVM Languages
            ["java"] = "source.java",
            ["kotlin"] = "source.kotlin",
            ["scala"] = "source.scala",
            ["groovy"] = "source.groovy",

            // Shell/Scripts
            ["shellscript"] = "source.shell",
            ["bash"] = "source.shell",
            ["powershell"] = "source.powershell",
            ["bat"] = "source.batchfile",

            // Data/Config
            ["json"] = "source.json",
            ["jsonc"] = "source.json.comments",
            ["xml"] = "text.xml",
            ["yaml"] = "source.yaml",
            ["toml"] = "source.toml",
            ["ini"] = "source.ini",
            ["properties"] = "source.ini",

            // Markup
            ["markdown"] = "text.html.markdown",
            ["latex"] = "text.tex.latex",
            ["restructuredtext"] = "text.restructuredtext",

            // Database
            ["sql"] = "source.sql",

            // Build/Config
            ["dockerfile"] = "source.dockerfile",
            ["makefile"] = "source.makefile",
            ["cmake"] = "source.cmake",

            // Other
            ["diff"] = "source.diff",
            ["gitignore"] = "source.ignore",
        };
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var entry in _installations.Values)
        {
            entry.Installation.Dispose();
        }
        _installations.Clear();
    }

    #endregion
}
