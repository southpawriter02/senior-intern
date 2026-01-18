using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;
using AIntern.Core.Models.Terminal;
using AIntern.Desktop.Messages;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel representing a single chat message in the UI.
/// Supports streaming content updates for assistant messages.
/// </summary>
/// <remarks>
/// <para>
/// This ViewModel wraps the <see cref="ChatMessage"/> domain model
/// and adds UI-specific properties like <see cref="IsStreaming"/>.
/// </para>
/// <para>
/// <b>v0.4.1g:</b> Added code block support with streaming integration.
/// </para>
/// <para>
/// <b>v0.5.4h:</b> Added command block support for terminal command extraction.
/// </para>
/// </remarks>
public partial class ChatMessageViewModel : ViewModelBase
{
    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ OBSERVABLE PROPERTIES                                                    │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Gets or sets the unique identifier for this message.
    /// Matches the domain model ID for correlation.
    /// </summary>
    [ObservableProperty]
    private Guid _id;

    /// <summary>
    /// Gets or sets the text content of the message.
    /// Updated incrementally during streaming generation.
    /// </summary>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// Gets or sets the role of the message sender.
    /// Determines display styling (User vs Assistant).
    /// </summary>
    [ObservableProperty]
    private MessageRole _role;

    /// <summary>
    /// Gets or sets whether the message content is still being streamed.
    /// True while tokens are being generated, false when complete.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCodeActions))]
    private bool _isStreaming;

    /// <summary>
    /// Gets or sets the UTC timestamp when this message was created.
    /// Used for display and sorting purposes.
    /// </summary>
    [ObservableProperty]
    private DateTime _timestamp;

    /// <summary>
    /// Gets or sets the number of tokens in this message.
    /// Only populated for completed assistant messages.
    /// </summary>
    [ObservableProperty]
    private int? _tokenCount;

    /// <summary>
    /// Gets or sets the time taken to generate this message.
    /// Only populated for assistant messages after generation completes.
    /// </summary>
    [ObservableProperty]
    private TimeSpan? _generationTime;

    /// <summary>
    /// Gets or sets the file contexts attached to this message.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.3.4h.</para>
    /// </remarks>
    [ObservableProperty]
    private ObservableCollection<FileContextViewModel> _attachedContexts = new();

    /// <summary>
    /// Gets or sets whether the attached context list is expanded.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.3.4h.</para>
    /// </remarks>
    [ObservableProperty]
    private bool _isContextExpanded;

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CODE BLOCK PROPERTIES (v0.4.1g)                                          │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Collection of code blocks extracted from this message.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCodeActions))]
    private ObservableCollection<CodeBlockViewModel> _codeBlocks = new();

    /// <summary>
    /// Whether this message contains code blocks.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCodeActions))]
    private bool _hasCodeBlocks;

    /// <summary>
    /// Whether this message contains applicable code blocks.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCodeActions))]
    private bool _hasApplicableCode;

    /// <summary>
    /// Number of applicable code blocks.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    [ObservableProperty]
    private int _applicableBlockCount;

    /// <summary>
    /// Total number of code blocks.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    [ObservableProperty]
    private int _totalBlockCount;

    /// <summary>
    /// Number of blocks that have been applied.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    [ObservableProperty]
    private int _appliedBlockCount;

    /// <summary>
    /// The currently streaming code block, if any.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    [ObservableProperty]
    private CodeBlockViewModel? _currentStreamingBlock;

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COMMAND BLOCK PROPERTIES (v0.5.4h)                                       │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Collection of terminal commands extracted from this message.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// <para>
    /// Commands are automatically extracted when Content changes
    /// for assistant messages using the command extractor service.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowCommandActions))]
    private ObservableCollection<CommandBlockViewModel> _commandBlocks = new();

    /// <summary>
    /// Whether this message contains executable commands.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// </remarks>
    public bool HasCommands => CommandBlocks.Count > 0;

    /// <summary>
    /// Number of commands in the message.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// </remarks>
    public int CommandCount => CommandBlocks.Count;

    /// <summary>
    /// Number of dangerous commands that require confirmation.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// </remarks>
    public int DangerousCommandCount => CommandBlocks.Count(c => c.IsDangerous);

    /// <summary>
    /// Whether there are any dangerous commands.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// </remarks>
    public bool HasDangerousCommands => DangerousCommandCount > 0;

    /// <summary>
    /// Summary text for bulk actions.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// </remarks>
    public string CommandSummary => CommandCount switch
    {
        0 => "",
        1 => "1 command",
        _ => $"{CommandCount} commands"
    };

    /// <summary>
    /// Dangerous commands warning text.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// </remarks>
    public string DangerWarning => DangerousCommandCount switch
    {
        0 => "",
        1 => "⚠️ 1 dangerous command",
        _ => $"⚠️ {DangerousCommandCount} dangerous commands"
    };

    /// <summary>
    /// Whether to show the command actions toolbar.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// </remarks>
    public bool ShowCommandActions =>
        HasCommands
        && Role == MessageRole.Assistant
        && !IsStreaming;

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COMPUTED PROPERTIES                                                      │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Gets whether this message is from the user.
    /// Used for conditional styling in XAML.
    /// </summary>
    public bool IsUser => Role == MessageRole.User;

    /// <summary>
    /// Gets whether this message is from the assistant.
    /// Used for conditional styling in XAML.
    /// </summary>
    public bool IsAssistant => Role == MessageRole.Assistant;

    /// <summary>
    /// Gets the display label for the message sender role.
    /// Maps internal role enum to user-friendly display text.
    /// </summary>
    public string RoleLabel => Role switch
    {
        MessageRole.User => "You",           // Display name for user messages
        MessageRole.Assistant => "AIntern",  // App name for AI responses
        MessageRole.System => "System",      // System prompts (rarely shown)
        _ => "Unknown"                       // Fallback for safety
    };

    /// <summary>
    /// Gets the calculated tokens per second for this message.
    /// Only available for completed assistant messages with both TokenCount and GenerationTime.
    /// </summary>
    public double? TokensPerSecond
    {
        get
        {
            if (TokenCount.HasValue && GenerationTime.HasValue && GenerationTime.Value.TotalSeconds > 0)
            {
                return TokenCount.Value / GenerationTime.Value.TotalSeconds;
            }
            return null;
        }
    }

    /// <summary>
    /// Gets a formatted string of performance statistics.
    /// Shows token count and generation speed when available.
    /// </summary>
    /// <remarks>
    /// Example output: "127 tokens | 42.3 tok/s"
    /// Returns null if statistics are not available.
    /// </remarks>
    public string? PerformanceStats
    {
        get
        {
            if (TokensPerSecond.HasValue && TokenCount.HasValue)
            {
                return $"{TokenCount} tokens | {TokensPerSecond:F1} tok/s";
            }
            return null;
        }
    }

    /// <summary>
    /// Gets whether this message has attached contexts.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.3.4h.</para>
    /// </remarks>
    public bool HasAttachedContexts => AttachedContexts.Count > 0;

    /// <summary>
    /// Gets the number of attached contexts.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.3.4h.</para>
    /// </remarks>
    public int AttachedContextCount => AttachedContexts.Count;

    /// <summary>
    /// Gets the total estimated tokens across all attached contexts.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.3.4h.</para>
    /// </remarks>
    public int TotalAttachedTokens => AttachedContexts.Sum(c => c.EstimatedTokens);

    /// <summary>
    /// Whether to show the code actions toolbar.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    public bool ShowCodeActions =>
        HasApplicableCode
        && Role == MessageRole.Assistant
        && !IsStreaming;

    /// <summary>
    /// Progress text for applied blocks.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    public string ApplyProgressText
    {
        get
        {
            if (ApplicableBlockCount == 0) return "";
            if (AppliedBlockCount == ApplicableBlockCount) return "All applied";
            return $"{AppliedBlockCount}/{ApplicableBlockCount} applied";
        }
    }

    /// <summary>
    /// Whether all applicable blocks have been applied.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    public bool IsFullyApplied =>
        ApplicableBlockCount > 0 && AppliedBlockCount == ApplicableBlockCount;

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CONSTRUCTORS                                                             │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageViewModel"/> class
    /// with default values. Creates new ID and timestamp.
    /// </summary>
    public ChatMessageViewModel()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        CodeBlocks.CollectionChanged += (_, _) => UpdateCodeBlockStats();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageViewModel"/> class
    /// from an existing <see cref="ChatMessage"/> domain model.
    /// </summary>
    /// <param name="message">The domain message to wrap.</param>
    public ChatMessageViewModel(ChatMessage message) : this()
    {
        // Copy all properties from domain model
        Id = message.Id;
        Content = message.Content;
        Role = message.Role;
        Timestamp = message.Timestamp;

        // Invert IsComplete to get IsStreaming
        IsStreaming = !message.IsComplete;

        // Copy performance statistics
        TokenCount = message.TokenCount;
        GenerationTime = message.GenerationTime;

        // v0.3.4h: Convert attached contexts to ViewModels
        foreach (var ctx in message.AttachedContexts)
        {
            AttachedContexts.Add(FileContextViewModel.FromFileContext(ctx));
        }
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CONTENT METHODS                                                          │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Appends a token to the message content during streaming.
    /// Called for each token received from the LLM.
    /// </summary>
    /// <param name="token">The token text to append.</param>
    public void AppendContent(string token)
    {
        // Concatenate token to existing content
        Content += token;
    }

    /// <summary>
    /// Marks the message as complete (no longer streaming).
    /// Called when generation finishes successfully.
    /// </summary>
    public void CompleteStreaming()
    {
        // Remove streaming indicator
        IsStreaming = false;
        CurrentStreamingBlock = null;
        OnPropertyChanged(nameof(ShowCodeActions));
    }

    /// <summary>
    /// Marks the message as cancelled by the user.
    /// Appends "[Cancelled]" to the content if not empty.
    /// </summary>
    public void MarkAsCancelled()
    {
        // Stop streaming indicator
        IsStreaming = false;

        // Complete any streaming block
        CurrentStreamingBlock?.CompleteStreaming(CurrentStreamingBlock.ToModel());
        CurrentStreamingBlock = null;
        
        // Add cancellation marker if there's content and it doesn't already trail off
        if (!string.IsNullOrEmpty(Content) && !Content.EndsWith("..."))
        {
            Content += " [Cancelled]";
        }
    }

    /// <summary>
    /// Converts this ViewModel back to a <see cref="ChatMessage"/> domain model.
    /// Used when saving to conversation history.
    /// </summary>
    /// <returns>A new <see cref="ChatMessage"/> instance with current values.</returns>
    public ChatMessage ToChatMessage() => new()
    {
        Id = Id,
        Content = Content,
        Role = Role,
        Timestamp = Timestamp,
        IsComplete = !IsStreaming,  // Invert IsStreaming to get IsComplete
        TokenCount = TokenCount,
        GenerationTime = GenerationTime,
        // v0.3.4h: Include attached contexts
        AttachedContexts = AttachedContexts.Select(c => c.ToFileContext()).ToList()
    };

    /// <summary>
    /// Toggles the expanded state of the attached contexts.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.3.4h.</para>
    /// </remarks>
    [RelayCommand]
    private void ToggleContextExpanded()
    {
        IsContextExpanded = !IsContextExpanded;
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CODE BLOCK PARSING (v0.4.1g)                                             │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Parse code blocks from the message content (non-streaming).
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    public void ParseCodeBlocks(
        ICodeBlockParserService parser,
        IReadOnlyList<string>? context = null)
    {
        if (Role != MessageRole.Assistant || string.IsNullOrEmpty(Content))
            return;

        var proposal = parser.CreateProposal(Content, Id, context);

        CodeBlocks.Clear();
        foreach (var block in proposal.CodeBlocks)
        {
            var vm = new CodeBlockViewModel(block)
            {
                MessageId = Id
            };
            vm.PropertyChanged += OnBlockPropertyChanged;
            CodeBlocks.Add(vm);
        }

        UpdateCodeBlockStats();
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STREAMING CODE BLOCK METHODS (v0.4.1g)                                   │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Begin a new code block during streaming.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    public CodeBlockViewModel BeginCodeBlock(PartialCodeBlock partial)
    {
        var vm = new CodeBlockViewModel(partial)
        {
            MessageId = Id,
            IsStreaming = true
        };
        vm.PropertyChanged += OnBlockPropertyChanged;

        CodeBlocks.Add(vm);
        CurrentStreamingBlock = vm;
        UpdateCodeBlockStats();

        return vm;
    }

    /// <summary>
    /// Update the currently streaming block with new content.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    public void UpdateStreamingBlock(string content)
    {
        CurrentStreamingBlock?.AppendContent(content);
    }

    /// <summary>
    /// Complete the current streaming block.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    public void CompleteCurrentBlock(CodeBlock finalBlock)
    {
        if (CurrentStreamingBlock != null)
        {
            CurrentStreamingBlock.CompleteStreaming(finalBlock);
            CurrentStreamingBlock = null;
        }
        UpdateCodeBlockStats();
    }

    /// <summary>
    /// Get a code block by ID.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    public CodeBlockViewModel? GetCodeBlock(Guid blockId)
    {
        return CodeBlocks.FirstOrDefault(b => b.Id == blockId);
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ STATISTICS (v0.4.1g)                                                     │
    // └─────────────────────────────────────────────────────────────────────────┘

    private void UpdateCodeBlockStats()
    {
        TotalBlockCount = CodeBlocks.Count;

        ApplicableBlockCount = CodeBlocks.Count(b =>
            b.BlockType is CodeBlockType.CompleteFile or CodeBlockType.Snippet or CodeBlockType.Config
            && b.HasTargetPath);

        AppliedBlockCount = CodeBlocks.Count(b =>
            b.Status == CodeBlockStatus.Applied);

        HasCodeBlocks = TotalBlockCount > 0;
        HasApplicableCode = ApplicableBlockCount > 0;

        OnPropertyChanged(nameof(ApplyProgressText));
        OnPropertyChanged(nameof(IsFullyApplied));
        OnPropertyChanged(nameof(ShowCodeActions));
    }

    private void OnBlockPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CodeBlockViewModel.Status))
        {
            UpdateCodeBlockStats();
        }
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ CODE BLOCK COMMANDS (v0.4.1g)                                            │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Copy all code blocks to clipboard.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    [RelayCommand]
    private void CopyAllCode()
    {
        var allCode = string.Join("\n\n", CodeBlocks.Select(b => b.Content));
        WeakReferenceMessenger.Default.Send(
            new CopyToClipboardRequestMessage(allCode)
            {
                SourceDescription = $"All code blocks ({CodeBlocks.Count})"
            });
    }

    /// <summary>
    /// Apply all applicable code blocks.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(HasApplicableCode))]
    private void ApplyAllCode()
    {
        WeakReferenceMessenger.Default.Send(
            new ApplyAllChangesRequestMessage(this) { SkipDiffPreview = false });
    }

    /// <summary>
    /// Reject all applicable code blocks.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.4.1g.</para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(HasApplicableCode))]
    private void RejectAllCode()
    {
        foreach (var block in CodeBlocks.Where(b =>
            b.Status == CodeBlockStatus.Pending && b.IsApplicable))
        {
            block.Reject();
        }
        UpdateCodeBlockStats();
    }

    // ┌─────────────────────────────────────────────────────────────────────────┐
    // │ COMMAND BLOCK METHODS (v0.5.4h)                                          │
    // └─────────────────────────────────────────────────────────────────────────┘

    /// <summary>
    /// Set the extracted command blocks for this message.
    /// </summary>
    /// <param name="commandViewModels">The command block ViewModels to set.</param>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// <para>
    /// Called by ChatViewModel after extracting commands from the message content.
    /// </para>
    /// </remarks>
    public void SetCommandBlocks(IEnumerable<CommandBlockViewModel> commandViewModels)
    {
        // Dispose existing commands
        foreach (var cmd in CommandBlocks)
        {
            cmd.Dispose();
        }
        CommandBlocks.Clear();

        // Add new commands
        foreach (var vm in commandViewModels)
        {
            CommandBlocks.Add(vm);
        }

        NotifyCommandProperties();
    }

    /// <summary>
    /// Notify all command-related property changes.
    /// </summary>
    private void NotifyCommandProperties()
    {
        OnPropertyChanged(nameof(HasCommands));
        OnPropertyChanged(nameof(CommandCount));
        OnPropertyChanged(nameof(DangerousCommandCount));
        OnPropertyChanged(nameof(HasDangerousCommands));
        OnPropertyChanged(nameof(CommandSummary));
        OnPropertyChanged(nameof(DangerWarning));
        OnPropertyChanged(nameof(ShowCommandActions));
    }

    /// <summary>
    /// Execute all commands in sequence, stopping on first failure.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// <para>
    /// Dangerous commands that have not been confirmed are skipped.
    /// Execution stops at the first command that fails.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task ExecuteAllCommandsAsync()
    {
        if (!HasCommands)
            return;

        foreach (var block in CommandBlocks)
        {
            // Skip if dangerous and not confirmed
            if (block.IsDangerous && !block.DangerConfirmed)
                continue;

            // Skip if not executable
            if (!block.CanExecute)
                continue;

            await block.ExecuteCommand.ExecuteAsync(null);

            // Stop if one fails
            if (block.Status == CommandBlockStatus.Failed)
                break;
        }
    }

    /// <summary>
    /// Copy all commands to clipboard as a script.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// <para>
    /// Commands are joined with newlines to form an executable script.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private void CopyAllCommands()
    {
        if (!HasCommands)
            return;

        var allCommands = string.Join("\n", CommandBlocks.Select(b => b.CommandText));
        WeakReferenceMessenger.Default.Send(
            new CopyToClipboardRequestMessage(allCommands)
            {
                SourceDescription = $"All commands ({CommandBlocks.Count})"
            });
    }

    /// <summary>
    /// Dispose all command blocks when the message is no longer needed.
    /// </summary>
    /// <remarks>
    /// <para>Added in v0.5.4h.</para>
    /// </remarks>
    public void DisposeCommands()
    {
        foreach (var block in CommandBlocks)
        {
            block.Dispose();
        }
        CommandBlocks.Clear();
    }
}

