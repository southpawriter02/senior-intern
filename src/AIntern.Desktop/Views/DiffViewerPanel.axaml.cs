using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Microsoft.Extensions.Logging;
using AIntern.Desktop.ViewModels;

namespace AIntern.Desktop.Views;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ DIFF VIEWER PANEL CODE-BEHIND (v0.4.2e)                                  │
// │ Implements synchronized scrolling and hunk navigation.                   │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Side-by-side diff viewer panel with synchronized scrolling between panels.
/// </summary>
/// <remarks>
/// <para>Added in v0.4.2e.</para>
/// <para>
/// This code-behind handles:
/// - Bidirectional synchronized scrolling between Original and Proposed panels
/// - Hunk navigation via ViewModel events
/// - Proper cleanup when DataContext changes
/// </para>
/// </remarks>
public partial class DiffViewerPanel : UserControl
{
    private readonly ILogger<DiffViewerPanel>? _logger;

    /// <summary>
    /// Guard flag to prevent infinite scroll synchronization loops.
    /// </summary>
    /// <remarks>
    /// When Panel A scrolls, it updates Panel B. If we don't guard,
    /// Panel B's scroll change would update Panel A, creating an infinite loop.
    /// </remarks>
    private bool _isSyncingScroll;

    /// <summary>
    /// Reference to currently subscribed ViewModel for event cleanup.
    /// </summary>
    private DiffViewerViewModel? _subscribedViewModel;

    /// <summary>
    /// Initializes the DiffViewerPanel.
    /// </summary>
    public DiffViewerPanel()
    {
        InitializeComponent();

        _logger?.LogDebug("DiffViewerPanel initialized");
    }

    /// <summary>
    /// Called when the template is applied. Wires up scroll synchronization.
    /// </summary>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Wire up synchronized scrolling event handlers
        if (OriginalScrollViewer != null)
        {
            OriginalScrollViewer.ScrollChanged += OnOriginalScrollChanged;
            _logger?.LogTrace("Subscribed to OriginalScrollViewer.ScrollChanged");
        }

        if (ProposedScrollViewer != null)
        {
            ProposedScrollViewer.ScrollChanged += OnProposedScrollChanged;
            _logger?.LogTrace("Subscribed to ProposedScrollViewer.ScrollChanged");
        }
    }

    /// <summary>
    /// Subscribe to ViewModel events when DataContext changes.
    /// </summary>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Unsubscribe from previous ViewModel
        if (_subscribedViewModel != null)
        {
            _subscribedViewModel.HunkNavigationRequested -= OnHunkNavigationRequested;
            _logger?.LogTrace("Unsubscribed from previous ViewModel");
            _subscribedViewModel = null;
        }

        // Subscribe to new ViewModel
        if (DataContext is DiffViewerViewModel vm)
        {
            vm.HunkNavigationRequested += OnHunkNavigationRequested;
            _subscribedViewModel = vm;
            _logger?.LogTrace("Subscribed to DiffViewerViewModel.HunkNavigationRequested");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Synchronized Scrolling
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles scroll changes on the Original panel.
    /// Synchronizes the Proposed panel if sync is enabled.
    /// </summary>
    private void OnOriginalScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Guard against infinite loop
        if (_isSyncingScroll) return;

        // Check if synchronized scrolling is enabled
        if (DataContext is DiffViewerViewModel { SynchronizedScroll: true } vm)
        {
            _isSyncingScroll = true;
            try
            {
                // Copy scroll offset from Original to Proposed
                if (ProposedScrollViewer != null && OriginalScrollViewer != null)
                {
                    ProposedScrollViewer.Offset = OriginalScrollViewer.Offset;
                    _logger?.LogTrace("Synced scroll Original → Proposed: {Offset}",
                        OriginalScrollViewer.Offset);
                }
            }
            finally
            {
                _isSyncingScroll = false;
            }
        }
    }

    /// <summary>
    /// Handles scroll changes on the Proposed panel.
    /// Synchronizes the Original panel if sync is enabled.
    /// </summary>
    private void OnProposedScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Guard against infinite loop
        if (_isSyncingScroll) return;

        // Check if synchronized scrolling is enabled
        if (DataContext is DiffViewerViewModel { SynchronizedScroll: true } vm)
        {
            _isSyncingScroll = true;
            try
            {
                // Copy scroll offset from Proposed to Original
                if (OriginalScrollViewer != null && ProposedScrollViewer != null)
                {
                    OriginalScrollViewer.Offset = ProposedScrollViewer.Offset;
                    _logger?.LogTrace("Synced scroll Proposed → Original: {Offset}",
                        ProposedScrollViewer.Offset);
                }
            }
            finally
            {
                _isSyncingScroll = false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Hunk Navigation
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles hunk navigation events from the ViewModel.
    /// Scrolls to bring the specified hunk into view.
    /// </summary>
    private void OnHunkNavigationRequested(object? sender, int hunkIndex)
    {
        _logger?.LogDebug("Hunk navigation requested: index={Index}", hunkIndex);
        ScrollToHunk(hunkIndex);
    }

    /// <summary>
    /// Scrolls to bring the specified hunk into view.
    /// </summary>
    /// <param name="hunkIndex">0-based index of the hunk to scroll to.</param>
    private void ScrollToHunk(int hunkIndex)
    {
        // Get the ItemsControl containing the hunks
        if (OriginalScrollViewer?.Content is ItemsControl itemsControl)
        {
            // Find the container for the specified hunk index
            var container = itemsControl.ContainerFromIndex(hunkIndex);

            if (container is Control hunkControl)
            {
                // Scroll the control into view
                hunkControl.BringIntoView();
                _logger?.LogTrace("Scrolled hunk {Index} into view", hunkIndex);
            }
            else
            {
                _logger?.LogWarning("Could not find container for hunk index {Index}", hunkIndex);
            }
        }
    }
}
