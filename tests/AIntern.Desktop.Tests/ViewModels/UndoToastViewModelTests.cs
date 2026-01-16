using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

/// <summary>
/// Unit tests for v0.4.3h UndoToastViewModel.
/// </summary>
public class UndoToastViewModelTests : IDisposable
{
    private readonly UndoToastViewModel _vm;

    public UndoToastViewModelTests()
    {
        _vm = new UndoToastViewModel();
    }

    public void Dispose()
    {
        _vm.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_Default_InitializesCorrectly()
    {
        Assert.False(_vm.IsVisible);
        Assert.False(_vm.IsUndoing);
        Assert.Equal(string.Empty, _vm.Message);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FormattedTimeRemaining Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FormattedTimeRemaining_Zero_ReturnsZero()
    {
        _vm.TimeRemaining = TimeSpan.Zero;
        Assert.Equal("0:00", _vm.FormattedTimeRemaining);
    }

    [Fact]
    public void FormattedTimeRemaining_Under60Seconds_FormatsCorrectly()
    {
        _vm.TimeRemaining = TimeSpan.FromSeconds(45);
        Assert.Equal("0:45", _vm.FormattedTimeRemaining);
    }

    [Fact]
    public void FormattedTimeRemaining_Over1Minute_FormatsCorrectly()
    {
        _vm.TimeRemaining = TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(30));
        Assert.Equal("2:30", _vm.FormattedTimeRemaining);
    }

    [Fact]
    public void FormattedTimeRemaining_Over1Hour_IncludesHours()
    {
        _vm.TimeRemaining = TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(15));
        Assert.Contains("1", _vm.FormattedTimeRemaining);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsExpiringSoon Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsExpiringSoon_Under30Seconds_ReturnsTrue()
    {
        _vm.TimeRemaining = TimeSpan.FromSeconds(25);
        Assert.True(_vm.IsExpiringSoon);
    }

    [Fact]
    public void IsExpiringSoon_Over30Seconds_ReturnsFalse()
    {
        _vm.TimeRemaining = TimeSpan.FromSeconds(60);
        Assert.False(_vm.IsExpiringSoon);
    }

    [Fact]
    public void IsExpiringSoon_Zero_ReturnsFalse()
    {
        _vm.TimeRemaining = TimeSpan.Zero;
        Assert.False(_vm.IsExpiringSoon);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ProgressPercentage Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ProgressPercentage_FullTime_Returns100()
    {
        _vm.TotalUndoWindow = TimeSpan.FromMinutes(3);
        _vm.TimeRemaining = TimeSpan.FromMinutes(3);
        Assert.Equal(100, _vm.ProgressPercentage, 1);
    }

    [Fact]
    public void ProgressPercentage_HalfTime_Returns50()
    {
        _vm.TotalUndoWindow = TimeSpan.FromMinutes(2);
        _vm.TimeRemaining = TimeSpan.FromMinutes(1);
        Assert.Equal(50, _vm.ProgressPercentage, 1);
    }

    [Fact]
    public void ProgressPercentage_Expired_Returns0()
    {
        _vm.TotalUndoWindow = TimeSpan.FromMinutes(3);
        _vm.TimeRemaining = TimeSpan.Zero;
        Assert.Equal(0, _vm.ProgressPercentage);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Show/Hide Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Show_SetsIsVisibleTrue()
    {
        _vm.Show("/test/file.cs", FileChangeType.Modified, TimeSpan.FromMinutes(3));
        Assert.True(_vm.IsVisible);
    }

    [Fact]
    public void Show_SetsFileInfo()
    {
        _vm.Show("/test/file.cs", FileChangeType.Created, TimeSpan.FromMinutes(3));
        
        Assert.Equal("/test/file.cs", _vm.FilePath);
        Assert.Equal("file.cs", _vm.FileName);
        Assert.Equal(FileChangeType.Created, _vm.ChangeType);
    }

    [Fact]
    public void Hide_SetsIsVisibleFalse()
    {
        _vm.Show("/test/file.cs", FileChangeType.Modified, TimeSpan.FromMinutes(3));
        _vm.Hide();
        Assert.False(_vm.IsVisible);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CanUndo Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CanUndo_WhenTimeRemainingAndNotUndoing_ReturnsTrue()
    {
        _vm.TimeRemaining = TimeSpan.FromMinutes(1);
        _vm.IsUndoing = false;
        Assert.True(_vm.CanUndo);
    }

    [Fact]
    public void CanUndo_WhenNoTimeRemaining_ReturnsFalse()
    {
        _vm.TimeRemaining = TimeSpan.Zero;
        _vm.IsUndoing = false;
        Assert.False(_vm.CanUndo);
    }

    [Fact]
    public void CanUndo_WhenUndoing_ReturnsFalse()
    {
        _vm.TimeRemaining = TimeSpan.FromMinutes(1);
        _vm.IsUndoing = true;
        Assert.False(_vm.CanUndo);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dispose Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var vm = new UndoToastViewModel();
        vm.Dispose();
        vm.Dispose(); // Should not throw
    }
}
