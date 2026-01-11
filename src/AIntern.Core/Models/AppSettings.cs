namespace AIntern.Core.Models;

public sealed class AppSettings
{
    // Model Settings
    public string? LastModelPath { get; set; }
    public uint DefaultContextSize { get; set; } = 4096;
    public int DefaultGpuLayers { get; set; } = -1; // -1 = auto-detect
    public uint DefaultBatchSize { get; set; } = 512;

    // Inference Settings
    public float Temperature { get; set; } = 0.7f;
    public float TopP { get; set; } = 0.9f;
    public int MaxTokens { get; set; } = 2048;

    // System Prompt Settings
    public Guid? CurrentSystemPromptId { get; set; }

    // UI Settings
    public string Theme { get; set; } = "Dark";
    public double SidebarWidth { get; set; } = 280;

    // Window State
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 800;
    public double? WindowX { get; set; }
    public double? WindowY { get; set; }
}
