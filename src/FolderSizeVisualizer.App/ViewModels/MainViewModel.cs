using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using FolderSizeVisualizer.Core.Models;
using FolderSizeVisualizer.Core.Services;
using Microsoft.Win32;

namespace FolderSizeVisualizer.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly FolderScanner _scanner = new();
    private readonly FileOperationService _fileOperations = new();
    private readonly SettingsService _settingsService;
    private CancellationTokenSource? _scanCancellation;
    private FileNode? _currentNode;
    private AppSettings _settings = new();
    private string _folderPath = "";
    private double _progress;
    private string _statusText = "";
    private SortMode _selectedSortMode = SortMode.SizeDescending;
    private bool _isSettingsOpen;

    public MainViewModel()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FolderSizeVisualizer");
        _settingsService = new SettingsService(Path.Combine(configDir, "settings.json"));

        AnalyzeCommand = new RelayCommand(async _ => await AnalyzeAsync(), _ => Directory.Exists(FolderPath));
        BrowseCommand = new RelayCommand(_ => BrowseAsync());
        ToggleSettingsCommand = new RelayCommand(_ => { IsSettingsOpen = !IsSettingsOpen; return Task.CompletedTask; });
        ExpandCommand = new RelayCommand(async node => await ExpandAsync(node as FileNode));
        OpenLocationCommand = new RelayCommand(node => OpenLocationAsync(node as FileNode));
        DeleteCommand = new RelayCommand(async node => await DeleteAsync(node as FileNode));
        ChooseBackgroundCommand = new RelayCommand(async _ => await ChooseBackgroundAsync());
        GoToBreadcrumbCommand = new RelayCommand(async path => await NavigateToPathAsync(path as string));
        CopyPathCommand = new RelayCommand(_ => CopyPathAsync());
        BackCommand = new RelayCommand(async _ => await BackAsync(), _ => Breadcrumbs.Count > 1);
        FocusTreemapCommand = new RelayCommand(_ => { IsSettingsOpen = false; return Task.CompletedTask; });

        StatusText = UiText("SelectFolder");
        _ = LoadSettingsAsync();
    }

    public string FolderPath
    {
        get => _folderPath;
        set
        {
            if (SetProperty(ref _folderPath, value))
                ((RelayCommand)AnalyzeCommand).RaiseCanExecuteChanged();
        }
    }
    public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public bool IsSettingsOpen { get => _isSettingsOpen; set => SetProperty(ref _isSettingsOpen, value); }
    public ObservableCollection<FileNode> VisibleNodes { get; } = new();
    public ObservableCollection<string> StepMessages { get; } = new();
    public ObservableCollection<string> Breadcrumbs { get; } = new();
    public IReadOnlyList<SortMode> SortModes { get; } = Enum.GetValues<SortMode>();
    public IReadOnlyList<AppLanguage> Languages { get; } = Enum.GetValues<AppLanguage>();
    public IReadOnlyList<ThemeMode> Themes { get; } = Enum.GetValues<ThemeMode>();
    public IReadOnlyList<DeleteMode> DeleteModes { get; } = Enum.GetValues<DeleteMode>();
    public ICommand AnalyzeCommand { get; }
    public ICommand BrowseCommand { get; }
    public ICommand ToggleSettingsCommand { get; }
    public ICommand ExpandCommand { get; }
    public ICommand OpenLocationCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ChooseBackgroundCommand { get; }
    public ICommand GoToBreadcrumbCommand { get; }
    public ICommand CopyPathCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand FocusTreemapCommand { get; }
    public string TotalSizeText => FormatBytes(VisibleNodes.Sum(n => n.SizeBytes));
    public int FileCount => VisibleNodes.Count(n => n.Kind == FileNodeKind.File);
    public int FolderCount => VisibleNodes.Count(n => n.Kind == FileNodeKind.Directory);

    public SortMode SelectedSortMode
    {
        get => _selectedSortMode;
        set
        {
            if (!SetProperty(ref _selectedSortMode, value)) return;
            RefreshVisibleNodes();
        }
    }

    public AppLanguage SelectedLanguage
    {
        get => _settings.Language;
        set { _settings.Language = value; OnPropertyChanged(); StatusText = UiText("SelectFolder"); _ = SaveSettingsAsync(); }
    }

    public ThemeMode SelectedTheme
    {
        get => _settings.Theme;
        set { _settings.Theme = value; OnPropertyChanged(); _ = SaveSettingsAsync(); }
    }

    public DeleteMode SelectedDeleteMode
    {
        get => _settings.DeleteMode;
        set { _settings.DeleteMode = value; OnPropertyChanged(); _ = SaveSettingsAsync(); }
    }

    public string? BackgroundImagePath
    {
        get => _settings.BackgroundImagePath;
        set { _settings.BackgroundImagePath = value; OnPropertyChanged(); _ = SaveSettingsAsync(); }
    }

    public double VisualizationOpacity
    {
        get => _settings.VisualizationOpacity;
        set { _settings.VisualizationOpacity = value; OnPropertyChanged(); _ = SaveSettingsAsync(); }
    }

    public double BackgroundBlur
    {
        get => _settings.BackgroundBlur;
        set { _settings.BackgroundBlur = value; OnPropertyChanged(); _ = SaveSettingsAsync(); }
    }

    private async Task LoadSettingsAsync()
    {
        _settings = await _settingsService.LoadAsync();
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(SelectedDeleteMode));
        OnPropertyChanged(nameof(BackgroundImagePath));
        OnPropertyChanged(nameof(VisualizationOpacity));
        OnPropertyChanged(nameof(BackgroundBlur));
    }

    private Task SaveSettingsAsync() => _settingsService.SaveAsync(_settings);

    private Task BrowseAsync()
    {
        var dialog = new OpenFolderDialog { Title = "Select folder to analyze" };
        if (dialog.ShowDialog() == true) FolderPath = dialog.FolderName;
        return Task.CompletedTask;
    }

    private async Task AnalyzeAsync()
    {
        if (!Directory.Exists(FolderPath))
        {
            StatusText = UiText("PathMissing");
            return;
        }

        _scanCancellation?.Cancel();
        _scanCancellation = new CancellationTokenSource();
        await LoadFolderAsync(FolderPath, _scanCancellation.Token);
    }

    private async Task NavigateToPathAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        FolderPath = path;
        _scanCancellation?.Cancel();
        _scanCancellation = new CancellationTokenSource();
        await LoadFolderAsync(path, _scanCancellation.Token);
    }

    private async Task ExpandAsync(FileNode? node)
    {
        if (node is null || node.Kind != FileNodeKind.Directory || !Directory.Exists(node.FullPath)) return;
        FolderPath = node.FullPath;
        _scanCancellation?.Cancel();
        _scanCancellation = new CancellationTokenSource();
        await LoadFolderAsync(node.FullPath, _scanCancellation.Token);
    }

    private async Task LoadFolderAsync(string path, CancellationToken cancellationToken)
    {
        VisibleNodes.Clear();
        StepMessages.Clear();
        Progress = 0;
        AddStep(UiText("ReadingFirstLevel"));

        try
        {
            _currentNode = await _scanner.ScanFirstLevelAsync(path, cancellationToken);
            RefreshBreadcrumbs(path);
            RefreshVisibleNodes();

            var directories = _currentNode.Children.Where(n => n.Kind == FileNodeKind.Directory).ToList();
            for (var i = 0; i < directories.Count; i++)
            {
                var directory = directories[i];
                await _scanner.FillDirectorySizeAsync(directory, new Progress<string>(p => AddStep($"Scanning {p}")), cancellationToken);
                Progress = directories.Count == 0 ? 100 : (i + 1) * 100d / directories.Count;
                RefreshVisibleNodes();
            }

            Progress = 100;
            AddStep(UiText("AnalysisComplete"));
        }
        catch (OperationCanceledException)
        {
            AddStep(UiText("ScanCancelled"));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
        {
            StatusText = ex.Message;
            AddStep(ex.Message);
        }
    }

    private void RefreshVisibleNodes()
    {
        if (_currentNode is null) return;
        VisibleNodes.Clear();
        foreach (var node in NodeSorter.Sort(_currentNode.Children, SelectedSortMode))
            VisibleNodes.Add(node);
        OnPropertyChanged(nameof(TotalSizeText));
        OnPropertyChanged(nameof(FileCount));
        OnPropertyChanged(nameof(FolderCount));
    }

    private void RefreshBreadcrumbs(string path)
    {
        Breadcrumbs.Clear();
        var directory = new DirectoryInfo(path);
        var stack = new Stack<string>();
        while (directory is not null)
        {
            stack.Push(directory.FullName);
            directory = directory.Parent;
        }

        foreach (var item in stack) Breadcrumbs.Add(item);
        ((RelayCommand)BackCommand).RaiseCanExecuteChanged();
    }

    private Task OpenLocationAsync(FileNode? node)
    {
        if (node is null) return Task.CompletedTask;
        var argument = node.Kind == FileNodeKind.Directory ? $"\"{node.FullPath}\"" : $"/select,\"{node.FullPath}\"";
        Process.Start(new ProcessStartInfo("explorer.exe", argument) { UseShellExecute = true });
        return Task.CompletedTask;
    }

    private async Task DeleteAsync(FileNode? node)
    {
        if (node is null) return;
        var warning = SelectedDeleteMode == DeleteMode.Direct
            ? $"Permanently delete {node.Name}?"
            : $"Move {node.Name} to Recycle Bin?";
        if (MessageBox.Show(warning, "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        _fileOperations.Delete(node, SelectedDeleteMode);
        _currentNode?.Children.Remove(node);
        RefreshVisibleNodes();
        AddStep($"Deleted {node.Name}");
        await Task.CompletedTask;
    }

    private Task CopyPathAsync()
    {
        if (!string.IsNullOrWhiteSpace(FolderPath))
        {
            Clipboard.SetText(FolderPath);
            AddStep(UiText("PathCopied"));
        }

        return Task.CompletedTask;
    }

    private async Task BackAsync()
    {
        if (Breadcrumbs.Count <= 1) return;
        var parentPath = Breadcrumbs[^2];
        await NavigateToPathAsync(parentPath);
    }

    private async Task ChooseBackgroundAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose background image",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp"
        };
        if (dialog.ShowDialog() != true) return;

        var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FolderSizeVisualizer", "Backgrounds");
        Directory.CreateDirectory(configDir);
        var target = Path.Combine(configDir, $"{Guid.NewGuid()}{Path.GetExtension(dialog.FileName)}");
        File.Copy(dialog.FileName, target);
        BackgroundImagePath = target;
        await SaveSettingsAsync();
    }

    private void AddStep(string message)
    {
        StatusText = message;
        StepMessages.Insert(0, message);
        while (StepMessages.Count > 5) StepMessages.RemoveAt(StepMessages.Count - 1);
    }

    private string UiText(string key)
    {
        var chinese = SelectedLanguage == AppLanguage.Chinese;
        return key switch
        {
            "SelectFolder" => chinese ? "请选择或粘贴文件夹路径。" : "Select or paste a folder path.",
            "PathMissing" => chinese ? "文件夹路径不存在。" : "Folder path does not exist.",
            "ReadingFirstLevel" => chinese ? "正在读取首层结构..." : "Reading first level...",
            "AnalysisComplete" => chinese ? "分析完成。" : "Analysis complete.",
            "ScanCancelled" => chinese ? "扫描已取消。" : "Scan cancelled.",
            "PathCopied" => chinese ? "路径已复制。" : "Path copied.",
            _ => key
        };
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}
