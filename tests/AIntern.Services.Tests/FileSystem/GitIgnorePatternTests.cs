using Xunit;
using AIntern.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace AIntern.Services.Tests.FileSystem;

/// <summary>
/// Unit tests for .gitignore pattern matching in <see cref="FileSystemService"/>.
/// </summary>
public class GitIgnorePatternTests : IDisposable
{
    private readonly FileSystemService _service;
    private readonly string _testDirectory;

    public GitIgnorePatternTests()
    {
        var logger = new Mock<ILogger<FileSystemService>>();
        _service = new FileSystemService(logger.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"GitIgnoreTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, true); }
            catch { /* Cleanup best effort */ }
        }
    }

    #region ShouldIgnore Tests

    [Fact]
    public void ShouldIgnore_MatchesDirectoryPattern()
    {
        // Arrange
        var nodeModulesDir = Path.Combine(_testDirectory, "node_modules");
        Directory.CreateDirectory(nodeModulesDir);
        var patterns = new List<string> { "node_modules/" };

        // Act
        var result = _service.ShouldIgnore(nodeModulesDir, _testDirectory, patterns);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldIgnore_MatchesFilePattern()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.user");
        File.WriteAllText(filePath, "test");
        var patterns = new List<string> { "*.user" };

        // Act
        var result = _service.ShouldIgnore(filePath, _testDirectory, patterns);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldIgnore_SkipsComments()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "test");
        var patterns = new List<string> { "# This is a comment", "*.txt" };

        // Act
        var result = _service.ShouldIgnore(filePath, _testDirectory, patterns);

        // Assert
        Assert.True(result); // Matched *.txt, ignored comment
    }

    [Fact]
    public void ShouldIgnore_HandleNegation()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "important.log");
        File.WriteAllText(filePath, "test");
        var patterns = new List<string> { "*.log", "!important.log" };

        // Act
        var result = _service.ShouldIgnore(filePath, _testDirectory, patterns);

        // Assert
        Assert.False(result); // Negation overrides
    }

    [Fact]
    public void ShouldIgnore_ReturnsFalseForNonMatching()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.cs");
        File.WriteAllText(filePath, "test");
        var patterns = new List<string> { "*.txt", "*.log" };

        // Act
        var result = _service.ShouldIgnore(filePath, _testDirectory, patterns);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldIgnore_EmptyPatterns_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "test");

        // Act
        var result = _service.ShouldIgnore(filePath, _testDirectory, []);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldIgnore_MatchesNestedDirectory()
    {
        // Arrange
        var binDir = Path.Combine(_testDirectory, "src", "bin");
        Directory.CreateDirectory(binDir);
        var patterns = new List<string> { "bin/" };

        // Act
        var result = _service.ShouldIgnore(binDir, _testDirectory, patterns);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region LoadGitIgnorePatternsAsync Tests

    [Fact]
    public async Task LoadGitIgnorePatternsAsync_LoadsGitignoreFile()
    {
        // Arrange
        var gitignorePath = Path.Combine(_testDirectory, ".gitignore");
        await File.WriteAllTextAsync(gitignorePath, "*.log\n# comment\ntemp/");

        // Act
        var patterns = await _service.LoadGitIgnorePatternsAsync(_testDirectory);

        // Assert
        Assert.Contains("*.log", patterns);
        Assert.Contains("# comment", patterns);
        Assert.Contains("temp/", patterns);
    }

    [Fact]
    public async Task LoadGitIgnorePatternsAsync_IncludesDefaults()
    {
        // Act (no .gitignore file)
        var patterns = await _service.LoadGitIgnorePatternsAsync(_testDirectory);

        // Assert - should include default patterns
        Assert.Contains(".git/", patterns);
        Assert.Contains("node_modules/", patterns);
        Assert.Contains("bin/", patterns);
        Assert.Contains("obj/", patterns);
    }

    #endregion
}
