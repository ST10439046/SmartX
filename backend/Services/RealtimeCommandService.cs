// Real-time command service: contains the reusable logic for coordinating commands sent to sensor clients.
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using SmartIotApi.Models;

namespace SmartIotApi.Services;

public sealed record StreamPacket(string SensorMac, string DataType, double DataValue, DateTime Timestamp, bool IsCritical, string? ErrorCode = null);
public sealed record DeviceSnapshot(string SensorMac, string? NodeId, string? Category, bool IsActive, DateTime LastSeenUtc);
public sealed record TimelineEntry(string SensorMac, string DataType, double DataValue, DateTime Timestamp, bool IsCritical);
public sealed record SuggestedAction(string SensorMac, string DeviceName, string Severity, string Message, string Recommendation, DateTime CreatedAtUtc);
public sealed record CommandUndoEntry(string SensorMac, string CommandType, string PreviousCommandType, DateTime CreatedAtUtc);

/// <summary>
/// Coordinates the in-memory data structures required for Part 2. Locks protect compound
/// operations because telemetry and operator commands can arrive concurrently.
/// </summary>
public sealed class RealtimeCommandService
{
    private readonly object _gate = new();
    private readonly Queue<StreamPacket> _routineQueue = new();
    private readonly PriorityQueue<StreamPacket, int> _criticalQueue = new();
    private readonly Stack<CommandUndoEntry> _undoStack = new();
    private readonly Dictionary<string, DeviceSnapshot> _devices = new(StringComparer.OrdinalIgnoreCase);
    private readonly SortedDictionary<DateTime, List<TimelineEntry>> _timeline = new();
    private readonly HashSet<string> _activeAlerts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> _recentQueries = new();
    private readonly Dictionary<string, int> _actionCounts = new(StringComparer.OrdinalIgnoreCase);
    private long _processedCount;

    public void RegisterDevices(IEnumerable<Sensor> sensors)
    {
        lock (_gate)
        {
            foreach (var sensor in sensors)
            {
                var key = sensor.Macaddress.Trim();
                if (key.Length == 0) continue;
                if (!_devices.TryGetValue(key, out var current))
                    _devices[key] = new DeviceSnapshot(key, sensor.NodeId, sensor.Category, sensor.IsActive, DateTime.UtcNow);
                else
                    _devices[key] = current with { NodeId = sensor.NodeId, Category = sensor.Category, IsActive = sensor.IsActive };
            }
        }
    }

    public void Enqueue(StreamPacket packet)
    {
        lock (_gate)
        {
            if (packet.IsCritical)
            {
                var priority = string.IsNullOrWhiteSpace(packet.ErrorCode) ? 1 : 0;
                _criticalQueue.Enqueue(packet, priority);
                if (!string.IsNullOrWhiteSpace(packet.ErrorCode))
                    _activeAlerts.Add($"{packet.SensorMac}:{packet.ErrorCode}");
            }
            else
            {
                _routineQueue.Enqueue(packet);
            }
        }
    }

    /// <summary>Processes critical packets before routine FIFO packets.</summary>
    public IReadOnlyList<TimelineEntry> Drain(int maxItems = 100)
    {
        var processed = new List<TimelineEntry>();
        lock (_gate)
        {
            var remaining = Math.Clamp(maxItems, 1, 1000);
            while (remaining-- > 0 && (_criticalQueue.Count > 0 || _routineQueue.Count > 0))
            {
                var packet = _criticalQueue.Count > 0
                    ? _criticalQueue.Dequeue()
                    : _routineQueue.Dequeue();

                var entry = new TimelineEntry(packet.SensorMac, packet.DataType, packet.DataValue, packet.Timestamp, packet.IsCritical);
                if (!_timeline.TryGetValue(packet.Timestamp, out var entries))
                {
                    entries = new List<TimelineEntry>();
                    _timeline.Add(packet.Timestamp, entries);
                }
                entries.Add(entry);
                _devices[packet.SensorMac] = _devices.TryGetValue(packet.SensorMac, out var device)
                    ? device with { LastSeenUtc = packet.Timestamp }
                    : new DeviceSnapshot(packet.SensorMac, null, null, true, packet.Timestamp);
                processed.Add(entry);
                Interlocked.Increment(ref _processedCount);
            }
        }
        return processed;
    }

    public object GetSnapshot(int timelineLimit = 100)
    {
        lock (_gate)
        {
            var timeline = _timeline.Reverse()
                .SelectMany(pair => pair.Value.AsEnumerable().Reverse())
                .Take(Math.Clamp(timelineLimit, 1, 500))
                .ToList();

            return new
            {
                QueueDepth = _routineQueue.Count,
                CriticalQueueDepth = _criticalQueue.Count,
                ProcessedCount = Interlocked.Read(ref _processedCount),
                Devices = _devices.Values.OrderBy(d => d.NodeId).ThenBy(d => d.SensorMac).ToList(),
                Timeline = timeline,
                ActiveAlerts = _activeAlerts.OrderBy(a => a).ToList(),
                RecentQueries = _recentQueries.ToArray()
            };
        }
    }

    public bool TryGetDevice(string sensorMac, out DeviceSnapshot? device)
    {
        lock (_gate) return _devices.TryGetValue(sensorMac, out device);
    }

    public void RecordQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        lock (_gate)
        {
            var normalized = query.Trim();
            _recentQueries.Enqueue(normalized);
            while (_recentQueries.Count > 30) _recentQueries.Dequeue();
            _actionCounts[normalized] = _actionCounts.GetValueOrDefault(normalized) + 1;
        }
    }

    public IReadOnlyList<SuggestedAction> BuildSuggestions(IEnumerable<Sensor> sensors, IEnumerable<TelemetryDatum> recentTelemetry)
    {
        var suggestions = new List<SuggestedAction>();
        var sensorLookup = sensors.ToDictionary(s => s.Macaddress, StringComparer.OrdinalIgnoreCase);

        foreach (var reading in recentTelemetry.Where(t => t.DataValue.HasValue))
        {
            if (!sensorLookup.TryGetValue(reading.SensorMac, out var sensor)) continue;
            var type = (reading.DataType ?? "").ToLowerInvariant();
            var value = reading.DataValue!.Value;
            string? message = null;
            string severity = "warning";
            string recommendation = "Inspect the latest readings and verify the device is operating normally.";

            if ((type.Contains("moisture") || type.Contains("humidity")) && value < 20)
            {
                message = $"{sensor.NodeId ?? sensor.Macaddress}: low moisture reading ({value:0.##}).";
                recommendation = "Check irrigation status and inspect the associated pump.";
                severity = "critical";
            }
            else if (type.Contains("temperature") && value >= 40)
            {
                message = $"{sensor.NodeId ?? sensor.Macaddress}: high temperature ({value:0.##}).";
                recommendation = "Inspect ventilation and confirm the temperature sensor is calibrated.";
                severity = "critical";
            }
            else if ((type.Contains("power") || type.Contains("watt")) && value >= 1000)
            {
                message = $"{sensor.NodeId ?? sensor.Macaddress}: elevated power reading ({value:0.##}).";
                recommendation = "Check the power circuit for overload before issuing further commands.";
                severity = "critical";
            }

            if (message is not null)
                suggestions.Add(new SuggestedAction(sensor.Macaddress, sensor.NodeId ?? sensor.Macaddress, severity, message, recommendation, reading.Timestamp ?? DateTime.UtcNow));
        }

        lock (_gate)
        {
            var recent = _recentQueries.ToArray();
            if (recent.Length >= 3 && recent.TakeLast(3).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1)
            {
                suggestions.Add(new SuggestedAction("", "Operator activity", "info",
                    $"The operator has repeatedly searched for “{recent[^1]}”.",
                    "Review this device's recent telemetry and alerts; consider pinning it for faster access.",
                    DateTime.UtcNow));
            }
        }

        return suggestions
            .GroupBy(s => $"{s.SensorMac}|{s.Message}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderByDescending(s => s.Severity == "critical")
            .ThenByDescending(s => s.CreatedAtUtc)
            .Take(20)
            .ToList();
    }

    public void RecordCommand(string sensorMac, string commandType, string previousCommandType)
    {
        lock (_gate)
        {
            _undoStack.Push(new CommandUndoEntry(sensorMac, commandType, previousCommandType, DateTime.UtcNow));
            _actionCounts[commandType] = _actionCounts.GetValueOrDefault(commandType) + 1;
        }
    }

    public bool TryPopUndo(out CommandUndoEntry? entry)
    {
        lock (_gate)
        {
            if (_undoStack.Count == 0) { entry = null; return false; }
            entry = _undoStack.Pop();
            return true;
        }
    }

    public object GetCommandMetrics()
    {
        lock (_gate)
        {
            return new
            {
                QueueDepth = _routineQueue.Count,
                CriticalQueueDepth = _criticalQueue.Count,
                UndoDepth = _undoStack.Count,
                DeviceRegistryCount = _devices.Count,
                TimelineTimestampCount = _timeline.Count,
                ActiveAlertCount = _activeAlerts.Count,
                ProcessedCount = Interlocked.Read(ref _processedCount)
            };
        }
    }
}
