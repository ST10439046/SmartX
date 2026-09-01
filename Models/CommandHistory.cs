using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartIotApi.Models;

public partial class CommandHistory
{
    public int CommandId { get; set; }

    public string SensorMac { get; set; } = null!;

    public string? CommandType { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the sensor that received this command.
    /// Set to JsonIgnore to prevent circular reference serialization.
    /// </summary>
    [JsonIgnore]
    public virtual Sensor SensorMacNavigation { get; set; } = null!;
}
