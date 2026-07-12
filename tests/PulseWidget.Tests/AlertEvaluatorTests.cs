using PulseWidget.Models;
using PulseWidget.Services;

namespace PulseWidget.Tests;

public sealed class AlertEvaluatorTests
{
    [Fact]
    public void Evaluate_requires_sustained_temperature_before_alerting()
    {
        var evaluator = new AlertEvaluator();
        var settings = new AppSettings
        {
            AlertsEnabled = true,
            CpuTemperatureThreshold = 80,
            AlertDurationSeconds = 10
        };
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        Assert.Empty(evaluator.Evaluate(Snapshot(start, 85), settings));
        Assert.Empty(evaluator.Evaluate(Snapshot(start.AddSeconds(9), 85), settings));
        var alerts = evaluator.Evaluate(Snapshot(start.AddSeconds(10), 85), settings);

        Assert.Single(alerts);
        Assert.Equal("Temperatura da CPU", alerts[0].Metric);
    }

    [Fact]
    public void Evaluate_resets_after_hysteresis_boundary()
    {
        var evaluator = new AlertEvaluator();
        var settings = new AppSettings
        {
            CpuTemperatureThreshold = 80,
            AlertDurationSeconds = 3,
            AlertCooldownMinutes = 1,
            AlertHysteresisDegrees = 5
        };
        var start = DateTime.UtcNow;

        evaluator.Evaluate(Snapshot(start, 85), settings);
        Assert.Single(evaluator.Evaluate(Snapshot(start.AddSeconds(3), 85), settings));
        evaluator.Evaluate(Snapshot(start.AddSeconds(4), 74), settings);
        evaluator.Evaluate(Snapshot(start.AddMinutes(2), 85), settings);

        Assert.Single(evaluator.Evaluate(Snapshot(start.AddMinutes(2).AddSeconds(3), 85), settings));
    }

    private static SensorSnapshot Snapshot(DateTime timestamp, double? cpuTemperature)
    {
        return SensorSnapshot.Unavailable("test") with
        {
            Timestamp = timestamp,
            CpuTemperature = cpuTemperature
        };
    }
}
