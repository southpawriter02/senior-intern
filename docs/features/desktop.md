# Desktop Layer Specification

The `AIntern.Desktop` project provides the Avalonia-based user interface using the MVVM pattern.

## Overview

- **Framework**: Avalonia UI 11.2.1
- **Pattern**: MVVM with CommunityToolkit.Mvvm
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Theme**: Fluent Dark

---

## Project Structure

```
AIntern.Desktop/
├── App.axaml(.cs)          # Application entry and DI setup
├── Program.cs              # Serilog configuration and startup
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── ViewModels/
│   ├── ViewModelBase.cs
│   ├── MainWindowViewModel.cs
│   ├── ChatViewModel.cs
│   ├── ChatMessageViewModel.cs
│   └── ModelSelectorViewModel.cs
├── Views/
│   ├── MainWindow.axaml
│   ├── ChatView.axaml
│   ├── ChatMessageControl.axaml
│   └── ModelSelectorView.axaml
└── Themes/
    └── Dark.axaml
```

---

## ViewModels

### MainWindowViewModel

Root ViewModel coordinating child ViewModels:
- Hosts `ChatViewModel` and `ModelSelectorViewModel`
- Manages overall application state

### ChatViewModel

Handles chat interaction:
- Message input and submission
- Streaming response display
- Send/cancel controls

### ModelSelectorViewModel

LLM model management:
- File picker for GGUF models
- Load progress display
- Model status indicators

---

## Dependency Injection

Services registered in `ServiceCollectionExtensions.AddAInternServices()`:

| Registration | Lifetime | Purpose |
|--------------|----------|---------|
| `ISettingsService` | Singleton | Persistent settings |
| `ILlmService` | Singleton | LLM inference |
| `IConversationService` | Singleton | Conversation state |
| `MainWindowViewModel` | Transient | Main window |
| `ChatViewModel` | Transient | Chat panel |
| `ModelSelectorViewModel` | Transient | Model picker |

---

## Logging

Serilog is configured in `Program.cs`:
- **Console Sink**: Compact format for development
- **File Sink**: Rolling daily files in `logs/` directory (7-day retention)

Logs are accessible via `ILogger<T>` injection in any service.
