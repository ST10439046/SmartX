using SmartIotApi.Models;

namespace SmartIotApi.Services;

public sealed record StreamPacket(string SensorMac, string DataType, double DataValue, DateTime Timestamp, bool IsCritical, string? ErrorCode = null);
public sealed record DeviceSnapshot(string SensorMac, string? NodeId, string? Category, bool IsActive, DateTime LastSeenUtc);
public sealed record TimelineEntry(string SensorMac, string DataType, double DataValue, DateTime Timestamp, bool IsCritical);
public sealed record SuggestedAction(string SensorMac, string DeviceName, string Severity, string Message, string Recommendation, DateTime CreatedAtUtc);
public sealed record CommandUndoEntry(string SensorMac, string CommandType, string PreviousCommandType, DateTime CreatedAtUtc);

/// <summary>Thread-safe in-memory structures required for the Part 2 stream.</summary>
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
    private long _processedCount;

    public void RegisterDevices(IEnumerable<Sensor> sensors) { lock (_gate) foreach (var s in sensors) { var key = s.Macaddress.Trim(); if (key.Length == 0) continue; _devices[key] = _devices.TryGetValue(key, out var old) ? old with { NodeId = s.NodeId, Category = s.Category, IsActive = s.IsActive } : new(key, s.NodeId, s.Category, s.IsActive, DateTime.UtcNow); } }
    public void Enqueue(StreamPacket packet) { lock (_gate) { if (packet.IsCritical) { _criticalQueue.Enqueue(packet, string.IsNullOrWhiteSpace(packet.ErrorCode) ? 1 : 0); if (!string.IsNullOrWhiteSpace(packet.ErrorCode)) _activeAlerts.Add($"{packet.SensorMac}:{packet.ErrorCode}"); } else _routineQueue.Enqueue(packet); } }
    public IReadOnlyList<TimelineEntry> Drain(int maxItems = 100) { var result = new List<TimelineEntry>(); lock (_gate) { for (var i = 0; i < Math.Clamp(maxItems, 1, 1000) && (_criticalQueue.Count > 0 || _routineQueue.Count > 0); i++) { var p = _criticalQueue.Count > 0 ? _criticalQueue.Dequeue() : _routineQueue.Dequeue(); var entry = new TimelineEntry(p.SensorMac, p.DataType, p.DataValue, p.Timestamp, p.IsCritical); if (!_timeline.TryGetValue(p.Timestamp, out var list)) { list = new(); _timeline.Add(p.Timestamp, list); } list.Add(entry); _devices[p.SensorMac] = _devices.TryGetValue(p.SensorMac, out var d) ? d with { LastSeenUtc = p.Timestamp } : new(p.SensorMac, null, null, true, p.Timestamp); result.Add(entry); Interlocked.Increment(ref _processedCount); } } return result; }
    public object GetSnapshot(int limit = 100) { lock (_gate) return new { QueueDepth = _routineQueue.Count, CriticalQueueDepth = _criticalQueue.Count, ProcessedCount = Interlocked.Read(ref _processedCount), Devices = _devices.Values.OrderBy(d => d.NodeId).ThenBy(d => d.SensorMac).ToList(), Timeline = _timeline.Reverse().SelectMany(p => p.Value.AsEnumerable().Reverse()).Take(Math.Clamp(limit, 1, 500)).ToList(), ActiveAlerts = _activeAlerts.OrderBy(a => a).ToList(), RecentQueries = _recentQueries.ToArray() }; }
    public bool TryGetDevice(string mac, out DeviceSnapshot? device) { lock (_gate) return _devices.TryGetValue(mac, out device); }
    public void RecordQuery(string query) { if (string.IsNullOrWhiteSpace(query)) return; lock (_gate) { _recentQueries.Enqueue(query.Trim()); while (_recentQueries.Count > 30) _recentQueries.Dequeue(); } }
    public IReadOnlyList<SuggestedAction> BuildSuggestions(IEnumerable<Sensor> sensors, IEnumerable<TelemetryDatum> telemetry) { var lookup = sensors.ToDictionary(s => s.Macaddress, StringComparer.OrdinalIgnoreCase); var result = new List<SuggestedAction>(); foreach (var t in telemetry.Where(x => x.DataValue.HasValue)) { if (!lookup.TryGetValue(t.SensorMac, out var s)) continue; var type = (t.DataType ?? "").ToLowerInvariant(); var value = t.DataValue!.Value; string? message = null; var severity = "warning"; var recommendation = "Inspect the latest readings and verify device operation."; if ((type.Contains("moisture") || type.Contains("humidity")) && value < 20) { message = $"{s.NodeId ?? s.Macaddress}: low moisture reading ({value:0.##})."; severity = "critical"; recommendation = "Check irrigation status and inspect the associated pump."; } else if (type.Contains("temperature") && value >= 40) { message = $"{s.NodeId ?? s.Macaddress}: high temperature ({value:0.##})."; severity = "critical"; recommendation = "Inspect ventilation and verify sensor calibration."; } else if ((type.Contains("power") || type.Contains("watt")) && value >= 1000) { message = $"{s.NodeId ?? s.Macaddress}: elevated power reading ({value:0.##})."; severity = "critical"; recommendation = "Check the circuit for overload before issuing commands."; } if (message != null) result.Add(new(s.Macaddress, s.NodeId ?? s.Macaddress, severity, message, recommendation, t.Timestamp ?? DateTime.UtcNow)); } lock (_gate) { var q = _recentQueries.ToArray(); if (q.Length >= 3 && q.TakeLast(3).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1) result.Add(new("", "Operator activity", "info", $"Repeated searches for ‘{q[^1]}’ were detected.", "Review recent telemetry and alerts for this device.", DateTime.UtcNow)); } return result.GroupBy(s => $"{s.SensorMac}|{s.Message}", StringComparer.OrdinalIgnoreCase).Select(g => g.First()).OrderByDescending(s => s.Severity == "critical").ThenByDescending(s => s.CreatedAtUtc).Take(20).ToList(); }
    public void RecordCommand(string mac, string command, string inverse) { lock (_gate) _undoStack.Push(new(mac, command, inverse, DateTime.UtcNow)); }
    public bool TryPopUndo(out CommandUndoEntry? entry) { lock (_gate) { if (_undoStack.Count == 0) { entry = null; return false; } entry = _undoStack.Pop(); return true; } }
    public object GetCommandMetrics() { lock (_gate) return new { QueueDepth = _routineQueue.Count, CriticalQueueDepth = _criticalQueue.Count, UndoDepth = _undoStack.Count, DeviceRegistryCount = _devices.Count, TimelineTimestampCount = _timeline.Count, ActiveAlertCount = _activeAlerts.Count, ProcessedCount = Interlocked.Read(ref _processedCount) }; }
}
