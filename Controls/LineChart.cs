using System.Windows;
using System.Windows.Media;
using PulseWidget.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace PulseWidget.Controls;

public sealed class LineChart : FrameworkElement
{
    private readonly ChartHistory _history = new();

    public static readonly DependencyProperty PrimaryStrokeProperty = DependencyProperty.Register(
        nameof(PrimaryStroke), typeof(Brush), typeof(LineChart),
        new FrameworkPropertyMetadata(Brushes.MediumSpringGreen, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SecondaryStrokeProperty = DependencyProperty.Register(
        nameof(SecondaryStroke), typeof(Brush), typeof(LineChart),
        new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(LineChart),
        new FrameworkPropertyMetadata(110d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DashedProperty = DependencyProperty.Register(
        nameof(Dashed), typeof(bool), typeof(LineChart),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty WindowMinutesProperty = DependencyProperty.Register(
        nameof(WindowMinutes), typeof(int), typeof(LineChart),
        new FrameworkPropertyMetadata(2, FrameworkPropertyMetadataOptions.AffectsRender, OnWindowChanged));

    public static readonly DependencyProperty PrimaryThresholdProperty = DependencyProperty.Register(
        nameof(PrimaryThreshold), typeof(double), typeof(LineChart),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SecondaryThresholdProperty = DependencyProperty.Register(
        nameof(SecondaryThreshold), typeof(double), typeof(LineChart),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

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

    public int WindowMinutes
    {
        get => (int)GetValue(WindowMinutesProperty);
        set => SetValue(WindowMinutesProperty, value);
    }

    public double PrimaryThreshold
    {
        get => (double)GetValue(PrimaryThresholdProperty);
        set => SetValue(PrimaryThresholdProperty, value);
    }

    public double SecondaryThreshold
    {
        get => (double)GetValue(SecondaryThresholdProperty);
        set => SetValue(SecondaryThresholdProperty, value);
    }

    public void AddPoints(DateTime timestamp, double? primary, double? secondary)
    {
        _history.Append(timestamp, primary, secondary);
        InvalidateVisual();
    }

    public SeriesStatistics GetStatistics(bool primary) => _history.GetStatistics(primary);

    public void Clear()
    {
        _history.Clear();
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (ActualWidth <= 1 || ActualHeight <= 1 || !double.IsFinite(Maximum) || Maximum <= 0)
        {
            return;
        }

        var gridBrush = new SolidColorBrush(Color.FromArgb(26, 255, 255, 255));
        gridBrush.Freeze();
        var gridPen = new Pen(gridBrush, 1);
        gridPen.Freeze();
        foreach (var ratio in new[] { 0.25, 0.5, 0.75, 1.0 })
        {
            var y = ActualHeight - ratio * ActualHeight;
            drawingContext.DrawLine(gridPen, new Point(0, y), new Point(ActualWidth, y));
        }

        DrawThreshold(drawingContext, PrimaryThreshold, PrimaryStroke);
        DrawThreshold(drawingContext, SecondaryThreshold, SecondaryStroke);

        if (_history.Samples.Count == 0)
        {
            return;
        }

        DrawSeries(drawingContext, true, PrimaryStroke);
        DrawSeries(drawingContext, false, SecondaryStroke);
    }

    private void DrawThreshold(DrawingContext context, double threshold, Brush stroke)
    {
        if (!double.IsFinite(threshold) || threshold < 0 || threshold > Maximum)
        {
            return;
        }

        var y = ActualHeight - threshold / Maximum * ActualHeight;
        var thresholdBrush = stroke.Clone();
        thresholdBrush.Opacity = 0.45;
        var pen = new Pen(thresholdBrush, 1) { DashStyle = new DashStyle([2, 4], 0) };
        context.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
    }

    private void DrawSeries(DrawingContext drawingContext, bool primary, Brush stroke)
    {
        var samples = _history.Samples;
        var latest = samples[^1].Timestamp;
        var start = latest - TimeSpan.FromMinutes(WindowMinutes);
        var windowTicks = Math.Max(1, (latest - start).Ticks);
        var geometry = new StreamGeometry();
        var lastPoint = default(Point);
        var hasPoint = false;
        var activeFigure = false;

        using (var line = geometry.Open())
        {
            foreach (var sample in samples)
            {
                var value = primary ? sample.Primary : sample.Secondary;
                if (!value.HasValue)
                {
                    activeFigure = false;
                    continue;
                }

                var x = Math.Clamp((sample.Timestamp - start).Ticks / (double)windowTicks, 0, 1) * ActualWidth;
                var y = ActualHeight - Math.Clamp(value.Value / Maximum, 0, 1) * ActualHeight;
                var point = new Point(x, y);
                if (!activeFigure)
                {
                    line.BeginFigure(point, false, false);
                    activeFigure = true;
                }
                else
                {
                    line.LineTo(point, true, false);
                }

                hasPoint = true;
                lastPoint = point;
            }
        }

        geometry.Freeze();
        var pen = new Pen(stroke, Dashed ? 1.7 : 2.2);
        if (Dashed)
        {
            pen.DashStyle = new DashStyle([4, 3], 0);
        }

        drawingContext.DrawGeometry(null, pen, geometry);
        if (hasPoint)
        {
            var radius = Dashed ? 2.3 : 3;
            drawingContext.DrawEllipse(stroke, null, lastPoint, radius, radius);
        }
    }

    private static void OnWindowChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var chart = (LineChart)dependencyObject;
        chart._history.Window = TimeSpan.FromMinutes(Math.Clamp((int)args.NewValue, 1, 60));
    }
}
