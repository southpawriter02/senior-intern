namespace AIntern.Services.Tests;

using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

/// <summary>
/// Unit tests for BlockClassificationService (v0.4.1d).
/// </summary>
public class BlockClassificationServiceTests
{
    private readonly Mock<ILanguageDetectionService> _mockLanguageService;
    private readonly Mock<ILogger<BlockClassificationService>> _mockLogger;
    private readonly BlockClassificationService _service;

    public BlockClassificationServiceTests()
    {
        _mockLanguageService = new Mock<ILanguageDetectionService>();
        _mockLogger = new Mock<ILogger<BlockClassificationService>>();
        _service = new BlockClassificationService(
            _mockLanguageService.Object,
            _mockLogger.Object);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ SHELL LANGUAGE TESTS (3)                                                 │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void ClassifyBlock_BashLanguage_ReturnsCommand()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage("bash")).Returns(true);

        // Act
        var result = _service.ClassifyBlock("npm install", "bash", "Run the command:");

        // Assert
        Assert.Equal(CodeBlockType.Command, result);
    }

    [Fact]
    public void ClassifyBlock_PowershellLanguage_ReturnsCommand()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage("powershell")).Returns(true);

        // Act
        var result = _service.ClassifyBlock("Get-Process", "powershell", "Execute this:");

        // Assert
        Assert.Equal(CodeBlockType.Command, result);
    }

    [Fact]
    public void ClassifyBlock_CmdLanguage_ReturnsCommand()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage("cmd")).Returns(true);

        // Act
        var result = _service.ClassifyBlock("dir /s", "cmd", "Run:");

        // Assert
        Assert.Equal(CodeBlockType.Command, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CONFIG LANGUAGE TESTS (3)                                                │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void ClassifyBlock_JsonLanguage_ReturnsConfig()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage("json")).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage("json")).Returns(true);

        // Act
        var result = _service.ClassifyBlock("{\"key\": \"value\"}", "json", "Configuration:");

        // Assert
        Assert.Equal(CodeBlockType.Config, result);
    }

    [Fact]
    public void ClassifyBlock_YamlLanguage_ReturnsConfig()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage("yaml")).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage("yaml")).Returns(true);

        // Act
        var result = _service.ClassifyBlock("key: value", "yaml", "Settings:");

        // Assert
        Assert.Equal(CodeBlockType.Config, result);
    }

    [Fact]
    public void ClassifyBlock_XmlLanguage_ReturnsConfig()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage("xml")).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage("xml")).Returns(true);

        // Act
        var result = _service.ClassifyBlock("<config/>", "xml", "XML file:");

        // Assert
        Assert.Equal(CodeBlockType.Config, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ OUTPUT INDICATOR TESTS (2)                                               │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void ClassifyBlock_OutputColonIndicator_ReturnsOutput()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock(
            "Hello World",
            "plaintext",
            "The output: will be displayed here");

        // Assert
        Assert.Equal(CodeBlockType.Output, result);
    }

    [Fact]
    public void ClassifyBlock_WillPrintIndicator_ReturnsOutput()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock(
            "42",
            null,
            "This code will print the number");

        // Assert
        Assert.Equal(CodeBlockType.Output, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ EXAMPLE INDICATOR TESTS (3)                                              │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void ClassifyBlock_ForExampleIndicator_ReturnsExample()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock(
            "int x = 5;",
            "csharp",
            "For example, you might write something like this:");

        // Assert
        Assert.Equal(CodeBlockType.Example, result);
    }

    [Fact]
    public void ClassifyBlock_SupposeWeHaveIndicator_ReturnsExample()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock(
            "const data = [];",
            "javascript",
            "Suppose we have an array like this:");

        // Assert
        Assert.Equal(CodeBlockType.Example, result);
    }

    [Fact]
    public void ClassifyBlock_HypotheticallyIndicator_ReturnsExample()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock(
            "class User { }",
            "csharp",
            "Hypothetically, if we had a user class:");

        // Assert
        Assert.Equal(CodeBlockType.Example, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ APPLY INDICATOR TESTS (3)                                                │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void ClassifyBlock_UpdateIndicator_ReturnsCompleteFile()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock(
            "int x = 10;",
            "csharp",
            "Update your code to:");

        // Assert
        Assert.Equal(CodeBlockType.CompleteFile, result);
    }

    [Fact]
    public void ClassifyBlock_HeresTheIndicator_ReturnsCompleteFile()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock(
            "function test() {}",
            "javascript",
            "Here's the fix:");

        // Assert
        Assert.Equal(CodeBlockType.CompleteFile, result);
    }

    [Fact]
    public void ClassifyBlock_FixedVersionIndicator_ReturnsCompleteFile()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock(
            "return result;",
            "csharp",
            "Here's the fixed version:");

        // Assert
        Assert.Equal(CodeBlockType.CompleteFile, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COMPLETE FILE STRUCTURE TESTS (5)                                        │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void ClassifyBlock_CSharpWithNamespace_ReturnsCompleteFile()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);
        var content = @"namespace MyApp;
using System;
public class Program { }";

        // Act
        var result = _service.ClassifyBlock(content, "csharp", "");

        // Assert
        Assert.Equal(CodeBlockType.CompleteFile, result);
    }

    [Fact]
    public void ClassifyBlock_JavaScriptWithImport_ReturnsCompleteFile()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);
        var content = @"import React from 'react';
export default function App() { return <div/>; }";

        // Act
        var result = _service.ClassifyBlock(content, "javascript", "");

        // Assert
        Assert.Equal(CodeBlockType.CompleteFile, result);
    }

    [Fact]
    public void ClassifyBlock_PythonWithImport_ReturnsCompleteFile()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);
        var content = @"import os
def main():
    pass";

        // Act
        var result = _service.ClassifyBlock(content, "python", "");

        // Assert
        Assert.Equal(CodeBlockType.CompleteFile, result);
    }

    [Fact]
    public void ClassifyBlock_GoWithPackage_ReturnsCompleteFile()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);
        var content = @"package main
func main() { }";

        // Act
        var result = _service.ClassifyBlock(content, "go", "");

        // Assert
        Assert.Equal(CodeBlockType.CompleteFile, result);
    }

    [Fact]
    public void ClassifyBlock_RustWithUse_ReturnsCompleteFile()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);
        var content = @"use std::io;
fn main() { }";

        // Act
        var result = _service.ClassifyBlock(content, "rust", "");

        // Assert
        Assert.Equal(CodeBlockType.CompleteFile, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ DEFAULT SNIPPET TESTS (2)                                                │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void ClassifyBlock_PartialCode_ReturnsSnippet()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock(
            "x = 5;",
            "csharp",
            "This line of code sets x to 5.");

        // Assert
        Assert.Equal(CodeBlockType.Snippet, result);
    }

    [Fact]
    public void ClassifyBlock_NoContext_ReturnsSnippet()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock("int x = 5;", "csharp", "");

        // Assert
        Assert.Equal(CodeBlockType.Snippet, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CONFIDENCE SCORE TESTS (6)                                               │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void GetClassificationConfidence_Command_Returns095()
    {
        var block = new CodeBlock { BlockType = CodeBlockType.Command };
        Assert.Equal(0.95f, _service.GetClassificationConfidence(block));
    }

    [Fact]
    public void GetClassificationConfidence_Config_Returns090()
    {
        var block = new CodeBlock { BlockType = CodeBlockType.Config };
        Assert.Equal(0.90f, _service.GetClassificationConfidence(block));
    }

    [Fact]
    public void GetClassificationConfidence_Output_Returns085()
    {
        var block = new CodeBlock { BlockType = CodeBlockType.Output };
        Assert.Equal(0.85f, _service.GetClassificationConfidence(block));
    }

    [Fact]
    public void GetClassificationConfidence_CompleteFile_Returns080()
    {
        var block = new CodeBlock { BlockType = CodeBlockType.CompleteFile };
        Assert.Equal(0.80f, _service.GetClassificationConfidence(block));
    }

    [Fact]
    public void GetClassificationConfidence_Example_Returns075()
    {
        var block = new CodeBlock { BlockType = CodeBlockType.Example };
        Assert.Equal(0.75f, _service.GetClassificationConfidence(block));
    }

    [Fact]
    public void GetClassificationConfidence_Snippet_Returns070()
    {
        var block = new CodeBlock { BlockType = CodeBlockType.Snippet };
        Assert.Equal(0.70f, _service.GetClassificationConfidence(block));
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ EDGE CASE TESTS (7)                                                      │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Fact]
    public void ClassifyBlock_EmptyContent_ReturnsSnippet()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock("", "csharp", "Some context");

        // Assert
        Assert.Equal(CodeBlockType.Snippet, result);
    }

    [Fact]
    public void ClassifyBlock_NullLanguage_SkipsLanguageChecks()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(null)).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(null)).Returns(false);

        // Act
        var result = _service.ClassifyBlock("x = 5", null, "");

        // Assert
        Assert.Equal(CodeBlockType.Snippet, result);
    }

    [Fact]
    public void ClassifyBlock_EmptySurroundingText_SkipsIndicatorScoring()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock("x = 5;", "csharp", "");

        // Assert - Should return Snippet since no indicators and no complete structure
        Assert.Equal(CodeBlockType.Snippet, result);
    }

    [Fact]
    public void ClassifyBlock_MixedIndicators_ExampleWinsWhenHigher()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Context has both example and apply indicators, but example has more
        var context = "For example, such as, like this, consider the following. Update this.";

        // Act
        var result = _service.ClassifyBlock("x = 5;", "csharp", context);

        // Assert - Example score (4) > Apply score (1)
        Assert.Equal(CodeBlockType.Example, result);
    }

    [Fact]
    public void ClassifyBlock_MixedIndicators_ApplyWinsWithStructure()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Context has both, but apply has more
        var context = "Update and modify and replace this code.";
        var content = "namespace Foo; public class Bar { }";

        // Act
        var result = _service.ClassifyBlock(content, "csharp", context);

        // Assert - Apply wins with structure
        Assert.Equal(CodeBlockType.CompleteFile, result);
    }

    [Fact]
    public void ClassifyBlock_TieScore_ApplyTakesPrecedence()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Equal number of each indicator
        var context = "For example update this.";

        // Act
        var result = _service.ClassifyBlock("x = 5;", "csharp", context);

        // Assert - When example score = apply score, example doesn't win (score must be GREATER)
        // But since applyScore > 0, we get CompleteFile
        Assert.Equal(CodeBlockType.CompleteFile, result);
    }

    [Fact]
    public void ClassifyBlock_CaseInsensitiveMatching()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock(
            "x = 5;",
            "csharp",
            "FOR EXAMPLE, SUPPOSE WE HAVE this code:");

        // Assert - Case-insensitive matching should still find indicators
        Assert.Equal(CodeBlockType.Example, result);
    }

    [Fact]
    public void GetClassificationConfidence_NullBlock_ReturnsZero()
    {
        // Act
        var result = _service.GetClassificationConfidence(null!);

        // Assert
        Assert.Equal(0.0f, result);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ HAS COMPLETE FILE STRUCTURE TESTS (5)                                    │
    // └─────────────────────────────────────────────────────────────────────────┘

    [Theory]
    [InlineData("java", "public class MyClass { }", true)]
    [InlineData("java", "package com.example;", true)]
    [InlineData("java", "int x = 5;", false)]
    public void ClassifyBlock_JavaStructure_DetectsCorrectly(
        string language, string content, bool expectComplete)
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);

        // Act
        var result = _service.ClassifyBlock(content, language, "");

        // Assert
        if (expectComplete)
            Assert.Equal(CodeBlockType.CompleteFile, result);
        else
            Assert.Equal(CodeBlockType.Snippet, result);
    }

    [Theory]
    [InlineData("json", "{\"key\": \"value\"}", true)]
    [InlineData("json", "[1, 2, 3]", false)] // Array doesn't match {} pattern
    [InlineData("json", "key: value", false)]
    public void ClassifyBlock_JsonStructure_ViaConfigLanguage(
        string language, string content, bool isConfig)
    {
        // Arrange - JSON is detected as config language first
        _mockLanguageService.Setup(x => x.IsShellLanguage(language)).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(language)).Returns(isConfig);

        // Act
        var result = _service.ClassifyBlock(content, language, "");

        // Assert
        if (isConfig)
            Assert.Equal(CodeBlockType.Config, result);
        else
            Assert.Equal(CodeBlockType.Snippet, result);
    }

    [Fact]
    public void ClassifyBlock_TypeScriptWithExport_ReturnsCompleteFile()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.IsShellLanguage(It.IsAny<string>())).Returns(false);
        _mockLanguageService.Setup(x => x.IsConfigLanguage(It.IsAny<string>())).Returns(false);
        var content = "export interface User { name: string; }";

        // Act
        var result = _service.ClassifyBlock(content, "typescript", "");

        // Assert
        Assert.Equal(CodeBlockType.CompleteFile, result);
    }

    [Fact]
    public void Constructor_NullLanguageService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BlockClassificationService(null!, null));
    }
}
