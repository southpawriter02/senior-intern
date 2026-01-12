# Services Layer Specification

The `AIntern.Services` project provides concrete implementations of the core interfaces, integrating with external libraries and system APIs.

## Overview

This layer implements:
- **LlmService**: LLamaSharp-based local LLM inference
- **ConversationService**: In-memory conversation state management
- **SettingsService**: JSON-based persistent settings

---

## LlmService

Provides local LLM inference using the [LLamaSharp](https://github.com/SciSharp/LLamaSharp) library.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        LlmService                           │
├─────────────────────────────────────────────────────────────┤
│  _loadLock (SemaphoreSlim)     │  _inferenceLock            │
│  Protects model load/unload    │  Protects inference        │
├────────────────────────────────┴────────────────────────────┤
│  LLamaWeights → LLamaContext → InteractiveExecutor          │
└─────────────────────────────────────────────────────────────┘
```

### Thread Safety

- Model loading and inference use separate locks
- Status properties (`IsModelLoaded`, `CurrentModelPath`) can be queried during inference
- `CancelCurrentInference()` uses a linked `CancellationTokenSource`

### GPU Acceleration

| Platform | Default Behavior |
|----------|------------------|
| macOS | Metal acceleration (all layers on GPU) |
| Windows | CPU-only (user-configurable for CUDA) |
| Linux | CPU-only (user-configurable for CUDA) |

---

## ConversationService

Manages the current active conversation in memory.

### Behavior

- Single conversation at a time
- Messages are stored in chronological order
- `UpdatedAt` timestamp updated on any modification
- `ConversationChanged` event fired on all state changes

### Future Enhancements

- Multiple conversation support
- Persistence to local storage
- Conversation search and filtering

---

## SettingsService

Persists application settings to JSON file storage.

### Storage Location

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\AIntern\settings.json` |
| macOS | `~/Library/Application Support/AIntern/settings.json` |
| Linux | `~/.config/AIntern/settings.json` |

### Error Handling

- Corrupted settings files fall back to defaults
- Save failures are silently ignored to prevent crashes
- Settings directory is auto-created on first use
