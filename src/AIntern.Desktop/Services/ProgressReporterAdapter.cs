using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Services;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PROGRESS REPORTER ADAPTER (v0.4.4h)                                     │
// │ Adapts ApplyProgressViewModel to IProgress<BatchApplyProgress>.         │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Adapts the ApplyProgressViewModel to IProgress&lt;BatchApplyProgress&gt;.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.4h.</para>
/// </remarks>
public class ProgressReporterAdapter : IProgress<BatchApplyProgress>
{
    private readonly ApplyProgressViewModel _viewModel;

    /// <summary>
    /// Create a new progress reporter adapter.
    /// </summary>
    /// <param name="viewModel">The ViewModel to update.</param>
    public ProgressReporterAdapter(ApplyProgressViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    /// <inheritdoc/>
    public void Report(BatchApplyProgress value)
    {
        _viewModel.Update(value);
    }
}
