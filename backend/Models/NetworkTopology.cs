// Network topology model: represents the connections or structure of the sensor network.
﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartIotApi.Models;

public partial class NetworkTopology
{
    public int LinkId { get; set; }

    public string FromNodeMac { get; set; } = null!;

    public string ToNodeMac { get; set; } = null!;

    public double? SignalStrength { get; set; }

    /// <summary>
    /// Navigation property to the source sensor in the network link.
    /// Set to JsonIgnore to prevent circular reference serialization.
    /// </summary>
    [JsonIgnore]
    public virtual Sensor FromNodeMacNavigation { get; set; } = null!;

    /// <summary>
    /// Navigation property to the destination sensor in the network link.
    /// Set to JsonIgnore to prevent circular reference serialization.
    /// </summary>
    [JsonIgnore]
    public virtual Sensor ToNodeMacNavigation { get; set; } = null!;
}
