using System;
using System.Collections.Generic;

namespace SmartIotApi.Models;

public partial class TelemetryDatum
{
    public int TelemetryId { get; set; }

    public string SensorMac { get; set; } = null!;

    public double? DataValue { get; set; }

    public string? DataType { get; set; }

    public DateTime? Timestamp { get; set; }

    public virtual Sensor SensorMacNavigation { get; set; } = null!;
}
