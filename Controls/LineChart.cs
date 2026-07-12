using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace PulseWidget.Controls;

public sealed class LineChart : FrameworkElement
{
    private const int Capacity = 120;
    private readonly double[][] _series = Enumerable.Range(0, 2)
        .Select(_ => Enumerable.Repeat(double.NaN, Capacity).ToArray())
        .ToArray();
    private int _count;
    private int _nextIndex;

    public static readonly DependencyProperty PrimaryStrokeProperty = DependencyProperty.Register(
        nameof(PrimaryStroke),
        typeof(Brush),
        typeof(LineChart),
        new FrameworkPropertyMetadata(Brushes.MediumSpringGreen, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SecondaryStrokeProperty = DependencyProperty.Register(
        nameof(SecondaryStroke),
        typeof(Brush),
        typeof(LineChart),
        new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(LineChart),
        new FrameworkPropertyMetadata(110d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DashedProperty = DependencyProperty.Register(
        nameof(Dashed),
        typeof(bool),
        typeof(LineChart),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush PrimaryStroke
    {
        get => (Brush)GetValue(PrimaryStrokeProperty);
        set => SetValue(PrimaryStrokeProperty, value);
    }

    public Brush SecondaryStroke
    {
        get => (Brush)GetValue(SecondaryStrokeProperty);
        set => SetValue(SecondaryStrokeProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public bool Dashed
    {
        get => (bool)GetValue(DashedProperty);
        set => SetValue(DashedProperty, value);
    }

    public void AddPoints(double? primary, double? secondary)
    {
        _series[0][_nextIndex] = primary ?? double.NaN;
        _series[1][_nextIndex] = secondary ?? double.NaN;
        _nextIndex = (_nextIndex + 1) % Capacity;
        _count = Math.Min(Capacity, _count + 1);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 1 || height <= 1)
        {
            return;
        }

        var gridBrush = new SolidColorBrush(Color.FromArgb(26, 255, 255, 255));
        gridBrush.Freeze();
        var gridPen = new Pen(gridBrush, 1);
        gridPen.Freeze();
        foreach (var value in new[] { 25d, 50d, 75d, 100d })
        {
            var y = height - value / Maximum * height;
            drawingContext.DrawLine(gridPen, new Point(0, y), new Point(width, y));
        }

        if (_count < 2 || Maximum <= 0)
        {
            return;
        }

        DrawSeries(drawingContext, _series[0], PrimaryStroke, Dashed);
        DrawSeries(drawingContext, _series[1], SecondaryStroke, Dashed);
    }

    private void DrawSeries(DrawingContext drawingContext, double[] points, Brush stroke, bool dashed)
    {
        var width = ActualWidth;
        var height = ActualHeight;
        var lineGeometry = new StreamGeometry();
        var lastPoint = default(Point);
        var hasPoint = false;
        var hasActiveFigure = false;

        using (var line = lineGeometry.Open())
        {
            for (var index = 0; index < _count; index++)
            {
                var bufferIndex = (_nextIndex - _count + index + Capacity) % Capacity;
                var value = points[bufferIndex];
                if (double.IsNaN(value))
                {
                    hasActiveFigure = false;
                    continue;
                }

                var x = index * width / (Capacity - 1);
                var y = height - Math.Clamp(value / Maximum, 0, 1) * height;
                var point = new Point(x, y);

                if (!hasActiveFigure)
                {
                    line.BeginFigure(point, false, false);
                    hasActiveFigure = true;
                }
                else
                {
                    line.LineTo(point, true, false);
                }

                hasPoint = true;
                lastPoint = point;
            }
        }

        lineGeometry.Freeze();
        var linePen = new Pen(stroke, dashed ? 1.7 : 2.2);
        if (dashed)
        {
            linePen.DashStyle = new DashStyle([4, 3], 0);
        }

        drawingContext.DrawGeometry(null, linePen, lineGeometry);

        if (hasPoint)
        {
            var radius = dashed ? 2.3 : 3;
            drawingContext.DrawEllipse(stroke, null, lastPoint, radius, radius);
        }
    }
}
