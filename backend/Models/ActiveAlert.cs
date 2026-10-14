// Active alert entity: describes an alert record that the application can store and display.
﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartIotApi.Models;

public partial class ActiveAlert
{
    public int AlertId { get; set; }

    public string SensorMac { get; set; } = null!;

    public string? ErrorCode { get; set; }

    public string? Description { get; set; }

    public bool? IsResolved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Navigation property to the sensor that generated this alert.
    /// Set to JsonIgnore to prevent circular reference serialization issues.
    /// The SensorMac foreign key provides the reference needed by clients.
    /// </summary>
    [JsonIgnore]
    public virtual Sensor SensorMacNavigation { get; set; } = null!;
}
