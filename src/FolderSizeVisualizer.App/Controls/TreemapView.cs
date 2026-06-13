using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FolderSizeVisualizer.Core.Models;
using FolderSizeVisualizer.Core.Services;

namespace FolderSizeVisualizer.App.Controls;

public sealed class TreemapView : FrameworkElement
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(TreemapView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnItemsSourceChanged));

    public static readonly DependencyProperty ExpandCommandProperty =
        DependencyProperty.Register(nameof(ExpandCommand), typeof(ICommand), typeof(TreemapView));

    public static readonly DependencyProperty OpenLocationCommandProperty =
        DependencyProperty.Register(nameof(OpenLocationCommand), typeof(ICommand), typeof(TreemapView));

    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(TreemapView));

    public static readonly DependencyProperty BackCommandProperty =
        DependencyProperty.Register(nameof(BackCommand), typeof(ICommand), typeof(TreemapView));

    private readonly List<TreemapRectangle> _hitRegions = new();
    private FileNode? _hoveredNode;

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? ExpandCommand
    {
        get => (ICommand?)GetValue(ExpandCommandProperty);
        set => SetValue(ExpandCommandProperty, value);
    }

    public ICommand? OpenLocationCommand
    {
        get => (ICommand?)GetValue(OpenLocationCommandProperty);
        set => SetValue(OpenLocationCommandProperty, value);
    }

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public ICommand? BackCommand
    {
        get => (ICommand?)GetValue(BackCommandProperty);
        set => SetValue(BackCommandProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= ((TreemapView)d).OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += ((TreemapView)d).OnCollectionChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    protected override void OnRender(DrawingContext drawingContext)
    {
        _hitRegions.Clear();
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
        var nodes = ItemsSource?.OfType<FileNode>().ToList() ?? new List<FileNode>();
        var rects = TreemapLayoutService.Layout(nodes, 0, 0, ActualWidth, ActualHeight);
        _hitRegions.AddRange(rects);

        for (var i = 0; i < rects.Count; i++)
        {
            var rect = rects[i];
            var bounds = new Rect(rect.X + 2, rect.Y + 2, Math.Max(0, rect.Width - 4), Math.Max(0, rect.Height - 4));
            var brush = new SolidColorBrush(PickColor(rect.Node, i));
            drawingContext.DrawRoundedRectangle(brush, null, bounds, 6, 6);

            if (bounds.Width > 96 && bounds.Height > 36)
            {
                var label = $"{rect.Node.Name}\n{FormatBytes(rect.Node.SizeBytes)}";
                var text = new FormattedText(label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 12, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip)
                {
                    MaxTextWidth = bounds.Width - 14,
                    MaxTextHeight = bounds.Height - 10,
                    Trimming = TextTrimming.CharacterEllipsis
                };
                drawingContext.DrawText(text, new Point(bounds.X + 8, bounds.Y + 6));
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var point = e.GetPosition(this);
        _hoveredNode = _hitRegions.FirstOrDefault(r =>
            point.X >= r.X && point.X <= r.X + r.Width &&
            point.Y >= r.Y && point.Y <= r.Y + r.Height)?.Node;
        ToolTip = _hoveredNode is null
            ? null
            : $"{_hoveredNode.Name}\n{FormatBytes(_hoveredNode.SizeBytes)}\n{_hoveredNode.Kind}\n{_hoveredNode.FullPath}";
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        if (_hoveredNode is null) return;

        var menu = new ContextMenu();
        AddMenuItem(menu, Text("OpenLocationLabel", "Open location"), OpenLocationCommand, _hoveredNode);
        if (_hoveredNode.Kind == FileNodeKind.Directory) AddMenuItem(menu, Text("ExpandLabel", "Expand"), ExpandCommand, _hoveredNode);
        AddMenuItem(menu, Text("BackLabel", "Back to parent"), BackCommand, _hoveredNode);
        AddMenuItem(menu, Text("DeleteLabel", "Delete"), DeleteCommand, _hoveredNode);
        menu.IsOpen = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_hoveredNode?.Kind == FileNodeKind.Directory && ExpandCommand?.CanExecute(_hoveredNode) == true)
            ExpandCommand.Execute(_hoveredNode);
    }

    private static void AddMenuItem(ItemsControl menu, string header, ICommand? command, FileNode node)
    {
        var item = new MenuItem { Header = header, Command = command, CommandParameter = node };
        menu.Items.Add(item);
    }

    private static string Text(string key, string fallback)
        => Application.Current.TryFindResource(key) as string ?? fallback;

    private static Color PickColor(FileNode node, int index)
    {
        var palette = new[]
        {
            Color.FromRgb(53, 107, 219),
            Color.FromRgb(23, 150, 122),
            Color.FromRgb(199, 115, 44),
            Color.FromRgb(129, 93, 181),
            Color.FromRgb(184, 66, 88),
            Color.FromRgb(72, 130, 150)
        };
        return node.Kind == FileNodeKind.Directory ? palette[index % palette.Length] : Color.FromRgb(83, 96, 116);
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
