// ============================================================================
// File: IFontService.cs
// Path: src/AIntern.Core/Interfaces/IFontService.cs
// Description: Interface for font detection and management.
// Created: 2026-01-19
// AI Intern v0.5.5e - Terminal Settings Models
// ============================================================================

namespace AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ IFontService (v0.5.5e)                                                       │
// │ Service for detecting and managing available system fonts.                  │
// │                                                                              │
// │ Responsibilities:                                                           │
// │   - Detect available monospace fonts on the system                          │
// │   - Check if specific fonts are available                                   │
// │   - Resolve font fallback lists                                             │
// │   - Provide platform-specific font recommendations                          │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for detecting and managing available system fonts.
/// </summary>
/// <remarks>
/// <para>
/// This service provides font detection capabilities for the terminal:
/// </para>
/// <list type="bullet">
///   <item><description>Discover available monospace fonts</description></item>
///   <item><description>Validate font availability</description></item>
///   <item><description>Resolve font fallback chains</description></item>
///   <item><description>Provide platform-appropriate recommendations</description></item>
/// </list>
/// <para>Added in v0.5.5e.</para>
/// </remarks>
public interface IFontService
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Font Discovery
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets a list of available monospace fonts on the system.
    /// </summary>
    /// <returns>Sorted list of available monospace font names.</returns>
    /// <remarks>
    /// Results are cached for performance. The list includes commonly
    /// used programming fonts that are detected on the system.
    /// </remarks>
    IReadOnlyList<string> GetMonospaceFonts();

    /// <summary>
    /// Gets recommended fonts for the current platform.
    /// </summary>
    /// <returns>List of available recommended fonts.</returns>
    /// <remarks>
    /// Returns platform-specific fonts (e.g., SF Mono on macOS, Cascadia Code
    /// on Windows) plus commonly installed cross-platform fonts.
    /// Only fonts actually available on the system are returned.
    /// </remarks>
    IReadOnlyList<string> GetRecommendedFonts();

    // ═══════════════════════════════════════════════════════════════════════════
    // Font Validation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Checks if a specific font family is available on the system.
    /// </summary>
    /// <param name="fontFamily">The font family name to check.</param>
    /// <returns>True if the font is available.</returns>
    bool IsFontAvailable(string fontFamily);

    /// <summary>
    /// Gets the best available font from a comma-separated fallback list.
    /// </summary>
    /// <param name="fontList">Comma-separated font names (e.g., "SF Mono, Menlo, monospace").</param>
    /// <returns>The first available font, or "monospace" if none found.</returns>
    /// <remarks>
    /// <para>
    /// This method processes a CSS-style font fallback list and returns
    /// the first font that is actually available on the system.
    /// </para>
    /// <para>
    /// Example: "Fira Code, JetBrains Mono, Consolas" would return
    /// "Fira Code" if installed, otherwise "JetBrains Mono", etc.
    /// </para>
    /// </remarks>
    string GetBestAvailableFont(string fontList);
}
