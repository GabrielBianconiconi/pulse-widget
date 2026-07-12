using PulseWidget.Models;

namespace PulseWidget.Services;

public sealed class AlertEvaluator
{
    private readonly AlertState _cpuState = new();
    private readonly AlertState _gpuState = new();

    public IReadOnlyList<AlertEvent> Evaluate(SensorSnapshot snapshot, AppSettings settings)
    {
        if (!settings.AlertsEnabled)
        {
            _cpuState.Reset();
            _gpuState.Reset();
            return [];
        }

        var events = new List<AlertEvent>(2);
        EvaluateMetric(
            "Temperatura da CPU",
            snapshot.CpuTemperature,
            settings.CpuTemperatureThreshold,
            snapshot.Timestamp,
            settings,
            _cpuState,
            events);
        EvaluateMetric(
            "Temperatura da GPU",
            snapshot.GpuTemperature,
            settings.GpuTemperatureThreshold,
            snapshot.Timestamp,
            settings,
            _gpuState,
            events);
        return events;
    }

    private static void EvaluateMetric(
        string metric,
        double? value,
        double threshold,
        DateTime timestamp,
        AppSettings settings,
        AlertState state,
        ICollection<AlertEvent> events)
    {
        if (!value.HasValue || !double.IsFinite(value.Value))
        {
            state.Reset();
            return;
        }

        if (value.Value >= threshold)
        {
            state.ViolationSince ??= timestamp;
            var sustained = timestamp - state.ViolationSince.Value >= TimeSpan.FromSeconds(settings.AlertDurationSeconds);
            var cooledDown = !state.LastAlert.HasValue ||
                             timestamp - state.LastAlert.Value >= TimeSpan.FromMinutes(settings.AlertCooldownMinutes);
            if (sustained && !state.Active && cooledDown)
            {
                state.Active = true;
                state.LastAlert = timestamp;
                events.Add(new AlertEvent(metric, value.Value, threshold, timestamp));
            }

            return;
        }

        if (value.Value <= threshold - settings.AlertHysteresisDegrees)
        {
            state.ViolationSince = null;
            state.Active = false;
        }
    }

    private sealed class AlertState
    {
        public DateTime? ViolationSince { get; set; }

        public DateTime? LastAlert { get; set; }

        public bool Active { get; set; }

        public void Reset()
        {
            ViolationSince = null;
            Active = false;
        }
    }
}
