using System.Text.Json;
using FolderSizeVisualizer.Core.Models;

namespace FolderSizeVisualizer.Core.Services;

public sealed class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_settingsPath)) return new AppSettings();
        await using var stream = File.OpenRead(_settingsPath);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream) ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, new JsonSerializerOptions { WriteIndented = true });
    }
}
