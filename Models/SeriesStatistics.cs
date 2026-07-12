namespace PulseWidget.Models;

public readonly record struct SeriesStatistics(double? Minimum, double? Average, double? Maximum)
{
    public static SeriesStatistics Empty => new(null, null, null);
}
