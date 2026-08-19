using System;
using System.Collections.Generic;

namespace SmartIotApi.Models;

public partial class ActiveAlert
{
    public int AlertId { get; set; }

    public string SensorMac { get; set; } = null!;

    public string? ErrorCode { get; set; }

    public string? Description { get; set; }

    public bool? IsResolved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Sensor SensorMacNavigation { get; set; } = null!;
}
