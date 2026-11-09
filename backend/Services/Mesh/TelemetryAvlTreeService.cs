using SmartIotApi.Models;

namespace SmartIotApi.Services.Mesh;

/// <summary>
/// Thread-safe in-memory AVL index for telemetry timestamps. SQL Server remains the durable source of truth.
/// The index is hydrated after database seeding and updated only after successful writes.
/// </summary>
public sealed class TelemetryAvlTreeService
{
    private readonly object _sync = new();
    private TelemetryAvlTree<TelemetryDatum> _tree = new();

    public void Rebuild(IEnumerable<TelemetryDatum> records)
    {
        lock (_sync)
        {
            var replacement = new TelemetryAvlTree<TelemetryDatum>();
            foreach (var record in records)
                if (record.Timestamp.HasValue)
                    replacement.Add(record.Timestamp.Value, record);
            _tree = replacement;
        }
    }

    public void Add(TelemetryDatum record)
    {
        if (!record.Timestamp.HasValue) return;
        lock (_sync)
            _tree.Add(record.Timestamp.Value, record);
    }

    public IReadOnlyList<TelemetryDatum> Range(DateTime fromUtc, DateTime toUtc, string? sensorMac = null)
    {
        if (fromUtc > toUtc)
            throw new ArgumentException("Range start must not be after range end.");
        lock (_sync)
        {
            var records = _tree.Range(fromUtc, toUtc);
            return string.IsNullOrWhiteSpace(sensorMac)
                ? records.Take(10000).ToList()
                : records.Where(record => string.Equals(record.SensorMac, sensorMac.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Take(10000).ToList();
        }
    }

    public int Count
    {
        get { lock (_sync) return _tree.Count; }
    }
}
