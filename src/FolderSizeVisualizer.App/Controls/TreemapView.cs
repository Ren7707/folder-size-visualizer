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
    private const double MinZoomX = 1.0;
    private const double MaxZoomX = 12.0;
    private const double ZoomStep = 1.18;

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
    private double _zoomX = MinZoomX;
    private double _zoomOriginX;

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
        var view = (TreemapView)d;
        if (e.OldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= view.OnCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += view.OnCollectionChanged;
        view.ResetZoom();
    }

    public static double CoerceZoom(double value) => Math.Clamp(value, MinZoomX, MaxZoomX);

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => ResetZoom();

    protected override void OnRender(DrawingContext drawingContext)
    {
        _hitRegions.Clear();
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
        var nodes = ItemsSource?.OfType<FileNode>().ToList() ?? new List<FileNode>();
        var rects = TreemapLayoutService.Layout(nodes, 0, 0, ActualWidth, ActualHeight);
        _hitRegions.AddRange(rects);

        drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
        drawingContext.PushTransform(new ScaleTransform(_zoomX, 1, _zoomOriginX, 0));

        for (var i = 0; i < rects.Count; i++)
        {
            var rect = rects[i];
            var bounds = new Rect(rect.X + 2, rect.Y + 2, Math.Max(0, rect.Width - 4), Math.Max(0, rect.Height - 4));
            var brush = new SolidColorBrush(PickColor(rect.Node, i));
            drawingContext.DrawRoundedRectangle(brush, null, bounds, 6, 6);

            if (bounds.Width > 96 && bounds.Height > 28)
            {
                var label = bounds.Width > 172 && bounds.Height > 42
                    ? $"{rect.Node.Name}\n{FormatBytes(rect.Node.SizeBytes)}"
                    : rect.Node.Name;
                var text = new FormattedText(label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 12, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip)
                {
                    MaxTextWidth = Math.Max(0, bounds.Width - 14),
                    MaxTextHeight = Math.Max(0, bounds.Height - 10),
                    TextAlignment = TextAlignment.Center,
                    Trimming = TextTrimming.CharacterEllipsis
                };
                var textPoint = new Point(
                    bounds.X + Math.Max(0, (bounds.Width - text.Width) / 2),
                    bounds.Y + Math.Max(0, (bounds.Height - text.Height) / 2));
                drawingContext.DrawText(text, textPoint);
            }
        }

        drawingContext.Pop();
        drawingContext.Pop();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        UpdateHoveredNode(e.GetPosition(this));
        ToolTip = _hoveredNode is null
            ? null
            : $"{_hoveredNode.Name}\n{FormatBytes(_hoveredNode.SizeBytes)}\n{_hoveredNode.Kind}\n{_hoveredNode.FullPath}";
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        ApplyWheelZoom(e);
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        ApplyWheelZoom(e);
    }

    private void ApplyWheelZoom(MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        _zoomOriginX = e.GetPosition(this).X;
        _zoomX = CoerceZoom(e.Delta > 0 ? _zoomX * ZoomStep : _zoomX / ZoomStep);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        UpdateHoveredNode(e.GetPosition(this));
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
        UpdateHoveredNode(e.GetPosition(this));
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

    private Point ToContentPoint(Point point)
        => new(_zoomOriginX + (point.X - _zoomOriginX) / _zoomX, point.Y);

    private void UpdateHoveredNode(Point viewPoint)
    {
        var point = ToContentPoint(viewPoint);
        _hoveredNode = _hitRegions.FirstOrDefault(r =>
            point.X >= r.X && point.X <= r.X + r.Width &&
            point.Y >= r.Y && point.Y <= r.Y + r.Height)?.Node;
    }

    private void ResetZoom()
    {
        _zoomX = MinZoomX;
        _zoomOriginX = 0;
        InvalidateVisual();
    }

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
