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
