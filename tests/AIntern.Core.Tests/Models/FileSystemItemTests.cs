using Xunit;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Models;

/// <summary>
/// Unit tests for the <see cref="FileSystemItem"/> class.
/// Verifies factory methods, computed properties, and size formatting.
/// </summary>
public class FileSystemItemTests
{
    #region Factory Method Tests

    /// <summary>
    /// Verifies that FromFileInfo creates a correct FileSystemItem.
    /// </summary>
    [Fact]
    public void FromFileInfo_CreatesCorrectItem()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test content");
        var fileInfo = new FileInfo(tempFile);

        try
        {
            // Act
            var item = FileSystemItem.FromFileInfo(fileInfo);

            // Assert
            Assert.Equal(tempFile, item.Path);
            Assert.Equal(Path.GetFileName(tempFile), item.Name);
            Assert.Equal(FileSystemItemType.File, item.Type);
            Assert.Equal(12, item.Size); // "test content" = 12 bytes
            Assert.True(item.IsFile);
            Assert.False(item.IsDirectory);
            Assert.False(item.HasChildren);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Verifies that FromDirectoryInfo creates a correct FileSystemItem.
    /// </summary>
    [Fact]
    public void FromDirectoryInfo_CreatesCorrectItem()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var dirInfo = new DirectoryInfo(tempDir);

        try
        {
            // Act
            var item = FileSystemItem.FromDirectoryInfo(dirInfo, hasChildren: true);

            // Assert
            Assert.Equal(tempDir, item.Path);
            Assert.Equal(FileSystemItemType.Directory, item.Type);
            Assert.Null(item.Size);
            Assert.True(item.IsDirectory);
            Assert.False(item.IsFile);
            Assert.True(item.HasChildren);
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    #endregion

    #region FormattedSize Tests

    /// <summary>
    /// Verifies FormattedSize formats various sizes correctly.
    /// </summary>
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(1572864, "1.5 MB")]
    [InlineData(1073741824, "1.0 GB")]
    public void FormattedSize_FormatsCorrectly(long bytes, string expected)
    {
        // Arrange
        var item = new FileSystemItem
        {
            Path = "/test/file.txt",
            Name = "file.txt",
            Type = FileSystemItemType.File,
            Size = bytes
        };

        // Act & Assert
        Assert.Equal(expected, item.FormattedSize);
    }

    /// <summary>
    /// Verifies FormattedSize returns empty string for directories.
    /// </summary>
    [Fact]
    public void FormattedSize_ForDirectory_ReturnsEmpty()
    {
        // Arrange
        var item = new FileSystemItem
        {
            Path = "/test/folder",
            Name = "folder",
            Type = FileSystemItemType.Directory,
            Size = null
        };

        // Act & Assert
        Assert.Equal(string.Empty, item.FormattedSize);
    }

    #endregion

    #region Extension Tests

    /// <summary>
    /// Verifies that Extension returns the file extension for files.
    /// </summary>
    [Fact]
    public void Extension_ForFile_ReturnsExtension()
    {
        // Arrange
        var item = new FileSystemItem
        {
            Path = "/path/to/file.cs",
            Name = "file.cs",
            Type = FileSystemItemType.File
        };

        // Act & Assert
        Assert.Equal(".cs", item.Extension);
    }

    /// <summary>
    /// Verifies that Extension returns empty for directories.
    /// </summary>
    [Fact]
    public void Extension_ForDirectory_ReturnsEmpty()
    {
        // Arrange
        var item = new FileSystemItem
        {
            Path = "/path/to/folder",
            Name = "folder",
            Type = FileSystemItemType.Directory
        };

        // Act & Assert
        Assert.Equal(string.Empty, item.Extension);
    }

    #endregion

    #region Language Detection Tests

    /// <summary>
    /// Verifies that Language is detected for known file types.
    /// </summary>
    [Theory]
    [InlineData("file.cs", "csharp")]
    [InlineData("script.py", "python")]
    [InlineData("app.ts", "typescript")]
    [InlineData("data.json", "json")]
    public void Language_ForKnownExtension_ReturnsLanguage(string fileName, string expected)
    {
        // Arrange
        var item = new FileSystemItem
        {
            Path = $"/path/{fileName}",
            Name = fileName,
            Type = FileSystemItemType.File
        };

        // Act & Assert
        Assert.Equal(expected, item.Language);
    }

    /// <summary>
    /// Verifies that Language is null for directories.
    /// </summary>
    [Fact]
    public void Language_ForDirectory_ReturnsNull()
    {
        // Arrange
        var item = new FileSystemItem
        {
            Path = "/path/to/folder",
            Name = "folder",
            Type = FileSystemItemType.Directory
        };

        // Act & Assert
        Assert.Null(item.Language);
    }

    #endregion

    #region Hidden File Tests

    /// <summary>
    /// Verifies that files starting with dot are marked as hidden.
    /// </summary>
    [Fact]
    public void FromFileInfo_DotFile_MarkedAsHidden()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var hiddenFile = Path.Combine(tempDir, ".hidden_test_file");
        File.WriteAllText(hiddenFile, "test");
        var fileInfo = new FileInfo(hiddenFile);

        try
        {
            // Act
            var item = FileSystemItem.FromFileInfo(fileInfo);

            // Assert
            Assert.True(item.IsHidden);
        }
        finally
        {
            File.Delete(hiddenFile);
        }
    }

    #endregion
}
