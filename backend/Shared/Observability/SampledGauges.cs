using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Shared.Observability;

/// <summary>
/// Gauges whose values come from periodic database queries (queue lengths, failed grants, dead
/// letters). Samplers write the latest values; Prometheus scrapes read them without touching the DB.
/// </summary>
public sealed class SampledGauges : IDisposable
{
    public const string METER_NAME = "Shared.Platform.Sampled";

    private readonly Meter _meter = new(METER_NAME);
    private readonly ConcurrentDictionary<string, IReadOnlyList<Measurement<long>>> _values = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, bool> _registered = new(StringComparer.Ordinal);

    public void Set(string name, string description, long value, params KeyValuePair<string, object?>[] tags) =>
        Set(name, description, [new Measurement<long>(value, tags)]);

    public void Set(string name, string description, IReadOnlyList<Measurement<long>> measurements)
    {
        _values[name] = measurements;

        if (_registered.TryAdd(name, true))
        {
            _meter.CreateObservableGauge(name, () => _values.GetValueOrDefault(name) ?? [], unit: null, description);
        }
    }

    public void Dispose() => _meter.Dispose();
}
