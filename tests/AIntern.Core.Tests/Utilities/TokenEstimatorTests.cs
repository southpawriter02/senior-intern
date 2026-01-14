using Xunit;
using AIntern.Core.Utilities;

namespace AIntern.Core.Tests.Utilities;

/// <summary>
/// Unit tests for the <see cref="TokenEstimator"/> class.
/// Verifies token estimation, language multipliers, and budget checks.
/// </summary>
public class TokenEstimatorTests
{
    #region Basic Estimation Tests

    /// <summary>
    /// Verifies that Estimate calculates approximate token count.
    /// </summary>
    [Fact]
    public void Estimate_CalculatesApproximateTokens()
    {
        // Arrange - 350 chars at 3.5 chars/token = 100 tokens
        var content = new string('a', 350);

        // Act
        var tokens = TokenEstimator.Estimate(content);

        // Assert
        Assert.Equal(100, tokens);
    }

    /// <summary>
    /// Verifies that Estimate returns 0 for null content.
    /// </summary>
    [Fact]
    public void Estimate_NullContent_ReturnsZero()
    {
        // Act & Assert
        Assert.Equal(0, TokenEstimator.Estimate(null));
    }

    /// <summary>
    /// Verifies that Estimate returns 0 for empty content.
    /// </summary>
    [Fact]
    public void Estimate_EmptyContent_ReturnsZero()
    {
        // Act & Assert
        Assert.Equal(0, TokenEstimator.Estimate(string.Empty));
    }

    /// <summary>
    /// Verifies that Estimate applies language multiplier.
    /// </summary>
    [Fact]
    public void Estimate_WithLanguage_AppliesMultiplier()
    {
        // Arrange
        var content = new string('a', 350); // 100 base tokens

        // Act
        var csharpTokens = TokenEstimator.Estimate(content, "csharp");
        var markdownTokens = TokenEstimator.Estimate(content, "markdown");

        // Assert - C# has 1.2x multiplier, markdown has 0.9x
        Assert.Equal(120, csharpTokens);  // 100 * 1.2
        Assert.Equal(90, markdownTokens); // 100 * 0.9
    }

    #endregion

    #region Language Multiplier Tests

    /// <summary>
    /// Verifies that GetLanguageMultiplier returns correct values for known languages.
    /// </summary>
    [Theory]
    [InlineData("csharp", 1.2)]
    [InlineData("c#", 1.2)]
    [InlineData("java", 1.2)]
    [InlineData("cpp", 1.25)]
    [InlineData("python", 1.0)]
    [InlineData("json", 1.3)]
    [InlineData("xml", 1.35)]
    [InlineData("markdown", 0.9)]
    [InlineData("text", 0.85)]
    public void GetLanguageMultiplier_ReturnsCorrectValue(string language, double expected)
    {
        // Act & Assert
        Assert.Equal(expected, TokenEstimator.GetLanguageMultiplier(language));
    }

    /// <summary>
    /// Verifies that GetLanguageMultiplier returns 1.0 for unknown languages.
    /// </summary>
    [Fact]
    public void GetLanguageMultiplier_UnknownLanguage_ReturnsDefault()
    {
        // Act & Assert
        Assert.Equal(1.0, TokenEstimator.GetLanguageMultiplier("unknown"));
        Assert.Equal(1.0, TokenEstimator.GetLanguageMultiplier(null));
    }

    /// <summary>
    /// Verifies that language matching is case-insensitive.
    /// </summary>
    [Fact]
    public void GetLanguageMultiplier_CaseInsensitive()
    {
        // Act & Assert
        Assert.Equal(TokenEstimator.GetLanguageMultiplier("csharp"), TokenEstimator.GetLanguageMultiplier("CSHARP"));
        Assert.Equal(TokenEstimator.GetLanguageMultiplier("python"), TokenEstimator.GetLanguageMultiplier("Python"));
    }

    #endregion

    #region MaxContentLength Tests

    /// <summary>
    /// Verifies that MaxContentLength calculates correctly.
    /// </summary>
    [Fact]
    public void MaxContentLength_ReturnsCorrectValue()
    {
        // Arrange - 100 tokens * 3.5 chars/token = 350 chars
        var budget = 100;

        // Act
        var maxLength = TokenEstimator.MaxContentLength(budget);

        // Assert
        Assert.Equal(350, maxLength);
    }

    /// <summary>
    /// Verifies that MaxContentLength applies language adjustment.
    /// </summary>
    [Fact]
    public void MaxContentLength_WithLanguage_AdjustsForMultiplier()
    {
        // Arrange - 100 tokens, C# has 1.2 multiplier
        // 100 * 3.5 / 1.2 = 291.67 → 291
        var budget = 100;

        // Act
        var maxLength = TokenEstimator.MaxContentLength(budget, "csharp");

        // Assert
        Assert.Equal(291, maxLength);
    }

    /// <summary>
    /// Verifies that MaxContentLength returns 0 for zero budget.
    /// </summary>
    [Fact]
    public void MaxContentLength_ZeroBudget_ReturnsZero()
    {
        // Act & Assert
        Assert.Equal(0, TokenEstimator.MaxContentLength(0));
    }

    #endregion

    #region ExceedsBudget Tests

    /// <summary>
    /// Verifies that ExceedsBudget returns true when content exceeds budget.
    /// </summary>
    [Fact]
    public void ExceedsBudget_LargeContent_ReturnsTrue()
    {
        // Arrange - 700 chars = 200 tokens, budget = 150
        var content = new string('a', 700);

        // Act & Assert
        Assert.True(TokenEstimator.ExceedsBudget(content, 150));
    }

    /// <summary>
    /// Verifies that ExceedsBudget returns false when content is within budget.
    /// </summary>
    [Fact]
    public void ExceedsBudget_SmallContent_ReturnsFalse()
    {
        // Arrange - 100 chars = ~29 tokens, budget = 50
        var content = new string('a', 100);

        // Act & Assert
        Assert.False(TokenEstimator.ExceedsBudget(content, 50));
    }

    #endregion

    #region BaseCharsPerToken Tests

    /// <summary>
    /// Verifies that BaseCharsPerToken returns the expected constant.
    /// </summary>
    [Fact]
    public void BaseCharsPerToken_Returns3Point5()
    {
        // Act & Assert
        Assert.Equal(3.5, TokenEstimator.BaseCharsPerToken);
    }

    #endregion
}
