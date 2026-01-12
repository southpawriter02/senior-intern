# Core Layer Specification

The `AIntern.Core` project contains the domain models, interfaces, events, and exceptions that define the application's core contracts.

## Overview

This layer has **zero external dependencies** and defines the abstractions that higher layers implement. It follows Domain-Driven Design principles with a clean separation between:
- **Models**: Data structures representing domain entities
- **Interfaces**: Service contracts for dependency injection
- **Events**: EventArgs classes for inter-component communication
- **Exceptions**: Domain-specific exception types

---

## Models

### ChatMessage

Represents a single message in a conversation.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier (auto-generated) |
| `Role` | `MessageRole` | Sender role: System, User, or Assistant |
| `Content` | `string` | Message text content |
| `Timestamp` | `DateTime` | UTC creation time |
| `IsComplete` | `bool` | Whether streaming is complete |
| `TokenCount` | `int?` | Token count (if known) |
| `GenerationTime` | `TimeSpan?` | Generation duration (Assistant only) |

### Conversation

A container for a sequence of messages with metadata.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `Title` | `string` | Display title (default: "New Conversation") |
| `Messages` | `List<ChatMessage>` | Ordered message collection |
| `SystemPrompt` | `string?` | Optional system instructions |
| `ModelPath` | `string?` | Path to associated model |

### AppSettings

Persistent configuration stored between sessions.

**Model Settings**: `LastModelPath`, `DefaultContextSize`, `DefaultGpuLayers`, `DefaultBatchSize`  
**Inference Settings**: `Temperature`, `TopP`, `MaxTokens`  
**UI Settings**: `Theme`, `SidebarWidth`, `WindowWidth`, `WindowHeight`

### ModelLoadOptions / InferenceOptions

Record types for configuring model loading and text generation.

---

## Interfaces

### ILlmService

Core LLM operations:
- `LoadModelAsync()` - Load a GGUF model with progress reporting
- `UnloadModelAsync()` - Release model resources
- `GenerateStreamingAsync()` - Stream token-by-token generation
- `CancelCurrentInference()` - Cancel ongoing generation

### IConversationService

Conversation state management:
- `AddMessage()` / `UpdateMessage()` - Modify message history
- `ClearConversation()` / `CreateNewConversation()` - Reset state
- `ConversationChanged` event for UI binding

### ISettingsService

Persisted settings management:
- `LoadSettingsAsync()` / `SaveSettingsAsync()` - JSON file I/O
- `SettingsChanged` event for reactive updates

---

## Events

| Event Args | Purpose |
|------------|---------|
| `ModelStateChangedEventArgs` | Model load/unload notifications |
| `SettingsChangedEventArgs` | Settings update notifications |
| `InferenceProgressEventArgs` | Token generation progress |

---

## Exceptions

| Exception | When Thrown |
|-----------|-------------|
| `LlmException` | Base class for all LLM errors |
| `ModelLoadException` | Model file not found or load failure |
| `InferenceException` | Generation error (e.g., no model loaded) |
