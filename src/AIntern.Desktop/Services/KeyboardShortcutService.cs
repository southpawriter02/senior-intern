namespace AIntern.Desktop.Services;

using Avalonia.Input;
using AIntern.Desktop.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Interface for managing keyboard shortcuts (v0.3.5g).
/// </summary>
public interface IKeyboardShortcutService
{
    /// <summary>
    /// Registers a keyboard shortcut.
    /// </summary>
    void Register(Key key, KeyModifiers modifiers, string commandId, string description);

    /// <summary>
    /// Handles a key press and executes matching command.
    /// </summary>
    bool HandleKeyPress(KeyEventArgs e);

    /// <summary>
    /// Gets all registered shortcuts.
    /// </summary>
    IReadOnlyList<ShortcutInfo> GetAllShortcuts();

    /// <summary>
    /// Event raised when a command should be executed.
    /// </summary>
    event EventHandler<string>? CommandRequested;
}

/// <summary>
/// Service for managing keyboard shortcuts (v0.3.5g).
/// </summary>
public sealed class KeyboardShortcutService : IKeyboardShortcutService
{
    private readonly Dictionary<(Key, KeyModifiers), ShortcutInfo> _shortcuts = new();
    private readonly ILogger<KeyboardShortcutService> _logger;

    /// <inheritdoc />
    public event EventHandler<string>? CommandRequested;

    /// <summary>
    /// Initializes the keyboard shortcut service with default shortcuts.
    /// </summary>
    public KeyboardShortcutService(ILogger<KeyboardShortcutService> logger)
    {
        _logger = logger;
        _logger.LogDebug("[INIT] KeyboardShortcutService initializing");
        RegisterDefaultShortcuts();
        _logger.LogInformation("[INIT] KeyboardShortcutService registered {Count} shortcuts", _shortcuts.Count);
    }

    /// <inheritdoc />
    public void Register(Key key, KeyModifiers modifiers, string commandId, string description)
    {
        var shortcutKey = (key, modifiers);
        _shortcuts[shortcutKey] = new ShortcutInfo
        {
            Key = key,
            Modifiers = modifiers,
            CommandId = commandId,
            Description = description,
            Category = GetCategory(commandId)
        };
        _logger.LogDebug("[REG] Registered shortcut: {Key}+{Mods} → {Command}", key, modifiers, commandId);
    }

    /// <inheritdoc />
    public bool HandleKeyPress(KeyEventArgs e)
    {
        var key = (e.Key, e.KeyModifiers);

        if (_shortcuts.TryGetValue(key, out var shortcut))
        {
            _logger.LogDebug("[KEY] Matched shortcut: {Command}", shortcut.CommandId);
            CommandRequested?.Invoke(this, shortcut.CommandId);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<ShortcutInfo> GetAllShortcuts()
    {
        return _shortcuts.Values
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Description)
            .ToList();
    }

    /// <summary>
    /// Registers the default application shortcuts.
    /// </summary>
    private void RegisterDefaultShortcuts()
    {
        // === Workspace ===
        Register(Key.O, KeyModifiers.Control, "workspace.open", "Open Folder");
        Register(Key.O, KeyModifiers.Control | KeyModifiers.Shift, "file.open", "Open File");

        // === File ===
        Register(Key.N, KeyModifiers.Control, "file.new", "New File");
        Register(Key.S, KeyModifiers.Control, "file.save", "Save");
        Register(Key.S, KeyModifiers.Control | KeyModifiers.Shift, "file.saveAll", "Save All");

        // === Editor ===
        Register(Key.W, KeyModifiers.Control, "editor.closeTab", "Close Tab");
        Register(Key.Tab, KeyModifiers.Control, "editor.nextTab", "Next Tab");
        Register(Key.Tab, KeyModifiers.Control | KeyModifiers.Shift, "editor.previousTab", "Previous Tab");
        Register(Key.G, KeyModifiers.Control, "editor.goToLine", "Go to Line");
        Register(Key.F, KeyModifiers.Control, "editor.find", "Find");
        Register(Key.H, KeyModifiers.Control, "editor.replace", "Find and Replace");
        Register(Key.P, KeyModifiers.Control, "quickOpen", "Quick Open");

        // === Chat ===
        Register(Key.Enter, KeyModifiers.Control, "chat.send", "Send Message");
        Register(Key.L, KeyModifiers.Control, "chat.clear", "Clear Chat");

        // === Context ===
        Register(Key.A, KeyModifiers.Control | KeyModifiers.Shift, "context.attachSelection", "Attach Selection");
        Register(Key.E, KeyModifiers.Control | KeyModifiers.Shift, "context.attachFile", "Attach Current File");

        // === Explorer ===
        Register(Key.F2, KeyModifiers.None, "explorer.rename", "Rename");

        // === Settings ===
        Register(Key.OemComma, KeyModifiers.Control, "settings.open", "Open Settings");

        // === Sidebar ===
        Register(Key.B, KeyModifiers.Control, "sidebar.toggle", "Toggle Sidebar");
    }

    /// <summary>
    /// Maps command ID prefix to category name.
    /// </summary>
    private static string GetCategory(string commandId)
    {
        var prefix = commandId.Split('.')[0];
        return prefix switch
        {
            "workspace" => "Workspace",
            "file" => "File",
            "editor" => "Editor",
            "chat" => "Chat",
            "context" => "Context",
            "explorer" => "Explorer",
            "settings" => "Settings",
            "sidebar" => "View",
            "quickOpen" => "Navigation",
            _ => "General"
        };
    }
}
