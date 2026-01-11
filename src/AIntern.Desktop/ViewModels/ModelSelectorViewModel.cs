using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SeniorIntern.Core.Events;
using SeniorIntern.Core.Exceptions;
using SeniorIntern.Core.Interfaces;
using SeniorIntern.Core.Models;

namespace SeniorIntern.Desktop.ViewModels;

public partial class ModelSelectorViewModel : ViewModelBase
{
    private readonly ILlmService _llmService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private bool _isModelLoaded;

    [ObservableProperty]
    private string? _modelFileName;

    [ObservableProperty]
    private string? _modelSizeDisplay;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _loadingStage;

    [ObservableProperty]
    private double _loadingProgress;

    [ObservableProperty]
    private bool _hasError;

    public ModelSelectorViewModel(ILlmService llmService, ISettingsService settingsService)
    {
        _llmService = llmService;
        _settingsService = settingsService;

        _llmService.ModelStateChanged += OnModelStateChanged;

        // Update state from service
        IsModelLoaded = _llmService.IsModelLoaded;
        ModelFileName = _llmService.CurrentModelName;
    }

    private void OnModelStateChanged(object? sender, ModelStateChangedEventArgs e)
    {
        IsModelLoaded = e.IsLoaded;
        ModelFileName = e.ModelName;

        if (e.IsLoaded && e.ModelPath is not null)
        {
            var fileInfo = new FileInfo(e.ModelPath);
            ModelSizeDisplay = FormatFileSize(fileInfo.Length);
        }
        else
        {
            ModelSizeDisplay = null;
        }
    }

    [RelayCommand]
    private async Task SelectModelAsync()
    {
        var window = GetMainWindow();
        if (window is null) return;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select GGUF Model",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("GGUF Models")
                {
                    Patterns = new[] { "*.gguf" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count == 0) return;

        var file = files[0];
        var path = file.TryGetLocalPath();

        if (path is null)
        {
            SetError("Could not access the selected file.");
            HasError = true;
            return;
        }

        await LoadModelAsync(path);
    }

    private async Task LoadModelAsync(string modelPath)
    {
        ClearError();
        HasError = false;
        IsLoading = true;
        LoadingProgress = 0;
        LoadingStage = "Initializing...";

        var progress = new Progress<ModelLoadProgress>(p =>
        {
            LoadingStage = p.Message ?? p.Stage;
            LoadingProgress = p.PercentComplete;
        });

        try
        {
            var settings = _settingsService.CurrentSettings;
            var options = new ModelLoadOptions(
                ContextSize: settings.DefaultContextSize,
                GpuLayerCount: settings.DefaultGpuLayers,
                BatchSize: settings.DefaultBatchSize
            );

            await _llmService.LoadModelAsync(modelPath, options, progress);

            // Save last used model path
            settings.LastModelPath = modelPath;
            await _settingsService.SaveSettingsAsync(settings);
        }
        catch (ModelLoadException ex)
        {
            SetError($"Failed to load model: {ex.Message}");
            HasError = true;
        }
        catch (OperationCanceledException)
        {
            // User cancelled - not an error
        }
        catch (Exception ex)
        {
            SetError($"Unexpected error: {ex.Message}");
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            LoadingStage = null;
        }
    }

    [RelayCommand]
    private async Task UnloadModelAsync()
    {
        try
        {
            await _llmService.UnloadModelAsync();
        }
        catch (Exception ex)
        {
            SetError($"Failed to unload model: {ex.Message}");
            HasError = true;
        }
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
