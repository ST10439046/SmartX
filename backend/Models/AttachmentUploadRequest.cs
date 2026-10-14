// Upload request model: describes the form fields the API binds when a file attachment is submitted.
﻿using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartIotApi.Models
{
    public class AttachmentUploadRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        public string SensorMac { get; set; } = string.Empty;

        public string FileType { get; set; } = "document";
    }
}