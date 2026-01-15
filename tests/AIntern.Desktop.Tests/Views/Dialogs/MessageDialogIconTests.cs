namespace AIntern.Desktop.Tests.Views.Dialogs;

using Xunit;
using AIntern.Desktop.Views.Dialogs;

/// <summary>
/// Unit tests for <see cref="MessageDialogIcon"/> enum.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.3g.</para>
/// </remarks>
public class MessageDialogIconTests
{
    /// <summary>
    /// Verifies that all expected icon values exist.
    /// </summary>
    [Fact]
    public void MessageDialogIcon_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)MessageDialogIcon.None);
        Assert.Equal(1, (int)MessageDialogIcon.Information);
        Assert.Equal(2, (int)MessageDialogIcon.Warning);
        Assert.Equal(3, (int)MessageDialogIcon.Error);
        Assert.Equal(4, (int)MessageDialogIcon.Question);
    }

    /// <summary>
    /// Verifies that there are exactly 5 icon types.
    /// </summary>
    [Fact]
    public void MessageDialogIcon_HasFiveValues()
    {
        // Act
        var values = System.Enum.GetValues<MessageDialogIcon>();

        // Assert
        Assert.Equal(5, values.Length);
    }
}
