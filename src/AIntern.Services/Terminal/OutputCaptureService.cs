// -----------------------------------------------------------------------
// <copyright file="OutputCaptureService.cs" company="AIntern">
//     Copyright (c) AIntern. All rights reserved.
//     Licensed under the MIT license. See LICENSE file in the project root.
// </copyright>
// -----------------------------------------------------------------------

namespace AIntern.Services.Terminal;

using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ OUTPUT CAPTURE SERVICE (v0.5.4d)                                        │
// │ Captures terminal output for AI context with ANSI stripping, truncation.│
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Captures terminal output for AI context.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.4d.</para>
/// <para>
/// This service provides:
/// </para>
/// <list type="bullet">
/// <item>
///     <term>Stream capture</term>
///     <description>
///     Accumulates output during command execution via terminal output events.
///     </description>
/// </item>
/// <item>
///     <term>Buffer capture</term>
///     <description>
///     On-demand capture of current terminal buffer content.
///     </description>
/// </item>
/// <item>
///     <term>Output processing</term>
///     <description>
///     ANSI stripping, line ending normalization, and intelligent truncation.
///     </description>
/// </item>
/// <item>
///     <term>History management</term>
///     <description>
///     Per-session capture history with automatic pruning.
///     </description>
/// </item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> All state is stored in concurrent collections for
/// safe access from terminal output events and service methods.
/// </para>
/// </remarks>
public sealed partial class OutputCaptureService : IOutputCaptureService
{
    // ═══════════════════════════════════════════════════════════════════════
    // DEPENDENCIES
    // ═══════════════════════════════════════════════════════════════════════

    private readonly ITerminalService _terminalService;
    private readonly ILogger<OutputCaptureService> _logger;

    // ═══════════════════════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Active stream captures, indexed by session ID.
    /// </summary>
    /// <remarks>
    /// Only one capture can be active per session at a time.
    /// </remarks>
    private readonly ConcurrentDictionary<Guid, CaptureContext> _activeCaptures = new();

    /// <summary>
    /// All captures, indexed by capture ID for direct lookup.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, TerminalOutputCapture> _captureHistory = new();

    /// <summary>
    /// Per-session capture history (queue of capture IDs), for ordered retrieval.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, Queue<Guid>> _sessionCaptureHistory = new();

    /// <summary>
    /// Current configuration settings.
    /// </summary>
    private OutputCaptureSettings _settings = new();

    // ═══════════════════════════════════════════════════════════════════════
    // REGEX (Source-Generated for performance)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Regex pattern to match ANSI escape sequences (CSI and OSC sequences).
    /// </summary>
    /// <remarks>
    /// <para>Matches:</para>
    /// <list type="bullet">
    /// <item>
    ///     <description>
    ///     CSI sequences: \x1B[...m (colors, cursor, etc.)
    ///     </description>
    /// </item>
    /// <item>
    ///     <description>
    ///     OSC sequences: \x1B]...BEL or \x1B]...ST (titles, URLs, etc.)
    ///     </description>
    /// </item>
    /// </list>
    /// </remarks>
    [GeneratedRegex(@"\x1B\[[0-9;]*[a-zA-Z]|\x1B\].*?(?:\x07|\x1B\\)", RegexOptions.Compiled)]
    private static partial Regex AnsiEscapePattern();

    // ═══════════════════════════════════════════════════════════════════════
    // PROPERTIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public OutputCaptureSettings Settings => _settings;

    // ═══════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputCaptureService"/> class.
    /// </summary>
    /// <param name="terminalService">Terminal service for buffer access.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public OutputCaptureService(
        ITerminalService terminalService,
        ILogger<OutputCaptureService> logger)
    {
        _terminalService = terminalService;
        _logger = logger;

        // Subscribe to terminal output for stream capture
        _terminalService.OutputReceived += OnOutputReceived;

        _logger.LogDebug("OutputCaptureService initialized");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONFIGURATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public void Configure(OutputCaptureSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _logger.LogDebug(
            "Output capture configured: MaxLength={Length}, MaxLines={Lines}, Mode={Mode}, StripAnsi={StripAnsi}",
            settings.MaxCaptureLength,
            settings.MaxCaptureLines,
            settings.TruncationMode,
            settings.StripAnsiSequences);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // STREAM CAPTURE
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public void StartCapture(Guid sessionId, string? commandContext = null)
    {
        var context = new CaptureContext
        {
            SessionId = sessionId,
            CommandContext = commandContext,
            StartedAt = DateTime.UtcNow,
            Buffer = new StringBuilder()
        };

        // If there's an existing capture, log warning but proceed (overwrite)
        if (_activeCaptures.TryGetValue(sessionId, out _))
        {
            _logger.LogWarning(
                "Starting new capture for session {Session} while previous capture was active (discarded)",
                sessionId);
        }

        _activeCaptures[sessionId] = context;

        var commandPreview = commandContext?.Substring(0, Math.Min(50, commandContext?.Length ?? 0));
        _logger.LogDebug(
            "Started capture for session {Session}, command: {Command}",
            sessionId, commandPreview);
    }

    /// <inheritdoc/>
    public bool IsCaptureActive(Guid sessionId)
    {
        return _activeCaptures.ContainsKey(sessionId);
    }

    /// <summary>
    /// Handles terminal output events for stream capture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is called from the terminal service's output thread.
    /// We only append to the buffer here to minimize processing time.
    /// </para>
    /// </remarks>
    private void OnOutputReceived(object? sender, TerminalOutputEventArgs e)
    {
        // Only capture if there's an active capture for this session
        if (_activeCaptures.TryGetValue(e.SessionId, out var context))
        {
            context.Buffer.Append(e.Data);
            _logger.LogTrace("Captured {Length} bytes for session {Session}",
                e.Data.Length, e.SessionId);
        }
    }

    /// <inheritdoc/>
    public Task<TerminalOutputCapture?> StopCaptureAsync(Guid sessionId, CancellationToken ct = default)
    {
        // Try to remove the active capture
        if (!_activeCaptures.TryRemove(sessionId, out var context))
        {
            _logger.LogDebug("No active capture to stop for session {Session}", sessionId);
            return Task.FromResult<TerminalOutputCapture?>(null);
        }

        // Get raw output from buffer
        var rawOutput = context.Buffer.ToString();
        var originalLength = rawOutput.Length;

        // Process output (strip ANSI, normalize, truncate)
        var processedOutput = ProcessOutput(rawOutput);

        // Get session info for context
        var session = FindSession(sessionId);

        // Create capture result
        var capture = new TerminalOutputCapture
        {
            SessionId = sessionId,
            SessionName = session?.Name,
            Command = context.CommandContext,
            Output = processedOutput,
            IsTruncated = processedOutput.Contains("(truncated)"),
            OriginalLength = originalLength,
            StartedAt = context.StartedAt,
            CompletedAt = DateTime.UtcNow,
            WorkingDirectory = session?.WorkingDirectory,
            CaptureMode = OutputCaptureMode.LastCommand
        };

        // Add to history
        AddToCaptureHistory(sessionId, capture);

        _logger.LogDebug(
            "Stopped capture for session {Session}: {Length} chars (original: {Original}, truncated: {Truncated})",
            sessionId, capture.Output.Length, originalLength, capture.IsTruncated);

        return Task.FromResult<TerminalOutputCapture?>(capture);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BUFFER CAPTURE
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public Task<TerminalOutputCapture> CaptureBufferAsync(
        Guid sessionId,
        OutputCaptureMode mode = OutputCaptureMode.FullBuffer,
        int? lineCount = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Capturing buffer for session {Session}, mode={Mode}, lineCount={LineCount}",
            sessionId, mode, lineCount);

        // Get buffer from terminal service
        var buffer = _terminalService.GetBuffer(sessionId);
        if (buffer == null)
        {
            _logger.LogWarning("No buffer found for session {Session}", sessionId);
            throw new InvalidOperationException($"No buffer found for session {sessionId}");
        }

        // Get session info
        var session = FindSession(sessionId);

        // Extract content based on mode
        string rawOutput = mode switch
        {
            OutputCaptureMode.FullBuffer => buffer.GetAllText(),
            OutputCaptureMode.LastNLines => GetLastLines(buffer.GetAllText(), lineCount ?? 50),
            OutputCaptureMode.Selection => buffer.GetSelectedText() ?? string.Empty,
            _ => buffer.GetAllText()
        };

        var originalLength = rawOutput.Length;

        // Process output
        var processedOutput = ProcessOutput(rawOutput);

        // Create capture
        var capture = new TerminalOutputCapture
        {
            SessionId = sessionId,
            SessionName = session?.Name,
            Command = null,
            Output = processedOutput,
            IsTruncated = processedOutput.Contains("(truncated)"),
            OriginalLength = originalLength,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            WorkingDirectory = session?.WorkingDirectory,
            CaptureMode = mode
        };

        // Add to history
        AddToCaptureHistory(sessionId, capture);

        _logger.LogDebug("Captured {Mode} for session {Session}: {Length} chars",
            mode, sessionId, capture.Output.Length);

        return Task.FromResult(capture);
    }

    /// <inheritdoc/>
    public Task<TerminalOutputCapture?> CaptureSelectionAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Capturing selection for session {Session}", sessionId);

        // Get buffer
        var buffer = _terminalService.GetBuffer(sessionId);
        if (buffer == null)
        {
            _logger.LogWarning("No buffer found for session {Session}", sessionId);
            return Task.FromResult<TerminalOutputCapture?>(null);
        }

        // Get selected text
        var selectedText = buffer.GetSelectedText();
        if (string.IsNullOrEmpty(selectedText))
        {
            _logger.LogDebug("No selection to capture for session {Session}", sessionId);
            return Task.FromResult<TerminalOutputCapture?>(null);
        }

        var originalLength = selectedText.Length;
        var session = FindSession(sessionId);

        // Process output
        var processedOutput = ProcessOutput(selectedText);

        // Create capture
        var capture = new TerminalOutputCapture
        {
            SessionId = sessionId,
            SessionName = session?.Name,
            Command = null,
            Output = processedOutput,
            IsTruncated = processedOutput.Contains("(truncated)"),
            OriginalLength = originalLength,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            WorkingDirectory = session?.WorkingDirectory,
            CaptureMode = OutputCaptureMode.Selection
        };

        // Add to history
        AddToCaptureHistory(sessionId, capture);

        _logger.LogDebug("Captured selection for session {Session}: {Length} chars",
            sessionId, capture.Output.Length);

        return Task.FromResult<TerminalOutputCapture?>(capture);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HISTORY
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public IReadOnlyList<TerminalOutputCapture> GetRecentCaptures(Guid sessionId, int count = 10)
    {
        if (!_sessionCaptureHistory.TryGetValue(sessionId, out var queue))
        {
            _logger.LogDebug("No capture history for session {Session}", sessionId);
            return Array.Empty<TerminalOutputCapture>();
        }

        // Get capture IDs and look up in history
        // Queue is in order of capture, but we want most recent first
        var captures = queue
            .Reverse()
            .Take(count)
            .Select(id => _captureHistory.GetValueOrDefault(id))
            .Where(c => c != null)
            .Cast<TerminalOutputCapture>()
            .ToList();

        _logger.LogTrace("Retrieved {Count} recent captures for session {Session}",
            captures.Count, sessionId);

        return captures.AsReadOnly();
    }

    /// <inheritdoc/>
    public TerminalOutputCapture? GetCapture(Guid captureId)
    {
        return _captureHistory.GetValueOrDefault(captureId);
    }

    /// <inheritdoc/>
    public void ClearHistory(Guid sessionId)
    {
        if (_sessionCaptureHistory.TryRemove(sessionId, out var queue))
        {
            // Remove all captures for this session
            foreach (var captureId in queue)
            {
                _captureHistory.TryRemove(captureId, out _);
            }
            _logger.LogDebug("Cleared capture history for session {Session}", sessionId);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // OUTPUT PROCESSING
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Processes raw terminal output according to settings.
    /// </summary>
    /// <param name="output">Raw terminal output.</param>
    /// <returns>Processed output.</returns>
    private string ProcessOutput(string output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return string.Empty;
        }

        _logger.LogTrace("Processing output: {Length} chars, StripAnsi={StripAnsi}, Normalize={Normalize}",
            output.Length, _settings.StripAnsiSequences, _settings.NormalizeLineEndings);

        // Step 1: Strip ANSI escape sequences if configured
        if (_settings.StripAnsiSequences)
        {
            output = StripAnsiSequences(output);
        }

        // Step 2: Normalize line endings if configured
        if (_settings.NormalizeLineEndings)
        {
            output = NormalizeLineEndings(output);
        }

        // Step 3: Truncate if exceeds limits
        output = TruncateOutput(output);

        // Step 4: Trim whitespace
        return output.Trim();
    }

    /// <summary>
    /// Strips ANSI escape sequences from output.
    /// </summary>
    /// <param name="output">Raw output with ANSI codes.</param>
    /// <returns>Clean output without ANSI codes.</returns>
    private string StripAnsiSequences(string output)
    {
        var result = AnsiEscapePattern().Replace(output, string.Empty);
        _logger.LogTrace("Stripped ANSI: {Original} → {Result} chars",
            output.Length, result.Length);
        return result;
    }

    /// <summary>
    /// Normalizes line endings to LF (\n).
    /// </summary>
    /// <param name="output">Output with mixed line endings.</param>
    /// <returns>Output with uniform LF line endings.</returns>
    private static string NormalizeLineEndings(string output)
    {
        return output.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>
    /// Truncates output according to settings.
    /// </summary>
    /// <param name="output">Processed output.</param>
    /// <returns>Truncated output (if needed).</returns>
    private string TruncateOutput(string output)
    {
        var wasTruncated = false;

        // Step 1: Check line limit
        var lines = output.Split('\n');
        if (lines.Length > _settings.MaxCaptureLines)
        {
            _logger.LogDebug("Truncating lines: {Count} > {Max}",
                lines.Length, _settings.MaxCaptureLines);

            wasTruncated = true;
            lines = _settings.TruncationMode switch
            {
                TruncationMode.KeepStart => lines.Take(_settings.MaxCaptureLines).ToArray(),
                TruncationMode.KeepEnd => lines.Skip(lines.Length - _settings.MaxCaptureLines).ToArray(),
                TruncationMode.KeepBoth => KeepBothEnds(lines, _settings.MaxCaptureLines),
                _ => lines.Take(_settings.MaxCaptureLines).ToArray()
            };

            output = string.Join('\n', lines);
        }

        // Step 2: Check character limit
        if (output.Length > _settings.MaxCaptureLength)
        {
            _logger.LogDebug("Truncating chars: {Length} > {Max}",
                output.Length, _settings.MaxCaptureLength);

            wasTruncated = true;
            var max = _settings.MaxCaptureLength;

            output = _settings.TruncationMode switch
            {
                TruncationMode.KeepStart => output[..max] + "\n...(truncated)",
                TruncationMode.KeepEnd => "...(truncated)\n" + output[^max..],
                TruncationMode.KeepBoth => output[..(max / 2)] + "\n...(truncated)...\n" + output[^(max / 2)..],
                _ => output[..max] + "\n...(truncated)"
            };
        }

        if (wasTruncated)
        {
            _logger.LogTrace("Output truncated to {Length} chars", output.Length);
        }

        return output;
    }

    /// <summary>
    /// Keeps both the beginning and end of lines, truncating the middle.
    /// </summary>
    /// <param name="lines">All lines.</param>
    /// <param name="maxLines">Maximum lines to keep.</param>
    /// <returns>Lines with middle truncated.</returns>
    private static string[] KeepBothEnds(string[] lines, int maxLines)
    {
        var keepCount = maxLines / 2;
        var start = lines.Take(keepCount);
        var end = lines.Skip(lines.Length - keepCount);
        return start.Concat(new[] { "...(truncated)..." }).Concat(end).ToArray();
    }

    /// <summary>
    /// Extracts the last N lines from text.
    /// </summary>
    /// <param name="text">Full text.</param>
    /// <param name="lineCount">Number of lines to keep.</param>
    /// <returns>Last N lines.</returns>
    private static string GetLastLines(string text, int lineCount)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var lines = text.Split('\n');
        if (lines.Length <= lineCount)
        {
            return text;
        }

        return string.Join('\n', lines.Skip(lines.Length - lineCount));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HISTORY MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds a capture to the history with automatic pruning.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <param name="capture">Capture to add.</param>
    private void AddToCaptureHistory(Guid sessionId, TerminalOutputCapture capture)
    {
        // Add to global capture history
        _captureHistory[capture.Id] = capture;

        // Add to session history queue
        var queue = _sessionCaptureHistory.GetOrAdd(sessionId, _ => new Queue<Guid>());
        queue.Enqueue(capture.Id);

        // Prune old captures if over limit
        while (queue.Count > _settings.CaptureHistorySize)
        {
            if (queue.TryDequeue(out var oldId))
            {
                _captureHistory.TryRemove(oldId, out _);
                _logger.LogTrace("Pruned old capture {CaptureId} from history", oldId);
            }
        }

        _logger.LogTrace("Added capture {CaptureId} to history (session {Session}, {Count} in history)",
            capture.Id, sessionId, queue.Count);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Finds a terminal session by ID.
    /// </summary>
    /// <param name="sessionId">Session ID.</param>
    /// <returns>Session info, or null if not found.</returns>
    private TerminalSession? FindSession(Guid sessionId)
    {
        var sessions = _terminalService.Sessions;
        return sessions?.FirstOrDefault(s => s.Id == sessionId);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // NESTED TYPES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Context for an active stream capture.
    /// </summary>
    private sealed class CaptureContext
    {
        /// <summary>Session being captured.</summary>
        public Guid SessionId { get; init; }

        /// <summary>Optional command text for context.</summary>
        public string? CommandContext { get; init; }

        /// <summary>When capture started.</summary>
        public DateTime StartedAt { get; init; }

        /// <summary>Buffer accumulating output.</summary>
        public StringBuilder Buffer { get; init; } = new();
    }
}
