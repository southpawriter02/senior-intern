namespace AIntern.Desktop.Controls.Terminal;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using AIntern.Core.Models.Terminal;
using Microsoft.Extensions.Logging;
using SkiaSharp;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalRenderer (v0.5.2b)                                               │
// │ SkiaSharp-based Avalonia control for hardware-accelerated terminal      │
// │ rendering with theme, selection, and cursor support.                     │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// SkiaSharp-based terminal buffer renderer.
/// </summary>
/// <remarks>
/// <para>
/// Renders terminal cells, text attributes, cursor, and selection highlighting
/// using hardware-accelerated graphics through the Avalonia SkiaSharp backend.
/// Key features:
/// <list type="bullet">
///   <item><description>Full TerminalBuffer rendering with scrollback support</description></item>
///   <item><description>Text attributes: bold, dim, underline, strikethrough, inverse</description></item>
///   <item><description>256-color palette and true color (RGB) support via TerminalTheme</description></item>
///   <item><description>Cursor rendering with Block, Underline, and Bar styles</description></item>
///   <item><description>Text selection highlighting with configurable opacity</description></item>
///   <item><description>Wide character (CJK, emoji) support</description></item>
/// </list>
/// </para>
/// <para>
/// Rendering Pipeline:
/// <code>
/// Buffer.ContentChanged → InvalidateVisual → Render → TerminalRenderOperation → SkiaSharp
/// </code>
/// </para>
/// <para>Added in v0.5.2b.</para>
/// </remarks>
public class TerminalRenderer : Control
{
    #region Private Fields

    /// <summary>
    /// The terminal buffer to render.
    /// </summary>
    private TerminalBuffer? _buffer;

    /// <summary>
    /// The color theme for rendering.
    /// </summary>
    private TerminalTheme _theme = TerminalTheme.Dark;

    /// <summary>
    /// Font metrics calculator for text measurement and coordinate conversion.
    /// </summary>
    private readonly TerminalFontMetrics _fontMetrics;

    /// <summary>
    /// Current text selection (null if no selection).
    /// </summary>
    private TerminalSelection? _selection;

    /// <summary>
    /// Whether the cursor should currently be visible (for blink animation).
    /// </summary>
    private bool _cursorVisible = true;

    /// <summary>
    /// Optional logger for diagnostic output.
    /// </summary>
    private readonly ILogger? _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // Search Highlighting Fields (v0.5.5c)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Current search state for highlighting results.
    /// </summary>
    /// <remarks>Added in v0.5.5c for terminal search UI.</remarks>
    private TerminalSearchState? _searchState;

    /// <summary>
    /// Visual style for search result highlighting.
    /// </summary>
    /// <remarks>Added in v0.5.5c for terminal search UI.</remarks>
    private SearchHighlightStyle _highlightStyle = SearchHighlightStyle.Default;

    #endregion

    #region Styled Properties

    /// <summary>
    /// Styled property for the terminal font family.
    /// </summary>
    public static readonly StyledProperty<string> FontFamilyProperty =
        AvaloniaProperty.Register<TerminalRenderer, string>(
            nameof(FontFamily),
            defaultValue: "Cascadia Mono");

    /// <summary>
    /// Styled property for the terminal font size.
    /// </summary>
    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<TerminalRenderer, double>(
            nameof(FontSize),
            defaultValue: 14.0);

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets or sets the font family for terminal text.
    /// </summary>
    /// <remarks>
    /// If the specified font is not available, falls back to common monospace fonts
    /// (Consolas, Monaco, Courier New) or the system default.
    /// </remarks>
    public string FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size for terminal text in points.
    /// </summary>
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// Gets the font metrics for calculating terminal dimensions.
    /// </summary>
    /// <remarks>
    /// Used by parent controls to determine terminal size in cells
    /// based on available pixel dimensions.
    /// </remarks>
    public TerminalFontMetrics Metrics => _fontMetrics;

    /// <summary>
    /// Gets or sets the current text selection.
    /// </summary>
    /// <remarks>
    /// Setting this property invalidates the visual and triggers a re-render.
    /// Set to null to clear the selection.
    /// </remarks>
    public TerminalSelection? Selection
    {
        get => _selection;
        set
        {
            if (_selection != value)
            {
                _selection = value;
                _logger?.LogDebug(
                    "[TerminalRenderer] Selection changed: {Selection}",
                    value != null ? $"({value.StartLine},{value.StartColumn})-({value.EndLine},{value.EndColumn})" : "null");
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the cursor should be visible.
    /// </summary>
    /// <remarks>
    /// Used for cursor blink animation. This property controls the blink state,
    /// while the buffer's CursorVisible property controls whether the cursor
    /// should be shown at all.
    /// </remarks>
    public bool CursorVisible
    {
        get => _cursorVisible;
        set
        {
            if (_cursorVisible != value)
            {
                _cursorVisible = value;
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the terminal has input focus.
    /// </summary>
    /// <remarks>
    /// Checks if this control is the focused element.
    /// Affects cursor rendering (solid vs outline).
    /// </remarks>
    public bool HasTerminalFocus
    {
        get
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var focusedElement = topLevel?.FocusManager?.GetFocusedElement();

            // Check if this control is focused
            if (focusedElement == this)
                return true;

            // Check if the focused element is a descendant of this control
            if (focusedElement is Visual visual)
            {
                // Walk up from focused element looking for this renderer
                return visual.FindAncestorOfType<TerminalRenderer>() == this;
            }

            return false;
        }
    }

    #endregion

    #region Static Constructor

    /// <summary>
    /// Static constructor for TerminalRenderer.
    /// </summary>
    static TerminalRenderer()
    {
        // Register properties that affect rendering
        AffectsRender<TerminalRenderer>(FontFamilyProperty, FontSizeProperty);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalRenderer"/> control.
    /// </summary>
    public TerminalRenderer() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalRenderer"/> control
    /// with optional logging support.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public TerminalRenderer(ILogger? logger)
    {
        _logger = logger;
        _fontMetrics = new TerminalFontMetrics(logger);

        // Clip rendering to control bounds
        ClipToBounds = true;

        // Make the control focusable for keyboard input
        Focusable = true;

        _logger?.LogDebug("[TerminalRenderer] Created new instance");
    }

    #endregion

    #region Lifecycle Overrides

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // ─────────────────────────────────────────────────────────────────
        // Update font metrics when font properties change
        // ─────────────────────────────────────────────────────────────────
        if (change.Property == FontFamilyProperty || change.Property == FontSizeProperty)
        {
            _fontMetrics.Update(FontFamily, (float)FontSize);
            _logger?.LogDebug(
                "[TerminalRenderer] Font changed: family={FontFamily}, size={FontSize}",
                FontFamily, FontSize);
            InvalidateVisual();
        }
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // ─────────────────────────────────────────────────────────────────
        // Initialize font metrics on first load
        // ─────────────────────────────────────────────────────────────────
        _fontMetrics.Update(FontFamily, (float)FontSize);
        _logger?.LogDebug("[TerminalRenderer] Loaded and font metrics initialized");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the terminal buffer to render.
    /// </summary>
    /// <param name="buffer">The terminal buffer to render.</param>
    /// <remarks>
    /// Subscribes to the buffer's <see cref="TerminalBuffer.ContentChanged"/> event
    /// for automatic re-rendering when content changes. The previous buffer's
    /// subscription is automatically removed.
    /// </remarks>
    public void SetBuffer(TerminalBuffer buffer)
    {
        // ─────────────────────────────────────────────────────────────────
        // Unsubscribe from previous buffer if any
        // ─────────────────────────────────────────────────────────────────
        if (_buffer != null)
        {
            _buffer.ContentChanged -= OnBufferContentChanged;
            _logger?.LogDebug("[TerminalRenderer] Unsubscribed from previous buffer");
        }

        _buffer = buffer;

        // ─────────────────────────────────────────────────────────────────
        // Subscribe to new buffer's content changed event
        // ─────────────────────────────────────────────────────────────────
        if (_buffer != null)
        {
            _buffer.ContentChanged += OnBufferContentChanged;
            _logger?.LogDebug(
                "[TerminalRenderer] Buffer set: {Cols}×{Rows} cells",
                _buffer.Columns, _buffer.Rows);
        }

        InvalidateVisual();
    }

    /// <summary>
    /// Sets the color theme for rendering.
    /// </summary>
    /// <param name="theme">The terminal theme to use.</param>
    public void SetTheme(TerminalTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        _theme = theme;
        _logger?.LogDebug("[TerminalRenderer] Theme changed: {ThemeName}", theme.Name);
        InvalidateVisual();
    }

    /// <summary>
    /// Forces a redraw of the terminal content.
    /// </summary>
    public void Refresh()
    {
        InvalidateVisual();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Search Highlighting Methods (v0.5.5c)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets the current search state for highlighting results.
    /// </summary>
    /// <param name="state">The search state containing results to highlight, or null to clear.</param>
    /// <remarks>
    /// <para>
    /// When a search state is set, all matching results are highlighted
    /// with the configured <see cref="SearchHighlightStyle"/>. The current
    /// result is rendered with a distinct style (typically a border).
    /// </para>
    /// <para>Added in v0.5.5c.</para>
    /// </remarks>
    public void SetSearchState(TerminalSearchState? state)
    {
        _searchState = state;
        _logger?.LogDebug(
            "[TerminalRenderer] Search state changed: {HasResults} results, Current={Index}",
            state?.Results.Count ?? 0,
            state?.CurrentResultIndex ?? -1);
        InvalidateVisual();
    }

    /// <summary>
    /// Sets the highlight style for search results.
    /// </summary>
    /// <param name="style">The style to use for highlighting.</param>
    /// <remarks>Added in v0.5.5c.</remarks>
    public void SetHighlightStyle(SearchHighlightStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        _highlightStyle = style;
        _logger?.LogDebug("[TerminalRenderer] Highlight style set");
        InvalidateVisual();
    }

    /// <summary>
    /// Scrolls the terminal viewport to ensure the given search result is visible.
    /// </summary>
    /// <param name="result">The search result to scroll to.</param>
    /// <remarks>
    /// <para>
    /// If the result is already visible within the current viewport, no scroll
    /// occurs. Otherwise, the viewport is scrolled to center the result line.
    /// </para>
    /// <para>Added in v0.5.5c.</para>
    /// </remarks>
    public void ScrollToResult(TerminalSearchResult result)
    {
        if (result == null || _buffer == null)
            return;

        // Calculate current viewport bounds
        var viewportStart = _buffer.TotalLines - _buffer.Rows - _buffer.ScrollOffset;
        var viewportEnd = viewportStart + _buffer.Rows;

        // Check if already visible
        if (result.LineIndex >= viewportStart && result.LineIndex < viewportEnd)
        {
            // Already visible, just redraw to update highlight
            _logger?.LogDebug(
                "[TerminalRenderer] Result at line {Line} already visible",
                result.LineIndex);
            InvalidateVisual();
            return;
        }

        // Calculate scroll offset to center the result
        var targetLine = result.LineIndex - (_buffer.Rows / 2);
        var maxScroll = Math.Max(0, _buffer.TotalLines - _buffer.Rows);
        var newOffset = Math.Max(0, Math.Min(maxScroll - targetLine, maxScroll));

        _logger?.LogDebug(
            "[TerminalRenderer] Scrolling to result: Line={Line}, NewOffset={Offset}",
            result.LineIndex,
            newOffset);

        _buffer.ScrollOffset = newOffset;
        InvalidateVisual();
    }

    /// <summary>
    /// Gets the current search state.
    /// </summary>
    /// <remarks>Added in v0.5.5c.</remarks>
    public TerminalSearchState? SearchState => _searchState;

    #endregion

    #region Rendering

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // ─────────────────────────────────────────────────────────────────
        // Skip rendering if buffer or metrics are invalid
        // ─────────────────────────────────────────────────────────────────
        if (_buffer == null || !_fontMetrics.IsValid)
        {
            _logger?.LogDebug(
                "[TerminalRenderer] Skipping render: buffer={HasBuffer}, valid={IsValid}",
                _buffer != null, _fontMetrics.IsValid);
            return;
        }

        // ─────────────────────────────────────────────────────────────────
        // Create and dispatch custom drawing operation for SkiaSharp
        // ─────────────────────────────────────────────────────────────────
        var renderOperation = new TerminalRenderOperation(
            bounds: new Rect(0, 0, Bounds.Width, Bounds.Height),
            buffer: _buffer,
            theme: _theme,
            metrics: _fontMetrics,
            selection: _selection,
            showCursor: _cursorVisible && _buffer.CursorVisible,
            isFocused: HasTerminalFocus,
            searchState: _searchState,
            highlightStyle: _highlightStyle);

        context.Custom(renderOperation);
    }

    /// <summary>
    /// Handles buffer content changes by marshaling to the UI thread and invalidating.
    /// </summary>
    private void OnBufferContentChanged(object? sender, EventArgs e)
    {
        // ─────────────────────────────────────────────────────────────────
        // Marshal to UI thread since buffer changes may come from PTY thread
        // ─────────────────────────────────────────────────────────────────
        Avalonia.Threading.Dispatcher.UIThread.Post(InvalidateVisual);
    }

    #endregion

    #region Nested Class: TerminalRenderOperation

    /// <summary>
    /// Custom drawing operation for SkiaSharp rendering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implements <see cref="ICustomDrawOperation"/> to access the SkiaSharp canvas
    /// directly for hardware-accelerated rendering. The operation captures all
    /// necessary state at construction time for thread-safe rendering.
    /// </para>
    /// <para>
    /// Rendering steps:
    /// <list type="number">
    ///   <item><description>Clear canvas with theme background</description></item>
    ///   <item><description>Render visible lines from buffer</description></item>
    ///   <item><description>For each cell: background, character, decorations</description></item>
    ///   <item><description>Draw cursor if visible</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private sealed class TerminalRenderOperation : ICustomDrawOperation
    {
        #region Fields

        /// <summary>
        /// Bounding rectangle for the render operation.
        /// </summary>
        private readonly Rect _bounds;

        /// <summary>
        /// The terminal buffer containing lines and cells to render.
        /// </summary>
        private readonly TerminalBuffer _buffer;

        /// <summary>
        /// Color theme for resolving colors.
        /// </summary>
        private readonly TerminalTheme _theme;

        /// <summary>
        /// Font metrics for positioning.
        /// </summary>
        private readonly TerminalFontMetrics _metrics;

        /// <summary>
        /// Text selection for highlighting (null if no selection).
        /// </summary>
        private readonly TerminalSelection? _selection;

        /// <summary>
        /// Whether to draw the cursor.
        /// </summary>
        private readonly bool _showCursor;

        /// <summary>
        /// Whether the terminal has focus (affects cursor style).
        /// </summary>
        private readonly bool _isFocused;

        // ─────────────────────────────────────────────────────────────────────
        // Search Highlighting Fields (v0.5.5c)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Search state for result highlighting (null if no search).
        /// </summary>
        /// <remarks>Added in v0.5.5c.</remarks>
        private readonly TerminalSearchState? _searchState;

        /// <summary>
        /// Visual style for search highlights.
        /// </summary>
        /// <remarks>Added in v0.5.5c.</remarks>
        private readonly SearchHighlightStyle _highlightStyle;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new terminal render operation.
        /// </summary>
        public TerminalRenderOperation(
            Rect bounds,
            TerminalBuffer buffer,
            TerminalTheme theme,
            TerminalFontMetrics metrics,
            TerminalSelection? selection,
            bool showCursor,
            bool isFocused,
            TerminalSearchState? searchState,
            SearchHighlightStyle highlightStyle)
        {
            _bounds = bounds;
            _buffer = buffer;
            _theme = theme;
            _metrics = metrics;
            _selection = selection;
            _showCursor = showCursor;
            _isFocused = isFocused;
            _searchState = searchState;
            _highlightStyle = highlightStyle;
        }

        #endregion

        #region ICustomDrawOperation Implementation

        /// <inheritdoc />
        public Rect Bounds => _bounds;

        /// <inheritdoc />
        public bool HitTest(Point p) => _bounds.Contains(p);

        /// <inheritdoc />
        /// <remarks>
        /// Always returns false to force re-rendering on every frame.
        /// Terminal content changes frequently and we don't track dirty regions.
        /// </remarks>
        public bool Equals(ICustomDrawOperation? other) => false;

        /// <inheritdoc />
        public void Dispose()
        {
            // No resources to dispose - all objects are borrowed references
        }

        /// <inheritdoc />
        public void Render(ImmediateDrawingContext context)
        {
            // ─────────────────────────────────────────────────────────────
            // Get SkiaSharp canvas via API lease
            // The TryGetFeature method returns object in Avalonia 11
            // ─────────────────────────────────────────────────────────────
            var leaseFeature = context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) as ISkiaSharpApiLeaseFeature;
            if (leaseFeature == null)
                return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            RenderTerminal(canvas);
        }

        #endregion

        #region Rendering Implementation

        /// <summary>
        /// Renders the terminal content to the SkiaSharp canvas.
        /// </summary>
        private void RenderTerminal(SKCanvas canvas)
        {
            // ─────────────────────────────────────────────────────────────
            // Step 1: Clear canvas with background color
            // ─────────────────────────────────────────────────────────────
            var bgColor = ToSKColor(_theme.Background);
            canvas.Clear(bgColor);

            // Get rendering dimensions
            var charWidth = _metrics.CharWidth;
            var lineHeight = _metrics.LineHeight;
            var baseline = _metrics.Baseline;

            // ─────────────────────────────────────────────────────────────
            // Step 2: Create reusable paint objects
            // ─────────────────────────────────────────────────────────────
            using var textPaint = new SKPaint
            {
                Typeface = _metrics.Typeface,
                TextSize = _metrics.FontSize,
                IsAntialias = true
            };

            using var bgPaint = new SKPaint { Style = SKPaintStyle.Fill };
            using var linePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            };

            // ─────────────────────────────────────────────────────────────
            // Step 3: Get visible lines and render each
            // ─────────────────────────────────────────────────────────────
            var lines = _buffer.GetVisibleLines();
            float y = 0;

            // Calculate the absolute line index for selection checking
            var lineIndex = _buffer.TotalLines - _buffer.Rows - _buffer.ScrollOffset;

            foreach (var line in lines)
            {
                RenderLine(canvas, line, y, lineIndex, charWidth, lineHeight, baseline,
                    textPaint, bgPaint, linePaint);

                y += lineHeight;
                lineIndex++;
            }

            // ─────────────────────────────────────────────────────────────
            // Step 4: Draw search result highlights (v0.5.5c)
            // ─────────────────────────────────────────────────────────────
            RenderSearchHighlights(canvas, charWidth, lineHeight);

            // ─────────────────────────────────────────────────────────────
            // Step 5: Draw cursor if visible and not scrolled back
            // ─────────────────────────────────────────────────────────────
            if (_showCursor && _buffer.ScrollOffset == 0)
            {
                DrawCursor(canvas, charWidth, lineHeight, baseline);
            }
        }

        /// <summary>
        /// Renders a single line of terminal cells.
        /// </summary>
        private void RenderLine(
            SKCanvas canvas,
            TerminalLine line,
            float y,
            int lineIndex,
            float charWidth,
            float lineHeight,
            float baseline,
            SKPaint textPaint,
            SKPaint bgPaint,
            SKPaint linePaint)
        {
            float x = 0;
            var cells = line.ReadOnlyCells;

            for (int col = 0; col < cells.Length && col < _buffer.Columns; col++)
            {
                ref readonly var cell = ref cells[col];

                // ─────────────────────────────────────────────────────────
                // Skip continuation cells (second cell of wide characters)
                // ─────────────────────────────────────────────────────────
                if (cell.IsContinuation)
                {
                    x += charWidth;
                    continue;
                }

                var attrs = cell.Attributes;
                var isSelected = _selection?.Contains(lineIndex, col) ?? false;

                // ─────────────────────────────────────────────────────────
                // Draw cell background if needed
                // ─────────────────────────────────────────────────────────
                var hasBg = !attrs.Background.IsDefault || attrs.Inverse || isSelected;
                if (hasBg)
                {
                    SKColor cellBg;
                    if (isSelected)
                    {
                        // Selection takes priority - use selection color with alpha
                        cellBg = ToSKColor(_theme.Selection).WithAlpha(_theme.SelectionAlpha);
                    }
                    else if (attrs.Inverse)
                    {
                        // Inverse: use foreground color as background
                        cellBg = ResolveColor(attrs.Foreground, true);
                    }
                    else
                    {
                        // Normal background color
                        cellBg = ResolveColor(attrs.Background, false);
                    }

                    bgPaint.Color = cellBg;
                    
                    // Account for wide characters (cell.Width may be 1 or 2)
                    canvas.DrawRect(x, y, charWidth * cell.Width, lineHeight, bgPaint);
                }

                // ─────────────────────────────────────────────────────────
                // Draw character if visible
                // ─────────────────────────────────────────────────────────
                var charValue = cell.Character.Value;
                if (charValue != ' ' && charValue != 0 && !attrs.Hidden)
                {
                    // Determine foreground color (with inverse handling)
                    var fgColor = attrs.Inverse
                        ? ResolveColor(attrs.Background, false)
                        : ResolveColor(attrs.Foreground, true);

                    textPaint.Color = fgColor;
                    textPaint.FakeBoldText = attrs.Bold;

                    // Handle dim attribute (reduce alpha)
                    if (attrs.Dim)
                    {
                        textPaint.Color = fgColor.WithAlpha(128);
                    }

                    // Draw the character
                    canvas.DrawText(
                        cell.Character.ToString(),
                        x,
                        y + baseline,
                        textPaint);

                    // ─────────────────────────────────────────────────────
                    // Draw text decorations
                    // ─────────────────────────────────────────────────────
                    linePaint.Color = textPaint.Color;

                    // Underline: 2 pixels from bottom of cell
                    if (attrs.Underline)
                    {
                        var underlineY = y + lineHeight - 2;
                        canvas.DrawLine(x, underlineY, x + charWidth * cell.Width, underlineY, linePaint);
                    }

                    // Strikethrough: vertically centered
                    if (attrs.Strikethrough)
                    {
                        var strikeY = y + lineHeight / 2;
                        canvas.DrawLine(x, strikeY, x + charWidth * cell.Width, strikeY, linePaint);
                    }
                }

                // Advance x position (accounting for wide characters)
                x += charWidth * cell.Width;
            }
        }

        /// <summary>
        /// Draws the terminal cursor at the current position.
        /// </summary>
        private void DrawCursor(SKCanvas canvas, float charWidth, float lineHeight, float baseline)
        {
            // Calculate cursor position in pixels
            var cursorX = _buffer.CursorX * charWidth;
            var cursorY = _buffer.CursorY * lineHeight;

            // ─────────────────────────────────────────────────────────────
            // Create cursor paint with focus-dependent style
            // Focused: solid fill, Unfocused: outline only
            // ─────────────────────────────────────────────────────────────
            using var cursorPaint = new SKPaint
            {
                Color = ToSKColor(_theme.Cursor),
                Style = _isFocused ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
                StrokeWidth = 1
            };

            // ─────────────────────────────────────────────────────────────
            // Draw cursor based on style setting
            // ─────────────────────────────────────────────────────────────
            switch (_theme.CursorStyle)
            {
                case CursorStyle.Block:
                    // Full cell block cursor
                    canvas.DrawRect(cursorX, cursorY, charWidth, lineHeight, cursorPaint);

                    // Draw character under cursor in inverse color when focused
                    if (_isFocused)
                    {
                        DrawCharacterUnderCursor(canvas, baseline, cursorX, cursorY);
                    }
                    break;

                case CursorStyle.Underline:
                    // Thin bar at bottom of cell
                    canvas.DrawRect(cursorX, cursorY + lineHeight - 2, charWidth, 2, cursorPaint);
                    break;

                case CursorStyle.Bar:
                    // Thin vertical bar at left of cell
                    canvas.DrawRect(cursorX, cursorY, 2, lineHeight, cursorPaint);
                    break;
            }
        }

        /// <summary>
        /// Draws the character beneath the cursor in inverse color (for block cursor).
        /// </summary>
        private void DrawCharacterUnderCursor(SKCanvas canvas, float baseline, float cursorX, float cursorY)
        {
            var line = _buffer.GetLine(_buffer.CursorY);
            if (line == null || _buffer.CursorX >= line.Length)
                return;

            var cell = line[_buffer.CursorX];
            if (cell.Character.Value == ' ' || cell.Character.Value == 0)
                return;

            // Draw character in background color (inverse of cursor)
            using var textPaint = new SKPaint
            {
                Typeface = _metrics.Typeface,
                TextSize = _metrics.FontSize,
                Color = ToSKColor(_theme.Background),
                IsAntialias = true
            };

            canvas.DrawText(
                cell.Character.ToString(),
                cursorX,
                cursorY + baseline,
                textPaint);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Search Highlight Rendering (v0.5.5c)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Renders search result highlights on the canvas.
        /// </summary>
        /// <param name="canvas">The SkiaSharp canvas.</param>
        /// <param name="charWidth">Width of a single character cell.</param>
        /// <param name="lineHeight">Height of a single line.</param>
        /// <remarks>
        /// <para>
        /// Draws overlays for all visible search results. Regular matches
        /// are rendered with semi-transparent yellow, while the current
        /// match uses an opaque orange with a border.
        /// </para>
        /// <para>Added in v0.5.5c.</para>
        /// </remarks>
        private void RenderSearchHighlights(SKCanvas canvas, float charWidth, float lineHeight)
        {
            // Skip if no search state or no results
            if (_searchState == null || !_searchState.HasResults)
                return;

            // Calculate viewport bounds (absolute line indices)
            var viewportStart = _buffer.TotalLines - _buffer.Rows - _buffer.ScrollOffset;
            var viewportEnd = viewportStart + _buffer.Rows;

            // Create paints for regular and current match highlights
            using var matchPaint = new SKPaint
            {
                Color = ParseHighlightColor(_highlightStyle.MatchBackground, (float)_highlightStyle.MatchOpacity),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var currentMatchPaint = new SKPaint
            {
                Color = ParseHighlightColor(_highlightStyle.CurrentMatchBackground, 1.0f),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var currentMatchBorderPaint = new SKPaint
            {
                Color = ParseHighlightColor(_highlightStyle.CurrentMatchBorder, 1.0f),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = _highlightStyle.CurrentMatchBorderThickness,
                IsAntialias = true
            };

            // Get current result for comparison
            var currentResult = _searchState.CurrentResult;

            // Iterate all results (only draw visible ones)
            foreach (var result in _searchState.Results)
            {
                // Skip results outside visible viewport
                if (result.LineIndex < viewportStart || result.LineIndex >= viewportEnd)
                    continue;

                // Calculate screen position
                var screenLine = result.LineIndex - viewportStart;
                var y = screenLine * lineHeight;
                var x = result.StartColumn * charWidth;
                var width = result.Length * charWidth;

                var rect = new SKRect(x, y, x + width, y + lineHeight);

                // Check if this is the current result
                var isCurrent = currentResult != null &&
                                result.LineIndex == currentResult.LineIndex &&
                                result.StartColumn == currentResult.StartColumn;

                // Draw highlight
                canvas.DrawRect(rect, isCurrent ? currentMatchPaint : matchPaint);

                // Draw border for current match
                if (isCurrent)
                {
                    canvas.DrawRect(rect, currentMatchBorderPaint);
                }
            }
        }

        /// <summary>
        /// Parses a hex color string to SKColor with opacity.
        /// </summary>
        private static SKColor ParseHighlightColor(string hexColor, float opacity)
        {
            // Try to parse the hex color
            if (SKColor.TryParse(hexColor, out var color))
            {
                return color.WithAlpha((byte)(255 * opacity));
            }

            // Fallback to yellow
            return new SKColor(255, 255, 0, (byte)(255 * opacity));
        }

        #endregion

        #region Color Helpers

        /// <summary>
        /// Resolves a terminal color to an SKColor using the theme.
        /// </summary>
        private SKColor ResolveColor(TerminalColor color, bool isForeground)
        {
            var resolved = _theme.ResolveColor(color, isForeground);
            return ToSKColor(resolved);
        }

        /// <summary>
        /// Converts a TerminalColor to an SKColor.
        /// </summary>
        private static SKColor ToSKColor(TerminalColor color) =>
            new(color.R, color.G, color.B);

        #endregion
    }

    #endregion
}
