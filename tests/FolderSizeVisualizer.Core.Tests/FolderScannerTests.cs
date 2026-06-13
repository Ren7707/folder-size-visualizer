using FolderSizeVisualizer.Core.Models;
using FolderSizeVisualizer.Core.Services;
using System.IO;

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

    [Fact]
    public async Task FillDirectorySizeCalculatesNestedFiles()
    {
        var root = Directory.CreateTempSubdirectory();
        var child = Directory.CreateDirectory(Path.Combine(root.FullName, "child"));
        await File.WriteAllTextAsync(Path.Combine(child.FullName, "nested.txt"), "1234567");

        var scanner = new FolderScanner();
        var node = new FileNode { Name = "child", FullPath = child.FullName, Kind = FileNodeKind.Directory };

        var size = await scanner.FillDirectorySizeAsync(node, null, CancellationToken.None);

        Assert.Equal(7, size);
        Assert.Equal(ScanState.Complete, node.ScanState);
        root.Delete(true);
    }
}
