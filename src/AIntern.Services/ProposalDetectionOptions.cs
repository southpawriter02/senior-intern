namespace AIntern.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PROPOSAL DETECTION OPTIONS (v0.4.4h)                                    │
// │ Configuration options for multi-file proposal detection.                │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Configuration options for proposal detection.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4h.</para>
/// </remarks>
public class ProposalDetectionOptions
{
    /// <summary>
    /// Minimum number of files required to show the proposal panel.
    /// Single files use the existing code block UI.
    /// </summary>
    public int MinimumFilesForPanel { get; set; } = 2;

    /// <summary>
    /// Whether to automatically detect proposals.
    /// </summary>
    public bool EnableAutoDetection { get; set; } = true;

    /// <summary>
    /// Languages to ignore when detecting proposals.
    /// These are typically output/display languages.
    /// </summary>
    public HashSet<string> IgnoredLanguages { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "output",
        "text",
        "plaintext",
        "console",
        "log",
        "stdout",
        "stderr"
    };

    /// <summary>
    /// Maximum number of proposals to cache.
    /// </summary>
    public int MaxCacheSize { get; set; } = 100;
}
