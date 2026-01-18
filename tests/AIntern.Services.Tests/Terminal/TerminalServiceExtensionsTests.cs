using AIntern.Core.Interfaces;
using AIntern.Services.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AIntern.Services.Tests.Terminal;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ TERMINAL SERVICE EXTENSIONS TESTS (v0.5.1f)                             │
// │ Unit tests for DI registration extension methods.                       │
// └─────────────────────────────────────────────────────────────────────────┘

/// <summary>
/// Unit tests for <see cref="TerminalServiceExtensions"/>.
/// </summary>
/// <remarks>
/// <para>Added in v0.5.1f.</para>
/// <para>
/// These tests verify that the terminal service extension methods
/// correctly register services with the dependency injection container.
/// </para>
/// </remarks>
public class TerminalServiceExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────────
    // AddTerminalServices Tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Unit Test:</b> AddTerminalServices returns the service collection.<br/>
    /// <b>Arrange:</b> Empty service collection.<br/>
    /// <b>Act:</b> Call AddTerminalServices.<br/>
    /// <b>Assert:</b> Returns same service collection for chaining.
    /// </summary>
    [Fact]
    public void AddTerminalServices_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddTerminalServices();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    /// <b>Unit Test:</b> AddTerminalServices registers IShellDetectionService.<br/>
    /// <b>Arrange:</b> Service collection with logging.<br/>
    /// <b>Act:</b> Call AddTerminalServices and build provider.<br/>
    /// <b>Assert:</b> IShellDetectionService can be resolved.
    /// </summary>
    [Fact]
    public void AddTerminalServices_RegistersShellDetectionService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddTerminalServices();
        var provider = services.BuildServiceProvider();

        // Act
        var shellDetection = provider.GetService<IShellDetectionService>();

        // Assert
        Assert.NotNull(shellDetection);
        Assert.IsType<ShellDetectionService>(shellDetection);
    }

    /// <summary>
    /// <b>Unit Test:</b> AddTerminalServices registers ITerminalService.<br/>
    /// <b>Arrange:</b> Service collection with logging.<br/>
    /// <b>Act:</b> Call AddTerminalServices and build provider.<br/>
    /// <b>Assert:</b> ITerminalService can be resolved.
    /// </summary>
    [Fact]
    public void AddTerminalServices_RegistersTerminalService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddTerminalServices();
        var provider = services.BuildServiceProvider();

        // Act
        var terminalService = provider.GetService<ITerminalService>();

        // Assert
        Assert.NotNull(terminalService);
        Assert.IsType<TerminalService>(terminalService);
    }

    /// <summary>
    /// <b>Unit Test:</b> IShellDetectionService is registered as singleton.<br/>
    /// <b>Arrange:</b> Service collection with logging.<br/>
    /// <b>Act:</b> Resolve IShellDetectionService twice.<br/>
    /// <b>Assert:</b> Same instance returned.
    /// </summary>
    [Fact]
    public void AddTerminalServices_ShellDetectionService_IsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddTerminalServices();
        var provider = services.BuildServiceProvider();

        // Act
        var first = provider.GetService<IShellDetectionService>();
        var second = provider.GetService<IShellDetectionService>();

        // Assert
        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    /// <summary>
    /// <b>Unit Test:</b> ITerminalService is registered as singleton.<br/>
    /// <b>Arrange:</b> Service collection with logging.<br/>
    /// <b>Act:</b> Resolve ITerminalService twice.<br/>
    /// <b>Assert:</b> Same instance returned.
    /// </summary>
    [Fact]
    public void AddTerminalServices_TerminalService_IsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddTerminalServices();
        var provider = services.BuildServiceProvider();

        // Act
        var first = provider.GetService<ITerminalService>();
        var second = provider.GetService<ITerminalService>();

        // Assert
        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    /// <summary>
    /// <b>Unit Test:</b> ITerminalService has IShellDetectionService injected.<br/>
    /// <b>Arrange:</b> Service collection with logging.<br/>
    /// <b>Act:</b> Resolve both services.<br/>
    /// <b>Assert:</b> Both resolve successfully (ITerminalService depends on IShellDetectionService).
    /// </summary>
    [Fact]
    public void AddTerminalServices_TerminalServiceDependency_Resolves()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddTerminalServices();
        var provider = services.BuildServiceProvider();

        // Act - This would fail if IShellDetectionService wasn't registered
        var shellDetection = provider.GetRequiredService<IShellDetectionService>();
        var terminalService = provider.GetRequiredService<ITerminalService>();

        // Assert - Both resolved successfully
        Assert.NotNull(shellDetection);
        Assert.NotNull(terminalService);
    }

    /// <summary>
    /// <b>Unit Test:</b> Multiple calls to AddTerminalServices are safe.<br/>
    /// <b>Arrange:</b> Service collection.<br/>
    /// <b>Act:</b> Call AddTerminalServices twice.<br/>
    /// <b>Assert:</b> No exception, services registered.
    /// </summary>
    [Fact]
    public void AddTerminalServices_CalledMultipleTimes_NoException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        // Act - Should not throw
        services.AddTerminalServices();
        services.AddTerminalServices();
        var provider = services.BuildServiceProvider();

        // Assert - Can still resolve (last registration wins)
        var shellDetection = provider.GetService<IShellDetectionService>();
        Assert.NotNull(shellDetection);
    }

    /// <summary>
    /// <b>Unit Test:</b> Services can be composed with GetRequiredService.<br/>
    /// <b>Arrange:</b> Service collection with logging.<br/>
    /// <b>Act:</b> Use GetRequiredService to resolve.<br/>
    /// <b>Assert:</b> No exception thrown.
    /// </summary>
    [Fact]
    public void AddTerminalServices_GetRequiredService_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddTerminalServices();
        var provider = services.BuildServiceProvider();

        // Act & Assert - Should not throw InvalidOperationException
        var exception = Record.Exception(() =>
        {
            _ = provider.GetRequiredService<IShellDetectionService>();
            _ = provider.GetRequiredService<ITerminalService>();
        });

        Assert.Null(exception);
    }
}
