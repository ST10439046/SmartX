// Command-stream API controller: handles HTTP requests that interact with the real-time sensor command feature.
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

    public CommandStreamController(SmartIotContext db, RealtimeCommandService stream)
    {
        _db = db;
        _stream = stream;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var sensors = await _db.Sensors.AsNoTracking().ToListAsync(ct);
        await EnsureDefaultRulesAsync(sensors, ct);
        _stream.RegisterDevices(sensors);
        var telemetry = await _db.TelemetryData.AsNoTracking().OrderByDescending(x => x.Timestamp).Take(200).ToListAsync(ct);
        var alerts = await _db.ActiveAlerts.AsNoTracking().Where(x => x.IsResolved != true)
            .OrderByDescending(x => x.CreatedAt).Take(100)
            .Select(x => new { x.AlertId, x.SensorMac, x.ErrorCode, x.Description, x.CreatedAt, x.AcknowledgedAt, x.AcknowledgedBy }).ToListAsync(ct);
        var commands = await _db.CommandHistories.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(50)
            .Select(x => new { x.CommandId, x.SensorMac, x.CommandType, x.Status, x.CreatedAt }).ToListAsync(ct);
        var deviceStates = await _db.CommandDeviceStates.AsNoTracking().ToListAsync(ct);
        var rules = await _db.AlertRules.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        return Ok(new { stream = _stream.GetSnapshot(), metrics = _stream.GetCommandMetrics(), sensors, telemetry, alerts, commands, deviceStates, alertRules = rules });
    }

    [HttpPost("telemetry")]
    public async Task<IActionResult> EnqueueTelemetry(StreamPacketRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SensorMac) || string.IsNullOrWhiteSpace(request.DataType))
            return BadRequest(new { message = "SensorMac and DataType are required." });

        var mac = request.SensorMac.Trim();
        var sensor = await _db.Sensors.AsNoTracking().FirstOrDefaultAsync(s => s.Macaddress == mac, ct);
        if (sensor is null) return NotFound(new { message = "Sensor was not found." });
        await EnsureDefaultRulesAsync(new[] { sensor }, ct);

        var type = request.DataType.Trim();
        var timestamp = request.Timestamp ?? DateTime.UtcNow;
        var matchedRules = await _db.AlertRules.Where(r => r.IsEnabled &&
            (r.SensorMac == null || r.SensorMac == mac) && r.DataType.ToLower() == type.ToLower()).ToListAsync(ct);
        var breachedRules = matchedRules.Where(r => IsRuleBreached(r.Comparison, request.DataValue, r.Threshold)).ToList();
        var critical = request.IsCritical || IsCritical(type, request.DataValue) || breachedRules.Any(r => r.Severity == "critical");

        _stream.RegisterDevices(new[] { sensor });
        _stream.Enqueue(new(mac, type, request.DataValue, timestamp, critical, request.ErrorCode));
        var processed = _stream.Drain();
        _db.TelemetryData.Add(new TelemetryDatum { SensorMac = mac, DataType = type, DataValue = request.DataValue, Timestamp = timestamp });
        _db.OperatorActivities.Add(new OperatorActivity { ActivityType = "telemetry", SensorMac = mac, Detail = $"{type}={request.DataValue:0.###}", NormalizedDetail = type.ToLowerInvariant(), CreatedAt = timestamp });

        // Resolve rule alerts automatically when a new reading returns inside the configured range.
        foreach (var rule in matchedRules)
        {
            var code = $"RULE-{rule.AlertRuleId}";
            var activeAlert = await _db.ActiveAlerts.FirstOrDefaultAsync(a =>
                a.SensorMac == mac && a.ErrorCode == code && a.IsResolved != true, ct);
            var breached = breachedRules.Any(r => r.AlertRuleId == rule.AlertRuleId);
            if (breached && activeAlert is null)
            {
                _db.ActiveAlerts.Add(new ActiveAlert
                {
                    SensorMac = mac,
                    ErrorCode = code,
                    Description = $"[{rule.Severity.ToUpperInvariant()}] {rule.Name}: {type} reading {request.DataValue} breached {rule.Comparison} {rule.Threshold}.",
                    IsResolved = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (!breached && activeAlert is not null)
            {
                activeAlert.IsResolved = true;
                _stream.ResolveAlert(mac, code);
            }
        }

        // Keep built-in safety alerts for installations without a matching custom rule.
        var builtInCritical = request.IsCritical || IsCritical(type, request.DataValue);
        if (builtInCritical)
        {
            var code = string.IsNullOrWhiteSpace(request.ErrorCode) ? "AUTO-THRESHOLD" : request.ErrorCode.Trim();
            if (!await _db.ActiveAlerts.AnyAsync(a => a.SensorMac == mac && a.ErrorCode == code && a.IsResolved != true, ct))
                _db.ActiveAlerts.Add(new ActiveAlert { SensorMac = mac, ErrorCode = code, Description = $"Critical stream reading: {type} = {request.DataValue}", IsResolved = false, CreatedAt = DateTime.UtcNow });
        }
        else
        {
            var recoveredAlerts = await _db.ActiveAlerts.Where(a => a.SensorMac == mac &&
                (a.ErrorCode == "AUTO-THRESHOLD" || (a.ErrorCode != null && !a.ErrorCode.StartsWith("RULE-"))) && a.IsResolved != true).ToListAsync(ct);
            foreach (var recovered in recoveredAlerts)
            {
                recovered.IsResolved = true;
                if (!string.IsNullOrWhiteSpace(recovered.ErrorCode))
                    _stream.ResolveAlert(mac, recovered.ErrorCode);
            }
        }

        await _db.SaveChangesAsync(ct);
        return Accepted(new
        {
            accepted = true,
            isCritical = critical,
            triggeredRules = breachedRules.Select(r => new { r.AlertRuleId, r.Name, r.Severity }),
            processedCount = processed.Count,
            metrics = _stream.GetCommandMetrics()
        });
    }

    [HttpPost("commands")]
    public async Task<IActionResult> IssueCommand(OverrideCommandRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SensorMac) || string.IsNullOrWhiteSpace(request.CommandType))
            return BadRequest(new { message = "SensorMac and CommandType are required." });

        var mac = request.SensorMac.Trim();
        var command = request.CommandType.Trim().ToUpperInvariant();
        if (command is not ("ON" or "OFF" or "OPEN" or "CLOSE" or "START" or "STOP" or "ENABLE" or "DISABLE"))
            return BadRequest(new { message = "Supported commands: ON, OFF, OPEN, CLOSE, START, STOP, ENABLE, DISABLE." });

        var sensor = await _db.Sensors.FirstOrDefaultAsync(s => s.Macaddress == mac, ct);
        if (sensor is null) return NotFound(new { message = "Sensor was not found." });

        var inverse = command switch
        {
            "ON" => "OFF", "OFF" => "ON", "OPEN" => "CLOSE", "CLOSE" => "OPEN",
            "START" => "STOP", "STOP" => "START", "ENABLE" => "DISABLE", _ => "ENABLE"
        };

        var state = await _db.CommandDeviceStates.FirstOrDefaultAsync(s => s.SensorMac == mac, ct);
        var previousState = state?.CurrentState ?? "UNKNOWN";
        if (state is null)
        {
            state = new CommandDeviceState { SensorMac = mac, CurrentState = command, UpdatedAt = DateTime.UtcNow };
            _db.CommandDeviceStates.Add(state);
        }
        else
        {
            state.CurrentState = command;
            state.UpdatedAt = DateTime.UtcNow;
        }

        _db.CommandHistories.Add(new CommandHistory
        {
            SensorMac = mac,
            CommandType = command,
            Status = "Applied (simulated)",
            CreatedAt = DateTime.UtcNow
        });
        _db.OperatorActivities.Add(new OperatorActivity { ActivityType = "command", SensorMac = mac, Detail = command, NormalizedDetail = command.ToLowerInvariant(), CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        _stream.RegisterDevices(new[] { sensor });
        _stream.RecordCommand(mac, command, previousState);

        return Ok(new
        {
            success = true,
            simulated = true,
            sensorMac = mac,
            commandType = command,
            currentState = command,
            previousState,
            undoAvailable = true,
            message = $"{displayName(sensor)} state set to {command}. The simulated state is saved and will persist after restarting the API; no physical hardware is connected."
        });
    }

    [HttpPost("undo")]
    public async Task<IActionResult> Undo(CancellationToken ct)
    {
        if (!_stream.TryPopUndo(out var item) || item is null)
            return Conflict(new { message = "No commands are available to undo in this API session." });

        var state = await _db.CommandDeviceStates.FirstOrDefaultAsync(s => s.SensorMac == item.SensorMac, ct);
        if (state is null)
            return NotFound(new { message = "The original device state no longer exists." });

        state.CurrentState = item.PreviousCommandType;
        state.UpdatedAt = DateTime.UtcNow;
        _db.CommandHistories.Add(new CommandHistory
        {
            SensorMac = item.SensorMac,
            CommandType = $"UNDO:{item.PreviousCommandType}",
            Status = "Applied (simulated)",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return Ok(new
        {
            success = true,
            sensorMac = item.SensorMac,
            currentState = state.CurrentState,
            message = $"Simulated device state reverted to {state.CurrentState}."
        });
    }

    [HttpGet("alert-rules")]
    public async Task<IActionResult> GetAlertRules(CancellationToken ct)
    {
        var sensors = await _db.Sensors.AsNoTracking().ToListAsync(ct);
        await EnsureDefaultRulesAsync(sensors, ct);
        return Ok(await _db.AlertRules.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct));
    }

    [HttpPost("alert-rules")]
    public async Task<IActionResult> CreateAlertRule(AlertRuleRequest request, CancellationToken ct)
    {
        var validation = await ValidateRule(request, ct);
        if (validation is not null) return BadRequest(new { message = validation });
        var rule = new AlertRule
        {
            Name = request.Name.Trim(),
            SensorMac = string.IsNullOrWhiteSpace(request.SensorMac) ? null : request.SensorMac.Trim(),
            DataType = request.DataType.Trim(),
            Comparison = request.Comparison,
            Threshold = request.Threshold,
            Severity = request.Severity.ToLowerInvariant(),
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTime.UtcNow
        };
        _db.AlertRules.Add(rule);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAlertRules), new { id = rule.AlertRuleId }, rule);
    }

    [HttpPut("alert-rules/{id:int}")]
    public async Task<IActionResult> UpdateAlertRule(int id, AlertRuleRequest request, CancellationToken ct)
    {
        var rule = await _db.AlertRules.FirstOrDefaultAsync(r => r.AlertRuleId == id, ct);
        if (rule is null) return NotFound(new { message = "Alert rule was not found." });
        var validation = await ValidateRule(request, ct);
        if (validation is not null) return BadRequest(new { message = validation });
        rule.Name = request.Name.Trim();
        rule.SensorMac = string.IsNullOrWhiteSpace(request.SensorMac) ? null : request.SensorMac.Trim();
        rule.DataType = request.DataType.Trim();
        rule.Comparison = request.Comparison;
        rule.Threshold = request.Threshold;
        rule.Severity = request.Severity.ToLowerInvariant();
        rule.IsEnabled = request.IsEnabled;
        await _db.SaveChangesAsync(ct);
        return Ok(rule);
    }

    [HttpDelete("alert-rules/{id:int}")]
    public async Task<IActionResult> DeleteAlertRule(int id, CancellationToken ct)
    {
        var rule = await _db.AlertRules.FirstOrDefaultAsync(r => r.AlertRuleId == id, ct);
        if (rule is null) return NotFound(new { message = "Alert rule was not found." });
        _db.AlertRules.Remove(rule);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("device/{sensorMac}")]
    public async Task<IActionResult> Device(string sensorMac, CancellationToken ct)
    {
        var sensor = await _db.Sensors.AsNoTracking().FirstOrDefaultAsync(s => s.Macaddress == sensorMac, ct);
        if (sensor is null) return NotFound();
        _stream.RegisterDevices(new[] { sensor });
        _stream.TryGetDevice(sensorMac, out var device);
        var telemetry = await _db.TelemetryData.AsNoTracking().Where(x => x.SensorMac == sensorMac).OrderByDescending(x => x.Timestamp).Take(100).ToListAsync(ct);
        var commands = await _db.CommandHistories.AsNoTracking().Where(x => x.SensorMac == sensorMac).OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync(ct);
        var state = await _db.CommandDeviceStates.AsNoTracking().FirstOrDefaultAsync(s => s.SensorMac == sensorMac, ct);
        return Ok(new { device, state, telemetry, commands });
    }

    [HttpGet("insights")]
    public async Task<IActionResult> Insights(CancellationToken ct)
    {
        var sensors = await _db.Sensors.AsNoTracking().ToListAsync(ct);
        var telemetry = await _db.TelemetryData.AsNoTracking().OrderByDescending(x => x.Timestamp).Take(300).ToListAsync(ct);
        var activities = await _db.OperatorActivities.AsNoTracking().Where(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .OrderByDescending(a => a.CreatedAt).Take(2000).ToListAsync(ct);
        return Ok(_stream.BuildSuggestions(sensors, telemetry, activities));
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search(SearchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Query)) return BadRequest(new { message = "Query is required." });
        var query = request.Query.Trim();
        if (query.Length > 200) return BadRequest(new { message = "Query must be 200 characters or fewer." });

        _stream.RecordQuery(query);
        var normalized = query.ToLowerInvariant();
        var matchingSensor = await _db.Sensors.AsNoTracking().FirstOrDefaultAsync(s =>
            s.Macaddress.ToLower() == normalized ||
            (s.NodeId != null && s.NodeId.ToLower().Contains(normalized)) ||
            (s.Room != null && s.Room.ToLower().Contains(normalized)) ||
            (s.Zone != null && s.Zone.ToLower().Contains(normalized)) ||
            (s.Category != null && s.Category.ToLower().Contains(normalized)), ct);
        _db.OperatorActivities.Add(new OperatorActivity
        {
            ActivityType = "search",
            SensorMac = matchingSensor?.Macaddress,
            Detail = query,
            NormalizedDetail = matchingSensor?.Macaddress.ToLowerInvariant() ?? normalized,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        await PruneOperatorActivityAsync(ct);
        return Ok(new { recorded = true, associatedSensorMac = matchingSensor?.Macaddress });
    }

    [HttpPost("alerts/{id:int}/acknowledge")]
    public async Task<IActionResult> AcknowledgeAlert(int id, AcknowledgeAlertRequest request, CancellationToken ct)
    {
        var alert = await _db.ActiveAlerts.FirstOrDefaultAsync(a => a.AlertId == id, ct);
        if (alert is null) return NotFound(new { message = "Alert was not found." });
        if (alert.IsResolved == true) return Conflict(new { message = "Resolved alerts cannot be acknowledged as active alerts." });
        if (alert.AcknowledgedAt is not null)
            return Ok(new { success = true, alertId = id, acknowledgedAt = alert.AcknowledgedAt, acknowledgedBy = alert.AcknowledgedBy, message = "Alert was already acknowledged." });

        alert.AcknowledgedAt = DateTime.UtcNow;
        alert.AcknowledgedBy = string.IsNullOrWhiteSpace(request.AcknowledgedBy) ? "Operator" : request.AcknowledgedBy.Trim()[..Math.Min(120, request.AcknowledgedBy.Trim().Length)];
        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true, alertId = id, acknowledgedAt = alert.AcknowledgedAt, acknowledgedBy = alert.AcknowledgedBy, message = "Alert acknowledged. It remains active until the sensor returns to normal." });
    }

    private async Task PruneOperatorActivityAsync(CancellationToken ct)
    {
        await _db.OperatorActivities.Where(a => a.CreatedAt < DateTime.UtcNow.AddDays(-90)).ExecuteDeleteAsync(ct);
        var excessIds = await _db.OperatorActivities.AsNoTracking().OrderByDescending(a => a.CreatedAt)
            .Skip(5000).Select(a => a.OperatorActivityId).ToListAsync(ct);
        if (excessIds.Count > 0)
            await _db.OperatorActivities.Where(a => excessIds.Contains(a.OperatorActivityId)).ExecuteDeleteAsync(ct);
    }

    [HttpGet("metrics")]
    public IActionResult Metrics() => Ok(_stream.GetCommandMetrics());

    private async Task EnsureDefaultRulesAsync(IEnumerable<Sensor> sensors, CancellationToken ct)
    {
        var additions = new List<AlertRule>();

        foreach (var sensor in sensors)
        {
            // Defaults are provisioned once per sensor. A sensor with existing rules keeps
            // its customized configuration instead of having missing defaults re-added.
            if (await _db.AlertRules.AnyAsync(r => r.SensorMac == sensor.Macaddress, ct))
                continue;

            var category = sensor.Category?.Trim().ToLowerInvariant() ?? string.Empty;
            var label = sensor.NodeId?.Trim();
            if (string.IsNullOrWhiteSpace(label)) label = sensor.Macaddress;

            void Add(string metric, string comparison, double threshold, string severity, string description)
            {
                additions.Add(new AlertRule
                {
                    Name = $"{label} {description}",
                    SensorMac = sensor.Macaddress,
                    DataType = metric,
                    Comparison = comparison,
                    Threshold = threshold,
                    Severity = severity,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (category.Contains("environment"))
            {
                Add("temperature", ">=", 40, "critical", "temperature high");
                Add("temperature", "<=", 0, "warning", "temperature low");
                Add("humidity", ">=", 85, "warning", "humidity high");
                Add("moisture", "<", 20, "warning", "moisture low");
            }
            else if (category.Contains("power"))
            {
                Add("powerWattage", ">=", 1000, "critical", "power overload");
                Add("powerWattage", "<=", 0, "warning", "power reading invalid");
            }
            else if (category.Contains("actuator"))
            {
                Add("valveState", ">=", 2, "critical", "actuator state invalid high");
                Add("valveState", "<", 0, "critical", "actuator state invalid low");
            }
            else
            {
                Add("reading", ">=", 100, "warning", "reading high");
                Add("reading", "<", 0, "warning", "reading below zero");
            }
        }

        if (additions.Count == 0) return;
        _db.AlertRules.AddRange(additions);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<string?> ValidateRule(AlertRuleRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100) return "Rule name is required and must be 100 characters or fewer.";
        if (string.IsNullOrWhiteSpace(request.DataType) || request.DataType.Trim().Length > 50) return "Metric name is required and must be 50 characters or fewer.";
        if (request.Comparison is not (">" or ">=" or "<" or "<=" or "=")) return "Comparison must be >, >=, <, <= or =.";
        if (!double.IsFinite(request.Threshold)) return "Threshold must be a finite number.";
        if (request.Severity.ToLowerInvariant() is not ("info" or "warning" or "critical")) return "Severity must be info, warning or critical.";
        if (!string.IsNullOrWhiteSpace(request.SensorMac) &&
            !await _db.Sensors.AnyAsync(s => s.Macaddress == request.SensorMac.Trim(), ct))
            return "Selected sensor was not found.";
        return null;
    }

    private static bool IsRuleBreached(string comparison, double value, double threshold) => comparison switch
    {
        ">" => value > threshold,
        ">=" => value >= threshold,
        "<" => value < threshold,
        "<=" => value <= threshold,
        "=" => Math.Abs(value - threshold) < 0.000001,
        _ => false
    };

    private static bool IsCritical(string type, double value)
    {
        var t = type.ToLowerInvariant();
        return ((t.Contains("moisture") || t.Contains("humidity")) && value < 20) ||
               (t.Contains("temperature") && value >= 40) ||
               ((t.Contains("power") || t.Contains("watt")) && value >= 1000);
    }

    private static string displayName(Sensor sensor) => sensor.NodeId ?? sensor.Macaddress;
}

public sealed record StreamPacketRequest(string SensorMac, string DataType, double DataValue, DateTime? Timestamp = null, bool IsCritical = false, string? ErrorCode = null);
public sealed record OverrideCommandRequest(string SensorMac, string CommandType);
public sealed record SearchRequest(string Query);
public sealed record AcknowledgeAlertRequest(string? AcknowledgedBy = null);
public sealed record AlertRuleRequest(string Name, string? SensorMac, string DataType, string Comparison, double Threshold, string Severity = "warning", bool IsEnabled = true);
