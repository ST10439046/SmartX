using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartIotApi.Models;

public partial class Sensor
{
    public string Macaddress { get; set; } = null!;

    public string? Room { get; set; }

    public string? Zone { get; set; }

    public string? NodeId { get; set; }

    public string? Category { get; set; }

    public DateTime? RegisteredAt { get; set; }

    /// <summary>
    /// Indicates whether this sensor is currently active and operational.
    /// Default is true (active by default).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Foreign key to the DeploymentNode where this sensor is located in the hierarchy.
    /// Allows traversal up the hierarchy to validate the sensor's deployment path.
    /// </summary>
    public int? DeploymentNodeId { get; set; }

    public virtual ICollection<ActiveAlert> ActiveAlerts { get; set; } = new List<ActiveAlert>();

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<CommandHistory> CommandHistories { get; set; } = new List<CommandHistory>();

    public virtual ICollection<NetworkTopology> NetworkTopologyFromNodeMacNavigations { get; set; } = new List<NetworkTopology>();

    public virtual ICollection<NetworkTopology> NetworkTopologyToNodeMacNavigations { get; set; } = new List<NetworkTopology>();

    /// <summary>
    /// Navigation property to the deployment tier where this sensor is located.
    /// </summary>
    [JsonIgnore]
    public virtual DeploymentNode? DeploymentNode { get; set; }

    [JsonIgnore]
    public virtual ICollection<TelemetryDatum>? TelemetryData { get; set; } = new List<TelemetryDatum>();
}
