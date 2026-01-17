using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the QuickActionResult record (v0.4.5g).
/// </summary>
public sealed class QuickActionResultTests
{
    #region Factory Method Tests

    /// <summary>
    /// Verifies Success factory creates correct result.
    /// </summary>
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Act
        var result = QuickActionResult.Success(QuickActionType.Copy, "Copied!");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(QuickActionType.Copy, result.ActionType);
        Assert.Equal("Copied!", result.Message);
    }

    /// <summary>
    /// Verifies Success factory with data parameter.
    /// </summary>
    [Fact]
    public void Success_WithData_IncludesData()
    {
        // Arrange
        var data = new { Id = 123 };

        // Act
        var result = QuickActionResult.Success(
            QuickActionType.Apply,
            data: data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// Verifies Failure factory creates failure result.
    /// </summary>
    [Fact]
    public void Failure_CreatesFailureResult()
    {
        // Act
        var result = QuickActionResult.Failure(
            QuickActionType.Apply,
            "File not found");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(QuickActionType.Apply, result.ActionType);
        Assert.Equal("File not found", result.Message);
    }

    /// <summary>
    /// Verifies Failure factory with duration.
    /// </summary>
    [Fact]
    public void Failure_WithDuration_IncludesDuration()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(150);

        // Act
        var result = QuickActionResult.Failure(
            QuickActionType.ShowDiff,
            "Error",
            duration);

        // Assert
        Assert.Equal(duration, result.Duration);
    }

    #endregion

    #region DisplayMessage Tests

    /// <summary>
    /// Verifies DisplayMessage for Copy success.
    /// </summary>
    [Fact]
    public void DisplayMessage_CopySuccess_ReturnsCopied()
    {
        // Act
        var result = QuickActionResult.Success(QuickActionType.Copy);

        // Assert
        Assert.Equal("Copied!", result.DisplayMessage);
    }

    /// <summary>
    /// Verifies DisplayMessage for Apply success.
    /// </summary>
    [Fact]
    public void DisplayMessage_ApplySuccess_ReturnsApplied()
    {
        // Act
        var result = QuickActionResult.Success(QuickActionType.Apply);

        // Assert
        Assert.Equal("Applied!", result.DisplayMessage);
    }

    /// <summary>
    /// Verifies DisplayMessage for ShowDiff success.
    /// </summary>
    [Fact]
    public void DisplayMessage_ShowDiffSuccess_ReturnsShowingDiff()
    {
        // Act
        var result = QuickActionResult.Success(QuickActionType.ShowDiff);

        // Assert
        Assert.Equal("Showing diff...", result.DisplayMessage);
    }

    /// <summary>
    /// Verifies DisplayMessage for Reject success.
    /// </summary>
    [Fact]
    public void DisplayMessage_RejectSuccess_ReturnsRejected()
    {
        // Act
        var result = QuickActionResult.Success(QuickActionType.Reject);

        // Assert
        Assert.Equal("Rejected", result.DisplayMessage);
    }

    /// <summary>
    /// Verifies DisplayMessage for failure returns error message.
    /// </summary>
    [Fact]
    public void DisplayMessage_Failure_ReturnsErrorMessage()
    {
        // Act
        var result = QuickActionResult.Failure(
            QuickActionType.Apply,
            "Target file not found");

        // Assert
        Assert.Equal("Target file not found", result.DisplayMessage);
    }

    /// <summary>
    /// Verifies DisplayMessage for failure without message.
    /// </summary>
    [Fact]
    public void DisplayMessage_FailureNoMessage_ReturnsDefault()
    {
        // Arrange - create failure without message via record init
        var result = new QuickActionResult(
            IsSuccess: false,
            ActionType: QuickActionType.Apply,
            Message: null);

        // Assert
        Assert.Equal("Action failed", result.DisplayMessage);
    }

    /// <summary>
    /// Verifies DisplayMessage for InsertAtCursor success.
    /// </summary>
    [Fact]
    public void DisplayMessage_InsertSuccess_ReturnsInserted()
    {
        // Act
        var result = QuickActionResult.Success(QuickActionType.InsertAtCursor);

        // Assert
        Assert.Equal("Inserted!", result.DisplayMessage);
    }

    /// <summary>
    /// Verifies DisplayMessage for RunCommand success.
    /// </summary>
    [Fact]
    public void DisplayMessage_RunCommandSuccess_ReturnsRunning()
    {
        // Act
        var result = QuickActionResult.Success(QuickActionType.RunCommand);

        // Assert
        Assert.Equal("Running...", result.DisplayMessage);
    }

    #endregion

    #region Record Equality Tests

    /// <summary>
    /// Verifies record equality for same values.
    /// </summary>
    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var result1 = QuickActionResult.Success(QuickActionType.Copy);
        var result2 = QuickActionResult.Success(QuickActionType.Copy);

        // Assert
        Assert.Equal(result1, result2);
    }

    /// <summary>
    /// Verifies with expression creates modified copy.
    /// </summary>
    [Fact]
    public void With_Duration_CreatesModifiedCopy()
    {
        // Arrange
        var original = QuickActionResult.Success(QuickActionType.Copy);
        var duration = TimeSpan.FromSeconds(1);

        // Act
        var modified = original with { Duration = duration };

        // Assert
        Assert.Equal(TimeSpan.Zero, original.Duration);
        Assert.Equal(duration, modified.Duration);
    }

    #endregion
}
