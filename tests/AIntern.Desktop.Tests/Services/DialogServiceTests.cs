namespace AIntern.Desktop.Tests.Services;

using System;
using System.Threading.Tasks;
using Xunit;
using AIntern.Desktop.Services;

/// <summary>
/// Unit tests for <see cref="DialogService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Since DialogService requires an Avalonia runtime with a main window,
/// these tests verify behavior via exception handling and null handling
/// when no window is available.
/// </para>
/// <para>Added in v0.3.3g.</para>
/// </remarks>
public class DialogServiceTests
{
    #region Constructor Tests

    /// <summary>
    /// Verifies that DialogService can be constructed without a logger.
    /// </summary>
    [Fact]
    public void Constructor_WithoutLogger_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() => new DialogService());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that DialogService can be constructed with null logger.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() => new DialogService(null));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region ShowErrorAsync Tests

    /// <summary>
    /// Verifies that ShowErrorAsync completes without throwing when no window is available.
    /// </summary>
    [Fact]
    public async Task ShowErrorAsync_NoMainWindow_CompletesWithoutThrowing()
    {
        // Arrange
        var service = new DialogService();

        // Act
        var exception = await Record.ExceptionAsync(() =>
            service.ShowErrorAsync("Error", "Test message"));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region ShowInfoAsync Tests

    /// <summary>
    /// Verifies that ShowInfoAsync completes without throwing when no window is available.
    /// </summary>
    [Fact]
    public async Task ShowInfoAsync_NoMainWindow_CompletesWithoutThrowing()
    {
        // Arrange
        var service = new DialogService();

        // Act
        var exception = await Record.ExceptionAsync(() =>
            service.ShowInfoAsync("Info", "Test message"));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region ShowConfirmDialogAsync Tests

    /// <summary>
    /// Verifies that ShowConfirmDialogAsync returns null when no window is available.
    /// </summary>
    [Fact]
    public async Task ShowConfirmDialogAsync_NoMainWindow_ReturnsNull()
    {
        // Arrange
        var service = new DialogService();

        // Act
        var result = await service.ShowConfirmDialogAsync(
            "Confirm",
            "Test message",
            new[] { "Yes", "No" });

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ShowSaveDialogAsync Tests

    /// <summary>
    /// Verifies that ShowSaveDialogAsync returns null when no window is available.
    /// </summary>
    [Fact]
    public async Task ShowSaveDialogAsync_NoMainWindow_ReturnsNull()
    {
        // Arrange
        var service = new DialogService();

        // Act
        var result = await service.ShowSaveDialogAsync(
            "Save",
            "test.txt",
            new[] { ("Text Files", new[] { "txt" }) });

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ShowOpenFileDialogAsync Tests

    /// <summary>
    /// Verifies that ShowOpenFileDialogAsync returns null when no window is available.
    /// </summary>
    [Fact]
    public async Task ShowOpenFileDialogAsync_NoMainWindow_ReturnsNull()
    {
        // Arrange
        var service = new DialogService();

        // Act
        var result = await service.ShowOpenFileDialogAsync(
            "Open",
            new[] { ("All Files", new[] { "*" }) });

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that ShowOpenFileDialogAsync accepts allowMultiple parameter.
    /// </summary>
    [Fact]
    public async Task ShowOpenFileDialogAsync_WithAllowMultiple_ReturnsNull()
    {
        // Arrange
        var service = new DialogService();

        // Act
        var result = await service.ShowOpenFileDialogAsync(
            "Open",
            new[] { ("All Files", new[] { "*" }) },
            allowMultiple: true);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ShowFolderPickerAsync Tests

    /// <summary>
    /// Verifies that ShowFolderPickerAsync returns null when no window is available.
    /// </summary>
    [Fact]
    public async Task ShowFolderPickerAsync_NoMainWindow_ReturnsNull()
    {
        // Arrange
        var service = new DialogService();

        // Act
        var result = await service.ShowFolderPickerAsync("Select Folder");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ShowGoToLineDialogAsync Tests

    /// <summary>
    /// Verifies that ShowGoToLineDialogAsync returns null when no window is available.
    /// </summary>
    [Fact]
    public async Task ShowGoToLineDialogAsync_NoMainWindow_ReturnsNull()
    {
        // Arrange
        var service = new DialogService();

        // Act
        var result = await service.ShowGoToLineDialogAsync(100, 1);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
