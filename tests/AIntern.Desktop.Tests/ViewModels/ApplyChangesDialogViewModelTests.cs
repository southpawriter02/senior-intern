using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for v0.4.3f ApplyChangesDialogViewModel.
/// </summary>
public class ApplyChangesDialogViewModelTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_Default_InitializesCorrectly()
    {
        var vm = new ApplyChangesDialogViewModel();

        Assert.True(vm.CreateBackup);
        Assert.False(vm.IsApplying);
        Assert.True(vm.CanApply);
        Assert.True(vm.CanCancel);
    }

    [Fact]
    public void Constructor_WithDependencies_SetsProperties()
    {
        var mockChangeService = new Mock<IFileChangeService>();
        var codeBlock = new CodeBlock { TargetFilePath = "/test/file.cs" };
        var closeActionCalled = false;

        var vm = new ApplyChangesDialogViewModel(
            mockChangeService.Object,
            codeBlock,
            "/workspace",
            "original",
            "proposed",
            isNewFile: false,
            closeAction: _ => closeActionCalled = true);

        Assert.Equal("file.cs", vm.FileName);
        Assert.Equal("/test/file.cs", vm.FilePath);
        Assert.False(vm.IsNewFile);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Property Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CreateBackup_DefaultIsTrue()
    {
        var vm = new ApplyChangesDialogViewModel();
        Assert.True(vm.CreateBackup);
    }

    [Fact]
    public void ApplyButtonText_WhenNotApplying_ShowsApplyChanges()
    {
        var vm = new ApplyChangesDialogViewModel();
        Assert.Equal("Apply Changes", vm.ApplyButtonText);
    }

    [Fact]
    public void ShowError_WhenNoError_ReturnsFalse()
    {
        var vm = new ApplyChangesDialogViewModel();
        Assert.False(vm.ShowError);
    }

    [Fact]
    public void ShowError_WhenErrorSet_ReturnsTrue()
    {
        var vm = new ApplyChangesDialogViewModel
        {
            ErrorMessage = "Test error"
        };
        Assert.True(vm.ShowError);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsNewFile Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsNewFile_WhenTrue_AffectsDiffSummary()
    {
        var mockChangeService = new Mock<IFileChangeService>();
        var codeBlock = new CodeBlock { TargetFilePath = "/test/new.cs" };

        var vm = new ApplyChangesDialogViewModel(
            mockChangeService.Object,
            codeBlock,
            "/workspace",
            "",
            "new content\nline 2",
            isNewFile: true,
            closeAction: _ => { });

        Assert.True(vm.IsNewFile);
        Assert.Contains("new file", vm.DiffSummary);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CanApply/CanCancel Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CanApply_WhenNotApplying_ReturnsTrue()
    {
        var vm = new ApplyChangesDialogViewModel { IsApplying = false };
        Assert.True(vm.CanApply);
    }

    [Fact]
    public void CanApply_WhenApplying_ReturnsFalse()
    {
        var vm = new ApplyChangesDialogViewModel { IsApplying = true };
        Assert.False(vm.CanApply);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Cancel Command Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CancelCommand_InvokesCloseAction()
    {
        ApplyResult? closeResult = new ApplyResult { Success = true };
        var vm = new ApplyChangesDialogViewModel();
        vm.SetCloseAction(r => closeResult = r);

        vm.CancelCommand.Execute(null);

        Assert.Null(closeResult);
    }
}
