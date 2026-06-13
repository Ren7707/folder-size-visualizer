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
