using PulseWidget.Models;

namespace PulseWidget.Controls;

public sealed class ChartHistory
{
    private const int MaximumSamples = 10_000;
    private readonly List<ChartSample> _samples = [];

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(2);

    public IReadOnlyList<ChartSample> Samples => _samples;

    public void Append(DateTime timestamp, double? primary, double? secondary)
    {
        _samples.Add(new ChartSample(timestamp, Sanitize(primary), Sanitize(secondary)));
        Prune(timestamp);
    }

    public void Clear()
    {
        _samples.Clear();
    }

    public SeriesStatistics GetStatistics(bool primary)
    {
        var values = _samples
            .Select(sample => primary ? sample.Primary : sample.Secondary)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        return values.Length == 0
            ? SeriesStatistics.Empty
            : new SeriesStatistics(values.Min(), values.Average(), values.Max());
    }

    private void Prune(DateTime latestTimestamp)
    {
        var cutoff = latestTimestamp - Window;
        var removeCount = 0;
        while (removeCount < _samples.Count && _samples[removeCount].Timestamp < cutoff)
        {
            removeCount++;
        }

        if (_samples.Count - removeCount > MaximumSamples)
        {
            removeCount = _samples.Count - MaximumSamples;
        }

        if (removeCount > 0)
        {
            _samples.RemoveRange(0, removeCount);
        }
    }

    private static double? Sanitize(double? value)
    {
        return value.HasValue && double.IsFinite(value.Value) ? value : null;
    }
}
