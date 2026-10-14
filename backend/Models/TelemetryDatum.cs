// Telemetry datum entity: represents a single measured value associated with a sensor and timestamp.
﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartIotApi.Models;

public partial class TelemetryDatum
{
    public int TelemetryId { get; set; }

    public string SensorMac { get; set; } = null!;

    public double? DataValue { get; set; }

    public string? DataType { get; set; }

    public DateTime? Timestamp { get; set; }

    [JsonIgnore]
    public virtual Sensor? SensorMacNavigation { get; set; }
}
