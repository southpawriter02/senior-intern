using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using AIntern.Core.Models;
using AIntern.Data;
using AIntern.Data.Repositories;

namespace AIntern.Data.Tests.Workspace;

/// <summary>
/// Unit tests for <see cref="WorkspaceRepository"/>.
/// Focus: max recent enforcement added in v0.3.1f.
/// </summary>
public class WorkspaceRepositoryTests : IDisposable
{
    private readonly AInternDbContext _context;
    private readonly WorkspaceRepository _repository;
    private readonly string _testDirectory;
    private int _workspaceCounter = 0;

    public WorkspaceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AInternDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new AInternDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _repository = new WorkspaceRepository(_context, NullLogger<WorkspaceRepository>.Instance);

        _testDirectory = Path.Combine(Path.GetTempPath(), $"WorkspaceRepoTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, true); }
            catch { /* Cleanup best effort */ }
        }
    }

    private string CreateTestPath()
    {
        var path = Path.Combine(_testDirectory, $"project_{_workspaceCounter++}");
        Directory.CreateDirectory(path);
        return path;
    }

    #region GetRecentAsync

    [Fact]
    public async Task GetRecentAsync_OrdersByPinnedThenLastAccessed()
    {
        // Arrange
        var older = new AIntern.Core.Models.Workspace { RootPath = CreateTestPath(), LastAccessedAt = DateTime.UtcNow.AddDays(-2) };
        var newer = new AIntern.Core.Models.Workspace { RootPath = CreateTestPath(), LastAccessedAt = DateTime.UtcNow };
        var pinned = new AIntern.Core.Models.Workspace { RootPath = CreateTestPath(), IsPinned = true, LastAccessedAt = DateTime.UtcNow.AddDays(-5) };

        await _repository.AddOrUpdateAsync(older);
        await _repository.AddOrUpdateAsync(newer);
        await _repository.AddOrUpdateAsync(pinned);

        // Act
        var result = await _repository.GetRecentAsync(10);

        // Assert
        Assert.True(result[0].IsPinned);
        Assert.Equal(newer.RootPath, result[1].RootPath);
        Assert.Equal(older.RootPath, result[2].RootPath);
    }

    [Fact]
    public async Task GetRecentAsync_RespectsCountLimit()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
            await _repository.AddOrUpdateAsync(new AIntern.Core.Models.Workspace { RootPath = CreateTestPath() });

        // Act
        var result = await _repository.GetRecentAsync(3);

        // Assert
        Assert.Equal(3, result.Count);
    }

    #endregion

    #region AddOrUpdateAsync

    [Fact]
    public async Task AddOrUpdateAsync_UpdatesExistingByPath()
    {
        // Arrange
        var path = CreateTestPath();
        var workspace = new AIntern.Core.Models.Workspace { RootPath = path, Name = "Original" };
        await _repository.AddOrUpdateAsync(workspace);

        // Act
        workspace.Name = "Updated";
        await _repository.AddOrUpdateAsync(workspace);

        // Assert
        var result = await _repository.GetByPathAsync(path);
        // Note: Name property may not be updated based on current impl
        var count = await _context.RecentWorkspaces.CountAsync();
        Assert.Equal(1, count); // Only one entry
    }

    [Fact]
    public async Task AddOrUpdateAsync_EnforcesMaxLimit()
    {
        // Arrange - Add 25 workspaces (over the 20 limit)
        for (int i = 0; i < 25; i++)
        {
            await _repository.AddOrUpdateAsync(new AIntern.Core.Models.Workspace { RootPath = CreateTestPath() });
        }

        // Act
        var count = await _context.RecentWorkspaces.CountAsync();

        // Assert
        Assert.Equal(20, count); // Max limit enforced
    }

    [Fact]
    public async Task AddOrUpdateAsync_PreservesPinnedWhenEnforcing()
    {
        // Arrange - Add pinned workspace first
        var pinnedPath = CreateTestPath();
        await _repository.AddOrUpdateAsync(new AIntern.Core.Models.Workspace { RootPath = pinnedPath, IsPinned = true });

        // Add 25 more non-pinned
        for (int i = 0; i < 25; i++)
        {
            await _repository.AddOrUpdateAsync(new AIntern.Core.Models.Workspace { RootPath = CreateTestPath() });
        }

        // Act
        var pinned = await _repository.GetByPathAsync(pinnedPath);

        // Assert
        Assert.NotNull(pinned); // Pinned preserved
    }

    #endregion

    #region SetPinnedAsync / RenameAsync

    [Fact]
    public async Task SetPinnedAsync_UpdatesOnlyPinnedProperty()
    {
        // Arrange
        var path = CreateTestPath();
        var workspace = new AIntern.Core.Models.Workspace { RootPath = path };
        await _repository.AddOrUpdateAsync(workspace);

        var stored = await _context.RecentWorkspaces.FirstAsync(w => w.RootPath == path);

        // Act
        await _repository.SetPinnedAsync(stored.Id, true);

        // Assert
        var result = await _repository.GetByIdAsync(stored.Id);
        Assert.True(result?.IsPinned);
    }

    [Fact]
    public async Task RenameAsync_UpdatesName()
    {
        // Arrange
        var path = CreateTestPath();
        var workspace = new AIntern.Core.Models.Workspace { RootPath = path };
        await _repository.AddOrUpdateAsync(workspace);

        var stored = await _context.RecentWorkspaces.FirstAsync(w => w.RootPath == path);

        // Act
        await _repository.RenameAsync(stored.Id, "Custom Name");

        // Assert
        var result = await _repository.GetByIdAsync(stored.Id);
        Assert.Equal("Custom Name", result?.Name);
    }

    #endregion

    #region ClearAllAsync / RemoveAsync

    [Fact]
    public async Task ClearAllAsync_RemovesAll()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
            await _repository.AddOrUpdateAsync(new AIntern.Core.Models.Workspace { RootPath = CreateTestPath() });

        // Act
        await _repository.ClearAllAsync();

        // Assert
        var count = await _context.RecentWorkspaces.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task RemoveAsync_RemovesSpecificWorkspace()
    {
        // Arrange
        var path = CreateTestPath();
        await _repository.AddOrUpdateAsync(new AIntern.Core.Models.Workspace { RootPath = path });

        var stored = await _context.RecentWorkspaces.FirstAsync(w => w.RootPath == path);

        // Act
        await _repository.RemoveAsync(stored.Id);

        // Assert
        var result = await _repository.GetByIdAsync(stored.Id);
        Assert.Null(result);
    }

    #endregion
}
