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
