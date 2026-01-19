// ============================================================================
// File: FontService.cs
// Path: src/AIntern.Services/FontService.cs
// Description: Service for detecting available system fonts.
// Created: 2026-01-19
// AI Intern v0.5.5e - Terminal Settings Models
// ============================================================================

namespace AIntern.Services;

using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ FontService (v0.5.5e)                                                        │
// │ Detects and manages available monospace fonts for terminal use.             │
// │                                                                              │
// │ Features:                                                                    │
// │   - Lazy detection of available fonts                                       │
// │   - Platform-specific font recommendations                                  │
// │   - Font fallback chain resolution                                          │
// │   - Caching for performance                                                 │
// └─────────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Service for detecting and managing available system fonts.
/// </summary>
/// <remarks>
/// <para>
/// This service provides font detection capabilities:
/// </para>
/// <list type="bullet">
///   <item><description>Detects monospace fonts from a known list</description></item>
///   <item><description>Uses lazy initialization for performance</description></item>
///   <item><description>Provides platform-specific recommendations</description></item>
///   <item><description>Resolves font fallback chains</description></item>
/// </list>
/// <para>Added in v0.5.5e.</para>
/// </remarks>
public sealed class FontService : IFontService
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Constants - Known Monospace Fonts
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// List of known monospace fonts to check for availability.
    /// </summary>
    /// <remarks>
    /// Organized by platform origin, but all are checked on all platforms
    /// since users may have installed cross-platform fonts.
    /// </remarks>
    private static readonly string[] KnownMonospaceFonts =
    [
        // ─────────────────────────────────────────────────────────────────────
        // Windows-origin fonts
        // ─────────────────────────────────────────────────────────────────────
        "Cascadia Code",      // Windows Terminal default (Win10+)
        "Cascadia Mono",      // Non-ligature variant
        "Consolas",           // Classic Windows dev font
        "Courier New",        // Universal fallback
        "Lucida Console",     // Older Windows

        // ─────────────────────────────────────────────────────────────────────
        // macOS-origin fonts
        // ─────────────────────────────────────────────────────────────────────
        "SF Mono",            // Apple's modern monospace
        "Menlo",              // macOS default (10.6+)
        "Monaco",             // Classic Mac font

        // ─────────────────────────────────────────────────────────────────────
        // Linux-origin fonts
        // ─────────────────────────────────────────────────────────────────────
        "Ubuntu Mono",        // Ubuntu default
        "DejaVu Sans Mono",   // Common Linux font
        "Liberation Mono",    // RedHat liberation fonts
        "Noto Mono",          // Google Noto project
        "Source Code Pro",    // Adobe open-source

        // ─────────────────────────────────────────────────────────────────────
        // Cross-platform programming fonts
        // ─────────────────────────────────────────────────────────────────────
        "Fira Code",          // Ligature-rich programming font
        "JetBrains Mono",     // JetBrains IDE font
        "Hack",               // Open-source Hack font
        "Inconsolata",        // Popular Google font
        "Roboto Mono",        // Google's monospace Roboto
        "IBM Plex Mono",      // IBM design system
        "Anonymous Pro",      // Anonymous font family
        "Droid Sans Mono",    // Android-origin
        "Fantasque Sans Mono",// Quirky but readable
        "Input Mono",         // Font Bureau's Input
        "Iosevka",            // Narrow programming font
        "Victor Mono",        // Cursive italics variant
        "Monaspace",          // GitHub's new font
        "Geist Mono"          // Vercel's font
    ];

    // ═══════════════════════════════════════════════════════════════════════════
    // Fields
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logger for diagnostic output.</summary>
    private readonly ILogger<FontService> _logger;

    /// <summary>Cached list of available monospace fonts.</summary>
    private readonly Lazy<List<string>> _monospaceFonts;

    /// <summary>Cached list of recommended fonts for current platform.</summary>
    private readonly Lazy<List<string>> _recommendedFonts;

    /// <summary>Delegate for font availability checking (allows DI/mocking).</summary>
    private readonly Func<string, bool>? _fontChecker;

    // ═══════════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new <see cref="FontService"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown if logger is null.</exception>
    public FontService(ILogger<FontService> logger)
        : this(logger, null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="FontService"/> with custom font checker.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="fontChecker">Optional custom font availability checker.</param>
    /// <remarks>
    /// The fontChecker parameter allows injection of platform-specific
    /// font detection logic or mock implementations for testing.
    /// </remarks>
    public FontService(ILogger<FontService> logger, Func<string, bool>? fontChecker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fontChecker = fontChecker;

        // ─────────────────────────────────────────────────────────────────────
        // Initialize lazy loaders
        // ─────────────────────────────────────────────────────────────────────
        _monospaceFonts = new Lazy<List<string>>(DetectMonospaceFonts);
        _recommendedFonts = new Lazy<List<string>>(GetPlatformRecommendedFonts);

        _logger.LogDebug("FontService initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IFontService Implementation
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public IReadOnlyList<string> GetMonospaceFonts() => _monospaceFonts.Value;

    /// <inheritdoc />
    public IReadOnlyList<string> GetRecommendedFonts() => _recommendedFonts.Value;

    /// <inheritdoc />
    public bool IsFontAvailable(string fontFamily)
    {
        // ─────────────────────────────────────────────────────────────────────
        // Handle null/empty input
        // ─────────────────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            _logger.LogDebug("IsFontAvailable called with null/empty font name");
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Use injected checker if available, otherwise use known fonts list
        // ─────────────────────────────────────────────────────────────────────
        if (_fontChecker != null)
        {
            try
            {
                var available = _fontChecker(fontFamily);
                _logger.LogTrace("Font check via delegate: {Font} = {Available}", fontFamily, available);
                return available;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Font check failed for: {Font}", fontFamily);
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Fallback: Check if font is in known list (assumes common fonts exist)
        // This is a simplified check - real implementation should use platform APIs
        // ─────────────────────────────────────────────────────────────────────
        var isKnown = KnownMonospaceFonts.Contains(fontFamily, StringComparer.OrdinalIgnoreCase);
        
        // Generic fallback fonts are always "available"
        if (fontFamily.Equals("monospace", StringComparison.OrdinalIgnoreCase) ||
            fontFamily.Equals("mono", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        _logger.LogTrace("Font check (known list): {Font} = {Known}", fontFamily, isKnown);
        return isKnown;
    }

    /// <inheritdoc />
    public string GetBestAvailableFont(string fontList)
    {
        // ─────────────────────────────────────────────────────────────────────
        // Handle null/empty input
        // ─────────────────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(fontList))
        {
            _logger.LogDebug("GetBestAvailableFont called with empty list, returning monospace");
            return "monospace";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Split and process font list
        // ─────────────────────────────────────────────────────────────────────
        var fonts = fontList
            .Split(',')
            .Select(f => f.Trim().Trim('"', '\''))
            .Where(f => !string.IsNullOrEmpty(f));

        foreach (var font in fonts)
        {
            if (IsFontAvailable(font))
            {
                _logger.LogDebug("Selected font: {Font} from list: {List}", font, fontList);
                return font;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // No fonts found - fall back to generic monospace
        // ─────────────────────────────────────────────────────────────────────
        _logger.LogWarning("No fonts available from list: {List}, falling back to monospace", fontList);
        return "monospace";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Private Methods
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Detects available monospace fonts from the known list.
    /// </summary>
    private List<string> DetectMonospaceFonts()
    {
        _logger.LogDebug("Detecting available monospace fonts");

        var available = new List<string>();

        foreach (var font in KnownMonospaceFonts)
        {
            if (IsFontAvailable(font))
            {
                available.Add(font);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Sort alphabetically for consistent UI display
        // ─────────────────────────────────────────────────────────────────────
        available.Sort(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("Detected {Count} available monospace fonts", available.Count);
        return available;
    }

    /// <summary>
    /// Gets platform-specific recommended fonts.
    /// </summary>
    private List<string> GetPlatformRecommendedFonts()
    {
        _logger.LogDebug("Getting platform-recommended fonts");

        var recommended = new List<string>();

        // ─────────────────────────────────────────────────────────────────────
        // Platform-specific primary recommendations
        // ─────────────────────────────────────────────────────────────────────
        if (OperatingSystem.IsWindows())
        {
            recommended.AddRange(["Cascadia Code", "Cascadia Mono", "Consolas"]);
        }
        else if (OperatingSystem.IsMacOS())
        {
            recommended.AddRange(["SF Mono", "Menlo", "Monaco"]);
        }
        else // Linux
        {
            recommended.AddRange(["Ubuntu Mono", "DejaVu Sans Mono", "Liberation Mono"]);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Cross-platform favorites (commonly installed by developers)
        // ─────────────────────────────────────────────────────────────────────
        recommended.AddRange(["Fira Code", "JetBrains Mono", "Hack", "Source Code Pro"]);

        // ─────────────────────────────────────────────────────────────────────
        // Filter to only actually available fonts
        // ─────────────────────────────────────────────────────────────────────
        var available = recommended.Where(IsFontAvailable).ToList();

        _logger.LogDebug("Found {Count} recommended fonts available", available.Count);
        return available;
    }
}
