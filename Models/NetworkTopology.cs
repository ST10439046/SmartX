using System;
using System.Collections.Generic;

namespace SmartIotApi.Models;

public partial class NetworkTopology
{
    public int LinkId { get; set; }

    public string FromNodeMac { get; set; } = null!;

    public string ToNodeMac { get; set; } = null!;

    public double? SignalStrength { get; set; }

    public virtual Sensor FromNodeMacNavigation { get; set; } = null!;

    public virtual Sensor ToNodeMacNavigation { get; set; } = null!;
}
