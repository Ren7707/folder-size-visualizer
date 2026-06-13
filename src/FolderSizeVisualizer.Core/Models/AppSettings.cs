namespace FolderSizeVisualizer.Core.Models;

public sealed class AppSettings
{
    public AppLanguage Language { get; set; } = AppLanguage.Chinese;
    public ThemeMode Theme { get; set; } = ThemeMode.Light;
    public DeleteMode DeleteMode { get; set; } = DeleteMode.RecycleBin;
    public string? BackgroundImagePath { get; set; }
    public double VisualizationOpacity { get; set; } = 0.92;
    public double BackgroundBlur { get; set; } = 12;
}
