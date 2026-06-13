namespace FolderSizeVisualizer.Core.Models;

public sealed record TreemapRectangle(
    FileNode Node,
    double X,
    double Y,
    double Width,
    double Height);
