namespace AIntern.Core.Interfaces;

/// <summary>
/// Service for detecting and normalizing programming language identifiers (v0.4.1c).
/// </summary>
public interface ILanguageDetectionService
{
    /// <summary>
    /// Detect the programming language of a code block.
    /// Uses a priority system: fence language > file extension > content heuristics.
    /// </summary>
    /// <param name="fenceLanguage">Language specified in the fence (may be null).</param>
    /// <param name="content">The code content.</param>
    /// <param name="filePath">Optional file path for extension-based detection.</param>
    /// <returns>Tuple of (normalized language ID, display name).</returns>
    (string? Language, string? DisplayLanguage) DetectLanguage(
        string? fenceLanguage,
        string content,
        string? filePath = null);

    /// <summary>
    /// Get the canonical language ID for an alias.
    /// </summary>
    /// <param name="languageAlias">Language alias (e.g., "cs", "c#", "py").</param>
    /// <returns>Canonical language ID or null if unknown.</returns>
    string? NormalizeLanguageId(string languageAlias);

    /// <summary>
    /// Get the display name for a language ID.
    /// </summary>
    /// <param name="languageId">Canonical language ID (e.g., "csharp").</param>
    /// <returns>User-friendly display name (e.g., "C#") or null.</returns>
    string? GetDisplayName(string languageId);

    /// <summary>
    /// Get the primary file extension for a language.
    /// </summary>
    /// <param name="languageId">Canonical language ID.</param>
    /// <returns>Primary file extension with dot (e.g., ".cs") or null.</returns>
    string? GetFileExtension(string languageId);

    /// <summary>
    /// Get the language ID for a file extension.
    /// </summary>
    /// <param name="extension">File extension with or without dot.</param>
    /// <returns>Canonical language ID or null if unknown.</returns>
    string? GetLanguageForExtension(string extension);

    /// <summary>
    /// Check if a language is a shell/command language.
    /// </summary>
    /// <param name="languageId">Language ID to check.</param>
    /// <returns>True for bash, powershell, cmd, etc.</returns>
    bool IsShellLanguage(string? languageId);

    /// <summary>
    /// Check if a language is for configuration files.
    /// </summary>
    /// <param name="languageId">Language ID to check.</param>
    /// <returns>True for json, yaml, xml, toml, ini, etc.</returns>
    bool IsConfigLanguage(string? languageId);

    /// <summary>
    /// Get all supported language IDs.
    /// </summary>
    /// <returns>Collection of canonical language IDs.</returns>
    IReadOnlyCollection<string> GetSupportedLanguages();
}
