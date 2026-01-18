using System.Text;

namespace AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TerminalBuffer (v0.5.1b)                                                 │
// │ Manages the terminal screen buffer with scrollback support.              │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Manages the terminal screen buffer with scrollback history.
/// </summary>
/// <remarks>
/// <para>
/// The buffer consists of:
/// <list type="bullet">
///   <item><description>A scrollback region containing historical lines</description></item>
///   <item><description>A visible region containing the current screen</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safe via internal locking for concurrent access from the parser and renderer.
/// </para>
/// <para>Added in v0.5.1b.</para>
/// </remarks>
public sealed class TerminalBuffer
{
    #region Private Fields

    /// <summary>Lock for thread-safe access.</summary>
    private readonly object _lock = new();

    /// <summary>All lines (scrollback + visible).</summary>
    private readonly List<TerminalLine> _lines = new();

    /// <summary>Maximum scrollback lines to retain.</summary>
    private readonly int _maxScrollback;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the number of columns (width) in the terminal.
    /// </summary>
    public int Columns { get; private set; }

    /// <summary>
    /// Gets the number of visible rows (height) in the terminal.
    /// </summary>
    public int Rows { get; private set; }

    /// <summary>
    /// Gets or sets the current cursor column position (0-indexed).
    /// </summary>
    public int CursorX { get; set; }

    /// <summary>
    /// Gets or sets the current cursor row position (0-indexed, relative to visible area).
    /// </summary>
    public int CursorY { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cursor is visible.
    /// </summary>
    /// <remarks>Controlled by DECTCEM (DEC cursor enable mode) escape sequences.</remarks>
    public bool CursorVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the current scroll offset (0 = at bottom/live, positive = scrolled up).
    /// </summary>
    public int ScrollOffset { get; set; }

    /// <summary>
    /// Gets the total number of lines (scrollback + visible).
    /// </summary>
    public int TotalLines
    {
        get { lock (_lock) { return _lines.Count; } }
    }

    /// <summary>
    /// Gets the number of scrollback lines (above visible area).
    /// </summary>
    public int ScrollbackLines => Math.Max(0, TotalLines - Rows);

    /// <summary>
    /// Gets or sets the current text attributes for new characters.
    /// </summary>
    public TerminalAttributes CurrentAttributes { get; set; } = TerminalAttributes.Default;

    /// <summary>
    /// Gets or sets the saved cursor position (for DECSC/DECRC).
    /// </summary>
    public (int X, int Y) SavedCursor { get; set; }

    /// <summary>
    /// Gets or sets the saved attributes (for DECSC/DECRC).
    /// </summary>
    public TerminalAttributes SavedAttributes { get; set; } = TerminalAttributes.Default;

    /// <summary>
    /// Gets or sets the top margin for scrolling region (0-indexed).
    /// </summary>
    public int ScrollRegionTop { get; set; }

    /// <summary>
    /// Gets or sets the bottom margin for scrolling region (0-indexed, exclusive).
    /// </summary>
    public int ScrollRegionBottom { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether origin mode is enabled.
    /// </summary>
    /// <remarks>When true, cursor positions are relative to the scroll region.</remarks>
    public bool OriginMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether auto-wrap mode is enabled.
    /// </summary>
    /// <remarks>When true, cursor wraps to next line when reaching right margin.</remarks>
    public bool AutoWrapMode { get; set; } = true;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when buffer content changes.
    /// </summary>
    public event EventHandler? ContentChanged;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalBuffer"/> class.
    /// </summary>
    /// <param name="columns">Number of columns (width).</param>
    /// <param name="rows">Number of visible rows (height).</param>
    /// <param name="maxScrollback">Maximum scrollback lines to retain (default: 10,000).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when dimensions or scrollback are invalid.
    /// </exception>
    public TerminalBuffer(int columns, int rows, int maxScrollback = 10000)
    {
        if (columns <= 0)
            throw new ArgumentOutOfRangeException(nameof(columns), "Columns must be greater than zero.");
        if (rows <= 0)
            throw new ArgumentOutOfRangeException(nameof(rows), "Rows must be greater than zero.");
        if (maxScrollback < 0)
            throw new ArgumentOutOfRangeException(nameof(maxScrollback), "Max scrollback cannot be negative.");

        Columns = columns;
        Rows = rows;
        _maxScrollback = maxScrollback;
        ScrollRegionBottom = rows;

        InitializeLines();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the buffer with empty lines.
    /// </summary>
    private void InitializeLines()
    {
        lock (_lock)
        {
            _lines.Clear();
            for (int i = 0; i < Rows; i++)
            {
                _lines.Add(new TerminalLine(Columns));
            }
        }
    }

    #endregion

    #region Resize

    /// <summary>
    /// Resizes the terminal buffer to new dimensions.
    /// </summary>
    /// <param name="columns">New column count.</param>
    /// <param name="rows">New row count.</param>
    /// <remarks>
    /// Preserves content where possible. Adjusts cursor position if needed.
    /// </remarks>
    public void Resize(int columns, int rows)
    {
        if (columns <= 0 || rows <= 0)
            return;

        lock (_lock)
        {
            Columns = columns;
            Rows = rows;
            ScrollRegionBottom = rows;

            // Resize existing lines
            foreach (var line in _lines)
            {
                line.Resize(columns);
            }

            // Add or remove lines to match new row count
            while (_lines.Count < rows)
            {
                _lines.Add(new TerminalLine(columns));
            }

            // Trim scrollback if needed
            TrimScrollback();

            // Adjust cursor position
            CursorX = Math.Min(CursorX, columns - 1);
            CursorY = Math.Min(CursorY, rows - 1);

            OnContentChanged();
        }
    }

    #endregion

    #region Character Writing

    /// <summary>
    /// Writes a character at the current cursor position.
    /// </summary>
    /// <param name="c">The character to write.</param>
    public void WriteChar(char c)
    {
        WriteChar(new Rune(c));
    }

    /// <summary>
    /// Writes a Unicode rune at the current cursor position.
    /// </summary>
    /// <param name="rune">The rune to write.</param>
    public void WriteChar(Rune rune)
    {
        lock (_lock)
        {
            // Handle auto-wrap
            if (CursorX >= Columns)
            {
                if (AutoWrapMode)
                {
                    CursorX = 0;
                    LineFeed();
                }
                else
                {
                    CursorX = Columns - 1;
                }
            }

            var lineIndex = GetAbsoluteLineIndex(CursorY);
            if (lineIndex >= 0 && lineIndex < _lines.Count)
            {
                var line = _lines[lineIndex];
                if (CursorX < Columns)
                {
                    line.SetCell(CursorX, new TerminalCell
                    {
                        Character = rune,
                        Attributes = CurrentAttributes,
                        Width = 1,
                        IsContinuation = false
                    });
                    CursorX++;
                }
            }

            OnContentChanged();
        }
    }

    /// <summary>
    /// Writes a string at the current cursor position.
    /// </summary>
    /// <param name="text">The text to write.</param>
    public void WriteString(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            WriteChar(rune);
        }
    }

    #endregion

    #region Cursor Movement

    /// <summary>
    /// Performs a line feed (move cursor down, scroll if needed).
    /// </summary>
    public void LineFeed()
    {
        lock (_lock)
        {
            if (CursorY >= ScrollRegionBottom - 1)
            {
                // At bottom of scroll region, scroll up
                ScrollUp(1);
            }
            else if (CursorY < Rows - 1)
            {
                CursorY++;
            }
            OnContentChanged();
        }
    }

    /// <summary>
    /// Performs a carriage return (move cursor to column 0).
    /// </summary>
    public void CarriageReturn()
    {
        CursorX = 0;
    }

    /// <summary>
    /// Performs a backspace (move cursor left one position).
    /// </summary>
    public void Backspace()
    {
        if (CursorX > 0)
        {
            CursorX--;
        }
    }

    /// <summary>
    /// Performs a horizontal tab (move to next tab stop).
    /// </summary>
    /// <remarks>Tab stops are every 8 columns by default.</remarks>
    public void Tab()
    {
        CursorX = Math.Min(((CursorX / 8) + 1) * 8, Columns - 1);
    }

    /// <summary>
    /// Sets the cursor position (1-indexed VT100 coordinates).
    /// </summary>
    /// <param name="row">Row position (1-indexed).</param>
    /// <param name="column">Column position (1-indexed).</param>
    /// <remarks>
    /// Converts from VT100's 1-indexed coordinates to internal 0-indexed.
    /// Clamps values to valid range.
    /// </remarks>
    public void SetCursorPosition(int row, int column)
    {
        // Convert from 1-indexed to 0-indexed
        CursorY = Math.Clamp(row - 1, 0, Rows - 1);
        CursorX = Math.Clamp(column - 1, 0, Columns - 1);
    }

    /// <summary>
    /// Saves the current cursor position and attributes.
    /// </summary>
    public void SaveCursor()
    {
        SavedCursor = (CursorX, CursorY);
        SavedAttributes = CurrentAttributes;
    }

    /// <summary>
    /// Restores the saved cursor position and attributes.
    /// </summary>
    public void RestoreCursor()
    {
        (CursorX, CursorY) = SavedCursor;
        CurrentAttributes = SavedAttributes;
    }

    #endregion

    #region Scrolling

    /// <summary>
    /// Scrolls the content up by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll (default: 1).</param>
    public void ScrollUp(int lines = 1)
    {
        if (lines <= 0) return;

        lock (_lock)
        {
            for (int i = 0; i < lines; i++)
            {
                // Add a new line at the bottom of the scroll region
                var lineIndex = GetAbsoluteLineIndex(ScrollRegionBottom - 1);
                if (lineIndex >= 0 && lineIndex < _lines.Count)
                {
                    _lines.Insert(lineIndex + 1, new TerminalLine(Columns));
                }

                // Remove line at top of scroll region (becomes scrollback)
                // The line stays in the list as scrollback
            }

            TrimScrollback();
            OnContentChanged();
        }
    }

    /// <summary>
    /// Scrolls the content down by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll (default: 1).</param>
    public void ScrollDown(int lines = 1)
    {
        if (lines <= 0) return;

        lock (_lock)
        {
            for (int i = 0; i < lines; i++)
            {
                // Insert a new line at the top of the scroll region
                var lineIndex = GetAbsoluteLineIndex(ScrollRegionTop);
                if (lineIndex >= 0 && lineIndex < _lines.Count)
                {
                    _lines.Insert(lineIndex, new TerminalLine(Columns));
                }

                // Remove line at bottom of scroll region
                var bottomIndex = GetAbsoluteLineIndex(ScrollRegionBottom - 1);
                if (bottomIndex >= 0 && bottomIndex < _lines.Count)
                {
                    _lines.RemoveAt(bottomIndex);
                }
            }

            OnContentChanged();
        }
    }

    /// <summary>
    /// Sets the scrolling region margins.
    /// </summary>
    /// <param name="top">Top row (1-indexed).</param>
    /// <param name="bottom">Bottom row (1-indexed).</param>
    public void SetScrollRegion(int top, int bottom)
    {
        // Convert to 0-indexed
        ScrollRegionTop = Math.Clamp(top - 1, 0, Rows - 1);
        ScrollRegionBottom = Math.Clamp(bottom, 1, Rows);

        if (ScrollRegionTop >= ScrollRegionBottom)
        {
            // Invalid region, reset to full screen
            ScrollRegionTop = 0;
            ScrollRegionBottom = Rows;
        }
    }

    #endregion

    #region Clearing

    /// <summary>
    /// Clears the entire screen (ED 2).
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            foreach (var line in _lines)
            {
                line.Clear();
            }
            OnContentChanged();
        }
    }

    /// <summary>
    /// Clears from cursor to end of screen (ED 0).
    /// </summary>
    public void ClearToEnd()
    {
        lock (_lock)
        {
            // Clear from cursor to end of current line
            var lineIndex = GetAbsoluteLineIndex(CursorY);
            if (lineIndex >= 0 && lineIndex < _lines.Count)
            {
                _lines[lineIndex].Clear(CursorX, Columns - 1);
            }

            // Clear all lines below
            for (int y = CursorY + 1; y < Rows; y++)
            {
                var idx = GetAbsoluteLineIndex(y);
                if (idx >= 0 && idx < _lines.Count)
                {
                    _lines[idx].Clear();
                }
            }

            OnContentChanged();
        }
    }

    /// <summary>
    /// Clears from beginning of screen to cursor (ED 1).
    /// </summary>
    public void ClearToBeginning()
    {
        lock (_lock)
        {
            // Clear all lines above
            for (int y = 0; y < CursorY; y++)
            {
                var idx = GetAbsoluteLineIndex(y);
                if (idx >= 0 && idx < _lines.Count)
                {
                    _lines[idx].Clear();
                }
            }

            // Clear from beginning of current line to cursor
            var lineIndex = GetAbsoluteLineIndex(CursorY);
            if (lineIndex >= 0 && lineIndex < _lines.Count)
            {
                _lines[lineIndex].Clear(0, CursorX);
            }

            OnContentChanged();
        }
    }

    /// <summary>
    /// Clears the current line (EL 2).
    /// </summary>
    public void ClearLine()
    {
        lock (_lock)
        {
            var lineIndex = GetAbsoluteLineIndex(CursorY);
            if (lineIndex >= 0 && lineIndex < _lines.Count)
            {
                _lines[lineIndex].Clear();
            }
            OnContentChanged();
        }
    }

    /// <summary>
    /// Clears from cursor to end of line (EL 0).
    /// </summary>
    public void ClearLineToEnd()
    {
        lock (_lock)
        {
            var lineIndex = GetAbsoluteLineIndex(CursorY);
            if (lineIndex >= 0 && lineIndex < _lines.Count)
            {
                _lines[lineIndex].Clear(CursorX, Columns - 1);
            }
            OnContentChanged();
        }
    }

    /// <summary>
    /// Clears from beginning of line to cursor (EL 1).
    /// </summary>
    public void ClearLineToBeginning()
    {
        lock (_lock)
        {
            var lineIndex = GetAbsoluteLineIndex(CursorY);
            if (lineIndex >= 0 && lineIndex < _lines.Count)
            {
                _lines[lineIndex].Clear(0, CursorX);
            }
            OnContentChanged();
        }
    }

    #endregion

    #region Line Access

    /// <summary>
    /// Gets a visible line by screen row index.
    /// </summary>
    /// <param name="screenRow">Screen row (0-indexed).</param>
    /// <returns>The line at the specified row, or <c>null</c> if out of range.</returns>
    public TerminalLine? GetLine(int screenRow)
    {
        lock (_lock)
        {
            var index = GetAbsoluteLineIndex(screenRow);
            return index >= 0 && index < _lines.Count ? _lines[index] : null;
        }
    }

    /// <summary>
    /// Gets all visible lines as a list.
    /// </summary>
    /// <returns>A copy of the visible lines.</returns>
    /// <remarks>Returns a copy to prevent race conditions during rendering.</remarks>
    public IReadOnlyList<TerminalLine> GetVisibleLines()
    {
        lock (_lock)
        {
            var result = new List<TerminalLine>(Rows);
            for (int i = 0; i < Rows; i++)
            {
                var index = GetAbsoluteLineIndex(i);
                if (index >= 0 && index < _lines.Count)
                {
                    result.Add(_lines[index]);
                }
            }
            return result;
        }
    }

    #endregion

    #region Text Extraction

    /// <summary>
    /// Gets the text content of a selection.
    /// </summary>
    /// <param name="selection">The selection to extract, or <c>null</c> for all visible text.</param>
    /// <returns>The selected text content.</returns>
    public string GetSelectedText(TerminalSelection? selection = null)
    {
        lock (_lock)
        {
            if (selection == null)
            {
                return GetAllText();
            }

            var norm = selection.Normalized;
            var sb = new StringBuilder();

            for (int lineNum = norm.StartLine; lineNum <= norm.EndLine; lineNum++)
            {
                if (lineNum < 0 || lineNum >= _lines.Count)
                    continue;

                var line = _lines[lineNum];
                var startCol = lineNum == norm.StartLine ? norm.StartColumn : 0;
                var endCol = lineNum == norm.EndLine ? norm.EndColumn : line.Length - 1;

                if (selection.IsBlock)
                {
                    // Block selection: use same columns for all lines
                    startCol = Math.Min(norm.StartColumn, norm.EndColumn);
                    endCol = Math.Max(norm.StartColumn, norm.EndColumn);
                }

                for (int col = startCol; col <= endCol && col < line.Length; col++)
                {
                    var cell = line[col];
                    if (!cell.IsContinuation)
                    {
                        sb.Append(cell.Character.ToString());
                    }
                }

                if (lineNum < norm.EndLine && !selection.IsBlock)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets all text content from the buffer.
    /// </summary>
    /// <returns>The complete buffer text content.</returns>
    public string GetAllText()
    {
        lock (_lock)
        {
            var sb = new StringBuilder();
            foreach (var line in _lines)
            {
                sb.AppendLine(line.GetText());
            }
            return sb.ToString().TrimEnd();
        }
    }

    #endregion

    #region Reset

    /// <summary>
    /// Resets the buffer to initial state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            CursorX = 0;
            CursorY = 0;
            CursorVisible = true;
            ScrollOffset = 0;
            CurrentAttributes = TerminalAttributes.Default;
            SavedCursor = (0, 0);
            SavedAttributes = TerminalAttributes.Default;
            ScrollRegionTop = 0;
            ScrollRegionBottom = Rows;
            OriginMode = false;
            AutoWrapMode = true;

            InitializeLines();
            OnContentChanged();
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Converts a screen row index to an absolute line index.
    /// </summary>
    private int GetAbsoluteLineIndex(int screenRow)
    {
        // Screen rows are at the end of the line list
        // Apply scroll offset for viewing scrollback
        return _lines.Count - Rows + screenRow - ScrollOffset;
    }

    /// <summary>
    /// Trims scrollback to maximum limit.
    /// </summary>
    private void TrimScrollback()
    {
        var maxTotal = Rows + _maxScrollback;
        while (_lines.Count > maxTotal && _lines.Count > 0)
        {
            _lines.RemoveAt(0);
        }
    }

    /// <summary>
    /// Raises the ContentChanged event.
    /// </summary>
    private void OnContentChanged()
    {
        ContentChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
