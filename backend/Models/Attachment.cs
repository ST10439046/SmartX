using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartIotApi.Models;

public partial class Attachment
{
    public int AttachmentId { get; set; }

    public string SensorMac { get; set; } = null!;

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public string? FileType { get; set; }

    public DateTime? UploadedAt { get; set; }

    /// <summary>
    /// Navigation property to the sensor that owns this attachment.
    /// Set to JsonIgnore to prevent circular reference serialization.
    /// </summary>
    [JsonIgnore]
    public virtual Sensor SensorMacNavigation { get; set; } = null!;
}
