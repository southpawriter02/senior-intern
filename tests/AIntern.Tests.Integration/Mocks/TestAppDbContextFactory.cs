// ============================================================================
// File: TestAppDbContextFactory.cs
// Path: tests/AIntern.Tests.Integration/Mocks/TestAppDbContextFactory.cs
// Description: In-memory database context for testing history service.
// Version: v0.5.5j
// ============================================================================

namespace AIntern.Tests.Integration.Mocks;

using Microsoft.EntityFrameworkCore;
using AIntern.Data;

/// <summary>
/// In-memory database context factory for integration testing.
/// Creates fresh database instances for test isolation.
/// </summary>
/// <remarks>Added in v0.5.5j.</remarks>
public sealed class TestAppDbContextFactory : IDbContextFactory<AInternDbContext>, IDisposable
{
    // ═══════════════════════════════════════════════════════════════════════
    // Fields
    // ═══════════════════════════════════════════════════════════════════════

    private readonly DbContextOptions<AInternDbContext> _options;
    private bool _disposed;

    // ═══════════════════════════════════════════════════════════════════════
    // Constructor
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initializes a new test database factory with in-memory database.
    /// </summary>
    public TestAppDbContextFactory()
    {
        // Use unique database name for test isolation
        var databaseName = $"AInternTest_{Guid.NewGuid():N}";

        _options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        // Ensure database is created
        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IDbContextFactory Implementation
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public AInternDbContext CreateDbContext()
    {
        return new AInternDbContext(_options);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IDisposable Implementation
    // ═══════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;

        // Clean up in-memory database
        using var context = CreateDbContext();
        context.Database.EnsureDeleted();

        _disposed = true;
    }
}
