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

    [Fact]
    public void LayoutIgnoresZeroSizeNodes()
    {
        var nodes = new[]
        {
            new FileNode { Name = "empty", FullPath = "empty", Kind = FileNodeKind.File, SizeBytes = 0 },
            new FileNode { Name = "full", FullPath = "full", Kind = FileNodeKind.File, SizeBytes = 10 }
        };

        var result = TreemapLayoutService.Layout(nodes, 0, 0, 100, 100);

        Assert.Single(result);
        Assert.Equal("full", result[0].Node.Name);
    }
}
