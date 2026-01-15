namespace AIntern.Core.Models;

/// <summary>
/// Configuration for context attachment limits.
/// </summary>
/// <remarks>
/// <para>
/// Defines constraints on file attachments, token limits, and preview sizes
/// for the context attachment feature. All values can be configured via settings.
/// </para>
/// <para>Added in v0.3.4a.</para>
/// </remarks>
public sealed class ContextLimitsConfig
{
    /// <summary>
    /// Maximum number of files that can be attached at once.
    /// </summary>
    public int MaxFilesAttached { get; set; } = 10;

    /// <summary>
    /// Maximum tokens per individual file.
    /// </summary>
    /// <remarks>
    /// Files exceeding this limit will be truncated or rejected.
    /// </remarks>
    public int MaxTokensPerFile { get; set; } = 4000;

    /// <summary>
    /// Maximum total tokens across all attached contexts.
    /// </summary>
    /// <remarks>
    /// This is the overall context budget available for attachments.
    /// </remarks>
    public int MaxTotalContextTokens { get; set; } = 8000;

    /// <summary>
    /// Maximum file size in bytes (files larger are rejected).
    /// </summary>
    /// <remarks>
    /// Default: 500KB. Prevents loading extremely large files.
    /// </remarks>
    public int MaxFileSizeBytes { get; set; } = 500_000;

    /// <summary>
    /// Warning threshold as percentage of limit (0.0-1.0).
    /// </summary>
    /// <remarks>
    /// When usage exceeds this threshold, the UI shows a warning.
    /// </remarks>
    public double WarningThreshold { get; set; } = 0.8;

    /// <summary>
    /// Maximum lines to show in preview.
    /// </summary>
    public int MaxPreviewLines { get; set; } = 20;

    /// <summary>
    /// Maximum characters in preview content.
    /// </summary>
    public int MaxPreviewCharacters { get; set; } = 500;
}
