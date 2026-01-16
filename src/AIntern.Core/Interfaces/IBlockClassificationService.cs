namespace AIntern.Core.Interfaces;

using AIntern.Core.Models;

/// <summary>
/// Service for classifying the type/purpose of a code block (v0.4.1d).
/// </summary>
/// <remarks>
/// <para>
/// This service analyzes code blocks extracted from LLM responses and determines
/// their classification (CompleteFile, Snippet, Example, Command, Output, Config)
/// based on multiple factors:
/// </para>
/// <list type="bullet">
/// <item>Language type (shell commands, configuration files)</item>
/// <item>Surrounding context phrases (example indicators, apply indicators)</item>
/// <item>Content structure (complete file patterns)</item>
/// </list>
/// <para>
/// Classification determines what actions are available in the UI for each code block.
/// </para>
/// </remarks>
public interface IBlockClassificationService
{
    /// <summary>
    /// Classify a code block based on its content and surrounding context.
    /// </summary>
    /// <param name="content">The raw code content (without markdown fences).</param>
    /// <param name="language">Detected or specified language identifier (may be null).</param>
    /// <param name="surroundingText">Text before and after the code block for context analysis.</param>
    /// <returns>The classified block type.</returns>
    /// <remarks>
    /// Classification follows this priority order:
    /// <list type="number">
    /// <item>Shell language (bash, powershell, cmd) → Command</item>
    /// <item>Config language (json, yaml, xml) → Config</item>
    /// <item>Output indicators in context → Output</item>
    /// <item>Example indicators > Apply indicators → Example</item>
    /// <item>Apply indicators or complete structure → CompleteFile</item>
    /// <item>Default → Snippet</item>
    /// </list>
    /// </remarks>
    CodeBlockType ClassifyBlock(string content, string? language, string surroundingText);

    /// <summary>
    /// Get the confidence score for a classification.
    /// </summary>
    /// <param name="block">The classified code block.</param>
    /// <returns>
    /// Confidence score from 0.0 (uncertain) to 1.0 (certain).
    /// Higher scores indicate more certainty in the classification.
    /// </returns>
    /// <remarks>
    /// Confidence scores by type:
    /// <list type="bullet">
    /// <item>Command: 0.95 (shell language is explicit)</item>
    /// <item>Config: 0.90 (config language is explicit)</item>
    /// <item>Output: 0.85 (output indicators are clear)</item>
    /// <item>CompleteFile: 0.80 (structure-based)</item>
    /// <item>Example: 0.75 (context-based)</item>
    /// <item>Snippet: 0.70 (default classification)</item>
    /// </list>
    /// </remarks>
    float GetClassificationConfidence(CodeBlock block);
}
