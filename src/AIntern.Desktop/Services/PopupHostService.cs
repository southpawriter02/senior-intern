// -----------------------------------------------------------------------
// <copyright file="PopupHostService.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
// </copyright>
// <summary>
//     Service for showing and managing popup dialogs.
//     Added in v0.4.5e.
// </summary>
// -----------------------------------------------------------------------

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using AIntern.Desktop.Views;
using Microsoft.Extensions.Logging;

namespace AIntern.Desktop.Services;

/// <summary>
/// Service for showing and managing popup dialogs.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5e.</para>
/// </remarks>
public interface IPopupHostService
{
    /// <summary>
    /// Shows the snippet apply options popup and returns the result.
    /// </summary>
    /// <param name="anchor">Control to anchor the popup to.</param>
    /// <param name="filePath">Target file path.</param>
    /// <param name="snippetContent">Snippet content to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Apply result, or null if cancelled.</returns>
    Task<SnippetApplyResult?> ShowSnippetApplyOptionsAsync(
        Control anchor,
        string filePath,
        string snippetContent,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of <see cref="IPopupHostService"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.5e.</para>
/// </remarks>
public class PopupHostService : IPopupHostService
{
    private readonly ISnippetApplyService _snippetApplyService;
    private readonly IDiffService _diffService;
    private readonly ILogger<PopupHostService>? _logger;

    /// <summary>
    /// Creates a new instance of <see cref="PopupHostService"/>.
    /// </summary>
    public PopupHostService(
        ISnippetApplyService snippetApplyService,
        IDiffService diffService,
        ILogger<PopupHostService>? logger = null)
    {
        _snippetApplyService = snippetApplyService;
        _diffService = diffService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SnippetApplyResult?> ShowSnippetApplyOptionsAsync(
        Control anchor,
        string filePath,
        string snippetContent,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug(
            "Showing snippet apply options popup for: {FilePath}",
            filePath);

        var tcs = new TaskCompletionSource<SnippetApplyResult?>();

        // Create ViewModel
        var viewModel = new SnippetApplyOptionsViewModel(
            _snippetApplyService,
            _diffService);

        // Create View
        var popup = new Popup
        {
            PlacementTarget = anchor,
            Placement = PlacementMode.Bottom,
            IsLightDismissEnabled = true,
            Child = new SnippetApplyOptionsPopup
            {
                DataContext = viewModel
            }
        };

        // Handle close from ViewModel
        viewModel.RequestClose += (s, result) =>
        {
            _logger?.LogDebug("Popup closed with result: {HasResult}", result is not null);
            popup.IsOpen = false;
            tcs.TrySetResult(result);
        };

        // Handle light dismiss
        popup.Closed += (s, e) =>
        {
            _logger?.LogDebug("Popup dismissed");
            tcs.TrySetResult(null);
        };

        // Handle cancellation
        cancellationToken.Register(() =>
        {
            popup.IsOpen = false;
            tcs.TrySetCanceled();
        });

        // Initialize and show
        try
        {
            await viewModel.InitializeAsync(filePath, snippetContent, cancellationToken);
            popup.IsOpen = true;
            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            popup.IsOpen = false;
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show snippet apply options popup");
            popup.IsOpen = false;
            throw;
        }
    }
}
