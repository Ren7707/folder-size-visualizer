# Folder Size Visualizer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows WPF app that scans a folder and presents storage usage as an interactive treemap with safe deletion and user settings.

**Architecture:** Use a WPF shell backed by a testable .NET class library. Keep filesystem scanning, treemap layout, settings, sorting, localization, and file operations in focused services consumed by the WPF view model.

**Tech Stack:** .NET 8 SDK, WPF, xUnit, System.Text.Json, Microsoft.VisualBasic.FileIO for Recycle Bin deletion.

---

## Prerequisite

This machine currently has .NET runtimes but no .NET SDK. Install the .NET 8 SDK before executing implementation tasks.

Verify:

```powershell
dotnet --list-sdks
```

Expected: output includes an `8.0.x` SDK.

## File Structure

- Create: `FolderSizeVisualizer.sln`
- Create: `src/FolderSizeVisualizer.App/FolderSizeVisualizer.App.csproj`
- Create: `src/FolderSizeVisualizer.App/App.xaml`
- Create: `src/FolderSizeVisualizer.App/App.xaml.cs`
- Create: `src/FolderSizeVisualizer.App/MainWindow.xaml`
- Create: `src/FolderSizeVisualizer.App/MainWindow.xaml.cs`
- Create: `src/FolderSizeVisualizer.App/ViewModels/MainViewModel.cs`
- Create: `src/FolderSizeVisualizer.App/ViewModels/ObservableObject.cs`
- Create: `src/FolderSizeVisualizer.App/ViewModels/RelayCommand.cs`
- Create: `src/FolderSizeVisualizer.App/Controls/TreemapView.cs`
- Create: `src/FolderSizeVisualizer.App/Resources/Strings.zh-CN.xaml`
- Create: `src/FolderSizeVisualizer.App/Resources/Strings.en-US.xaml`
- Create: `src/FolderSizeVisualizer.App/Resources/Themes.Light.xaml`
- Create: `src/FolderSizeVisualizer.App/Resources/Themes.Dark.xaml`
- Create: `src/FolderSizeVisualizer.Core/FolderSizeVisualizer.Core.csproj`
- Create: `src/FolderSizeVisualizer.Core/Models/FileNode.cs`
- Create: `src/FolderSizeVisualizer.Core/Models/AppSettings.cs`
- Create: `src/FolderSizeVisualizer.Core/Models/TreemapRectangle.cs`
- Create: `src/FolderSizeVisualizer.Core/Services/FolderScanner.cs`
- Create: `src/FolderSizeVisualizer.Core/Services/TreemapLayoutService.cs`
- Create: `src/FolderSizeVisualizer.Core/Services/NodeSorter.cs`
- Create: `src/FolderSizeVisualizer.Core/Services/SettingsService.cs`
- Create: `src/FolderSizeVisualizer.Core/Services/FileOperationService.cs`
- Create: `tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj`
- Create: `tests/FolderSizeVisualizer.Core.Tests/FolderScannerTests.cs`
- Create: `tests/FolderSizeVisualizer.Core.Tests/TreemapLayoutServiceTests.cs`
- Create: `tests/FolderSizeVisualizer.Core.Tests/NodeSorterTests.cs`
- Create: `tests/FolderSizeVisualizer.Core.Tests/SettingsServiceTests.cs`

## Task 1: Scaffold Solution

**Files:**
- Create: `FolderSizeVisualizer.sln`
- Create: `src/FolderSizeVisualizer.App/FolderSizeVisualizer.App.csproj`
- Create: `src/FolderSizeVisualizer.Core/FolderSizeVisualizer.Core.csproj`
- Create: `tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj`

- [ ] **Step 1: Create solution and projects**

Run:

```powershell
dotnet new sln -n FolderSizeVisualizer
dotnet new wpf -n FolderSizeVisualizer.App -o src/FolderSizeVisualizer.App -f net8.0-windows
dotnet new classlib -n FolderSizeVisualizer.Core -o src/FolderSizeVisualizer.Core -f net8.0
dotnet new xunit -n FolderSizeVisualizer.Core.Tests -o tests/FolderSizeVisualizer.Core.Tests -f net8.0
dotnet sln add src/FolderSizeVisualizer.App/FolderSizeVisualizer.App.csproj
dotnet sln add src/FolderSizeVisualizer.Core/FolderSizeVisualizer.Core.csproj
dotnet sln add tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj
dotnet add src/FolderSizeVisualizer.App/FolderSizeVisualizer.App.csproj reference src/FolderSizeVisualizer.Core/FolderSizeVisualizer.Core.csproj
dotnet add tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj reference src/FolderSizeVisualizer.Core/FolderSizeVisualizer.Core.csproj
```

Expected: all commands complete without errors.

- [ ] **Step 2: Build empty solution**

Run:

```powershell
dotnet build FolderSizeVisualizer.sln
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```powershell
git add FolderSizeVisualizer.sln src tests
git commit -m "chore: scaffold WPF solution"
```

## Task 2: Core Models

**Files:**
- Create: `src/FolderSizeVisualizer.Core/Models/FileNode.cs`
- Create: `src/FolderSizeVisualizer.Core/Models/AppSettings.cs`
- Create: `src/FolderSizeVisualizer.Core/Models/TreemapRectangle.cs`

- [ ] **Step 1: Add model code**

Create `FileNode.cs`:

```csharp
namespace FolderSizeVisualizer.Core.Models;

public enum FileNodeKind { File, Directory }
public enum ScanState { Pending, Scanning, Complete, Error }
public enum SortMode { SizeDescending, Type, Name, ModifiedTime, FilesFirst, FoldersFirst }
public enum DeleteMode { RecycleBin, Direct }
public enum ThemeMode { Light, Dark }
public enum AppLanguage { Chinese, English }

public sealed class FileNode
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required FileNodeKind Kind { get; init; }
    public long SizeBytes { get; set; }
    public string Extension { get; init; } = "";
    public DateTime ModifiedTime { get; init; }
    public ScanState ScanState { get; set; } = ScanState.Pending;
    public string? ErrorMessage { get; set; }
    public List<FileNode> Children { get; } = new();
}
```

Create `AppSettings.cs`:

```csharp
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
```

Create `TreemapRectangle.cs`:

```csharp
namespace FolderSizeVisualizer.Core.Models;

public sealed record TreemapRectangle(
    FileNode Node,
    double X,
    double Y,
    double Width,
    double Height);
```

- [ ] **Step 2: Build**

Run:

```powershell
dotnet build FolderSizeVisualizer.sln
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```powershell
git add src/FolderSizeVisualizer.Core/Models
git commit -m "feat: add core storage models"
```

## Task 3: Sorting Service

**Files:**
- Create: `src/FolderSizeVisualizer.Core/Services/NodeSorter.cs`
- Create: `tests/FolderSizeVisualizer.Core.Tests/NodeSorterTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using FolderSizeVisualizer.Core.Models;
using FolderSizeVisualizer.Core.Services;

namespace FolderSizeVisualizer.Core.Tests;

public sealed class NodeSorterTests
{
    [Fact]
    public void SortBySizeDescendingOrdersLargestFirst()
    {
        var nodes = new[]
        {
            new FileNode { Name = "small.txt", FullPath = "small.txt", Kind = FileNodeKind.File, SizeBytes = 10 },
            new FileNode { Name = "large.bin", FullPath = "large.bin", Kind = FileNodeKind.File, SizeBytes = 50 }
        };

        var sorted = NodeSorter.Sort(nodes, SortMode.SizeDescending).ToList();

        Assert.Equal("large.bin", sorted[0].Name);
    }

    [Fact]
    public void FoldersFirstPlacesDirectoriesBeforeFiles()
    {
        var nodes = new[]
        {
            new FileNode { Name = "a.txt", FullPath = "a.txt", Kind = FileNodeKind.File },
            new FileNode { Name = "folder", FullPath = "folder", Kind = FileNodeKind.Directory }
        };

        var sorted = NodeSorter.Sort(nodes, SortMode.FoldersFirst).ToList();

        Assert.Equal(FileNodeKind.Directory, sorted[0].Kind);
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

```powershell
dotnet test tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj --filter NodeSorterTests
```

Expected: fails because `NodeSorter` does not exist.

- [ ] **Step 3: Implement sorter**

```csharp
using FolderSizeVisualizer.Core.Models;

namespace FolderSizeVisualizer.Core.Services;

public static class NodeSorter
{
    public static IEnumerable<FileNode> Sort(IEnumerable<FileNode> nodes, SortMode mode)
    {
        return mode switch
        {
            SortMode.SizeDescending => nodes.OrderByDescending(n => n.SizeBytes).ThenBy(n => n.Name),
            SortMode.Type => nodes.OrderBy(n => n.Extension).ThenByDescending(n => n.SizeBytes),
            SortMode.Name => nodes.OrderBy(n => n.Name),
            SortMode.ModifiedTime => nodes.OrderByDescending(n => n.ModifiedTime),
            SortMode.FilesFirst => nodes.OrderByDescending(n => n.Kind == FileNodeKind.File).ThenByDescending(n => n.SizeBytes),
            SortMode.FoldersFirst => nodes.OrderByDescending(n => n.Kind == FileNodeKind.Directory).ThenByDescending(n => n.SizeBytes),
            _ => nodes
        };
    }
}
```

- [ ] **Step 4: Run tests**

```powershell
dotnet test tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj --filter NodeSorterTests
```

Expected: tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/FolderSizeVisualizer.Core/Services/NodeSorter.cs tests/FolderSizeVisualizer.Core.Tests/NodeSorterTests.cs
git commit -m "feat: add node sorting"
```

## Task 4: Treemap Layout Service

**Files:**
- Create: `src/FolderSizeVisualizer.Core/Services/TreemapLayoutService.cs`
- Create: `tests/FolderSizeVisualizer.Core.Tests/TreemapLayoutServiceTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using FolderSizeVisualizer.Core.Models;
using FolderSizeVisualizer.Core.Services;

namespace FolderSizeVisualizer.Core.Tests;

public sealed class TreemapLayoutServiceTests
{
    [Fact]
    public void LayoutReturnsOneRectanglePerPositiveNode()
    {
        var nodes = new[]
        {
            new FileNode { Name = "a", FullPath = "a", Kind = FileNodeKind.File, SizeBytes = 30 },
            new FileNode { Name = "b", FullPath = "b", Kind = FileNodeKind.File, SizeBytes = 70 }
        };

        var result = TreemapLayoutService.Layout(nodes, 0, 0, 100, 100).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.Width > 0 && r.Height > 0));
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

```powershell
dotnet test tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj --filter TreemapLayoutServiceTests
```

Expected: fails because `TreemapLayoutService` does not exist.

- [ ] **Step 3: Implement simple slice-and-dice treemap**

```csharp
using FolderSizeVisualizer.Core.Models;

namespace FolderSizeVisualizer.Core.Services;

public static class TreemapLayoutService
{
    public static IReadOnlyList<TreemapRectangle> Layout(IEnumerable<FileNode> nodes, double x, double y, double width, double height)
    {
        var positive = nodes.Where(n => n.SizeBytes > 0).ToList();
        var total = positive.Sum(n => n.SizeBytes);
        if (total <= 0 || width <= 0 || height <= 0) return Array.Empty<TreemapRectangle>();

        var result = new List<TreemapRectangle>();
        var offset = 0.0;
        var horizontal = width >= height;

        foreach (var node in positive)
        {
            var ratio = (double)node.SizeBytes / total;
            if (horizontal)
            {
                var itemWidth = width * ratio;
                result.Add(new TreemapRectangle(node, x + offset, y, itemWidth, height));
                offset += itemWidth;
            }
            else
            {
                var itemHeight = height * ratio;
                result.Add(new TreemapRectangle(node, x, y + offset, width, itemHeight));
                offset += itemHeight;
            }
        }

        return result;
    }
}
```

- [ ] **Step 4: Run tests**

```powershell
dotnet test tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj --filter TreemapLayoutServiceTests
```

Expected: tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/FolderSizeVisualizer.Core/Services/TreemapLayoutService.cs tests/FolderSizeVisualizer.Core.Tests/TreemapLayoutServiceTests.cs
git commit -m "feat: add treemap layout"
```

## Task 5: Settings Service

**Files:**
- Create: `src/FolderSizeVisualizer.Core/Services/SettingsService.cs`
- Create: `tests/FolderSizeVisualizer.Core.Tests/SettingsServiceTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using FolderSizeVisualizer.Core.Models;
using FolderSizeVisualizer.Core.Services;

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
}
```

- [ ] **Step 2: Run tests and verify failure**

```powershell
dotnet test tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj --filter SettingsServiceTests
```

Expected: fails because `SettingsService` does not exist.

- [ ] **Step 3: Implement service**

```csharp
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
```

- [ ] **Step 4: Run tests**

```powershell
dotnet test tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj --filter SettingsServiceTests
```

Expected: tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/FolderSizeVisualizer.Core/Services/SettingsService.cs tests/FolderSizeVisualizer.Core.Tests/SettingsServiceTests.cs
git commit -m "feat: persist app settings"
```

## Task 6: Folder Scanner

**Files:**
- Create: `src/FolderSizeVisualizer.Core/Services/FolderScanner.cs`
- Create: `tests/FolderSizeVisualizer.Core.Tests/FolderScannerTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using FolderSizeVisualizer.Core.Models;
using FolderSizeVisualizer.Core.Services;

namespace FolderSizeVisualizer.Core.Tests;

public sealed class FolderScannerTests
{
    [Fact]
    public async Task ScanFirstLevelReturnsImmediateChildren()
    {
        var root = Directory.CreateTempSubdirectory();
        await File.WriteAllTextAsync(Path.Combine(root.FullName, "a.txt"), "12345");
        Directory.CreateDirectory(Path.Combine(root.FullName, "child"));

        var scanner = new FolderScanner();
        var node = await scanner.ScanFirstLevelAsync(root.FullName, CancellationToken.None);

        Assert.Equal(2, node.Children.Count);
        Assert.Contains(node.Children, n => n.Name == "a.txt" && n.SizeBytes == 5);
        root.Delete(true);
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

```powershell
dotnet test tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj --filter FolderScannerTests
```

Expected: fails because `FolderScanner` does not exist.

- [ ] **Step 3: Implement first-level scan and recursive size fill**

```csharp
using FolderSizeVisualizer.Core.Models;

namespace FolderSizeVisualizer.Core.Services;

public sealed class FolderScanner
{
    public Task<FileNode> ScanFirstLevelAsync(string folderPath, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(folderPath)) throw new DirectoryNotFoundException(folderPath);

        var directory = new DirectoryInfo(folderPath);
        var root = new FileNode
        {
            Name = directory.Name,
            FullPath = directory.FullName,
            Kind = FileNodeKind.Directory,
            ModifiedTime = directory.LastWriteTime,
            ScanState = ScanState.Scanning
        };

        foreach (var childDirectory in directory.EnumerateDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();
            root.Children.Add(new FileNode
            {
                Name = childDirectory.Name,
                FullPath = childDirectory.FullName,
                Kind = FileNodeKind.Directory,
                ModifiedTime = childDirectory.LastWriteTime,
                ScanState = ScanState.Pending
            });
        }

        foreach (var file in directory.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();
            root.Children.Add(new FileNode
            {
                Name = file.Name,
                FullPath = file.FullName,
                Kind = FileNodeKind.File,
                SizeBytes = file.Length,
                Extension = file.Extension,
                ModifiedTime = file.LastWriteTime,
                ScanState = ScanState.Complete
            });
        }

        root.SizeBytes = root.Children.Sum(c => c.SizeBytes);
        return Task.FromResult(root);
    }

    public async Task<long> FillDirectorySizeAsync(FileNode node, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        if (node.Kind == FileNodeKind.File) return node.SizeBytes;

        return await Task.Run(() =>
        {
            try
            {
                node.ScanState = ScanState.Scanning;
                progress?.Report(node.FullPath);
                node.SizeBytes = CalculateDirectorySize(node.FullPath, cancellationToken);
                node.ScanState = ScanState.Complete;
                return node.SizeBytes;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                node.ScanState = ScanState.Error;
                node.ErrorMessage = ex.Message;
                return node.SizeBytes;
            }
        }, cancellationToken);
    }

    private static long CalculateDirectorySize(string path, CancellationToken cancellationToken)
    {
        long total = 0;
        foreach (var file in Directory.EnumerateFiles(path))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { total += new FileInfo(file).Length; } catch (IOException) { }
        }

        foreach (var directory in Directory.EnumerateDirectories(path))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { total += CalculateDirectorySize(directory, cancellationToken); } catch (UnauthorizedAccessException) { }
        }

        return total;
    }
}
```

- [ ] **Step 4: Run tests**

```powershell
dotnet test tests/FolderSizeVisualizer.Core.Tests/FolderSizeVisualizer.Core.Tests.csproj --filter FolderScannerTests
```

Expected: tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/FolderSizeVisualizer.Core/Services/FolderScanner.cs tests/FolderSizeVisualizer.Core.Tests/FolderScannerTests.cs
git commit -m "feat: scan folder sizes"
```

## Task 7: File Operations

**Files:**
- Create: `src/FolderSizeVisualizer.Core/Services/FileOperationService.cs`

- [ ] **Step 1: Add service**

```csharp
using FolderSizeVisualizer.Core.Models;
using Microsoft.VisualBasic.FileIO;

namespace FolderSizeVisualizer.Core.Services;

public sealed class FileOperationService
{
    public void Delete(FileNode node, DeleteMode mode)
    {
        if (mode == DeleteMode.RecycleBin)
        {
            if (node.Kind == FileNodeKind.Directory)
                FileSystem.DeleteDirectory(node.FullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            else
                FileSystem.DeleteFile(node.FullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            return;
        }

        if (node.Kind == FileNodeKind.Directory)
            Directory.Delete(node.FullPath, recursive: true);
        else
            File.Delete(node.FullPath);
    }
}
```

- [ ] **Step 2: Build**

```powershell
dotnet build FolderSizeVisualizer.sln
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```powershell
git add src/FolderSizeVisualizer.Core/Services/FileOperationService.cs
git commit -m "feat: add file deletion service"
```

## Task 8: WPF MVVM Shell

**Files:**
- Create: `src/FolderSizeVisualizer.App/ViewModels/ObservableObject.cs`
- Create: `src/FolderSizeVisualizer.App/ViewModels/RelayCommand.cs`
- Create: `src/FolderSizeVisualizer.App/ViewModels/MainViewModel.cs`
- Modify: `src/FolderSizeVisualizer.App/MainWindow.xaml`
- Modify: `src/FolderSizeVisualizer.App/MainWindow.xaml.cs`

- [ ] **Step 1: Add MVVM helpers**

Create `ObservableObject.cs`:

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FolderSizeVisualizer.App.ViewModels;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

Create `RelayCommand.cs`:

```csharp
using System.Windows.Input;

namespace FolderSizeVisualizer.App.ViewModels;

public sealed class RelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public async void Execute(object? parameter) => await _execute(parameter);
    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

- [ ] **Step 2: Add main view model**

```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using FolderSizeVisualizer.Core.Models;
using FolderSizeVisualizer.Core.Services;

namespace FolderSizeVisualizer.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly FolderScanner _scanner = new();
    private CancellationTokenSource? _scanCancellation;
    private FileNode? _currentNode;
    private string _folderPath = "";
    private double _progress;
    private string _statusText = "";

    public string FolderPath { get => _folderPath; set { _folderPath = value; OnPropertyChanged(); } }
    public double Progress { get => _progress; set { _progress = value; OnPropertyChanged(); } }
    public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
    public ObservableCollection<FileNode> VisibleNodes { get; } = new();
    public ICommand AnalyzeCommand { get; }

    public MainViewModel()
    {
        AnalyzeCommand = new RelayCommand(async _ => await AnalyzeAsync());
    }

    private async Task AnalyzeAsync()
    {
        _scanCancellation?.Cancel();
        _scanCancellation = new CancellationTokenSource();
        VisibleNodes.Clear();
        Progress = 0;
        StatusText = "Scanning first level...";

        _currentNode = await _scanner.ScanFirstLevelAsync(FolderPath, _scanCancellation.Token);
        foreach (var node in NodeSorter.Sort(_currentNode.Children, SortMode.SizeDescending))
            VisibleNodes.Add(node);

        var directories = _currentNode.Children.Where(n => n.Kind == FileNodeKind.Directory).ToList();
        for (var i = 0; i < directories.Count; i++)
        {
            await _scanner.FillDirectorySizeAsync(directories[i], new Progress<string>(p => StatusText = p), _scanCancellation.Token);
            Progress = directories.Count == 0 ? 100 : (i + 1) * 100d / directories.Count;
            OnPropertyChanged(nameof(VisibleNodes));
        }

        StatusText = "Complete";
    }
}
```

- [ ] **Step 3: Add basic main window binding**

Replace `MainWindow.xaml`:

```xml
<Window x:Class="FolderSizeVisualizer.App.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:controls="clr-namespace:FolderSizeVisualizer.App.Controls"
        Title="Folder Size Visualizer" Width="1180" Height="760"
        MinWidth="900" MinHeight="560">
    <Grid Background="{DynamicResource AppBackgroundBrush}">
        <controls:TreemapView ItemsSource="{Binding VisibleNodes}" Margin="16"/>

        <Border VerticalAlignment="Top" Margin="24" Padding="12" CornerRadius="8"
                Background="{DynamicResource PanelBackgroundBrush}">
            <DockPanel LastChildFill="True">
                <Button DockPanel.Dock="Right" Width="96" Margin="8,0,0,0"
                        Content="{DynamicResource AnalyzeLabel}"
                        Command="{Binding AnalyzeCommand}"/>
                <TextBox Text="{Binding FolderPath, UpdateSourceTrigger=PropertyChanged}"
                         MinWidth="420"
                         VerticalContentAlignment="Center"/>
            </DockPanel>
        </Border>

        <Border VerticalAlignment="Bottom" Margin="24" Padding="12" CornerRadius="8"
                Background="{DynamicResource PanelBackgroundBrush}">
            <DockPanel>
                <TextBlock DockPanel.Dock="Left" Width="420" Text="{Binding StatusText}"/>
                <ProgressBar Height="8" Minimum="0" Maximum="100" Value="{Binding Progress}"/>
            </DockPanel>
        </Border>
    </Grid>
</Window>
```

Replace `MainWindow.xaml.cs`:

```csharp
using System.Windows;
using FolderSizeVisualizer.App.ViewModels;

namespace FolderSizeVisualizer.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
```

- [ ] **Step 4: Build**

```powershell
dotnet build FolderSizeVisualizer.sln
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```powershell
git add src/FolderSizeVisualizer.App
git commit -m "feat: add WPF shell and scan view model"
```

## Task 9: Treemap Control

**Files:**
- Create: `src/FolderSizeVisualizer.App/Controls/TreemapView.cs`
- Modify: `src/FolderSizeVisualizer.App/MainWindow.xaml`

- [ ] **Step 1: Add custom control**

Create `TreemapView.cs`:

```csharp
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FolderSizeVisualizer.Core.Models;
using FolderSizeVisualizer.Core.Services;

namespace FolderSizeVisualizer.App.Controls;

public sealed class TreemapView : FrameworkElement
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(TreemapView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    private readonly List<TreemapRectangle> _hitRegions = new();
    private FileNode? _hoveredNode;

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        _hitRegions.Clear();
        var nodes = ItemsSource?.OfType<FileNode>().ToList() ?? new List<FileNode>();
        var rects = TreemapLayoutService.Layout(nodes, 0, 0, ActualWidth, ActualHeight);
        _hitRegions.AddRange(rects);

        foreach (var rect in rects)
        {
            var brush = rect.Node.Kind == FileNodeKind.Directory ? Brushes.SteelBlue : Brushes.SeaGreen;
            var bounds = new Rect(rect.X + 1, rect.Y + 1, Math.Max(0, rect.Width - 2), Math.Max(0, rect.Height - 2));
            drawingContext.DrawRoundedRectangle(brush, null, bounds, 4, 4);

            if (bounds.Width > 80 && bounds.Height > 28)
            {
                var text = new FormattedText(
                    rect.Node.Name,
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    12,
                    Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);
                drawingContext.DrawText(text, new Point(bounds.X + 6, bounds.Y + 6));
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var point = e.GetPosition(this);
        _hoveredNode = _hitRegions.FirstOrDefault(r =>
            point.X >= r.X && point.X <= r.X + r.Width &&
            point.Y >= r.Y && point.Y <= r.Y + r.Height)?.Node;
        ToolTip = _hoveredNode is null ? null : $"{_hoveredNode.Name}\n{_hoveredNode.SizeBytes:N0} bytes\n{_hoveredNode.FullPath}";
    }
}
```

- [ ] **Step 2: Add context menu commands**

Extend `TreemapView.cs` with a `ContextMenu` opened from `OnMouseRightButtonUp`. The menu contains exactly these items:

```csharp
protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
{
    if (_hoveredNode is null) return;

    var menu = new System.Windows.Controls.ContextMenu();
    menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Open location", Tag = _hoveredNode });
    if (_hoveredNode.Kind == FileNodeKind.Directory)
        menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Expand", Tag = _hoveredNode });
    menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "Delete", Tag = _hoveredNode });
    menu.IsOpen = true;
}
```

- [ ] **Step 3: Manual test**

Run:

```powershell
dotnet run --project src/FolderSizeVisualizer.App/FolderSizeVisualizer.App.csproj
```

Expected: scanning a folder renders proportional rectangles.

- [ ] **Step 4: Commit**

```powershell
git add src/FolderSizeVisualizer.App/Controls/TreemapView.cs src/FolderSizeVisualizer.App/MainWindow.xaml
git commit -m "feat: render interactive treemap"
```

## Task 10: Settings, Theme, Language, Background

**Files:**
- Create: `src/FolderSizeVisualizer.App/Resources/Strings.zh-CN.xaml`
- Create: `src/FolderSizeVisualizer.App/Resources/Strings.en-US.xaml`
- Create: `src/FolderSizeVisualizer.App/Resources/Themes.Light.xaml`
- Create: `src/FolderSizeVisualizer.App/Resources/Themes.Dark.xaml`
- Modify: `src/FolderSizeVisualizer.App/ViewModels/MainViewModel.cs`
- Modify: `src/FolderSizeVisualizer.App/MainWindow.xaml`

- [ ] **Step 1: Add resource dictionaries**

Create `Strings.zh-CN.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">
    <sys:String x:Key="AnalyzeLabel">分析</sys:String>
    <sys:String x:Key="SettingsLabel">设置</sys:String>
    <sys:String x:Key="DeleteLabel">删除</sys:String>
</ResourceDictionary>
```

Create `Strings.en-US.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">
    <sys:String x:Key="AnalyzeLabel">Analyze</sys:String>
    <sys:String x:Key="SettingsLabel">Settings</sys:String>
    <sys:String x:Key="DeleteLabel">Delete</sys:String>
</ResourceDictionary>
```

Create `Themes.Light.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <SolidColorBrush x:Key="AppBackgroundBrush" Color="#F6F7FB"/>
    <SolidColorBrush x:Key="PanelBackgroundBrush" Color="#EFFFFFFF"/>
</ResourceDictionary>
```

Create `Themes.Dark.xaml`:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <SolidColorBrush x:Key="AppBackgroundBrush" Color="#15171C"/>
    <SolidColorBrush x:Key="PanelBackgroundBrush" Color="#E51F242D"/>
</ResourceDictionary>
```

- [ ] **Step 2: Add settings panel**

Add an in-window `Popup` or right-aligned `Border` to `MainWindow.xaml` containing:

```xml
<StackPanel Width="260">
    <TextBlock Text="{DynamicResource SettingsLabel}" FontWeight="SemiBold"/>
    <ComboBox ItemsSource="{Binding Languages}" SelectedItem="{Binding SelectedLanguage}"/>
    <ComboBox ItemsSource="{Binding Themes}" SelectedItem="{Binding SelectedTheme}"/>
    <ComboBox ItemsSource="{Binding DeleteModes}" SelectedItem="{Binding SelectedDeleteMode}"/>
    <Slider Minimum="0.4" Maximum="1" Value="{Binding VisualizationOpacity}"/>
    <Slider Minimum="0" Maximum="30" Value="{Binding BackgroundBlur}"/>
</StackPanel>
```

- [ ] **Step 3: Save settings**

Add these properties to `MainViewModel` and call `SettingsService.SaveAsync` when a property changes:

```csharp
public IReadOnlyList<AppLanguage> Languages { get; } = Enum.GetValues<AppLanguage>();
public IReadOnlyList<ThemeMode> Themes { get; } = Enum.GetValues<ThemeMode>();
public IReadOnlyList<DeleteMode> DeleteModes { get; } = Enum.GetValues<DeleteMode>();
public AppLanguage SelectedLanguage { get; set; } = AppLanguage.Chinese;
public ThemeMode SelectedTheme { get; set; } = ThemeMode.Light;
public DeleteMode SelectedDeleteMode { get; set; } = DeleteMode.RecycleBin;
public double VisualizationOpacity { get; set; } = 0.92;
public double BackgroundBlur { get; set; } = 12;
```

- [ ] **Step 4: Manual test**

Run:

```powershell
dotnet run --project src/FolderSizeVisualizer.App/FolderSizeVisualizer.App.csproj
```

Expected: language, theme, delete mode, and background image persist across restart.

- [ ] **Step 5: Commit**

```powershell
git add src/FolderSizeVisualizer.App src/FolderSizeVisualizer.Core/Services/SettingsService.cs
git commit -m "feat: add user settings and themes"
```

## Task 11: Final Verification And Packaging

**Files:**
- Modify only files directly related to failed verification checks.

- [ ] **Step 1: Run automated tests**

```powershell
dotnet test FolderSizeVisualizer.sln
```

Expected: all tests pass.

- [ ] **Step 2: Run app manually**

```powershell
dotnet run --project src/FolderSizeVisualizer.App/FolderSizeVisualizer.App.csproj
```

Expected:

- Choose folder works.
- Pasted path works.
- Progress updates.
- Treemap renders.
- Sorting changes layout.
- Hover shows details.
- Folder expansion works.
- Recycle Bin deletion works after confirmation.
- Settings persist.

- [ ] **Step 3: Publish lightweight build**

```powershell
dotnet publish src/FolderSizeVisualizer.App/FolderSizeVisualizer.App.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

Expected: publish folder contains a runnable Windows app build.

- [ ] **Step 4: Commit**

```powershell
git add .
git commit -m "chore: verify and publish app"
```

## Self-Review

- Spec coverage: path input, folder picker, progress, rolling status, treemap, sorting, expansion, context delete, settings, language, theme, background, and error handling are covered.
- Known execution blocker: .NET 8 SDK must be installed before implementation.
- Scope remains focused on the first version and excludes full disk scan, duplicate detection, cloud sync, database storage, plugin system, file preview, and batch cleanup automation.
