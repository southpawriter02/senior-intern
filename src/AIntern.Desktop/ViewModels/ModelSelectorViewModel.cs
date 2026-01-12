using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIntern.Core.Events;
using AIntern.Core.Exceptions;
using AIntern.Core.Interfaces;
using AIntern.Core.Models;

namespace AIntern.Desktop.ViewModels;

/// <summary>
/// ViewModel for the model selection and loading panel.
/// Handles GGUF model file selection, loading with progress, and unloading.
/// </summary>
/// <remarks>
/// <para>
/// Uses the Avalonia <see cref="IStorageProvider"/> for cross-platform file picker dialogs.
/// </para>
/// <para>
/// Automatically saves the last loaded model path to settings for restoration on next launch.
/// </para>
/// </remarks>
public partial class ModelSelectorViewModel : ViewModelBase
{
    // Service dependencies injected via constructor
    private readonly ILlmService _llmService;
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// Gets or sets whether a model is currently loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isModelLoaded;

    /// <summary>
    /// Gets or sets the filename of the loaded model.
    /// </summary>
    [ObservableProperty]
    private string? _modelFileName;

    /// <summary>
    /// Gets or sets the formatted file size of the loaded model.
    /// </summary>
    [ObservableProperty]
    private string? _modelSizeDisplay;

    /// <summary>
    /// Gets or sets whether a model is currently being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets or sets the current loading stage description.
    /// </summary>
    [ObservableProperty]
    private string? _loadingStage;

    /// <summary>
    /// Gets or sets the loading progress percentage (0-100).
    /// </summary>
    [ObservableProperty]
    private double _loadingProgress;

    /// <summary>
    /// Gets or sets whether an error occurred during the last operation.
    /// </summary>
    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelSelectorViewModel"/> class.
    /// </summary>
    /// <param name="llmService">The LLM service for model operations.</param>
    /// <param name="settingsService">The settings service for persisting model path.</param>
    public ModelSelectorViewModel(ILlmService llmService, ISettingsService settingsService)
    {
        _llmService = llmService;
        _settingsService = settingsService;

        // Subscribe to model state changes to keep UI in sync
        _llmService.ModelStateChanged += OnModelStateChanged;

        // Initialize state from current service state (in case model was pre-loaded)
        IsModelLoaded = _llmService.IsModelLoaded;
        ModelFileName = _llmService.CurrentModelName;
    }

    /// <summary>
    /// Handles model state change events to update UI properties.
    /// </summary>
    private void OnModelStateChanged(object? sender, ModelStateChangedEventArgs e)
    {
        // Sync local state with service state
        IsModelLoaded = e.IsLoaded;
        ModelFileName = e.ModelName;

        // Calculate and display file size when model is loaded
        if (e.IsLoaded && e.ModelPath is not null)
        {
            var fileInfo = new FileInfo(e.ModelPath);
            ModelSizeDisplay = FormatFileSize(fileInfo.Length);
        }
        else
        {
            // Clear size display when model is unloaded
            ModelSizeDisplay = null;
        }
    }

    /// <summary>
    /// Opens a file picker dialog for selecting a GGUF model file.
    /// </summary>
    [RelayCommand]
    private async Task SelectModelAsync()
    {
        // Get main window for hosting the file picker dialog
        var window = GetMainWindow();
        if (window is null) return;

        // Show native file picker with GGUF filter
        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select GGUF Model",
            AllowMultiple = false, // Only allow single file selection
            FileTypeFilter = new[]
            {
                // Primary filter for GGUF model files
                new FilePickerFileType("GGUF Models")
                {
                    Patterns = new[] { "*.gguf" }
                },
                // Fallback to show all files if needed
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        // User cancelled the dialog
        if (files.Count == 0) return;

        // Extract the local file path from the storage item
        var file = files[0];
        var path = file.TryGetLocalPath();

        // Handle sandboxed environments where local path may not be available
        if (path is null)
        {
            SetError("Could not access the selected file.");
            HasError = true;
            return;
        }

        // Proceed to load the selected model
        await LoadModelAsync(path);
    }

    /// <summary>
    /// Loads a model from the specified path with progress reporting.
    /// </summary>
    /// <param name="modelPath">The file path to the GGUF model.</param>
    private async Task LoadModelAsync(string modelPath)
    {
        // Reset UI state before starting load
        ClearError();
        HasError = false;
        IsLoading = true;
        LoadingProgress = 0;
        LoadingStage = "Initializing...";

        // Create progress reporter that updates UI properties
        var progress = new Progress<ModelLoadProgress>(p =>
        {
            // Update loading stage text and progress bar
            LoadingStage = p.Message ?? p.Stage;
            LoadingProgress = p.PercentComplete;
        });

        try
        {
            // Get current settings for model configuration
            var settings = _settingsService.CurrentSettings;
            
            // Build options from user settings
            var options = new ModelLoadOptions(
                ContextSize: settings.DefaultContextSize,   // Token context window
                GpuLayerCount: settings.DefaultGpuLayers,   // GPU offloading (-1 = auto)
                BatchSize: settings.DefaultBatchSize        // Processing batch size
            );

            // Perform the actual model load (may take several seconds)
            await _llmService.LoadModelAsync(modelPath, options, progress);

            // Persist the model path for next session
            settings.LastModelPath = modelPath;
            await _settingsService.SaveSettingsAsync(settings);
        }
        catch (ModelLoadException ex)
        {
            // Known model loading error (file not found, corrupt, etc.)
            SetError($"Failed to load model: {ex.Message}");
            HasError = true;
        }
        catch (OperationCanceledException)
        {
            // User cancelled the load - this is not an error condition
        }
        catch (Exception ex)
        {
            // Unexpected error - log and display to user
            SetError($"Unexpected error: {ex.Message}");
            HasError = true;
        }
        finally
        {
            // Always reset loading state, regardless of outcome
            IsLoading = false;
            LoadingStage = null;
        }
    }

    /// <summary>
    /// Unloads the currently loaded model and releases resources.
    /// </summary>
    [RelayCommand]
    private async Task UnloadModelAsync()
    {
        try
        {
            // Release model resources (VRAM, context, etc.)
            await _llmService.UnloadModelAsync();
        }
        catch (Exception ex)
        {
            // Unload failures are rare but should be reported
            SetError($"Failed to unload model: {ex.Message}");
            HasError = true;
        }
    }

    /// <summary>
    /// Gets the main application window for dialog hosting.
    /// </summary>
    /// <returns>The main window, or null if not available.</returns>
    private static Window? GetMainWindow()
    {
        // Access the application lifetime to get the main window
        // This pattern works for classic desktop applications
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    /// <summary>
    /// Formats a byte count as a human-readable file size.
    /// </summary>
    /// <param name="bytes">The file size in bytes.</param>
    /// <returns>A formatted string like "4.2 GB".</returns>
    private static string FormatFileSize(long bytes)
    {
        // Size unit labels in ascending order
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        double size = bytes;

        // Keep dividing by 1024 until we find the appropriate unit
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        // Format with up to 2 decimal places
        return $"{size:0.##} {sizes[order]}";
    }
}
