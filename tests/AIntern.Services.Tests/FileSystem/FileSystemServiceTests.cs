using Xunit;
using AIntern.Services;
using Microsoft.Extensions.Logging;
using Moq;
using AIntern.Core.Interfaces;

namespace AIntern.Services.Tests.FileSystem;

/// <summary>
/// Unit tests for <see cref="FileSystemService"/>.
/// </summary>
public class FileSystemServiceTests : IDisposable
{
    private readonly FileSystemService _service;
    private readonly string _testDirectory;

    public FileSystemServiceTests()
    {
        var logger = new Mock<ILogger<FileSystemService>>();
        _service = new FileSystemService(logger.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileSystemServiceTests_{Guid.NewGuid():N}");
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

    #region Directory Operations

    [Fact]
    public async Task GetDirectoryContentsAsync_ReturnsItems()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "file.txt"), "test");

        // Act
        var items = await _service.GetDirectoryContentsAsync(_testDirectory);

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.IsDirectory && i.Name == "subdir");
        Assert.Contains(items, i => i.IsFile && i.Name == "file.txt");
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_ExcludesHiddenByDefault()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, ".hidden"), "test");
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "visible.txt"), "test");

        // Act
        var items = await _service.GetDirectoryContentsAsync(_testDirectory, includeHidden: false);

        // Assert
        Assert.Single(items);
        Assert.Equal("visible.txt", items[0].Name);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_SortsFoldersFirst()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "aaa.txt"), "test");
        Directory.CreateDirectory(Path.Combine(_testDirectory, "zzz_folder"));

        // Act
        var items = await _service.GetDirectoryContentsAsync(_testDirectory);

        // Assert
        Assert.True(items[0].IsDirectory);
        Assert.True(items[1].IsFile);
    }

    [Fact]
    public async Task CreateDirectoryAsync_CreatesDirectory()
    {
        // Arrange
        var newDir = Path.Combine(_testDirectory, "newdir");

        // Act
        var result = await _service.CreateDirectoryAsync(newDir);

        // Assert
        Assert.True(Directory.Exists(newDir));
        Assert.Equal("newdir", result.Name);
        Assert.True(result.IsDirectory);
    }

    [Fact]
    public async Task DeleteDirectoryAsync_DeletesRecursively()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "todelete");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "file.txt"), "test");

        // Act
        await _service.DeleteDirectoryAsync(subDir);

        // Assert
        Assert.False(Directory.Exists(subDir));
    }

    #endregion

    #region File Operations

    [Fact]
    public async Task ReadFileAsync_ReturnsContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(filePath, "Hello World");

        // Act
        var content = await _service.ReadFileAsync(filePath);

        // Assert
        Assert.Equal("Hello World", content);
    }

    [Fact]
    public async Task WriteFileAsync_CreatesFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "newfile.txt");

        // Act
        await _service.WriteFileAsync(filePath, "New Content");

        // Assert
        Assert.True(File.Exists(filePath));
        Assert.Equal("New Content", await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public async Task WriteFileAsync_CreatesDirectoryIfNeeded()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nested", "deep", "file.txt");

        // Act
        await _service.WriteFileAsync(filePath, "content");

        // Assert
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task RenameAsync_RenamesFile()
    {
        // Arrange
        var originalPath = Path.Combine(_testDirectory, "original.txt");
        await File.WriteAllTextAsync(originalPath, "content");

        // Act
        var result = await _service.RenameAsync(originalPath, "renamed.txt");

        // Assert
        Assert.False(File.Exists(originalPath));
        Assert.True(File.Exists(result.Path));
        Assert.Equal("renamed.txt", result.Name);
    }

    [Fact]
    public async Task CopyFileAsync_CopiesFile()
    {
        // Arrange
        var sourcePath = Path.Combine(_testDirectory, "source.txt");
        var destPath = Path.Combine(_testDirectory, "copy.txt");
        await File.WriteAllTextAsync(sourcePath, "content");

        // Act
        var result = await _service.CopyFileAsync(sourcePath, destPath);

        // Assert
        Assert.True(File.Exists(sourcePath)); // Original still exists
        Assert.True(File.Exists(destPath));
        Assert.Equal("copy.txt", result.Name);
    }

    #endregion

    #region Existence Checks

    [Fact]
    public async Task FileExistsAsync_ReturnsTrueForExistingFile()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "exists.txt");
        await File.WriteAllTextAsync(filePath, "test");

        // Act & Assert
        Assert.True(await _service.FileExistsAsync(filePath));
        Assert.False(await _service.FileExistsAsync(Path.Combine(_testDirectory, "notexists.txt")));
    }

    [Fact]
    public async Task DirectoryExistsAsync_ReturnsTrueForExistingDir()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "existsdir");
        Directory.CreateDirectory(subDir);

        // Act & Assert
        Assert.True(await _service.DirectoryExistsAsync(subDir));
        Assert.False(await _service.DirectoryExistsAsync(Path.Combine(_testDirectory, "notexists")));
    }

    #endregion

    #region Utilities

    [Fact]
    public void GetRelativePath_ReturnsCorrectPath()
    {
        // Arrange
        var basePath = "/home/user/project";
        var fullPath = "/home/user/project/src/file.cs";

        // Act
        var result = _service.GetRelativePath(fullPath, basePath);

        // Assert
        Assert.Contains("src", result);
        Assert.Contains("file.cs", result);
    }

    [Fact]
    public void IsTextFile_ReturnsTrueForTextExtensions()
    {
        // Arrange
        var textFile = Path.Combine(_testDirectory, "text.cs");
        File.WriteAllText(textFile, "public class Test { }");

        // Act & Assert
        Assert.True(_service.IsTextFile(textFile));
    }

    [Fact]
    public void IsTextFile_ReturnsFalseForBinaryFile()
    {
        // Arrange (PNG signature)
        var binaryFile = Path.Combine(_testDirectory, "image.bin");
        File.WriteAllBytes(binaryFile, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // Act & Assert
        Assert.False(_service.IsTextFile(binaryFile));
    }

    [Fact]
    public void GetFileSize_ReturnsCorrectSize()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "sized.txt");
        File.WriteAllText(filePath, "12345"); // 5 bytes

        // Act
        var size = _service.GetFileSize(filePath);

        // Assert
        Assert.Equal(5, size);
    }

    [Fact]
    public async Task GetLineCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "lines.txt");
        await File.WriteAllTextAsync(filePath, "line1\nline2\nline3");

        // Act
        var count = await _service.GetLineCountAsync(filePath);

        // Assert
        Assert.Equal(3, count);
    }

    #endregion
}
