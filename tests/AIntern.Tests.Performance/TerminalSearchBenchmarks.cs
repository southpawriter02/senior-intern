// ============================================================================
// File: TerminalSearchBenchmarks.cs
// Path: tests/AIntern.Tests.Performance/TerminalSearchBenchmarks.cs
// Description: Performance benchmarks for terminal search operations.
// Version: v0.5.5j
// ============================================================================

namespace AIntern.Tests.Performance;

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using AIntern.Core.Models.Terminal;
using AIntern.Services.Terminal;
using AIntern.Tests.Integration.Mocks;

/// <summary>
/// Performance benchmarks for terminal search operations.
/// Measures search performance at various buffer sizes.
/// </summary>
/// <remarks>Added in v0.5.5j.</remarks>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class TerminalSearchBenchmarks
{
    // ═══════════════════════════════════════════════════════════════════════
    // Fields
    // ═══════════════════════════════════════════════════════════════════════

    private TerminalSearchService _searchService = null!;
    private TerminalBuffer _smallBuffer = null!;
    private TerminalBuffer _mediumBuffer = null!;
    private TerminalBuffer _largeBuffer = null!;
    private TerminalSearchOptions _defaultOptions = null!;
    private TerminalSearchState _defaultState = null!;

    // ═══════════════════════════════════════════════════════════════════════
    // Setup
    // ═══════════════════════════════════════════════════════════════════════

    [GlobalSetup]
    public void Setup()
    {
        _searchService = new TerminalSearchService(
            NullLogger<TerminalSearchService>.Instance);

        _smallBuffer = CreateBuffer(1000);
        _mediumBuffer = CreateBuffer(10000);
        _largeBuffer = CreateBuffer(100000);

        _defaultOptions = TerminalSearchOptions.Default;
        _defaultState = TerminalSearchState.Empty;
    }

    private static TerminalBuffer CreateBuffer(int lineCount)
    {
        var mockBuffer = new MockTerminalBuffer();
        for (int i = 0; i < lineCount; i++)
        {
            var hasError = i % 100 == 0;
            mockBuffer.AddLine(
                $"[{DateTime.Now:HH:mm:ss}] Log line {i}: " +
                $"Some content here{(hasError ? " ERROR[123]" : "")}");
        }
        return mockBuffer.ToTerminalBuffer();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Plain Text Search Benchmarks
    // ═══════════════════════════════════════════════════════════════════════

    [Benchmark]
    public async Task Search_PlainText_1kLines()
    {
        await _searchService.SearchAsync(
            _smallBuffer,
            "error",
            _defaultState,
            _defaultOptions);
    }

    [Benchmark]
    public async Task Search_PlainText_10kLines()
    {
        await _searchService.SearchAsync(
            _mediumBuffer,
            "error",
            _defaultState,
            _defaultOptions);
    }

    [Benchmark]
    public async Task Search_PlainText_100kLines()
    {
        await _searchService.SearchAsync(
            _largeBuffer,
            "error",
            _defaultState,
            _defaultOptions);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Regex Search Benchmarks
    // ═══════════════════════════════════════════════════════════════════════

    [Benchmark]
    public async Task Search_Regex_10kLines()
    {
        var state = _defaultState with { UseRegex = true };
        await _searchService.SearchAsync(
            _mediumBuffer,
            @"ERROR\[\d+\]",
            state,
            _defaultOptions);
    }

    [Benchmark]
    public async Task Search_Regex_100kLines()
    {
        var state = _defaultState with { UseRegex = true };
        await _searchService.SearchAsync(
            _largeBuffer,
            @"ERROR\[\d+\]",
            state,
            _defaultOptions);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Case Sensitive Search Benchmarks
    // ═══════════════════════════════════════════════════════════════════════

    [Benchmark]
    public async Task Search_CaseSensitive_100kLines()
    {
        var state = _defaultState with { CaseSensitive = true };
        await _searchService.SearchAsync(
            _largeBuffer,
            "ERROR",
            state,
            _defaultOptions);
    }
}
