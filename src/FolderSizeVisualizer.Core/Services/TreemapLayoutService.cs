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
