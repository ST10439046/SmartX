using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;
using SmartIotApi.Services.Mesh;

namespace SmartIotApi.Controllers;

[ApiController]
[Route("api/mesh")]
public sealed class MeshController(SmartIotContext db) : ControllerBase
{
    private static readonly TimeSpan OfflineAfter = TimeSpan.FromSeconds(30);

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        var sensors = await db.Sensors.AsNoTracking().ToListAsync(ct);
        var telemetry = await db.TelemetryData.AsNoTracking()
            .Where(t => t.Timestamp != null)
            .GroupBy(t => t.SensorMac)
            .Select(group => new { Mac = group.Key, LastSeen = group.Max(t => t.Timestamp) })
            .ToDictionaryAsync(x => x.Mac, x => x.LastSeen, ct);
        var now = DateTime.UtcNow;
        var nodes = sensors.Select(sensor =>
        {
            telemetry.TryGetValue(sensor.Macaddress, out var lastSeen);
            var state = !sensor.IsActive ? "Disabled" :
                lastSeen is null ? "Unknown" :
                now - lastSeen.Value > OfflineAfter ? "Offline" : "Online";
            return new
            {
                sensor.Macaddress, sensor.NodeId, sensor.Room, sensor.Zone, sensor.Category,
                sensor.IsActive, sensor.IsGateway, LastSeenUtc = lastSeen, Status = state
            };
        }).ToList();
        var links = await db.NetworkTopologies.AsNoTracking().Select(link => new
        {
            link.LinkId, link.FromNodeMac, link.ToNodeMac, link.SignalStrength,
            RoutingCost = 101d - link.SignalStrength
        }).ToListAsync(ct);
        return Ok(new
        {
            gateway = nodes.FirstOrDefault(node => node.IsGateway),
            nodes, links,
            summary = new
            {
                totalNodes = nodes.Count,
                onlineNodes = nodes.Count(node => node.Status == "Online"),
                offlineNodes = nodes.Count(node => node.Status == "Offline"),
                unknownNodes = nodes.Count(node => node.Status == "Unknown"),
                totalLinks = links.Count,
                connectedComponents = MeshAlgorithms.MinimumSpanningForest(BuildGraph(sensors.Where(sensor => sensor.IsActive), await db.NetworkTopologies.AsNoTracking().ToListAsync(ct))).Components.Count
            },
            signalStrengthMeaning = "Normalized simulated signal quality percentage (0-100), not measured dBm.",
            routingCostMeaning = "101 - signal quality; lower is better."
        });
    }

    [HttpPut("gateway")]
    public async Task<IActionResult> SetGateway(SetGatewayRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SensorMac))
            return BadRequest(new { message = "SensorMac is required." });
        var mac = request.SensorMac.Trim();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var sensor = await db.Sensors.FirstOrDefaultAsync(s => s.Macaddress == mac, ct);
        if (sensor is null) return NotFound(new { message = "Registered sensor was not found." });
        if (!sensor.IsActive) return Conflict(new { message = "A disabled sensor cannot be selected as the gateway." });
        var previousGateways = await db.Sensors.Where(s => s.IsGateway).ToListAsync(ct);
        foreach (var previousGateway in previousGateways)
            previousGateway.IsGateway = false;
        await db.SaveChangesAsync(ct);
        sensor.IsGateway = true;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { success = true, gatewayMac = sensor.Macaddress, message = "Gateway selection saved. An offline gateway remains selected until an operator changes it." });
    }

    [HttpGet("discovery")]
    public async Task<IActionResult> Discovery([FromQuery] string algorithm = "bfs", CancellationToken ct = default)
    {
        var (graph, gateway, error) = await LoadGraph(ct);
        if (error is not null) return error;
        var result = algorithm.ToLowerInvariant() switch
        {
            "bfs" => MeshAlgorithms.BreadthFirst(graph!, gateway!),
            "dfs" => MeshAlgorithms.DepthFirst(graph!, gateway!),
            _ => null
        };
        if (result is null) return BadRequest(new { message = "Algorithm must be bfs or dfs." });
        return Ok(new { algorithm = algorithm.ToUpperInvariant(), gateway, visitedNodes = result, visitedCount = result.Count });
    }

    [HttpGet("routes")]
    public async Task<IActionResult> Routes([FromQuery] string destination, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(destination))
            return BadRequest(new { message = "Destination sensor MAC is required." });
        var (graph, gateway, error) = await LoadGraph(ct);
        if (error is not null) return error;
        var route = MeshAlgorithms.ShortestPath(graph!, gateway!, destination);
        if (route is null) return NotFound(new { message = "No route exists from the selected gateway to that sensor." });
        return Ok(new { gateway, destination, route.Path, route.TotalCost, hops = route.Path.Count - 1 });
    }

    [HttpGet("telemetry/range")]
    public async Task<IActionResult> TelemetryRange(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] string? sensorMac,
        CancellationToken ct)
    {
        if (fromUtc > toUtc)
            return BadRequest(new { message = "fromUtc must be earlier than or equal to toUtc." });
        if ((toUtc - fromUtc).TotalDays > 31)
            return BadRequest(new { message = "Telemetry range is limited to 31 days per request." });
        var query = db.TelemetryData.AsNoTracking()
            .Where(item => item.Timestamp >= fromUtc && item.Timestamp <= toUtc);
        if (!string.IsNullOrWhiteSpace(sensorMac))
            query = query.Where(item => item.SensorMac == sensorMac.Trim());
        var records = await query.OrderBy(item => item.Timestamp).Take(10000).ToListAsync(ct);
        var index = new TelemetryAvlTree<TelemetryDatum>();
        foreach (var record in records)
            if (record.Timestamp.HasValue)
                index.Add(record.Timestamp.Value, record);
        var ordered = index.Range(fromUtc, toUtc);
        return Ok(new
        {
            fromUtc, toUtc, sensorMac,
            count = ordered.Count,
            index = "AVL timestamp index",
            records = ordered
        });
    }

    [HttpGet("mst")]
    public async Task<IActionResult> MinimumSpanningTree(CancellationToken ct)
    {
        var sensors = await db.Sensors.AsNoTracking().Where(s => s.IsActive).ToListAsync(ct);
        var links = await db.NetworkTopologies.AsNoTracking().ToListAsync(ct);
        var result = MeshAlgorithms.MinimumSpanningForest(BuildGraph(sensors, links));
        return Ok(new
        {
            algorithm = "Prim",
            edges = result.Edges.Select(edge => new { edge.From, edge.To, edge.SignalStrength, edge.RoutingCost }),
            components = result.Components,
            totalCost = result.TotalCost,
            note = "A minimum spanning forest is returned when the network is disconnected."
        });
    }

    [HttpPost("simulations/failures")]
    public async Task<IActionResult> SimulateFailure(FailureSimulationRequest request, CancellationToken ct)
    {
        var sensors = await db.Sensors.AsNoTracking().Where(s => s.IsActive).ToListAsync(ct);
        var links = await db.NetworkTopologies.AsNoTracking().ToListAsync(ct);
        var removed = new HashSet<string>(request.FailedSensorMacs ?? [], StringComparer.OrdinalIgnoreCase);
        foreach (var failedMac in removed)
            if (!sensors.Any(sensor => string.Equals(sensor.Macaddress, failedMac, StringComparison.OrdinalIgnoreCase)))
                return BadRequest(new { message = $"Unknown or inactive sensor in failure simulation: {failedMac}" });
        var failedLinks = new HashSet<int>(request.FailedLinkIds ?? []);
        var filtered = new MeshGraph();
        foreach (var sensor in sensors.Where(sensor => !removed.Contains(sensor.Macaddress))) filtered.AddVertex(sensor.Macaddress);
        foreach (var link in links.Where(link => !failedLinks.Contains(link.LinkId) &&
            !removed.Contains(link.FromNodeMac) && !removed.Contains(link.ToNodeMac)))
            filtered.AddEdge(link.FromNodeMac, link.ToNodeMac, link.SignalStrength);
        var gateway = sensors.FirstOrDefault(sensor => sensor.IsGateway)?.Macaddress;
        return Ok(new
        {
            simulated = true,
            failedSensors = removed,
            failedLinks,
            gatewayReachable = gateway is not null && !removed.Contains(gateway) && filtered.Vertices.Contains(gateway),
            discovery = gateway is not null && !removed.Contains(gateway) && filtered.Vertices.Contains(gateway)
                ? MeshAlgorithms.BreadthFirst(filtered, gateway) : Array.Empty<string>(),
            components = MeshAlgorithms.MinimumSpanningForest(filtered).Components,
            warning = "Simulation is isolated. No sensor, telemetry, or stored network link was modified."
        });
    }

    private async Task<(MeshGraph? Graph, string? Gateway, IActionResult? Error)> LoadGraph(CancellationToken ct)
    {
        var sensors = await db.Sensors.AsNoTracking().Where(s => s.IsActive).ToListAsync(ct);
        var gateway = sensors.FirstOrDefault(sensor => sensor.IsGateway)?.Macaddress;
        if (gateway is null)
            return (null, null, Conflict(new { message = "Select an active registered sensor as the gateway first." }));
        var links = await db.NetworkTopologies.AsNoTracking().ToListAsync(ct);
        var graph = BuildGraph(sensors, links);
        if (!graph.Vertices.Contains(gateway))
            return (null, gateway, Conflict(new { message = "The selected gateway is unavailable." }));
        return (graph, gateway, null);
    }

    private static MeshGraph BuildGraph(IEnumerable<Sensor> sensors, IEnumerable<NetworkTopology> links)
    {
        var active = sensors.Select(sensor => sensor.Macaddress).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var graph = new MeshGraph();
        foreach (var mac in active) graph.AddVertex(mac);
        foreach (var link in links)
            if (active.Contains(link.FromNodeMac) && active.Contains(link.ToNodeMac))
                graph.AddEdge(link.FromNodeMac, link.ToNodeMac, link.SignalStrength);
        return graph;
    }
}

public sealed record SetGatewayRequest(string SensorMac);
public sealed record FailureSimulationRequest(List<string>? FailedSensorMacs, List<int>? FailedLinkIds);
