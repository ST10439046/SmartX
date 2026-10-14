using SmartIotApi.Models;
using SmartIotApi.Services;
using Xunit;

namespace SmartIotApi.Tests;

public sealed class RealtimeCommandServiceTests
{
    [Fact]
    public void Drain_ProcessesCriticalPacketsBeforeRoutinePackets()
    {
        var service = new RealtimeCommandService();
        var now = DateTime.UtcNow;
        service.Enqueue(new StreamPacket("AA:BB:CC:DD:EE:01", "temperature", 22, now, false));
        service.Enqueue(new StreamPacket("AA:BB:CC:DD:EE:02", "temperature", 70, now.AddMilliseconds(1), true, "HOT"));

        var drained = service.Drain();

        Assert.Equal(2, drained.Count);
        Assert.Equal("AA:BB:CC:DD:EE:02", drained[0].SensorMac);
        Assert.Equal("AA:BB:CC:DD:EE:01", drained[1].SensorMac);
    }

    [Fact]
    public void BuildSuggestions_DetectsRepeatedSearchThenCommandPattern()
    {
        var service = new RealtimeCommandService();
        var sensor = new Sensor { Macaddress = "AA:BB:CC:DD:EE:01", NodeId = "PUMP-01", Room = "Pump Room", Category = "Actuator" };
        var start = DateTime.UtcNow.AddHours(-3);
        var activities = new[]
        {
            Activity("search", sensor.Macaddress, "pump room", sensor.Macaddress, start),
            Activity("command", sensor.Macaddress, "START", "start", start.AddMinutes(2)),
            Activity("search", sensor.Macaddress, "pump room", sensor.Macaddress, start.AddHours(1)),
            Activity("command", sensor.Macaddress, "START", "start", start.AddHours(1).AddMinutes(2))
        };

        var suggestions = service.BuildSuggestions(new[] { sensor }, Array.Empty<TelemetryDatum>(), activities);

        Assert.Contains(suggestions, suggestion =>
            suggestion.Message.Contains("Recurring operator pattern detected", StringComparison.Ordinal) &&
            suggestion.Message.Contains("2 observed sequences", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildSuggestions_DoesNotClaimPatternFromOneSequence()
    {
        var service = new RealtimeCommandService();
        var sensor = new Sensor { Macaddress = "AA:BB:CC:DD:EE:01", NodeId = "PUMP-01" };
        var now = DateTime.UtcNow.AddMinutes(-5);
        var activities = new[]
        {
            Activity("search", sensor.Macaddress, "PUMP-01", sensor.Macaddress, now),
            Activity("command", sensor.Macaddress, "START", "start", now.AddMinutes(1))
        };

        var suggestions = service.BuildSuggestions(new[] { sensor }, Array.Empty<TelemetryDatum>(), activities);

        Assert.DoesNotContain(suggestions, suggestion => suggestion.Message.Contains("Recurring operator pattern detected", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EnqueueAndDrain_AreSafeUnderConcurrentCalls()
    {
        var service = new RealtimeCommandService();
        var now = DateTime.UtcNow;
        await Task.WhenAll(Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
        {
            for (var i = 0; i < 25; i++)
                service.Enqueue(new StreamPacket($"AA:BB:CC:DD:{worker:X2}:{i:X2}", "reading", i, now.AddTicks(worker * 100 + i), i % 10 == 0));
        })));

        var processed = new List<TimelineEntry>();
        while (true)
        {
            var batch = service.Drain(37);
            if (batch.Count == 0) break;
            processed.AddRange(batch);
        }

        Assert.Equal(200, processed.Count);
    }

    private static OperatorActivity Activity(string type, string mac, string detail, string normalized, DateTime createdAt) =>
        new() { ActivityType = type, SensorMac = mac, Detail = detail, NormalizedDetail = normalized, CreatedAt = createdAt };
}
