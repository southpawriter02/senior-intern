namespace AIntern.Services.Tests;

using System.Collections.Generic;
using Xunit;
using AIntern.Core.Models;

/// <summary>
/// Unit tests for <see cref="TokenEstimationService"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4a.</para>
/// </remarks>
public class TokenEstimationServiceTests
{
    private readonly TokenEstimationService _service = new();

    #region EstimateTokens Default Tests

    /// <summary>
    /// Verifies that EstimateTokens returns 0 for null content.
    /// </summary>
    [Fact]
    public void EstimateTokens_NullContent_ReturnsZero()
    {
        // Act
        var result = _service.EstimateTokens(null!);

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Verifies that EstimateTokens returns 0 for empty content.
    /// </summary>
    [Fact]
    public void EstimateTokens_EmptyContent_ReturnsZero()
    {
        // Act
        var result = _service.EstimateTokens(string.Empty);

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Verifies that EstimateTokens returns at least 1 for non-empty content.
    /// </summary>
    [Fact]
    public void EstimateTokens_NonEmptyContent_ReturnsAtLeastOne()
    {
        // Act
        var result = _service.EstimateTokens("hello");

        // Assert
        Assert.True(result >= 1);
    }

    /// <summary>
    /// Verifies that longer content produces more tokens.
    /// </summary>
    [Fact]
    public void EstimateTokens_LongerContent_ProducesMoreTokens()
    {
        // Arrange
        var short_content = "hello world";
        var long_content = "hello world this is a much longer piece of text with many words";

        // Act
        var shortResult = _service.EstimateTokens(short_content);
        var longResult = _service.EstimateTokens(long_content);

        // Assert
        Assert.True(longResult > shortResult);
    }

    #endregion

    #region CharacterBased Estimation Tests

    /// <summary>
    /// Verifies CharacterBased estimation for simple text.
    /// </summary>
    [Fact]
    public void EstimateTokens_CharacterBased_SimpleText()
    {
        // Arrange (35 chars / 3.5 = 10 tokens)
        var content = "This is a test with thirty-five ch";

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.CharacterBased);

        // Assert
        Assert.Equal(10, result);
    }

    /// <summary>
    /// Verifies CharacterBased estimation for code.
    /// </summary>
    [Fact]
    public void EstimateTokens_CharacterBased_CodeContent()
    {
        // Arrange
        var code = "public void Main() { Console.WriteLine(\"Hello\"); }";

        // Act
        var result = _service.EstimateTokens(code, TokenEstimationMethod.CharacterBased);

        // Assert
        Assert.True(result > 10); // 51 chars / 3.5 ≈ 15 tokens
        Assert.True(result < 20);
    }

    /// <summary>
    /// Verifies CharacterBased estimation for special characters.
    /// </summary>
    [Fact]
    public void EstimateTokens_CharacterBased_SpecialChars()
    {
        // Arrange (7 chars / 3.5 = 2 tokens)
        var content = "!@#$%^&";

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.CharacterBased);

        // Assert
        Assert.Equal(2, result);
    }

    #endregion

    #region WordBased Estimation Tests

    /// <summary>
    /// Verifies WordBased estimation counts words correctly.
    /// </summary>
    [Fact]
    public void EstimateTokens_WordBased_CountsWords()
    {
        // Arrange
        var content = "one two three four five";

        // Act
        var result = _service.EstimateTokens(content, TokenEstimationMethod.WordBased);

        // Assert - 5 words / 0.75 ≈ 7 tokens
        Assert.True(result >= 5);
        Assert.True(result <= 10);
    }

    /// <summary>
    /// Verifies WordBased estimation includes punctuation weight.
    /// </summary>
    [Fact]
    public void EstimateTokens_WordBased_IncludesPunctuation()
    {
        // Arrange
        var withoutPunctuation = "hello world";
        var withPunctuation = "hello, world!";

        // Act
        var withoutResult = _service.EstimateTokens(withoutPunctuation, TokenEstimationMethod.WordBased);
        var withResult = _service.EstimateTokens(withPunctuation, TokenEstimationMethod.WordBased);

        // Assert - punctuation adds token weight
        Assert.True(withResult >= withoutResult);
    }

    /// <summary>
    /// Verifies WordBased estimation includes newline weight.
    /// </summary>
    [Fact]
    public void EstimateTokens_WordBased_IncludesNewlines()
    {
        // Arrange
        var singleLine = "hello world";
        var multiLine = "hello\nworld\n";

        // Act
        var singleResult = _service.EstimateTokens(singleLine, TokenEstimationMethod.WordBased);
        var multiResult = _service.EstimateTokens(multiLine, TokenEstimationMethod.WordBased);

        // Assert - newlines add token weight
        Assert.True(multiResult >= singleResult);
    }

    /// <summary>
    /// Verifies WordBased estimation includes whitespace sequence weight.
    /// </summary>
    [Fact]
    public void EstimateTokens_WordBased_IncludesWhitespaceSequences()
    {
        // Arrange
        var normalSpacing = "hello world";
        var indented = "hello    world"; // 4 spaces

        // Act
        var normalResult = _service.EstimateTokens(normalSpacing, TokenEstimationMethod.WordBased);
        var indentedResult = _service.EstimateTokens(indented, TokenEstimationMethod.WordBased);

        // Assert - whitespace sequences add token weight
        Assert.True(indentedResult >= normalResult);
    }

    #endregion

    #region BpeApproximate Estimation Tests

    /// <summary>
    /// Verifies BpeApproximate matches common programming tokens.
    /// </summary>
    [Fact]
    public void EstimateTokens_BpeApproximate_MatchesCommonTokens()
    {
        // Arrange
        var code = "public static void";

        // Act
        var result = _service.EstimateTokens(code, TokenEstimationMethod.BpeApproximate);

        // Assert - each keyword should be roughly 1 token + whitespace
        Assert.True(result >= 3);
        Assert.True(result <= 10);
    }

    /// <summary>
    /// Verifies BpeApproximate handles operators.
    /// </summary>
    [Fact]
    public void EstimateTokens_BpeApproximate_HandlesOperators()
    {
        // Arrange
        var code = "a => b == c";

        // Act
        var result = _service.EstimateTokens(code, TokenEstimationMethod.BpeApproximate);

        // Assert
        Assert.True(result >= 3);
    }

    /// <summary>
    /// Verifies BpeApproximate handles long words.
    /// </summary>
    [Fact]
    public void EstimateTokens_BpeApproximate_LongWordsSplitIntoMultipleTokens()
    {
        // Arrange
        var shortWord = "cat";
        var longWord = "supercalifragilisticexpialidocious";

        // Act
        var shortResult = _service.EstimateTokens(shortWord, TokenEstimationMethod.BpeApproximate);
        var longResult = _service.EstimateTokens(longWord, TokenEstimationMethod.BpeApproximate);

        // Assert - long word should produce more tokens
        Assert.True(longResult > shortResult);
    }

    /// <summary>
    /// Verifies BpeApproximate handles code with mixed content.
    /// </summary>
    [Fact]
    public void EstimateTokens_BpeApproximate_HandlesCodeBlock()
    {
        // Arrange
        var code = """
            public void Main()
            {
                Console.WriteLine("Hello");
            }
            """;

        // Act
        var result = _service.EstimateTokens(code, TokenEstimationMethod.BpeApproximate);

        // Assert - reasonable token count for this code
        Assert.True(result >= 10);
        Assert.True(result <= 50);
    }

    #endregion

    #region GetRecommendedContextLimit Tests

    /// <summary>
    /// Verifies GetRecommendedContextLimit returns the expected value.
    /// </summary>
    [Fact]
    public void GetRecommendedContextLimit_ReturnsDefaultValue()
    {
        // Act
        var result = _service.GetRecommendedContextLimit();

        // Assert
        Assert.Equal(8000, result);
    }

    #endregion

    #region WouldExceedLimit Tests

    /// <summary>
    /// Verifies WouldExceedLimit returns false when under limit.
    /// </summary>
    [Fact]
    public void WouldExceedLimit_UnderLimit_ReturnsFalse()
    {
        // Arrange
        var currentTokens = 100;
        var newContent = "hello world";

        // Act
        var result = _service.WouldExceedLimit(currentTokens, newContent);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies WouldExceedLimit returns false at limit.
    /// </summary>
    [Fact]
    public void WouldExceedLimit_AtLimit_ReturnsFalse()
    {
        // Arrange
        var content = "hello";
        var tokens = _service.EstimateTokens(content);
        var currentTokens = _service.GetRecommendedContextLimit() - tokens;

        // Act
        var result = _service.WouldExceedLimit(currentTokens, content);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies WouldExceedLimit returns true when over limit.
    /// </summary>
    [Fact]
    public void WouldExceedLimit_OverLimit_ReturnsTrue()
    {
        // Arrange
        var currentTokens = _service.GetRecommendedContextLimit();
        var newContent = "any content at all";

        // Act
        var result = _service.WouldExceedLimit(currentTokens, newContent);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region TruncateToTokenLimit Tests

    /// <summary>
    /// Verifies TruncateToTokenLimit returns content unchanged when under limit.
    /// </summary>
    [Fact]
    public void TruncateToTokenLimit_UnderLimit_ReturnsUnchanged()
    {
        // Arrange
        var content = "hello world";

        // Act
        var result = _service.TruncateToTokenLimit(content, 1000);

        // Assert
        Assert.Equal(content, result);
    }

    /// <summary>
    /// Verifies TruncateToTokenLimit truncates content when over limit.
    /// </summary>
    [Fact]
    public void TruncateToTokenLimit_OverLimit_TruncatesContent()
    {
        // Arrange
        var content = string.Join(" ", System.Linq.Enumerable.Repeat("word", 100));

        // Act
        var result = _service.TruncateToTokenLimit(content, 10);

        // Assert
        Assert.True(result.Length < content.Length);
        Assert.EndsWith("... (truncated)", result);
    }

    /// <summary>
    /// Verifies TruncateToTokenLimit handles empty content.
    /// </summary>
    [Fact]
    public void TruncateToTokenLimit_EmptyContent_ReturnsEmpty()
    {
        // Act
        var result = _service.TruncateToTokenLimit(string.Empty, 100);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    /// Verifies TruncateToTokenLimit handles null content.
    /// </summary>
    [Fact]
    public void TruncateToTokenLimit_NullContent_ReturnsEmpty()
    {
        // Act
        var result = _service.TruncateToTokenLimit(null!, 100);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region GetUsageBreakdown Tests

    /// <summary>
    /// Verifies GetUsageBreakdown with empty collection.
    /// </summary>
    [Fact]
    public void GetUsageBreakdown_EmptyCollection_ReturnsZeroTokens()
    {
        // Arrange
        var contents = Array.Empty<string>();

        // Act
        var result = _service.GetUsageBreakdown(contents);

        // Assert
        Assert.Equal(0, result.TotalTokens);
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// Verifies GetUsageBreakdown with single item.
    /// </summary>
    [Fact]
    public void GetUsageBreakdown_SingleItem_CalculatesCorrectly()
    {
        // Arrange
        var content = "hello world";
        var contents = new[] { content };
        var expectedTokens = _service.EstimateTokens(content);

        // Act
        var result = _service.GetUsageBreakdown(contents);

        // Assert
        Assert.Equal(expectedTokens, result.TotalTokens);
        Assert.Single(result.Items);
        Assert.Equal("Item 1", result.Items[0].Label);
    }

    /// <summary>
    /// Verifies GetUsageBreakdown with multiple items.
    /// </summary>
    [Fact]
    public void GetUsageBreakdown_MultipleItems_SumsTokensCorrectly()
    {
        // Arrange
        var contents = new[] { "hello", "world", "foo bar baz" };
        var expectedTotal = contents.Sum(c => _service.EstimateTokens(c));

        // Act
        var result = _service.GetUsageBreakdown(contents);

        // Assert
        Assert.Equal(expectedTotal, result.TotalTokens);
        Assert.Equal(3, result.Items.Count);
    }

    /// <summary>
    /// Verifies GetUsageBreakdown with labeled items.
    /// </summary>
    [Fact]
    public void GetUsageBreakdown_LabeledItems_UsesProvidedLabels()
    {
        // Arrange
        var labeledContents = new (string Label, string Content)[]
        {
            ("main.cs", "public class Program {}"),
            ("utils.ts", "export function helper() {}")
        };

        // Act
        var result = _service.GetUsageBreakdown(labeledContents);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("main.cs", result.Items[0].Label);
        Assert.Equal("utils.ts", result.Items[1].Label);
    }

    #endregion

    #region TokenUsageBreakdown Property Tests

    /// <summary>
    /// Verifies UsagePercentage calculation.
    /// </summary>
    [Fact]
    public void TokenUsageBreakdown_UsagePercentage_CalculatesCorrectly()
    {
        // Arrange
        var breakdown = new TokenUsageBreakdown
        {
            TotalTokens = 4000,
            RecommendedLimit = 8000
        };

        // Assert
        Assert.Equal(50.0, breakdown.UsagePercentage);
    }

    /// <summary>
    /// Verifies IsOverLimit when under limit.
    /// </summary>
    [Fact]
    public void TokenUsageBreakdown_IsOverLimit_UnderLimit_ReturnsFalse()
    {
        // Arrange
        var breakdown = new TokenUsageBreakdown
        {
            TotalTokens = 4000,
            RecommendedLimit = 8000
        };

        // Assert
        Assert.False(breakdown.IsOverLimit);
    }

    /// <summary>
    /// Verifies IsOverLimit when over limit.
    /// </summary>
    [Fact]
    public void TokenUsageBreakdown_IsOverLimit_OverLimit_ReturnsTrue()
    {
        // Arrange
        var breakdown = new TokenUsageBreakdown
        {
            TotalTokens = 10000,
            RecommendedLimit = 8000
        };

        // Assert
        Assert.True(breakdown.IsOverLimit);
    }

    /// <summary>
    /// Verifies RemainingTokens calculation.
    /// </summary>
    [Fact]
    public void TokenUsageBreakdown_RemainingTokens_CalculatesCorrectly()
    {
        // Arrange
        var breakdown = new TokenUsageBreakdown
        {
            TotalTokens = 3000,
            RecommendedLimit = 8000
        };

        // Assert
        Assert.Equal(5000, breakdown.RemainingTokens);
    }

    /// <summary>
    /// Verifies RemainingTokens returns 0 when over limit.
    /// </summary>
    [Fact]
    public void TokenUsageBreakdown_RemainingTokens_OverLimit_ReturnsZero()
    {
        // Arrange
        var breakdown = new TokenUsageBreakdown
        {
            TotalTokens = 10000,
            RecommendedLimit = 8000
        };

        // Assert
        Assert.Equal(0, breakdown.RemainingTokens);
    }

    /// <summary>
    /// Verifies IsWarning at 80% threshold.
    /// </summary>
    [Fact]
    public void TokenUsageBreakdown_IsWarning_AtThreshold_ReturnsTrue()
    {
        // Arrange
        var breakdown = new TokenUsageBreakdown
        {
            TotalTokens = 6400, // 80%
            RecommendedLimit = 8000
        };

        // Assert
        Assert.True(breakdown.IsWarning);
    }

    /// <summary>
    /// Verifies IsWarning is false when over limit.
    /// </summary>
    [Fact]
    public void TokenUsageBreakdown_IsWarning_OverLimit_ReturnsFalse()
    {
        // Arrange
        var breakdown = new TokenUsageBreakdown
        {
            TotalTokens = 9000,
            RecommendedLimit = 8000
        };

        // Assert
        Assert.False(breakdown.IsWarning);
        Assert.True(breakdown.IsOverLimit);
    }

    #endregion

    #region ContextLimitsConfig Tests

    /// <summary>
    /// Verifies ContextLimitsConfig has correct defaults.
    /// </summary>
    [Fact]
    public void ContextLimitsConfig_HasCorrectDefaults()
    {
        // Arrange
        var config = new ContextLimitsConfig();

        // Assert
        Assert.Equal(10, config.MaxFilesAttached);
        Assert.Equal(4000, config.MaxTokensPerFile);
        Assert.Equal(8000, config.MaxTotalContextTokens);
        Assert.Equal(500_000, config.MaxFileSizeBytes);
        Assert.Equal(0.8, config.WarningThreshold);
        Assert.Equal(20, config.MaxPreviewLines);
        Assert.Equal(500, config.MaxPreviewCharacters);
    }

    /// <summary>
    /// Verifies ContextLimitsConfig can be customized.
    /// </summary>
    [Fact]
    public void ContextLimitsConfig_CanBeCustomized()
    {
        // Arrange
        var config = new ContextLimitsConfig
        {
            MaxFilesAttached = 20,
            MaxTokensPerFile = 8000,
            MaxTotalContextTokens = 16000
        };

        // Assert
        Assert.Equal(20, config.MaxFilesAttached);
        Assert.Equal(8000, config.MaxTokensPerFile);
        Assert.Equal(16000, config.MaxTotalContextTokens);
    }

    #endregion
}
