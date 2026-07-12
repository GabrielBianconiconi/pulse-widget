using PulseWidget.Controls;
using PulseWidget.Models;

namespace PulseWidget.Tests;

public sealed class ChartHistoryTests
{
    [Fact]
    public void Append_prunes_samples_outside_time_window()
    {
        var history = new ChartHistory { Window = TimeSpan.FromMinutes(2) };
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        history.Append(start, 10, 20);
        history.Append(start.AddMinutes(1), 30, 40);
        history.Append(start.AddMinutes(3), 50, 60);

        Assert.Equal(2, history.Samples.Count);
        Assert.Equal(30, history.GetStatistics(true).Minimum);
        Assert.Equal(50, history.GetStatistics(true).Maximum);
    }

    [Fact]
    public void Append_ignores_non_finite_values_in_statistics()
    {
        var history = new ChartHistory();

        history.Append(DateTime.UtcNow, double.NaN, double.PositiveInfinity);

        Assert.Equal(SeriesStatistics.Empty, history.GetStatistics(true));
        Assert.Equal(SeriesStatistics.Empty, history.GetStatistics(false));
    }
}
