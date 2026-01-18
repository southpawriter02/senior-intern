namespace AIntern.Desktop.Controls.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────────┐
// │ TerminalControl (v0.5.2c)                                                    │
// │ Complete terminal control with input handling, selection, and clipboard.     │
// │ Wraps TerminalRenderer and manages terminal session attachment.              │
// └─────────────────────────────────────────────────────────────────────────────┘

using System;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;
using Microsoft.Extensions.Logging;

#region Type Documentation

/// <summary>
/// Complete terminal control with input handling, selection, and clipboard support.
/// Wraps <see cref="TerminalRenderer"/> and manages terminal session attachment.
/// </summary>
/// <remarks>
/// <para>
/// The TerminalControl is the main UI component for terminal interaction, providing:
/// <list type="bullet">
///   <item><description>Keyboard input with VT100/xterm escape sequence generation</description></item>
///   <item><description>Mouse text selection (single, double, triple click)</description></item>
///   <item><description>Clipboard integration (Ctrl+Shift+C/V)</description></item>
///   <item><description>Cursor blink animation using DispatcherTimer</description></item>
///   <item><description>Terminal size synchronization with PTY</description></item>
/// </list>
/// </para>
/// <para>
/// Key Sequence Mapping:
/// <code>
/// Special Keys:
///   Enter → \r (CR)
///   Escape → \x1B (ESC)
///   Tab → \t, Shift+Tab → \x1B[Z
///   Backspace → \x7F (DEL)
///   Delete → \x1B[3~
/// 
/// Arrow Keys:
///   Up → \x1B[A, Down → \x1B[B
///   Right → \x1B[C, Left → \x1B[D
/// 
/// Function Keys:
///   F1-F4 → \x1BOP through \x1BOS
///   F5-F12 → \x1B[15~ through \x1B[24~
/// 
/// Ctrl+Key:
///   Ctrl+C → \x03 (SIGINT)
///   Ctrl+Z → \x1A (SIGTSTP)
///   Ctrl+D → \x04 (EOF)
/// </code>
/// </para>
/// <para>Added in v0.5.2c.</para>
/// </remarks>

#endregion

public partial class TerminalControl : UserControl
{
    #region Private Fields

    /// <summary>
    /// Optional logger for diagnostic output.
    /// </summary>
    private readonly ILogger<TerminalControl>? _logger;

    /// <summary>
    /// The terminal service for session management.
    /// </summary>
    private ITerminalService? _terminalService;

    /// <summary>
    /// The current terminal session ID.
    /// </summary>
    private Guid _sessionId;

    /// <summary>
    /// The terminal buffer for the attached session.
    /// </summary>
    private TerminalBuffer? _buffer;

    #endregion

    #region Selection State

    /// <summary>
    /// Indicates whether the user is currently selecting text.
    /// </summary>
    private bool _isSelecting;

    /// <summary>
    /// The pixel position where selection started.
    /// </summary>
    private Point _selectionStartPixel;

    /// <summary>
    /// The current text selection.
    /// </summary>
    private TerminalSelection? _currentSelection;

    #endregion

    #region Cursor Blink State

    /// <summary>
    /// Timer for cursor blink animation.
    /// </summary>
    private DispatcherTimer? _cursorBlinkTimer;

    /// <summary>
    /// Current cursor blink state (true = visible).
    /// </summary>
    private bool _cursorBlinkState = true;

    #endregion

    #region Click Detection State

    /// <summary>
    /// Timestamp of the last click for double/triple click detection.
    /// </summary>
    private DateTime _lastClickTime;

    /// <summary>
    /// Position of the last click for double/triple click detection.
    /// </summary>
    private Point _lastClickPosition;

    /// <summary>
    /// Current click count (1 = single, 2 = double, 3 = triple).
    /// </summary>
    private int _clickCount;

    #endregion

    #region Constants

    /// <summary>
    /// Maximum time between clicks to be considered a multi-click (milliseconds).
    /// </summary>
    internal const int ClickTimeWindowMs = 500;

    /// <summary>
    /// Maximum distance between clicks to be considered a multi-click (pixels).
    /// </summary>
    internal const double ClickPositionTolerancePx = 5.0;

    /// <summary>
    /// Number of lines to scroll per mouse wheel tick.
    /// </summary>
    internal const int ScrollLinesPerTick = 3;

    #endregion

    #region Styled Properties

    /// <summary>
    /// Identifies the <see cref="TerminalFontFamily"/> styled property.
    /// </summary>
    public static readonly StyledProperty<string> TerminalFontFamilyProperty =
        AvaloniaProperty.Register<TerminalControl, string>(nameof(TerminalFontFamily), "Cascadia Mono");

    /// <summary>
    /// Identifies the <see cref="TerminalFontSize"/> styled property.
    /// </summary>
    public static readonly StyledProperty<double> TerminalFontSizeProperty =
        AvaloniaProperty.Register<TerminalControl, double>(nameof(TerminalFontSize), 14.0);

    /// <summary>
    /// Identifies the <see cref="TerminalTheme"/> styled property.
    /// </summary>
    public static readonly StyledProperty<TerminalTheme> TerminalThemeProperty =
        AvaloniaProperty.Register<TerminalControl, TerminalTheme>(nameof(TerminalTheme), Core.Models.Terminal.TerminalTheme.Dark);

    /// <summary>
    /// Gets or sets the font family for terminal text.
    /// </summary>
    /// <value>The font family name. Default is "Cascadia Mono".</value>
    public string TerminalFontFamily
    {
        get => GetValue(TerminalFontFamilyProperty);
        set => SetValue(TerminalFontFamilyProperty, value);
    }

    /// <summary>
    /// Gets or sets the font size for terminal text in points.
    /// </summary>
    /// <value>The font size in points. Default is 14.0.</value>
    public double TerminalFontSize
    {
        get => GetValue(TerminalFontSizeProperty);
        set => SetValue(TerminalFontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the terminal color theme.
    /// </summary>
    /// <value>The terminal theme. Default is <see cref="Core.Models.Terminal.TerminalTheme.Dark"/>.</value>
    public TerminalTheme TerminalTheme
    {
        get => GetValue(TerminalThemeProperty);
        set => SetValue(TerminalThemeProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the terminal size changes (columns/rows).
    /// </summary>
    public event EventHandler<TerminalSizeChangedEventArgs>? TerminalSizeChanged;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalControl"/> class.
    /// </summary>
    public TerminalControl() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalControl"/> class
    /// with optional logging support.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public TerminalControl(ILogger<TerminalControl>? logger)
    {
        _logger = logger;

        InitializeComponent();

        // Subscribe to pointer events
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
        DoubleTapped += OnDoubleTapped;

        // Subscribe to focus events
        GotFocus += OnGotFocus;
        LostFocus += OnLostFocus;

        // Subscribe to property changes
        PropertyChanged += OnControlPropertyChanged;

        _logger?.LogDebug("[TerminalControl] Instance created");
    }

    #endregion

    #region Lifecycle

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SetupCursorBlink();
        _logger?.LogDebug("[TerminalControl] Control loaded, cursor blink configured");
    }

    /// <inheritdoc />
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _cursorBlinkTimer?.Stop();
        _logger?.LogDebug("[TerminalControl] Control unloaded, cursor blink stopped");
    }

    #endregion

    #region Session Management

    /// <summary>
    /// Attaches to a terminal session.
    /// </summary>
    /// <param name="terminalService">The terminal service.</param>
    /// <param name="sessionId">The session ID to attach to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// Attaching to a session performs the following:
    /// <list type="number">
    ///   <item><description>Detaches from any previous session</description></item>
    ///   <item><description>Gets the buffer from the terminal service</description></item>
    ///   <item><description>Configures the renderer with buffer and theme</description></item>
    ///   <item><description>Subscribes to service events (OutputReceived, SessionStateChanged)</description></item>
    ///   <item><description>Synchronizes terminal size with PTY</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public async Task AttachSessionAsync(
        ITerminalService terminalService,
        Guid sessionId)
    {
        _logger?.LogInformation("[TerminalControl] Attaching to session {SessionId}", sessionId);

        // Detach from previous session
        if (_terminalService != null)
        {
            _logger?.LogDebug("[TerminalControl] Detaching from previous session");
            _terminalService.OutputReceived -= OnOutputReceived;
            _terminalService.SessionStateChanged -= OnSessionStateChanged;
        }

        _terminalService = terminalService;
        _sessionId = sessionId;
        _buffer = terminalService.GetBuffer(sessionId);

        // Configure renderer
        if (_buffer != null)
            Renderer.SetBuffer(_buffer);
        Renderer.SetTheme(TerminalTheme);

        // Subscribe to events
        _terminalService.OutputReceived += OnOutputReceived;
        _terminalService.SessionStateChanged += OnSessionStateChanged;

        // Sync size
        await SyncTerminalSizeAsync();

        // Reset cursor blink
        ResetCursorBlink();

        _logger?.LogInformation("[TerminalControl] Successfully attached to session {SessionId}", sessionId);
    }

    /// <summary>
    /// Detaches from the current session.
    /// </summary>
    /// <remarks>
    /// Unsubscribes from service events and clears the session reference.
    /// The renderer will continue to display the last buffer state until a new session is attached.
    /// </remarks>
    public void DetachSession()
    {
        _logger?.LogInformation("[TerminalControl] Detaching from session {SessionId}", _sessionId);

        if (_terminalService != null)
        {
            _terminalService.OutputReceived -= OnOutputReceived;
            _terminalService.SessionStateChanged -= OnSessionStateChanged;
        }

        _terminalService = null;
        _sessionId = Guid.Empty;
        _buffer = null;

        _logger?.LogDebug("[TerminalControl] Session detached and references cleared");
    }

    #endregion

    #region Property Change Handling

    /// <summary>
    /// Handles property changes for bounds and theme.
    /// </summary>
    private void OnControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
        {
            _logger?.LogDebug("[TerminalControl] Bounds changed, synchronizing terminal size");
            _ = SyncTerminalSizeAsync();
        }
        else if (e.Property == TerminalThemeProperty)
        {
            _logger?.LogDebug("[TerminalControl] Theme changed, updating renderer");
            Renderer.SetTheme(TerminalTheme);

            // Update cursor blink interval
            if (_cursorBlinkTimer != null)
            {
                _cursorBlinkTimer.Interval = TimeSpan.FromMilliseconds(TerminalTheme.CursorBlinkIntervalMs);
            }
        }
    }

    /// <summary>
    /// Synchronizes the terminal size with the PTY based on current bounds.
    /// </summary>
    private async Task SyncTerminalSizeAsync()
    {
        if (_terminalService == null || _sessionId == Guid.Empty)
        {
            _logger?.LogDebug("[TerminalControl] Cannot sync size: no active session");
            return;
        }

        var metrics = Renderer.Metrics;
        if (!metrics.IsValid)
        {
            _logger?.LogDebug("[TerminalControl] Cannot sync size: invalid font metrics");
            return;
        }

        var (cols, rows) = metrics.CalculateTerminalSize(Bounds.Width, Bounds.Height);

        if (cols > 0 && rows > 0)
        {
            _logger?.LogDebug("[TerminalControl] Resizing terminal to {Cols}x{Rows}", cols, rows);
            await _terminalService.ResizeAsync(_sessionId, cols, rows);
            TerminalSizeChanged?.Invoke(this, new TerminalSizeChangedEventArgs(cols, rows));
        }
    }

    #endregion

    #region Terminal Service Event Handlers

    /// <summary>
    /// Handles output received from the terminal session.
    /// </summary>
    private void OnOutputReceived(object? sender, TerminalOutputEventArgs e)
    {
        if (e.SessionId != _sessionId)
            return;

        // Reset cursor blink on output
        Dispatcher.UIThread.Post(ResetCursorBlink);
    }

    /// <summary>
    /// Handles session state changes.
    /// </summary>
    private void OnSessionStateChanged(object? sender, TerminalSessionStateEventArgs e)
    {
        if (e.SessionId != _sessionId)
            return;

        // Stop cursor blink on session end
        if (e.NewState == TerminalSessionState.Exited ||
            e.NewState == TerminalSessionState.Error)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _cursorBlinkTimer?.Stop();
                _logger?.LogInformation(
                    "[TerminalControl] Session {SessionId} ended with state {State}",
                    _sessionId, e.NewState);
            });
        }
    }

    #endregion

    #region Keyboard Input

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (_terminalService == null || _sessionId == Guid.Empty)
        {
            _logger?.LogDebug("[TerminalControl] Key ignored: no active session");
            return;
        }

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        // Clipboard shortcuts: Ctrl+Shift+C/V
        if (ctrl && shift && e.Key == Key.C)
        {
            _logger?.LogDebug("[TerminalControl] Copy shortcut triggered");
            _ = CopySelectionAsync();
            e.Handled = true;
            return;
        }

        if (ctrl && shift && e.Key == Key.V)
        {
            _logger?.LogDebug("[TerminalControl] Paste shortcut triggered");
            _ = PasteAsync();
            e.Handled = true;
            return;
        }

        // Convert key to terminal sequence
        var sequence = GetKeySequence(e.Key, e.KeyModifiers);
        if (sequence != null)
        {
            _logger?.LogDebug(
                "[TerminalControl] Key {Key}+{Modifiers} → sequence length {Length}",
                e.Key, e.KeyModifiers, sequence.Length);

            _ = _terminalService.WriteInputAsync(_sessionId, sequence);
            ClearSelection();
            ResetCursorBlink();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (_terminalService == null || _sessionId == Guid.Empty)
            return;

        if (!string.IsNullOrEmpty(e.Text))
        {
            _logger?.LogDebug("[TerminalControl] Text input: '{Text}'", e.Text);
            _ = _terminalService.WriteInputAsync(_sessionId, e.Text);
            ClearSelection();
            ResetCursorBlink();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Gets the VT100/xterm escape sequence for a key press.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="modifiers">The key modifiers.</param>
    /// <returns>The escape sequence string, or null if not handled.</returns>
    /// <remarks>
    /// This method is internal for testing purposes.
    /// </remarks>
    internal static string? GetKeySequence(Key key, KeyModifiers modifiers)
    {
        var ctrl = modifiers.HasFlag(KeyModifiers.Control);
        var alt = modifiers.HasFlag(KeyModifiers.Alt);
        var shift = modifiers.HasFlag(KeyModifiers.Shift);

        // Special keys
        var sequence = key switch
        {
            Key.Enter => "\r",
            Key.Escape => "\x1B",
            Key.Tab => shift ? "\x1B[Z" : "\t",
            Key.Back => "\x7F",
            Key.Delete => "\x1B[3~",
            Key.Insert => "\x1B[2~",
            Key.Up => "\x1B[A",
            Key.Down => "\x1B[B",
            Key.Right => "\x1B[C",
            Key.Left => "\x1B[D",
            Key.Home => ctrl ? "\x1B[1;5H" : "\x1B[H",
            Key.End => ctrl ? "\x1B[1;5F" : "\x1B[F",
            Key.PageUp => "\x1B[5~",
            Key.PageDown => "\x1B[6~",
            Key.F1 => "\x1BOP",
            Key.F2 => "\x1BOQ",
            Key.F3 => "\x1BOR",
            Key.F4 => "\x1BOS",
            Key.F5 => "\x1B[15~",
            Key.F6 => "\x1B[17~",
            Key.F7 => "\x1B[18~",
            Key.F8 => "\x1B[19~",
            Key.F9 => "\x1B[20~",
            Key.F10 => "\x1B[21~",
            Key.F11 => "\x1B[23~",
            Key.F12 => "\x1B[24~",
            _ => null
        };

        if (sequence != null)
            return sequence;

        // Ctrl+key combinations (excluding Shift to allow Ctrl+Shift+C/V for clipboard)
        if (ctrl && !alt && !shift)
        {
            return key switch
            {
                Key.C => "\x03",       // SIGINT
                Key.Z => "\x1A",       // SIGTSTP
                Key.D => "\x04",       // EOF
                Key.L => "\x0C",       // Clear screen
                Key.A => "\x01",       // Beginning of line
                Key.E => "\x05",       // End of line
                Key.K => "\x0B",       // Kill to end of line
                Key.U => "\x15",       // Kill to beginning of line
                Key.W => "\x17",       // Kill word backward
                Key.R => "\x12",       // Reverse search
                Key.P => "\x10",       // Previous history
                Key.N => "\x0E",       // Next history
                >= Key.A and <= Key.Z => ((char)(key - Key.A + 1)).ToString(),
                _ => null
            };
        }

        return null;
    }

    #endregion

    #region Mouse Input & Selection

    /// <summary>
    /// Handles pointer pressed events for selection and focus.
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_buffer == null)
            return;

        var point = e.GetCurrentPoint(Renderer);
        var position = point.Position;

        // Handle click count for double/triple click
        var now = DateTime.UtcNow;
        if ((now - _lastClickTime).TotalMilliseconds < ClickTimeWindowMs &&
            Math.Abs(position.X - _lastClickPosition.X) < ClickPositionTolerancePx &&
            Math.Abs(position.Y - _lastClickPosition.Y) < ClickPositionTolerancePx)
        {
            _clickCount++;
            _logger?.LogDebug("[TerminalControl] Multi-click detected: count={Count}", _clickCount);
        }
        else
        {
            _clickCount = 1;
        }

        _lastClickTime = now;
        _lastClickPosition = position;

        if (point.Properties.IsLeftButtonPressed)
        {
            Focus();

            if (_clickCount >= 3)
            {
                // Triple click - select line
                _logger?.LogDebug("[TerminalControl] Triple click - selecting line");
                SelectLine(position);
            }
            else if (_clickCount == 2)
            {
                // Double click - select word
                _logger?.LogDebug("[TerminalControl] Double click - selecting word");
                SelectWord(position);
            }
            else
            {
                // Single click - start character selection
                _logger?.LogDebug("[TerminalControl] Single click - starting selection");
                _isSelecting = true;
                _selectionStartPixel = position;
                _currentSelection = null;
                Renderer.Selection = null;
            }

            e.Handled = true;
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            // Right click - paste if no selection
            if (_currentSelection == null || _currentSelection.IsEmpty)
            {
                _logger?.LogDebug("[TerminalControl] Right click - pasting");
                _ = PasteAsync();
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles pointer moved events to update selection.
    /// </summary>
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isSelecting || _buffer == null)
            return;

        var position = e.GetPosition(Renderer);
        UpdateSelection(_selectionStartPixel, position);
    }

    /// <summary>
    /// Handles pointer released events to finish selection.
    /// </summary>
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isSelecting)
        {
            _logger?.LogDebug("[TerminalControl] Selection completed");
        }
        _isSelecting = false;
    }

    /// <summary>
    /// Handles mouse wheel events for scrolling.
    /// </summary>
    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_buffer == null)
            return;

        var delta = e.Delta.Y > 0 ? -ScrollLinesPerTick : ScrollLinesPerTick;
        var newOffset = Math.Clamp(
            _buffer.ScrollOffset + delta,
            0,
            _buffer.ScrollbackLines);

        _buffer.ScrollOffset = newOffset;

        // Update scrollbar
        VerticalScrollBar.Value = _buffer.ScrollbackLines - newOffset;

        _logger?.LogDebug("[TerminalControl] Scroll offset: {Offset}", newOffset);
        e.Handled = true;
    }

    /// <summary>
    /// Handles double-tap events (deferred to OnPointerPressed via click count).
    /// </summary>
    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Handled in OnPointerPressed via click count
    }

    /// <summary>
    /// Updates the current selection based on start and end pixel positions.
    /// </summary>
    private void UpdateSelection(Point start, Point end)
    {
        var metrics = Renderer.Metrics;
        var (startCol, startRow) = metrics.PixelToCell(start.X, start.Y);
        var (endCol, endRow) = metrics.PixelToCell(end.X, end.Y);

        var scrollbackStart = _buffer!.TotalLines - _buffer.Rows - _buffer.ScrollOffset;

        _currentSelection = new TerminalSelection
        {
            StartLine = scrollbackStart + startRow,
            StartColumn = startCol,
            EndLine = scrollbackStart + endRow,
            EndColumn = endCol
        };

        Renderer.Selection = _currentSelection;
    }

    /// <summary>
    /// Selects the word at the specified pixel position.
    /// </summary>
    private void SelectWord(Point position)
    {
        if (_buffer == null)
            return;

        var metrics = Renderer.Metrics;
        var (col, row) = metrics.PixelToCell(position.X, position.Y);

        var scrollbackStart = _buffer.TotalLines - _buffer.Rows - _buffer.ScrollOffset;
        var absoluteLine = scrollbackStart + row;

        if (absoluteLine < 0 || absoluteLine >= _buffer.TotalLines)
            return;

        var line = _buffer.GetLine(absoluteLine);
        if (line == null || line.Length == 0)
            return;

        // Clamp column to line length
        col = Math.Clamp(col, 0, line.Length - 1);

        // Find word boundaries
        var startCol = col;
        var endCol = col;

        // Expand left
        while (startCol > 0 && IsWordChar(line[startCol - 1].Character))
            startCol--;

        // Expand right
        while (endCol < line.Length - 1 && IsWordChar(line[endCol + 1].Character))
            endCol++;

        _currentSelection = new TerminalSelection
        {
            StartLine = absoluteLine,
            StartColumn = startCol,
            EndLine = absoluteLine,
            EndColumn = endCol
        };

        Renderer.Selection = _currentSelection;

        _logger?.LogDebug(
            "[TerminalControl] Word selected: line={Line}, cols={Start}-{End}",
            absoluteLine, startCol, endCol);
    }

    /// <summary>
    /// Selects the entire line at the specified pixel position.
    /// </summary>
    private void SelectLine(Point position)
    {
        if (_buffer == null)
            return;

        var metrics = Renderer.Metrics;
        var (_, row) = metrics.PixelToCell(position.X, position.Y);

        var scrollbackStart = _buffer.TotalLines - _buffer.Rows - _buffer.ScrollOffset;
        var absoluteLine = scrollbackStart + row;

        if (absoluteLine < 0 || absoluteLine >= _buffer.TotalLines)
            return;

        _currentSelection = new TerminalSelection
        {
            StartLine = absoluteLine,
            StartColumn = 0,
            EndLine = absoluteLine,
            EndColumn = _buffer.Columns - 1
        };

        Renderer.Selection = _currentSelection;

        _logger?.LogDebug("[TerminalControl] Line selected: {Line}", absoluteLine);
    }

    /// <summary>
    /// Determines whether a character is considered part of a word.
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns><c>true</c> if the character is a word character; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Word characters include letters, digits, underscore, and hyphen.
    /// This method is internal for testing purposes.
    /// </remarks>
    internal static bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '-';
    }

    /// <summary>
    /// Determines whether a Rune is considered part of a word.
    /// </summary>
    /// <param name="rune">The Rune to test.</param>
    /// <returns><c>true</c> if the Rune is a word character; otherwise, <c>false</c>.</returns>
    internal static bool IsWordChar(System.Text.Rune rune)
    {
        // For single BMP characters, convert and check
        if (rune.IsBmp)
        {
            var c = (char)rune.Value;
            return char.IsLetterOrDigit(c) || c == '_' || c == '-';
        }
        // For supplementary characters, check if letter or digit
        return Rune.IsLetterOrDigit(rune);
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    private void ClearSelection()
    {
        _currentSelection = null;
        Renderer.Selection = null;
    }

    #endregion

    #region Clipboard

    /// <summary>
    /// Copies the selected text to the clipboard.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CopySelectionAsync()
    {
        if (_currentSelection == null || _currentSelection.IsEmpty || _buffer == null)
        {
            _logger?.LogDebug("[TerminalControl] Copy skipped: no selection");
            return;
        }

        var text = _buffer.GetSelectedText(_currentSelection);
        if (!string.IsNullOrEmpty(text))
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
                _logger?.LogInformation(
                    "[TerminalControl] Copied {Length} characters to clipboard",
                    text.Length);
            }
        }
    }

    /// <summary>
    /// Pastes text from the clipboard to the terminal.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Line endings are normalized: CRLF and LF are converted to CR.
    /// </remarks>
    public async Task PasteAsync()
    {
        if (_terminalService == null || _sessionId == Guid.Empty)
        {
            _logger?.LogDebug("[TerminalControl] Paste skipped: no active session");
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
            return;

        var text = await clipboard.GetTextAsync();
        if (!string.IsNullOrEmpty(text))
        {
            // Convert Windows line endings to terminal format
            text = text.Replace("\r\n", "\r").Replace("\n", "\r");

            await _terminalService.WriteInputAsync(_sessionId, text);
            _logger?.LogInformation(
                "[TerminalControl] Pasted {Length} characters from clipboard",
                text.Length);
        }
    }

    /// <summary>
    /// Selects all text in the buffer.
    /// </summary>
    public void SelectAll()
    {
        if (_buffer == null)
            return;

        _currentSelection = new TerminalSelection
        {
            StartLine = 0,
            StartColumn = 0,
            EndLine = _buffer.TotalLines - 1,
            EndColumn = _buffer.Columns - 1
        };

        Renderer.Selection = _currentSelection;
        _logger?.LogDebug("[TerminalControl] Selected all text");
    }

    #endregion

    #region Cursor Blink

    /// <summary>
    /// Sets up the cursor blink timer based on theme settings.
    /// </summary>
    private void SetupCursorBlink()
    {
        _cursorBlinkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(TerminalTheme.CursorBlinkIntervalMs)
        };
        _cursorBlinkTimer.Tick += OnCursorBlinkTick;

        if (IsFocused && TerminalTheme.CursorBlink)
        {
            _cursorBlinkTimer.Start();
            _logger?.LogDebug("[TerminalControl] Cursor blink started");
        }
    }

    /// <summary>
    /// Handles cursor blink timer ticks.
    /// </summary>
    private void OnCursorBlinkTick(object? sender, EventArgs e)
    {
        _cursorBlinkState = !_cursorBlinkState;
        Renderer.CursorVisible = _cursorBlinkState;
    }

    /// <summary>
    /// Resets the cursor blink state (shows cursor and restarts timer).
    /// </summary>
    private void ResetCursorBlink()
    {
        _cursorBlinkState = true;
        Renderer.CursorVisible = true;
        _cursorBlinkTimer?.Stop();

        if (IsFocused && TerminalTheme.CursorBlink)
        {
            _cursorBlinkTimer?.Start();
        }
    }

    /// <summary>
    /// Handles the control gaining focus.
    /// </summary>
    private void OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        _logger?.LogDebug("[TerminalControl] Got focus");
        ResetCursorBlink();
    }

    /// <summary>
    /// Handles the control losing focus.
    /// </summary>
    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        _logger?.LogDebug("[TerminalControl] Lost focus");
        _cursorBlinkTimer?.Stop();
        Renderer.CursorVisible = true;
    }

    #endregion
}

#region Event Args

/// <summary>
/// Provides data for the <see cref="TerminalControl.TerminalSizeChanged"/> event.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.2c.</para>
/// </remarks>
public sealed class TerminalSizeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Columns { get; }

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    public int Rows { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalSizeChangedEventArgs"/> class.
    /// </summary>
    /// <param name="columns">The number of columns.</param>
    /// <param name="rows">The number of rows.</param>
    public TerminalSizeChangedEventArgs(int columns, int rows)
    {
        Columns = columns;
        Rows = rows;
    }
}

#endregion
