using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Moq;
using Xunit;

namespace AIntern.Desktop.Tests.ViewModels;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ SNIPPET APPLY OPTIONS VIEWMODEL TESTS (v0.4.5e)                          │
// └─────────────────────────────────────────────────────────────────────────┘

public class SnippetApplyOptionsViewModelTests
{
    private readonly Mock<ISnippetApplyService> _mockSnippetApplyService;
    private readonly Mock<IDiffService> _mockDiffService;
    private readonly SnippetApplyOptionsViewModel _viewModel;

    public SnippetApplyOptionsViewModelTests()
    {
        _mockSnippetApplyService = new Mock<ISnippetApplyService>();
        _mockDiffService = new Mock<IDiffService>();

        // Default setup for services
        _mockSnippetApplyService
            .Setup(s => s.SuggestLocationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SnippetLocationSuggestion?)null);
        
        _mockSnippetApplyService
            .Setup(s => s.DetectIndentationAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IndentationStyle.Default);

        _mockSnippetApplyService
            .Setup(s => s.PreviewSnippetAsync(
                It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<SnippetApplyOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SnippetApplyPreview());

        _viewModel = new SnippetApplyOptionsViewModel(
            _mockSnippetApplyService.Object,
            _mockDiffService.Object);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_InitializesDefaults()
    {
        Assert.Equal(SnippetInsertMode.ReplaceFile, _viewModel.InsertMode);
        Assert.Equal(1, _viewModel.TargetLine);
        Assert.Equal(1, _viewModel.StartLine);
        Assert.True(_viewModel.PreserveIndentation);
        Assert.False(_viewModel.AddBlankLineBefore);
        Assert.False(_viewModel.AddBlankLineAfter);
    }

    [Fact]
    public void Constructor_ThrowsOnNullSnippetService()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SnippetApplyOptionsViewModel(null!, _mockDiffService.Object));
    }

    [Fact]
    public void Constructor_ThrowsOnNullDiffService()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SnippetApplyOptionsViewModel(_mockSnippetApplyService.Object, null!));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Insert Mode Property Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsReplaceMode_TrueWhenReplaceLinesSelected()
    {
        _viewModel.InsertMode = SnippetInsertMode.Replace;
        Assert.True(_viewModel.IsReplaceMode);
    }

    [Fact]
    public void IsReplaceMode_FalseWhenOtherModeSelected()
    {
        _viewModel.InsertMode = SnippetInsertMode.Append;
        Assert.False(_viewModel.IsReplaceMode);
    }

    [Fact]
    public void IsInsertMode_TrueWhenInsertBeforeSelected()
    {
        _viewModel.InsertMode = SnippetInsertMode.InsertBefore;
        Assert.True(_viewModel.IsInsertMode);
    }

    [Fact]
    public void IsInsertMode_TrueWhenInsertAfterSelected()
    {
        _viewModel.InsertMode = SnippetInsertMode.InsertAfter;
        Assert.True(_viewModel.IsInsertMode);
    }

    [Fact]
    public void ShowTargetLine_FollowsIsInsertMode()
    {
        _viewModel.InsertMode = SnippetInsertMode.InsertAfter;
        Assert.True(_viewModel.ShowTargetLine);

        _viewModel.InsertMode = SnippetInsertMode.Append;
        Assert.False(_viewModel.ShowTargetLine);
    }

    [Fact]
    public void ShowRangeInputs_FollowsIsReplaceMode()
    {
        _viewModel.InsertMode = SnippetInsertMode.Replace;
        Assert.True(_viewModel.ShowRangeInputs);

        _viewModel.InsertMode = SnippetInsertMode.ReplaceFile;
        Assert.False(_viewModel.ShowRangeInputs);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Mode Boolean Properties Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsReplaceFileMode_SetsInsertModeWhenTrue()
    {
        _viewModel.InsertMode = SnippetInsertMode.Append;
        _viewModel.IsReplaceFileMode = true;
        Assert.Equal(SnippetInsertMode.ReplaceFile, _viewModel.InsertMode);
        Assert.True(_viewModel.IsReplaceFileMode);
    }

    [Fact]
    public void IsReplaceRangeMode_SetsInsertModeWhenTrue()
    {
        _viewModel.IsReplaceRangeMode = true;
        Assert.Equal(SnippetInsertMode.Replace, _viewModel.InsertMode);
        Assert.True(_viewModel.IsReplaceRangeMode);
    }

    [Fact]
    public void IsInsertAfterMode_SetsInsertModeWhenTrue()
    {
        _viewModel.IsInsertAfterMode = true;
        Assert.Equal(SnippetInsertMode.InsertAfter, _viewModel.InsertMode);
        Assert.True(_viewModel.IsInsertAfterMode);
    }

    [Fact]
    public void IsAppendMode_SetsInsertModeWhenTrue()
    {
        _viewModel.IsAppendMode = true;
        Assert.Equal(SnippetInsertMode.Append, _viewModel.InsertMode);
        Assert.True(_viewModel.IsAppendMode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Line Range Validation Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void EndLine_AdjustedWhenStartLineIncreasesBeyondIt()
    {
        _viewModel.StartLine = 5;
        _viewModel.EndLine = 10;

        _viewModel.StartLine = 15;

        Assert.True(_viewModel.EndLine >= _viewModel.StartLine);
    }

    [Fact]
    public void StartLine_AdjustedWhenEndLineDecreasesBelowIt()
    {
        _viewModel.StartLine = 10;
        _viewModel.EndLine = 20;

        _viewModel.EndLine = 5;

        Assert.True(_viewModel.StartLine <= _viewModel.EndLine);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Suggestion Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HasSuggestion_FalseWhenNoSuggestion()
    {
        Assert.False(_viewModel.HasSuggestion);
    }

    [Fact]
    public void SuggestionConfidenceLevel_ReturnsHigh()
    {
        var suggestion = new SnippetLocationSuggestion { Confidence = 0.9 };
        
        _mockSnippetApplyService
            .Setup(s => s.SuggestLocationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestion);

        // Simulate setting suggestion
        typeof(SnippetApplyOptionsViewModel)
            .GetProperty(nameof(SnippetApplyOptionsViewModel.Suggestion))!
            .SetValue(_viewModel, suggestion);

        Assert.Equal("High", _viewModel.SuggestionConfidenceLevel);
    }

    [Fact]
    public void SuggestionConfidenceLevel_ReturnsMedium()
    {
        var suggestion = new SnippetLocationSuggestion { Confidence = 0.6 };
        typeof(SnippetApplyOptionsViewModel)
            .GetProperty(nameof(SnippetApplyOptionsViewModel.Suggestion))!
            .SetValue(_viewModel, suggestion);

        Assert.Equal("Medium", _viewModel.SuggestionConfidenceLevel);
    }

    [Fact]
    public void SuggestionConfidenceLevel_ReturnsLow()
    {
        var suggestion = new SnippetLocationSuggestion { Confidence = 0.3 };
        typeof(SnippetApplyOptionsViewModel)
            .GetProperty(nameof(SnippetApplyOptionsViewModel.Suggestion))!
            .SetValue(_viewModel, suggestion);

        Assert.Equal("Low", _viewModel.SuggestionConfidenceLevel);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Preview Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void HasPreview_FalseWhenNoPreview()
    {
        Assert.False(_viewModel.HasPreview);
    }

    [Fact]
    public void CanApply_RequiresPreviewAndNoError()
    {
        // No preview, can't apply
        Assert.False(_viewModel.CanApply);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Command Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void CancelCommand_RaisesRequestClose()
    {
        SnippetApplyResult? result = new() { IsSuccess = true };
        _viewModel.RequestClose += (s, r) => result = r;

        _viewModel.CancelCommand.Execute(null);

        Assert.Null(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Property Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void FileName_ExtractedFromFilePath()
    {
        typeof(SnippetApplyOptionsViewModel)
            .GetProperty(nameof(SnippetApplyOptionsViewModel.FilePath))!
            .SetValue(_viewModel, "/path/to/file.cs");

        Assert.Equal("file.cs", _viewModel.FileName);
    }

    [Fact]
    public void IndentationDescription_ShowsUnknownWhenNull()
    {
        Assert.Equal("Unknown", _viewModel.IndentationDescription);
    }

    [Fact]
    public void IndentationDescription_ShowsDetectedStyle()
    {
        typeof(SnippetApplyOptionsViewModel)
            .GetProperty(nameof(SnippetApplyOptionsViewModel.DetectedIndentation))!
            .SetValue(_viewModel, IndentationStyle.TwoSpaces);

        Assert.Equal("2 spaces", _viewModel.IndentationDescription);
    }

    [Fact]
    public void InsertModeDescription_UpdatesWithMode()
    {
        _viewModel.InsertMode = SnippetInsertMode.Append;
        Assert.Equal("Add to end of file", _viewModel.InsertModeDescription);

        _viewModel.InsertMode = SnippetInsertMode.Prepend;
        Assert.Equal("Add to beginning of file", _viewModel.InsertModeDescription);
    }

    [Fact]
    public void AvailableInsertModes_HasAllModes()
    {
        Assert.Equal(6, _viewModel.AvailableInsertModes.Count);
        Assert.Contains(_viewModel.AvailableInsertModes, m => m.Mode == SnippetInsertMode.ReplaceFile);
        Assert.Contains(_viewModel.AvailableInsertModes, m => m.Mode == SnippetInsertMode.Replace);
        Assert.Contains(_viewModel.AvailableInsertModes, m => m.Mode == SnippetInsertMode.InsertBefore);
        Assert.Contains(_viewModel.AvailableInsertModes, m => m.Mode == SnippetInsertMode.InsertAfter);
        Assert.Contains(_viewModel.AvailableInsertModes, m => m.Mode == SnippetInsertMode.Append);
        Assert.Contains(_viewModel.AvailableInsertModes, m => m.Mode == SnippetInsertMode.Prepend);
    }
}
