using Xunit;
using AIntern.Core.Entities;
using AIntern.Core.Models;

namespace AIntern.Core.Tests.Entities;

/// <summary>
/// Unit tests for all entity classes in the AIntern.Core.Entities namespace.
/// Verifies default values, property initialization, and type correctness.
/// </summary>
/// <remarks>
/// <para>These tests ensure that entity classes have correct default values
/// for Entity Framework Core persistence.</para>
/// <para>Tests are organized by entity type using regions.</para>
/// </remarks>
public class EntityTests
{
    #region MessageRole Enum Tests

    /// <summary>
    /// Verifies that MessageRole.System has the expected integer value of 0.
    /// This ensures stable database storage.
    /// </summary>
    [Fact]
    public void MessageRole_System_ShouldEqual0()
    {
        // Assert
        Assert.Equal(0, (int)MessageRole.System);
    }

    /// <summary>
    /// Verifies that MessageRole.User has the expected integer value of 1.
    /// This ensures stable database storage.
    /// </summary>
    [Fact]
    public void MessageRole_User_ShouldEqual1()
    {
        // Assert
        Assert.Equal(1, (int)MessageRole.User);
    }

    /// <summary>
    /// Verifies that MessageRole.Assistant has the expected integer value of 2.
    /// This ensures stable database storage.
    /// </summary>
    [Fact]
    public void MessageRole_Assistant_ShouldEqual2()
    {
        // Assert
        Assert.Equal(2, (int)MessageRole.Assistant);
    }

    #endregion

    #region ConversationEntity Tests

    /// <summary>
    /// Verifies that a new ConversationEntity has the default title "New Conversation".
    /// </summary>
    [Fact]
    public void ConversationEntity_Constructor_SetsDefaultTitle()
    {
        // Arrange & Act
        var conversation = new ConversationEntity();

        // Assert
        Assert.Equal("New Conversation", conversation.Title);
    }

    /// <summary>
    /// Verifies that a new ConversationEntity initializes an empty Messages collection.
    /// </summary>
    [Fact]
    public void ConversationEntity_Constructor_InitializesEmptyMessagesCollection()
    {
        // Arrange & Act
        var conversation = new ConversationEntity();

        // Assert
        Assert.NotNull(conversation.Messages);
        Assert.Empty(conversation.Messages);
    }

    /// <summary>
    /// Verifies that a new ConversationEntity has MessageCount initialized to 0.
    /// </summary>
    [Fact]
    public void ConversationEntity_Constructor_SetsZeroMessageCount()
    {
        // Arrange & Act
        var conversation = new ConversationEntity();

        // Assert
        Assert.Equal(0, conversation.MessageCount);
    }

    /// <summary>
    /// Verifies that a new ConversationEntity has TotalTokenCount initialized to 0.
    /// </summary>
    [Fact]
    public void ConversationEntity_Constructor_SetsZeroTotalTokenCount()
    {
        // Arrange & Act
        var conversation = new ConversationEntity();

        // Assert
        Assert.Equal(0, conversation.TotalTokenCount);
    }

    /// <summary>
    /// Verifies that a new ConversationEntity is not archived by default.
    /// </summary>
    [Fact]
    public void ConversationEntity_Constructor_IsNotArchived()
    {
        // Arrange & Act
        var conversation = new ConversationEntity();

        // Assert
        Assert.False(conversation.IsArchived);
    }

    /// <summary>
    /// Verifies that a new ConversationEntity is not pinned by default.
    /// </summary>
    [Fact]
    public void ConversationEntity_Constructor_IsNotPinned()
    {
        // Arrange & Act
        var conversation = new ConversationEntity();

        // Assert
        Assert.False(conversation.IsPinned);
    }

    /// <summary>
    /// Verifies that a new ConversationEntity has null ModelPath.
    /// </summary>
    [Fact]
    public void ConversationEntity_Constructor_HasNullModelPath()
    {
        // Arrange & Act
        var conversation = new ConversationEntity();

        // Assert
        Assert.Null(conversation.ModelPath);
    }

    /// <summary>
    /// Verifies that a new ConversationEntity has null ModelName.
    /// </summary>
    [Fact]
    public void ConversationEntity_Constructor_HasNullModelName()
    {
        // Arrange & Act
        var conversation = new ConversationEntity();

        // Assert
        Assert.Null(conversation.ModelName);
    }

    /// <summary>
    /// Verifies that a new ConversationEntity has null SystemPromptId.
    /// </summary>
    [Fact]
    public void ConversationEntity_Constructor_HasNullSystemPromptId()
    {
        // Arrange & Act
        var conversation = new ConversationEntity();

        // Assert
        Assert.Null(conversation.SystemPromptId);
    }

    /// <summary>
    /// Verifies that a new ConversationEntity has null SystemPrompt navigation property.
    /// </summary>
    [Fact]
    public void ConversationEntity_Constructor_HasNullSystemPrompt()
    {
        // Arrange & Act
        var conversation = new ConversationEntity();

        // Assert
        Assert.Null(conversation.SystemPrompt);
    }

    #endregion

    #region MessageEntity Tests

    /// <summary>
    /// Verifies that a new MessageEntity has Content initialized to empty string.
    /// </summary>
    [Fact]
    public void MessageEntity_Constructor_SetsEmptyContent()
    {
        // Arrange & Act
        var message = new MessageEntity();

        // Assert
        Assert.Equal(string.Empty, message.Content);
    }

    /// <summary>
    /// Verifies that a new MessageEntity is marked as complete by default.
    /// </summary>
    [Fact]
    public void MessageEntity_Constructor_IsComplete()
    {
        // Arrange & Act
        var message = new MessageEntity();

        // Assert
        Assert.True(message.IsComplete);
    }

    /// <summary>
    /// Verifies that a new MessageEntity is not marked as edited by default.
    /// </summary>
    [Fact]
    public void MessageEntity_Constructor_IsNotEdited()
    {
        // Arrange & Act
        var message = new MessageEntity();

        // Assert
        Assert.False(message.IsEdited);
    }

    /// <summary>
    /// Verifies that a new MessageEntity has null TokenCount.
    /// </summary>
    [Fact]
    public void MessageEntity_Constructor_HasNullTokenCount()
    {
        // Arrange & Act
        var message = new MessageEntity();

        // Assert
        Assert.Null(message.TokenCount);
    }

    /// <summary>
    /// Verifies that a new MessageEntity has null GenerationTimeMs.
    /// </summary>
    [Fact]
    public void MessageEntity_Constructor_HasNullGenerationTimeMs()
    {
        // Arrange & Act
        var message = new MessageEntity();

        // Assert
        Assert.Null(message.GenerationTimeMs);
    }

    /// <summary>
    /// Verifies that a new MessageEntity has null TokensPerSecond.
    /// </summary>
    [Fact]
    public void MessageEntity_Constructor_HasNullTokensPerSecond()
    {
        // Arrange & Act
        var message = new MessageEntity();

        // Assert
        Assert.Null(message.TokensPerSecond);
    }

    #endregion

    #region SystemPromptEntity Tests

    /// <summary>
    /// Verifies that a new SystemPromptEntity has Name initialized to empty string.
    /// </summary>
    [Fact]
    public void SystemPromptEntity_Constructor_SetsEmptyName()
    {
        // Arrange & Act
        var prompt = new SystemPromptEntity();

        // Assert
        Assert.Equal(string.Empty, prompt.Name);
    }

    /// <summary>
    /// Verifies that a new SystemPromptEntity has Content initialized to empty string.
    /// </summary>
    [Fact]
    public void SystemPromptEntity_Constructor_SetsEmptyContent()
    {
        // Arrange & Act
        var prompt = new SystemPromptEntity();

        // Assert
        Assert.Equal(string.Empty, prompt.Content);
    }

    /// <summary>
    /// Verifies that a new SystemPromptEntity has Category defaulted to "General".
    /// </summary>
    [Fact]
    public void SystemPromptEntity_Constructor_SetsDefaultCategory()
    {
        // Arrange & Act
        var prompt = new SystemPromptEntity();

        // Assert
        Assert.Equal("General", prompt.Category);
    }

    /// <summary>
    /// Verifies that a new SystemPromptEntity is active by default.
    /// </summary>
    [Fact]
    public void SystemPromptEntity_Constructor_IsActive()
    {
        // Arrange & Act
        var prompt = new SystemPromptEntity();

        // Assert
        Assert.True(prompt.IsActive);
    }

    /// <summary>
    /// Verifies that a new SystemPromptEntity initializes an empty Conversations collection.
    /// </summary>
    [Fact]
    public void SystemPromptEntity_Constructor_InitializesEmptyConversationsCollection()
    {
        // Arrange & Act
        var prompt = new SystemPromptEntity();

        // Assert
        Assert.NotNull(prompt.Conversations);
        Assert.Empty(prompt.Conversations);
    }

    /// <summary>
    /// Verifies that a new SystemPromptEntity is not marked as default.
    /// </summary>
    [Fact]
    public void SystemPromptEntity_Constructor_IsNotDefault()
    {
        // Arrange & Act
        var prompt = new SystemPromptEntity();

        // Assert
        Assert.False(prompt.IsDefault);
    }

    /// <summary>
    /// Verifies that a new SystemPromptEntity is not marked as built-in.
    /// </summary>
    [Fact]
    public void SystemPromptEntity_Constructor_IsNotBuiltIn()
    {
        // Arrange & Act
        var prompt = new SystemPromptEntity();

        // Assert
        Assert.False(prompt.IsBuiltIn);
    }

    /// <summary>
    /// Verifies that a new SystemPromptEntity has UsageCount defaulted to 0.
    /// </summary>
    [Fact]
    public void SystemPromptEntity_Constructor_SetsZeroUsageCount()
    {
        // Arrange & Act
        var prompt = new SystemPromptEntity();

        // Assert
        Assert.Equal(0, prompt.UsageCount);
    }

    /// <summary>
    /// Verifies that a new SystemPromptEntity has null Description.
    /// </summary>
    [Fact]
    public void SystemPromptEntity_Constructor_HasNullDescription()
    {
        // Arrange & Act
        var prompt = new SystemPromptEntity();

        // Assert
        Assert.Null(prompt.Description);
    }

    /// <summary>
    /// Verifies that a new SystemPromptEntity has null TagsJson.
    /// </summary>
    [Fact]
    public void SystemPromptEntity_Constructor_HasNullTagsJson()
    {
        // Arrange & Act
        var prompt = new SystemPromptEntity();

        // Assert
        Assert.Null(prompt.TagsJson);
    }

    #endregion

    #region InferencePresetEntity Tests

    /// <summary>
    /// Verifies that a new InferencePresetEntity has Temperature defaulted to 0.7.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_SetsDefaultTemperature()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.Equal(0.7f, preset.Temperature);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity has TopP defaulted to 0.9.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_SetsDefaultTopP()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.Equal(0.9f, preset.TopP);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity has TopK defaulted to 40.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_SetsDefaultTopK()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.Equal(40, preset.TopK);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity has RepeatPenalty defaulted to 1.1.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_SetsDefaultRepeatPenalty()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.Equal(1.1f, preset.RepeatPenalty);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity has MaxTokens defaulted to 2048.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_SetsDefaultMaxTokens()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.Equal(2048, preset.MaxTokens);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity has ContextSize defaulted to 4096.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_SetsDefaultContextSize()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.Equal(4096, preset.ContextSize);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity has Seed defaulted to -1 (random).
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_SetsDefaultSeed()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.Equal(-1, preset.Seed);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity has UsageCount defaulted to 0.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_SetsZeroUsageCount()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.Equal(0, preset.UsageCount);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity is not marked as default.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_IsNotDefault()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.False(preset.IsDefault);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity is not marked as built-in.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_IsNotBuiltIn()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.False(preset.IsBuiltIn);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity has Name initialized to empty string.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_SetsEmptyName()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.Equal(string.Empty, preset.Name);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity has null Category.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_HasNullCategory()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.Null(preset.Category);
    }

    /// <summary>
    /// Verifies that a new InferencePresetEntity has null Description.
    /// </summary>
    [Fact]
    public void InferencePresetEntity_Constructor_HasNullDescription()
    {
        // Arrange & Act
        var preset = new InferencePresetEntity();

        // Assert
        Assert.Null(preset.Description);
    }

    #endregion
}
