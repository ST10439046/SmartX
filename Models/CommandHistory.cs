using System;
using System.Collections.Generic;

namespace SmartIotApi.Models;

public partial class CommandHistory
{
    public int CommandId { get; set; }

    public string SensorMac { get; set; } = null!;

    public string? CommandType { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Sensor SensorMacNavigation { get; set; } = null!;
}
