using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AIntern.Services;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Services.Tests;

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ EDITOR REFRESH SERVICE TESTS (v0.4.3i)                                   │
// │ Unit tests for EditorRefreshService.                                     │
// └─────────────────────────────────────────────────────────────────────────┘

public sealed class EditorRefreshServiceTests : IDisposable
{
    private readonly EditorRefreshService _service;
    private readonly Mock<ILogger<EditorRefreshService>> _mockLogger;

    public EditorRefreshServiceTests()
    {
        _mockLogger = new Mock<ILogger<EditorRefreshService>>();
        _service = new EditorRefreshService(_mockLogger.Object);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_WithoutDependencies_CreatesInstance()
    {
        // Act
        using var service = new EditorRefreshService();

        // Assert
        Assert.NotNull(service);
        Assert.False(service.IsSuspended);
    }

    [Fact]
    public void Constructor_WithLogger_CreatesInstance()
    {
        // Act
        using var service = new EditorRefreshService(_mockLogger.Object);

        // Assert
        Assert.NotNull(service);
        Assert.False(service.IsSuspended);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // RequestRefresh Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void RequestRefresh_ValidPath_RaisesEvent()
    {
        // Arrange
        EditorRefreshEventArgs? capturedArgs = null;
        _service.RefreshRequested += (s, e) => capturedArgs = e;

        // Act
        _service.RequestRefresh("/path/to/file.cs", RefreshReason.FileModified);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal("/path/to/file.cs", capturedArgs!.FilePath);
        Assert.Equal(RefreshReason.FileModified, capturedArgs.Reason);
        Assert.False(capturedArgs.IsUserInitiated);
    }

    [Fact]
    public void RequestRefresh_WithContent_IncludesContent()
    {
        // Arrange
        EditorRefreshEventArgs? capturedArgs = null;
        _service.RefreshRequested += (s, e) => capturedArgs = e;
        const string content = "public class Test { }";

        // Act
        _service.RequestRefresh("/path/to/file.cs", RefreshReason.FileModified, content);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(content, capturedArgs!.NewContent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequestRefresh_EmptyPath_ThrowsException(string? path)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.RequestRefresh(path!, RefreshReason.FileModified));
    }

    [Theory]
    [InlineData(RefreshReason.FileModified)]
    [InlineData(RefreshReason.Undone)]
    [InlineData(RefreshReason.ExternalChange)]
    [InlineData(RefreshReason.FileCreated)]
    [InlineData(RefreshReason.FileDeleted)]
    public void RequestRefresh_AllReasons_HandledCorrectly(RefreshReason reason)
    {
        // Arrange
        EditorRefreshEventArgs? capturedArgs = null;
        _service.RefreshRequested += (s, e) => capturedArgs = e;

        // Act
        _service.RequestRefresh("/path/to/file.cs", reason);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(reason, capturedArgs!.Reason);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Suspension Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void SuspendNotifications_SetsIsSuspended()
    {
        // Act
        var scope = _service.SuspendNotifications();

        // Assert
        Assert.True(_service.IsSuspended);
        scope.Dispose();
    }

    [Fact]
    public void SuspendNotifications_QueuesEvents()
    {
        // Arrange
        var eventCount = 0;
        _service.RefreshRequested += (s, e) => eventCount++;

        // Act - suspend and request
        using (_service.SuspendNotifications())
        {
            _service.RequestRefresh("/file1.cs", RefreshReason.FileModified);
            _service.RequestRefresh("/file2.cs", RefreshReason.FileModified);

            // Assert - no events during suspension
            Assert.Equal(0, eventCount);
        }

        // Assert - events raised after resume
        Assert.Equal(2, eventCount);
    }

    [Fact]
    public void SuspendNotifications_CoalescesDuplicates()
    {
        // Arrange
        var receivedFiles = new List<string>();
        _service.RefreshRequested += (s, e) => receivedFiles.Add(e.FilePath);

        // Act - suspend and request same file multiple times
        using (_service.SuspendNotifications())
        {
            _service.RequestRefresh("/file1.cs", RefreshReason.FileModified, "content1");
            _service.RequestRefresh("/file1.cs", RefreshReason.FileModified, "content2");
            _service.RequestRefresh("/file1.cs", RefreshReason.FileModified, "content3");
        }

        // Assert - only one event for the file (last one)
        Assert.Single(receivedFiles);
        Assert.Equal("/file1.cs", receivedFiles[0]);
    }

    [Fact]
    public void SuspendNotifications_NestedSuspension_RequiresAllResumes()
    {
        // Arrange
        var eventCount = 0;
        _service.RefreshRequested += (s, e) => eventCount++;

        // Act - nested suspension
        var outer = _service.SuspendNotifications();
        var inner = _service.SuspendNotifications();

        _service.RequestRefresh("/file.cs", RefreshReason.FileModified);

        inner.Dispose(); // First dispose
        Assert.Equal(0, eventCount); // Still suspended

        outer.Dispose(); // Second dispose
        Assert.Equal(1, eventCount); // Now events are raised
    }

    [Fact]
    public void SuspendNotifications_DisposingTwice_IsSafe()
    {
        // Arrange
        var scope = _service.SuspendNotifications();

        // Act & Assert - no exception
        scope.Dispose();
        scope.Dispose();
    }

    [Fact]
    public void Resume_WithMultipleFiles_ProcessesAllUnique()
    {
        // Arrange
        var receivedFiles = new HashSet<string>();
        _service.RefreshRequested += (s, e) => receivedFiles.Add(e.FilePath);

        // Act
        using (_service.SuspendNotifications())
        {
            _service.RequestRefresh("/file1.cs", RefreshReason.FileModified);
            _service.RequestRefresh("/file2.cs", RefreshReason.FileModified);
            _service.RequestRefresh("/file3.cs", RefreshReason.FileModified);
        }

        // Assert
        Assert.Equal(3, receivedFiles.Count);
        Assert.Contains("/file1.cs", receivedFiles);
        Assert.Contains("/file2.cs", receivedFiles);
        Assert.Contains("/file3.cs", receivedFiles);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsSuspended Property Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsSuspended_InitiallyFalse()
    {
        Assert.False(_service.IsSuspended);
    }

    [Fact]
    public void IsSuspended_TrueAfterSuspend()
    {
        // Act
        using var scope = _service.SuspendNotifications();

        // Assert
        Assert.True(_service.IsSuspended);
    }

    [Fact]
    public void IsSuspended_FalseAfterResume()
    {
        // Act
        var scope = _service.SuspendNotifications();
        scope.Dispose();

        // Assert
        Assert.False(_service.IsSuspended);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dispose Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Dispose_MultipleCallsAreSafe()
    {
        // Arrange
        using var service = new EditorRefreshService();

        // Act & Assert - no exception
        service.Dispose();
        service.Dispose();
    }
}
