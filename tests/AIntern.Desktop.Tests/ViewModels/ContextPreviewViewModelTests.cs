namespace AIntern.Desktop.Tests.ViewModels;

using System;
using Xunit;
using AIntern.Desktop.ViewModels;

/// <summary>
/// Unit tests for <see cref="ContextPreviewViewModel"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.3.4e.</para>
/// </remarks>
public class ContextPreviewViewModelTests
{
    #region ShowPreview Tests

    /// <summary>
    /// Verifies ShowPreview sets IsPreviewOpen to true.
    /// </summary>
    [Fact]
    public void ShowPreview_SetsIsPreviewOpenTrue()
    {
        // Arrange
        var vm = new ContextPreviewViewModel();
        var context = FileContextViewModel.FromFile("/test.cs", "code", 100);

        // Act
        vm.ShowPreview(context);

        // Assert
        Assert.True(vm.IsPreviewOpen);
    }

    /// <summary>
    /// Verifies ShowPreview sets SelectedPreviewContext.
    /// </summary>
    [Fact]
    public void ShowPreview_SetsSelectedPreviewContext()
    {
        // Arrange
        var vm = new ContextPreviewViewModel();
        var context = FileContextViewModel.FromFile("/test.cs", "code", 100);

        // Act
        vm.ShowPreview(context);

        // Assert
        Assert.Same(context, vm.SelectedPreviewContext);
    }

    /// <summary>
    /// Verifies ShowPreview with null does nothing.
    /// </summary>
    [Fact]
    public void ShowPreview_Null_NoEffect()
    {
        // Arrange
        var vm = new ContextPreviewViewModel();

        // Act
        vm.ShowPreview(null);

        // Assert
        Assert.False(vm.IsPreviewOpen);
        Assert.Null(vm.SelectedPreviewContext);
    }

    #endregion

    #region HidePreview Tests

    /// <summary>
    /// Verifies HidePreview sets IsPreviewOpen to false.
    /// </summary>
    [Fact]
    public void HidePreview_SetsIsPreviewOpenFalse()
    {
        // Arrange
        var vm = new ContextPreviewViewModel();
        vm.ShowPreview(FileContextViewModel.FromFile("/test.cs", "code", 100));

        // Act
        vm.HidePreview();

        // Assert
        Assert.False(vm.IsPreviewOpen);
    }

    /// <summary>
    /// Verifies HidePreview clears SelectedPreviewContext.
    /// </summary>
    [Fact]
    public void HidePreview_ClearsSelectedPreviewContext()
    {
        // Arrange
        var vm = new ContextPreviewViewModel();
        vm.ShowPreview(FileContextViewModel.FromFile("/test.cs", "code", 100));

        // Act
        vm.HidePreview();

        // Assert
        Assert.Null(vm.SelectedPreviewContext);
    }

    #endregion

    #region Commands Tests

    /// <summary>
    /// Verifies ShowPreviewCommand executes ShowPreview.
    /// </summary>
    [Fact]
    public void ShowPreviewCommand_ExecutesShowPreview()
    {
        // Arrange
        var vm = new ContextPreviewViewModel();
        var context = FileContextViewModel.FromFile("/test.cs", "code", 100);

        // Act
        vm.ShowPreviewCommand.Execute(context);

        // Assert
        Assert.True(vm.IsPreviewOpen);
        Assert.Same(context, vm.SelectedPreviewContext);
    }

    /// <summary>
    /// Verifies HidePreviewCommand executes HidePreview.
    /// </summary>
    [Fact]
    public void HidePreviewCommand_ExecutesHidePreview()
    {
        // Arrange
        var vm = new ContextPreviewViewModel();
        vm.ShowPreview(FileContextViewModel.FromFile("/test.cs", "code", 100));

        // Act
        vm.HidePreviewCommand.Execute(null);

        // Assert
        Assert.False(vm.IsPreviewOpen);
    }

    #endregion

    #region Event Tests

    /// <summary>
    /// Verifies OpenInEditorRequested event is raised.
    /// </summary>
    [Fact]
    public void OpenContextFileCommand_RaisesOpenInEditorRequested()
    {
        // Arrange
        var vm = new ContextPreviewViewModel();
        var context = FileContextViewModel.FromFile("/test.cs", "code", 100);
        vm.ShowPreview(context);
        FileContextViewModel? receivedContext = null;
        vm.OpenInEditorRequested += (_, ctx) => receivedContext = ctx;

        // Act
        vm.OpenContextFileCommand?.Execute(null);

        // Assert
        Assert.Same(context, receivedContext);
        Assert.False(vm.IsPreviewOpen);
    }

    /// <summary>
    /// Verifies RemoveContextRequested event is raised.
    /// </summary>
    [Fact]
    public void RemoveSelectedContextCommand_RaisesRemoveContextRequested()
    {
        // Arrange
        var vm = new ContextPreviewViewModel();
        var context = FileContextViewModel.FromFile("/test.cs", "code", 100);
        vm.ShowPreview(context);
        FileContextViewModel? receivedContext = null;
        vm.RemoveContextRequested += (_, ctx) => receivedContext = ctx;

        // Act
        vm.RemoveSelectedContextCommand?.Execute(null);

        // Assert
        Assert.Same(context, receivedContext);
        Assert.False(vm.IsPreviewOpen);
    }

    #endregion

    #region Initial State Tests

    /// <summary>
    /// Verifies initial state.
    /// </summary>
    [Fact]
    public void Constructor_InitialState()
    {
        // Arrange & Act
        var vm = new ContextPreviewViewModel();

        // Assert
        Assert.False(vm.IsPreviewOpen);
        Assert.Null(vm.SelectedPreviewContext);
        Assert.NotNull(vm.ShowPreviewCommand);
        Assert.NotNull(vm.HidePreviewCommand);
    }

    #endregion
}
