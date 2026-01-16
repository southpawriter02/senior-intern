namespace AIntern.Desktop.Tests.ViewModels;

using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Unit tests for <see cref="FileOperationItemViewModel"/>.
/// </summary>
public class FileOperationItemViewModelTests
{
    private readonly Mock<ILogger<FileOperationItemViewModel>> _loggerMock;

    public FileOperationItemViewModelTests()
    {
        _loggerMock = new Mock<ILogger<FileOperationItemViewModel>>();
    }

    // Helper to create a file operation
    private static FileOperation CreateFileOp(string path, FileOperationType type = FileOperationType.Create) => new()
    {
        Path = path,
        Content = "// code",
        Type = type
    };

    #region Identity Properties Tests

    [Fact]
    public void Name_DefaultsToEmpty()
    {
        var vm = new FileOperationItemViewModel();
        Assert.Equal(string.Empty, vm.Name);
    }

    [Fact]
    public void IsFile_WhenNotDirectory_ReturnsTrue()
    {
        var vm = new FileOperationItemViewModel { IsDirectory = false };
        Assert.True(vm.IsFile);
    }

    [Fact]
    public void IsFile_WhenDirectory_ReturnsFalse()
    {
        var vm = new FileOperationItemViewModel { IsDirectory = true };
        Assert.False(vm.IsFile);
    }

    #endregion

    #region Icon Tests

    [Fact]
    public void Icon_ForCSharpFile_ReturnsCSharp()
    {
        var vm = new FileOperationItemViewModel { Path = "Service.cs", IsDirectory = false };
        Assert.Equal("CSharp", vm.Icon);
    }

    [Fact]
    public void Icon_ForTypeScriptFile_ReturnsTypeScript()
    {
        var vm = new FileOperationItemViewModel { Path = "app.tsx", IsDirectory = false };
        Assert.Equal("TypeScript", vm.Icon);
    }

    [Fact]
    public void Icon_ForExpandedDirectory_ReturnsFolderOpen()
    {
        var vm = new FileOperationItemViewModel { IsDirectory = true, IsExpanded = true };
        Assert.Equal("FolderOpen", vm.Icon);
    }

    [Fact]
    public void Icon_ForCollapsedDirectory_ReturnsFolder()
    {
        var vm = new FileOperationItemViewModel { IsDirectory = true, IsExpanded = false };
        Assert.Equal("Folder", vm.Icon);
    }

    [Fact]
    public void Icon_ForUnknownExtension_ReturnsFile()
    {
        var vm = new FileOperationItemViewModel { Path = "file.xyz", IsDirectory = false };
        Assert.Equal("File", vm.Icon);
    }

    #endregion

    #region StatusText Tests

    [Fact]
    public void StatusText_WhenApplied_ReturnsCreated()
    {
        var vm = new FileOperationItemViewModel { OperationStatus = FileOperationStatus.Applied };
        Assert.Equal("Created", vm.StatusText);
    }

    [Fact]
    public void StatusText_WhenFailed_ReturnsFailed()
    {
        var vm = new FileOperationItemViewModel { OperationStatus = FileOperationStatus.Failed };
        Assert.Equal("Failed", vm.StatusText);
    }

    [Fact]
    public void StatusText_WhenSkipped_ReturnsSkipped()
    {
        var vm = new FileOperationItemViewModel { OperationStatus = FileOperationStatus.Skipped };
        Assert.Equal("Skipped", vm.StatusText);
    }

    [Fact]
    public void StatusText_WhenInProgress_ReturnsCreating()
    {
        var vm = new FileOperationItemViewModel { OperationStatus = FileOperationStatus.InProgress };
        Assert.Equal("Creating...", vm.StatusText);
    }

    [Fact]
    public void StatusText_WhenFileExists_ReturnsExists()
    {
        var vm = new FileOperationItemViewModel { FileExists = true };
        Assert.Equal("Exists", vm.StatusText);
    }

    [Fact]
    public void StatusText_WhenNoStatus_ReturnsEmpty()
    {
        var vm = new FileOperationItemViewModel();
        Assert.Equal("", vm.StatusText);
    }

    #endregion

    #region StatusIcon Tests

    [Fact]
    public void StatusIcon_WhenApplied_ReturnsCheck()
    {
        var vm = new FileOperationItemViewModel { OperationStatus = FileOperationStatus.Applied };
        Assert.Equal("Check", vm.StatusIcon);
    }

    [Fact]
    public void StatusIcon_WhenFailed_ReturnsError()
    {
        var vm = new FileOperationItemViewModel { OperationStatus = FileOperationStatus.Failed };
        Assert.Equal("Error", vm.StatusIcon);
    }

    [Fact]
    public void StatusIcon_WhenNoStatus_ReturnsNull()
    {
        var vm = new FileOperationItemViewModel();
        Assert.Null(vm.StatusIcon);
    }

    #endregion

    #region Validation Properties Tests

    [Fact]
    public void HasWarning_WhenWarningSeverity_ReturnsTrue()
    {
        var vm = new FileOperationItemViewModel
        {
            ValidationIssue = new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Message = "Warning"
            }
        };
        Assert.True(vm.HasWarning);
    }

    [Fact]
    public void HasError_WhenErrorSeverity_ReturnsTrue()
    {
        var vm = new FileOperationItemViewModel
        {
            ValidationIssue = new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Message = "Error"
            }
        };
        Assert.True(vm.HasError);
    }

    [Fact]
    public void HasValidationIssue_WhenIssueExists_ReturnsTrue()
    {
        var vm = new FileOperationItemViewModel
        {
            ValidationIssue = new ValidationIssue { Message = "Issue" }
        };
        Assert.True(vm.HasValidationIssue);
    }

    [Fact]
    public void HasValidationIssue_WhenNoIssue_ReturnsFalse()
    {
        var vm = new FileOperationItemViewModel();
        Assert.False(vm.HasValidationIssue);
    }

    #endregion

    #region Factory Methods Tests

    [Fact]
    public void FromTreeNode_CreatesViewModel()
    {
        // Arrange
        var node = new TreeNode
        {
            Name = "Test.cs",
            Path = "src/Test.cs",
            IsDirectory = false,
            Operation = CreateFileOp("src/Test.cs")
        };

        // Act
        var vm = FileOperationItemViewModel.FromTreeNode(node);

        // Assert
        Assert.Equal("Test.cs", vm.Name);
        Assert.Equal("src/Test.cs", vm.Path);
        Assert.False(vm.IsDirectory);
        Assert.NotNull(vm.Operation);
    }

    [Fact]
    public void FromTreeNode_CreatesChildViewModels()
    {
        // Arrange
        var node = new TreeNode
        {
            Name = "src",
            Path = "src",
            IsDirectory = true,
            Children = new List<TreeNode>
            {
                new TreeNode { Name = "A.cs", Path = "src/A.cs", IsDirectory = false },
                new TreeNode { Name = "B.cs", Path = "src/B.cs", IsDirectory = false }
            }
        };

        // Act
        var vm = FileOperationItemViewModel.FromTreeNode(node);

        // Assert
        Assert.True(vm.IsDirectory);
        Assert.Equal(2, vm.Children.Count);
    }

    [Fact]
    public void CreateDirectory_CreatesDirectoryViewModel()
    {
        var vm = FileOperationItemViewModel.CreateDirectory("Models", "src/Models");
        
        Assert.Equal("Models", vm.Name);
        Assert.Equal("src/Models", vm.Path);
        Assert.True(vm.IsDirectory);
        Assert.True(vm.IsExpanded);
    }

    [Fact]
    public void CreateFile_CreatesFileViewModel()
    {
        var operation = CreateFileOp("src/Test.cs");
        var vm = FileOperationItemViewModel.CreateFile(operation);
        
        Assert.Equal("Test.cs", vm.Name);
        Assert.Equal("src/Test.cs", vm.Path);
        Assert.False(vm.IsDirectory);
        Assert.Same(operation, vm.Operation);
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void Extension_ReturnsExtensionWithoutDot()
    {
        var vm = new FileOperationItemViewModel { Path = "file.cs" };
        Assert.Equal("cs", vm.Extension);
    }

    [Fact]
    public void Depth_CalculatesCorrectly()
    {
        var vm = new FileOperationItemViewModel { Path = "src/Models/User.cs" };
        Assert.Equal(2, vm.Depth);
    }

    [Fact]
    public void IsNewFile_WhenCreateOperation_ReturnsTrue()
    {
        var vm = new FileOperationItemViewModel
        {
            Operation = CreateFileOp("file.cs", FileOperationType.Create)
        };
        Assert.True(vm.IsNewFile);
    }

    [Fact]
    public void IsModification_WhenModifyOperation_ReturnsTrue()
    {
        var vm = new FileOperationItemViewModel
        {
            Operation = CreateFileOp("file.cs", FileOperationType.Modify)
        };
        Assert.True(vm.IsModification);
    }

    [Fact]
    public void HasChildren_WhenChildrenExist_ReturnsTrue()
    {
        var vm = new FileOperationItemViewModel();
        vm.Children.Add(new FileOperationItemViewModel { Name = "Child" });
        Assert.True(vm.HasChildren);
    }

    [Fact]
    public void HasChildren_WhenNoChildren_ReturnsFalse()
    {
        var vm = new FileOperationItemViewModel();
        Assert.False(vm.HasChildren);
    }

    #endregion

    #region Method Tests

    [Fact]
    public void ClearValidation_ClearsIssueAndFileExists()
    {
        var vm = new FileOperationItemViewModel
        {
            ValidationIssue = new ValidationIssue { Message = "Issue" },
            FileExists = true
        };

        vm.ClearValidation();

        Assert.Null(vm.ValidationIssue);
        Assert.False(vm.FileExists);
    }

    [Fact]
    public void ResetStatus_ClearsOperationStatus()
    {
        var vm = new FileOperationItemViewModel
        {
            OperationStatus = FileOperationStatus.Applied
        };

        vm.ResetStatus();

        Assert.Null(vm.OperationStatus);
    }

    #endregion
}
