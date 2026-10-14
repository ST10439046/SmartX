// Operator activity entity: stores searches, commands, and telemetry events for explainable pattern detection.
namespace SmartIotApi.Models;

public sealed class OperatorActivity
{
    public long OperatorActivityId { get; set; }
    public string ActivityType { get; set; } = null!;
    public string? SensorMac { get; set; }
    public string Detail { get; set; } = null!;
    public string NormalizedDetail { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
