namespace AIntern.Services;

using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

/// <summary>
/// Classifies code blocks based on language, context phrases, and content structure (v0.4.1d).
/// </summary>
/// <remarks>
/// <para>
/// This service implements multi-factor classification to determine the purpose of code blocks
/// extracted from LLM responses. Classification affects which UI actions are available:
/// </para>
/// <list type="bullet">
/// <item>CompleteFile/Snippet/Config: Can be applied to files</item>
/// <item>Command: Can be copied/executed in terminal</item>
/// <item>Example/Output: Read-only, for reference</item>
/// </list>
/// </remarks>
public sealed class BlockClassificationService : IBlockClassificationService
{
    private readonly ILanguageDetectionService _languageService;
    private readonly ILogger<BlockClassificationService>? _logger;

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ INDICATOR PHRASES                                                        │
    // │ These arrays define context phrases that suggest the purpose of code.    │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Phrases that strongly suggest example/illustration code (not meant to be applied).
    /// </summary>
    /// <remarks>
    /// Organized by signal strength:
    /// - Weak: "for example", "e.g.", "such as", "like this"
    /// - Medium: "example:", "would look like", "something like"
    /// - Strong: "consider the following", "suppose we have", "hypothetically"
    /// </remarks>
    private static readonly string[] ExampleIndicators =
    [
        "for example",
        "e.g.",
        "such as",
        "like this",
        "example:",
        "an example",
        "would look like",
        "something like",
        "here's an example",
        "consider the following",
        "suppose we have",
        "imagine",
        "let's say",
        "hypothetically",
        "for instance",
        "could be something like"
    ];

    /// <summary>
    /// Phrases that strongly suggest actionable/applicable code.
    /// </summary>
    /// <remarks>
    /// Organized by type:
    /// - Action words: "update", "modify", "change", "replace", "create"
    /// - Directive phrases: "add this", "here's the", "updated version"
    /// - Solution phrases: "the fix", "the solution", "use this instead"
    /// </remarks>
    private static readonly string[] ApplyIndicators =
    [
        "update",
        "modify",
        "change",
        "replace",
        "add this",
        "create",
        "here's the",
        "here is the",
        "updated version",
        "fixed version",
        "corrected",
        "the fix",
        "the solution",
        "should be",
        "needs to be",
        "change it to",
        "replace with",
        "use this instead"
    ];

    /// <summary>
    /// Phrases suggesting output/logs (read-only content).
    /// </summary>
    private static readonly string[] OutputIndicators =
    [
        "output:",
        "result:",
        "returns:",
        "produces:",
        "will print",
        "will output",
        "you'll see",
        "the output is",
        "this prints"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockClassificationService"/> class.
    /// </summary>
    /// <param name="languageService">Service for language detection and classification.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public BlockClassificationService(
        ILanguageDetectionService languageService,
        ILogger<BlockClassificationService>? logger = null)
    {
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
        _logger = logger;
    }

    /// <inheritdoc />
    public CodeBlockType ClassifyBlock(
        string content,
        string? language,
        string surroundingText)
    {
        // Normalize inputs for case-insensitive matching
        var lowerContext = (surroundingText ?? string.Empty).ToLowerInvariant();
        var lowerContent = (content ?? string.Empty).ToLowerInvariant();

        _logger?.LogDebug(
            "[INFO] Classifying block: language={Language}, contentLength={ContentLength}, contextLength={ContextLength}",
            language ?? "null",
            content?.Length ?? 0,
            surroundingText?.Length ?? 0);

        // ┌─────────────────────────────────────────────────────────────────────┐
        // │ STEP 1: Shell Language Check (highest priority)                      │
        // │ Shell languages are definitively Command blocks.                     │
        // └─────────────────────────────────────────────────────────────────────┘
        if (_languageService.IsShellLanguage(language))
        {
            _logger?.LogDebug("[INFO] Classified as Command (shell language: {Language})", language);
            return CodeBlockType.Command;
        }

        // ┌─────────────────────────────────────────────────────────────────────┐
        // │ STEP 2: Config Language Check                                        │
        // │ Configuration languages are definitively Config blocks.              │
        // └─────────────────────────────────────────────────────────────────────┘
        if (_languageService.IsConfigLanguage(language))
        {
            _logger?.LogDebug("[INFO] Classified as Config (config language: {Language})", language);
            return CodeBlockType.Config;
        }

        // ┌─────────────────────────────────────────────────────────────────────┐
        // │ STEP 3: Output Indicator Check                                       │
        // │ Context phrases like "output:", "result:" suggest Output blocks.     │
        // └─────────────────────────────────────────────────────────────────────┘
        if (ContainsAnyIndicator(lowerContext, OutputIndicators))
        {
            _logger?.LogDebug("[INFO] Classified as Output (output indicator found)");
            return CodeBlockType.Output;
        }

        // ┌─────────────────────────────────────────────────────────────────────┐
        // │ STEP 4: Example vs Apply Scoring                                     │
        // │ Compare indicator phrase counts to determine intent.                 │
        // └─────────────────────────────────────────────────────────────────────┘
        var exampleScore = CalculateIndicatorScore(lowerContext, ExampleIndicators);
        var applyScore = CalculateIndicatorScore(lowerContext, ApplyIndicators);

        _logger?.LogDebug(
            "[INFO] Indicator scores: example={ExampleScore}, apply={ApplyScore}",
            exampleScore,
            applyScore);

        if (exampleScore > applyScore && exampleScore > 0)
        {
            _logger?.LogDebug("[INFO] Classified as Example (example score wins: {Score})", exampleScore);
            return CodeBlockType.Example;
        }

        // ┌─────────────────────────────────────────────────────────────────────┐
        // │ STEP 5: Complete File Structure Check                                │
        // │ If apply indicators present OR content has complete file structure,  │
        // │ classify as CompleteFile.                                            │
        // └─────────────────────────────────────────────────────────────────────┘
        if (applyScore > 0 || HasCompleteFileStructure(content ?? string.Empty, language))
        {
            _logger?.LogDebug(
                "[INFO] Classified as CompleteFile (applyScore={ApplyScore}, hasStructure={HasStructure})",
                applyScore,
                HasCompleteFileStructure(content ?? string.Empty, language));
            return CodeBlockType.CompleteFile;
        }

        // ┌─────────────────────────────────────────────────────────────────────┐
        // │ STEP 6: Default to Snippet                                           │
        // │ Partial code without clear classification signals.                   │
        // └─────────────────────────────────────────────────────────────────────┘
        _logger?.LogDebug("[INFO] Classified as Snippet (default)");
        return CodeBlockType.Snippet;
    }

    /// <inheritdoc />
    public float GetClassificationConfidence(CodeBlock block)
    {
        if (block == null)
        {
            _logger?.LogWarning("[WARN] GetClassificationConfidence called with null block");
            return 0.0f;
        }

        // Higher confidence for explicit classifications
        var confidence = block.BlockType switch
        {
            CodeBlockType.Command => 0.95f,      // Shell language is explicit
            CodeBlockType.Config => 0.90f,       // Config language is explicit
            CodeBlockType.Output => 0.85f,       // Output indicators are clear
            CodeBlockType.CompleteFile => 0.80f, // Structure-based detection
            CodeBlockType.Example => 0.75f,      // Context-based detection
            CodeBlockType.Snippet => 0.70f,      // Default classification
            _ => 0.50f                           // Unknown type
        };

        _logger?.LogDebug(
            "[INFO] Classification confidence: type={Type}, confidence={Confidence}",
            block.BlockType,
            confidence);

        return confidence;
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ PRIVATE HELPER METHODS                                                   │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Calculate a score based on how many indicator phrases are present.
    /// </summary>
    /// <param name="text">The text to search (should be lowercase).</param>
    /// <param name="indicators">The indicator phrases to look for.</param>
    /// <returns>Count of matching indicators.</returns>
    private static int CalculateIndicatorScore(string text, string[] indicators)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return indicators.Count(indicator => text.Contains(indicator));
    }

    /// <summary>
    /// Check if any indicator phrase is present in the text.
    /// </summary>
    /// <param name="text">The text to search (should be lowercase).</param>
    /// <param name="indicators">The indicator phrases to look for.</param>
    /// <returns>True if any indicator is found.</returns>
    private static bool ContainsAnyIndicator(string text, string[] indicators)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return indicators.Any(indicator => text.Contains(indicator));
    }

    /// <summary>
    /// Detect if content has the structure of a complete file for the given language.
    /// </summary>
    /// <param name="content">The code content.</param>
    /// <param name="language">The detected language identifier.</param>
    /// <returns>True if the content appears to be a complete file.</returns>
    /// <remarks>
    /// Language-specific patterns:
    /// <list type="bullet">
    /// <item>C#: (namespace OR using) AND (class OR interface OR record OR struct)</item>
    /// <item>JS/TS: import OR export OR module.exports</item>
    /// <item>Python: (def OR class) AND (import OR shebang)</item>
    /// <item>Java: public class OR package</item>
    /// <item>Go: package AND func</item>
    /// <item>Rust: (fn OR struct) AND (use OR mod)</item>
    /// <item>XML: &lt;?xml OR &lt;Project OR &lt;configuration</item>
    /// <item>JSON: starts with { AND ends with }</item>
    /// <item>YAML: contains ": " AND doesn't start with -</item>
    /// </list>
    /// </remarks>
    private bool HasCompleteFileStructure(string content, string? language)
    {
        if (string.IsNullOrEmpty(language) || string.IsNullOrWhiteSpace(content))
        {
            _logger?.LogDebug("[INFO] HasCompleteFileStructure: no language or empty content");
            return false;
        }

        var trimmed = content.Trim();
        var normalized = language.ToLowerInvariant();

        var hasStructure = normalized switch
        {
            // C# / csharp: namespace or using + class/interface/record/struct
            "csharp" or "cs" =>
                (trimmed.Contains("namespace ") || trimmed.Contains("using "))
                && (trimmed.Contains("class ") || trimmed.Contains("interface ")
                    || trimmed.Contains("record ") || trimmed.Contains("struct ")),

            // JavaScript / TypeScript: import or export statements
            "javascript" or "js" or "typescript" or "ts" =>
                trimmed.Contains("import ") || trimmed.Contains("export ")
                || trimmed.Contains("module.exports"),

            // Python: def or class + import or shebang
            "python" or "py" =>
                (trimmed.Contains("def ") || trimmed.Contains("class "))
                && (trimmed.Contains("import ") || trimmed.StartsWith("#!")),

            // Java: public class or package declaration
            "java" =>
                trimmed.Contains("public class ") || trimmed.Contains("package "),

            // Go: package + func
            "go" or "golang" =>
                trimmed.Contains("package ") && trimmed.Contains("func "),

            // Rust: fn or struct + use or mod
            "rust" or "rs" =>
                (trimmed.Contains("fn ") || trimmed.Contains("struct "))
                && (trimmed.Contains("use ") || trimmed.Contains("mod ")),

            // XML: xml declaration or project/configuration root
            "xml" =>
                trimmed.StartsWith("<?xml") || trimmed.StartsWith("<Project")
                || trimmed.StartsWith("<configuration"),

            // JSON: object literal
            "json" =>
                trimmed.StartsWith('{') && trimmed.EndsWith('}'),

            // YAML: key-value pairs (not array)
            "yaml" or "yml" =>
                trimmed.Contains(": ") && !trimmed.StartsWith('-'),

            // Unknown language
            _ => false
        };

        _logger?.LogDebug(
            "[INFO] HasCompleteFileStructure: language={Language}, result={Result}",
            language,
            hasStructure);

        return hasStructure;
    }
}
