using System;
using System.Collections.Generic;

namespace SmartIotApi.Models;

public partial class Sensor
{
    public string Macaddress { get; set; } = null!;

    public string? Room { get; set; }

    public string? Zone { get; set; }

    public string? NodeId { get; set; }

    public string? Category { get; set; }

    public DateTime? RegisteredAt { get; set; }

    public virtual ICollection<ActiveAlert> ActiveAlerts { get; set; } = new List<ActiveAlert>();

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<CommandHistory> CommandHistories { get; set; } = new List<CommandHistory>();

    public virtual ICollection<NetworkTopology> NetworkTopologyFromNodeMacNavigations { get; set; } = new List<NetworkTopology>();

    public virtual ICollection<NetworkTopology> NetworkTopologyToNodeMacNavigations { get; set; } = new List<NetworkTopology>();

    public virtual ICollection<TelemetryDatum> TelemetryData { get; set; } = new List<TelemetryDatum>();
}
