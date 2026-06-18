using FolderSizeVisualizer.App.Controls;

namespace FolderSizeVisualizer.Core.Tests;

public sealed class TreemapViewTests
{
    [Theory]
    [InlineData(0.2, 1.0)]
    [InlineData(2.0, 2.0)]
    [InlineData(99.0, 6.0)]
    public void CoerceZoomKeepsZoomWithinReadableRange(double value, double expected)
    {
        Assert.Equal(expected, TreemapView.CoerceZoom(value));
    }
}
