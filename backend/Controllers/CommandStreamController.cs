using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;
using SmartIotApi.Services;

namespace SmartIotApi.Controllers;

[ApiController]
[Route("api/commandstream")]
public sealed class CommandStreamController : ControllerBase
{
    private readonly SmartIotContext _db;
    private readonly RealtimeCommandService _stream;
    public CommandStreamController(SmartIotContext db, RealtimeCommandService stream) { _db = db; _stream = stream; }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var sensors = await _db.Sensors.AsNoTracking().ToListAsync(ct); _stream.RegisterDevices(sensors);
        var telemetry = await _db.TelemetryData.AsNoTracking().OrderByDescending(x => x.Timestamp).Take(200).ToListAsync(ct);
        var alerts = await _db.ActiveAlerts.AsNoTracking().Where(x => x.IsResolved != true).OrderByDescending(x => x.CreatedAt).Take(100).Select(x => new { x.AlertId, x.SensorMac, x.ErrorCode, x.Description, x.CreatedAt }).ToListAsync(ct);
        var commands = await _db.CommandHistories.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(50).Select(x => new { x.CommandId, x.SensorMac, x.CommandType, x.Status, x.CreatedAt }).ToListAsync(ct);
        return Ok(new { stream = _stream.GetSnapshot(), metrics = _stream.GetCommandMetrics(), sensors, telemetry, alerts, commands });
    }

    [HttpPost("telemetry")]
    public async Task<IActionResult> EnqueueTelemetry(StreamPacketRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SensorMac) || string.IsNullOrWhiteSpace(request.DataType)) return BadRequest(new { message = "SensorMac and DataType are required." });
        var mac = request.SensorMac.Trim(); var sensor = await _db.Sensors.AsNoTracking().FirstOrDefaultAsync(s => s.Macaddress == mac, ct);
        if (sensor is null) return NotFound(new { message = "Sensor was not found." });
        var type = request.DataType.Trim(); var critical = request.IsCritical || IsCritical(type, request.DataValue); var timestamp = request.Timestamp ?? DateTime.UtcNow;
        _stream.RegisterDevices(new[] { sensor }); _stream.Enqueue(new(mac, type, request.DataValue, timestamp, critical, request.ErrorCode)); var processed = _stream.Drain();
        _db.TelemetryData.Add(new TelemetryDatum { SensorMac = mac, DataType = type, DataValue = request.DataValue, Timestamp = timestamp });
        if (critical) { var code = string.IsNullOrWhiteSpace(request.ErrorCode) ? "AUTO-THRESHOLD" : request.ErrorCode.Trim(); if (!await _db.ActiveAlerts.AnyAsync(a => a.SensorMac == mac && a.ErrorCode == code && a.IsResolved != true, ct)) _db.ActiveAlerts.Add(new ActiveAlert { SensorMac = mac, ErrorCode = code, Description = $"Critical stream reading: {type} = {request.DataValue}", IsResolved = false, CreatedAt = DateTime.UtcNow }); }
        await _db.SaveChangesAsync(ct); return Accepted(new { accepted = true, isCritical = critical, processedCount = processed.Count, metrics = _stream.GetCommandMetrics() });
    }

    [HttpPost("commands")]
    public async Task<IActionResult> IssueCommand(OverrideCommandRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SensorMac) || string.IsNullOrWhiteSpace(request.CommandType)) return BadRequest(new { message = "SensorMac and CommandType are required." });
        var mac = request.SensorMac.Trim(); var command = request.CommandType.Trim().ToUpperInvariant();
        if (command is not ("ON" or "OFF" or "OPEN" or "CLOSE" or "START" or "STOP" or "ENABLE" or "DISABLE")) return BadRequest(new { message = "Supported commands: ON, OFF, OPEN, CLOSE, START, STOP, ENABLE, DISABLE." });
        var sensor = await _db.Sensors.FirstOrDefaultAsync(s => s.Macaddress == mac, ct); if (sensor is null) return NotFound(new { message = "Sensor was not found." });
        var inverse = command switch { "ON" => "OFF", "OFF" => "ON", "OPEN" => "CLOSE", "CLOSE" => "OPEN", "START" => "STOP", "STOP" => "START", "ENABLE" => "DISABLE", _ => "ENABLE" };
        _db.CommandHistories.Add(new CommandHistory { SensorMac = mac, CommandType = command, Status = "Simulated", CreatedAt = DateTime.UtcNow }); await _db.SaveChangesAsync(ct);
        _stream.RegisterDevices(new[] { sensor }); _stream.RecordCommand(mac, command, inverse);
        return Ok(new { success = true, simulated = true, sensorMac = mac, commandType = command, undoAvailable = true, message = "Override saved to command history. This simulation does not actuate physical hardware." });
    }

    [HttpPost("undo")]
    public async Task<IActionResult> Undo(CancellationToken ct)
    {
        if (!_stream.TryPopUndo(out var item) || item is null) return Conflict(new { message = "No commands are available to undo in this API session." });
        if (!await _db.Sensors.AnyAsync(s => s.Macaddress == item.SensorMac, ct)) return NotFound(new { message = "The original device no longer exists." });
        _db.CommandHistories.Add(new CommandHistory { SensorMac = item.SensorMac, CommandType = $"UNDO:{item.PreviousCommandType}", Status = "Simulated", CreatedAt = DateTime.UtcNow }); await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, sensorMac = item.SensorMac, revertedTo = item.PreviousCommandType, message = $"Undo recorded as {item.PreviousCommandType}. Physical hardware is not controlled by this simulator." });
    }

    [HttpGet("device/{sensorMac}")]
    public async Task<IActionResult> Device(string sensorMac, CancellationToken ct)
    {
        var sensor = await _db.Sensors.AsNoTracking().FirstOrDefaultAsync(s => s.Macaddress == sensorMac, ct); if (sensor is null) return NotFound();
        _stream.RegisterDevices(new[] { sensor }); _stream.TryGetDevice(sensorMac, out var device);
        var telemetry = await _db.TelemetryData.AsNoTracking().Where(x => x.SensorMac == sensorMac).OrderByDescending(x => x.Timestamp).Take(100).ToListAsync(ct);
        var commands = await _db.CommandHistories.AsNoTracking().Where(x => x.SensorMac == sensorMac).OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync(ct);
        return Ok(new { device, telemetry, commands });
    }

    [HttpGet("insights")]
    public async Task<IActionResult> Insights(CancellationToken ct)
    {
        var sensors = await _db.Sensors.AsNoTracking().ToListAsync(ct); var telemetry = await _db.TelemetryData.AsNoTracking().OrderByDescending(x => x.Timestamp).Take(300).ToListAsync(ct);
        return Ok(_stream.BuildSuggestions(sensors, telemetry));
    }

    [HttpPost("search")]
    public IActionResult Search(SearchRequest request) { if (string.IsNullOrWhiteSpace(request.Query)) return BadRequest(new { message = "Query is required." }); _stream.RecordQuery(request.Query); return Ok(new { recorded = true }); }
    [HttpGet("metrics")] public IActionResult Metrics() => Ok(_stream.GetCommandMetrics());
    private static bool IsCritical(string type, double value) { var t = type.ToLowerInvariant(); return ((t.Contains("moisture") || t.Contains("humidity")) && value < 20) || (t.Contains("temperature") && value >= 40) || ((t.Contains("power") || t.Contains("watt")) && value >= 1000); }
}

public sealed record StreamPacketRequest(string SensorMac, string DataType, double DataValue, DateTime? Timestamp = null, bool IsCritical = false, string? ErrorCode = null);
public sealed record OverrideCommandRequest(string SensorMac, string CommandType);
public sealed record SearchRequest(string Query);
