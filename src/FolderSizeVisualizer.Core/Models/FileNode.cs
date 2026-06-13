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
