using FolderSizeVisualizer.Core.Models;
using FolderSizeVisualizer.Core.Services;
using System.IO;

namespace FolderSizeVisualizer.Core.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task SaveAndLoadRoundTripsSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var service = new SettingsService(path);
        var settings = new AppSettings { Theme = ThemeMode.Dark, DeleteMode = DeleteMode.Direct };

        await service.SaveAsync(settings);
        var loaded = await service.LoadAsync();

        Assert.Equal(ThemeMode.Dark, loaded.Theme);
        Assert.Equal(DeleteMode.Direct, loaded.DeleteMode);
        File.Delete(path);
    }

    [Fact]
    public async Task LoadMissingFileReturnsDefaults()
    {
        var service = new SettingsService(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));

        var loaded = await service.LoadAsync();

        Assert.Equal(AppLanguage.Chinese, loaded.Language);
        Assert.Equal(DeleteMode.RecycleBin, loaded.DeleteMode);
    }
}
