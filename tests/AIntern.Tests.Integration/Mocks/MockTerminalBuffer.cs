// ============================================================================
// File: MockTerminalBuffer.cs
// Path: tests/AIntern.Tests.Integration/Mocks/MockTerminalBuffer.cs
// Description: Simple buffer implementation for search testing.
// Version: v0.5.5j
// ============================================================================

namespace AIntern.Tests.Integration.Mocks;

using AIntern.Core.Models.Terminal;

/// <summary>
/// Mock terminal buffer for testing search functionality.
/// Provides a simplified implementation without full terminal emulation.
/// </summary>
/// <remarks>Added in v0.5.5j.</remarks>
public sealed class MockTerminalBuffer
{
    // ═══════════════════════════════════════════════════════════════════════
    // Fields
    // ═══════════════════════════════════════════════════════════════════════

    private readonly List<string> _lines = new();

    // ═══════════════════════════════════════════════════════════════════════
    // Properties
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Gets the total number of lines in the buffer.</summary>
    public int LineCount => _lines.Count;

    /// <summary>Gets the column count (default 80).</summary>
    public int ColumnCount => 80;

    /// <summary>Gets the scrollback line count (same as line count for mock).</summary>
    public int ScrollbackLines => _lines.Count;

    // ═══════════════════════════════════════════════════════════════════════
    // Public Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds a single line to the buffer.
    /// </summary>
    /// <param name="line">The line content to add.</param>
    public void AddLine(string line)
    {
        _lines.Add(line ?? string.Empty);
    }

    /// <summary>
    /// Adds multiple lines to the buffer.
    /// </summary>
    /// <param name="lines">The lines to add.</param>
    public void AddLines(params string[] lines)
    {
        _lines.AddRange(lines.Select(l => l ?? string.Empty));
    }

    /// <summary>
    /// Gets a line by index.
    /// </summary>
    /// <param name="lineIndex">The zero-based line index.</param>
    /// <returns>The line content or empty string if out of range.</returns>
    public string GetLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _lines.Count)
            return string.Empty;
        return _lines[lineIndex];
    }

    /// <summary>
    /// Gets a range of lines.
    /// </summary>
    /// <param name="startLine">The starting line index.</param>
    /// <param name="count">Number of lines to retrieve.</param>
    /// <returns>Enumerable of line contents.</returns>
    public IEnumerable<string> GetLines(int startLine, int count)
    {
        return _lines.Skip(startLine).Take(count);
    }

    /// <summary>
    /// Clears all lines from the buffer.
    /// </summary>
    public void Clear()
    {
        _lines.Clear();
    }

    /// <summary>
    /// Gets text content within a specified range.
    /// </summary>
    /// <param name="startLine">Starting line index.</param>
    /// <param name="startColumn">Starting column index.</param>
    /// <param name="endLine">Ending line index.</param>
    /// <param name="endColumn">Ending column index.</param>
    /// <returns>The text content within the range.</returns>
    public string GetText(int startLine, int startColumn, int endLine, int endColumn)
    {
        if (startLine == endLine)
        {
            var line = GetLine(startLine);
            var start = Math.Min(startColumn, line.Length);
            var length = Math.Min(endColumn - startColumn, line.Length - start);
            return length > 0 ? line.Substring(start, length) : string.Empty;
        }

        var result = new System.Text.StringBuilder();
        for (int i = startLine; i <= endLine && i < _lines.Count; i++)
        {
            if (i > startLine)
                result.AppendLine();
            result.Append(_lines[i]);
        }
        return result.ToString();
    }

    /// <summary>
    /// Converts the mock buffer to a real TerminalBuffer for testing.
    /// </summary>
    /// <returns>A TerminalBuffer populated with this mock's content.</returns>
    public TerminalBuffer ToTerminalBuffer()
    {
        var buffer = new TerminalBuffer(ColumnCount, Math.Max(LineCount, 24));
        foreach (var line in _lines)
        {
            buffer.WriteString(line);
            buffer.LineFeed();
            buffer.CarriageReturn();
        }
        return buffer;
    }
}
