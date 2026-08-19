using System;
using System.Collections.Generic;

namespace SmartIotApi.Models;

public partial class Attachment
{
    public int AttachmentId { get; set; }

    public string SensorMac { get; set; } = null!;

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public string? FileType { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual Sensor SensorMacNavigation { get; set; } = null!;
}
