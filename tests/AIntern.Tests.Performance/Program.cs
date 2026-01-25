// ============================================================================
// File: Program.cs
// Path: tests/AIntern.Tests.Performance/Program.cs
// Description: Entry point for running performance benchmarks.
// Version: v0.5.5j
// ============================================================================

using BenchmarkDotNet.Running;

namespace AIntern.Tests.Performance;

/// <summary>
/// Entry point for running performance benchmarks.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
