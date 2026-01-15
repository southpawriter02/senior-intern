namespace AIntern.Core.Models;

/// <summary>
/// Methods for estimating token counts.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4a.</para>
/// </remarks>
public enum TokenEstimationMethod
{
    /// <summary>
    /// Simple character-based estimation (~3.5 chars per token).
    /// </summary>
    /// <remarks>
    /// Fast O(1) algorithm but less accurate. Best for quick estimates
    /// and real-time typing scenarios where speed is critical.
    /// </remarks>
    CharacterBased,

    /// <summary>
    /// Word and punctuation based estimation (default method).
    /// </summary>
    /// <remarks>
    /// O(n) algorithm that considers word count, punctuation, newlines,
    /// and whitespace sequences. Good balance of speed and accuracy.
    /// </remarks>
    WordBased,

    /// <summary>
    /// BPE-approximate estimation simulating subword tokenization.
    /// </summary>
    /// <remarks>
    /// Most accurate for code, matches common programming tokens.
    /// Slower O(n×m) where m is the number of common token patterns.
    /// </remarks>
    BpeApproximate
}
