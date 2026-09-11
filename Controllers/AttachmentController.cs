using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;
using System.IO;

namespace SmartIotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentController : ControllerBase
    {
        private readonly SmartIotContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly string _attachmentsFolderPath;
        private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB

        public AttachmentController(SmartIotContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
            _attachmentsFolderPath = Path.Combine(_environment.ContentRootPath, "bin", "Debug", "net10.0", "attachments");

            // Ensure attachments directory exists
            if (!Directory.Exists(_attachmentsFolderPath))
            {
                Directory.CreateDirectory(_attachmentsFolderPath);
            }
        }

        /// <summary>
        /// Get all attachments
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Attachment>>> GetAttachments()
        {
            return await _context.Attachments.ToListAsync();
        }

        /// <summary>
        /// Get attachment details by attachment ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Attachment>> GetAttachment(int id)
        {
            var attachment = await _context.Attachments.FirstOrDefaultAsync(a => a.AttachmentId == id);

            if (attachment == null)
            {
                return NotFound();
            }

            return attachment;
        }

        /// <summary>
        /// Get all attachments for a specific sensor
        /// </summary>
        [HttpGet("sensor/{sensorMac}")]
        public async Task<ActionResult<IEnumerable<Attachment>>> GetAttachmentsBySensor(string sensorMac)
        {
            var attachments = await _context.Attachments
                .Where(a => a.SensorMac == sensorMac)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();

            return attachments;
        }

        /// <summary>
        /// Upload a file as an attachment for a sensor
        /// </summary>
        [HttpPost("upload")]
        public async Task<ActionResult<Attachment>> UploadAttachment(
     [FromForm] AttachmentUploadRequest request)
        {
            Console.WriteLine(
                $"UploadAttachment called - sensorMac: '{request.SensorMac}', fileType: '{request.FileType}'"
            );

            Console.WriteLine(
                $"File: {request.File?.FileName}, Size: {request.File?.Length}"
            );

            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest("No file provided");
            }

            if (string.IsNullOrWhiteSpace(request.SensorMac))
            {
                return BadRequest("sensorMac is required and cannot be empty");
            }

            if (request.File.Length > MaxFileSize)
            {
                return BadRequest(
                    $"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)} MB"
                );
            }

            // Validate sensor exists
            var sensor = await _context.Sensors.FindAsync(request.SensorMac);

            if (sensor == null)
            {
                return NotFound("Sensor not found");
            }

            // Validate file type
            var allowedExtensions = new[]
            {
        ".csv",
        ".json",
        ".txt",
        ".xml",
        ".log",
        ".pdf",
        ".xlsx",
        ".xls",
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".bmp"
    };

            var fileExtension = Path
                .GetExtension(request.File.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(
                    $"File type {fileExtension} is not allowed. " +
                    $"Allowed types: {string.Join(", ", allowedExtensions)}"
                );
            }

            try
            {
                var uniqueFileName =
                    $"{DateTime.UtcNow:yyyyMMddHHmmss}_" +
                    $"{Guid.NewGuid().ToString().Substring(0, 8)}" +
                    fileExtension;

                var safeSensorMac = request.SensorMac.Replace(":", "-");

                var sensorFolder = Path.Combine(
                    _attachmentsFolderPath,
                    safeSensorMac
                );

                if (!Directory.Exists(sensorFolder))
                {
                    Directory.CreateDirectory(sensorFolder);
                }

                var filePath = Path.Combine(
                    sensorFolder,
                    uniqueFileName
                );

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                var attachment = new Attachment
                {
                    SensorMac = request.SensorMac,
                    FileName = request.File.FileName,
                    FilePath = filePath,
                    FileType = request.FileType,
                    UploadedAt = DateTime.UtcNow
                };

                _context.Attachments.Add(attachment);

                await _context.SaveChangesAsync();

                return CreatedAtAction(
                    "GetAttachment",
                    new { id = attachment.AttachmentId },
                    attachment
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex}");

                return StatusCode(
                    500,
                    $"Error uploading file: {ex.Message}"
                );
            }
        }
        /// <summary>
        /// Download a file attachment
        /// </summary>
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var attachment = await _context.Attachments.FindAsync(id);
            if (attachment == null)
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(attachment.FilePath))
            {
                return NotFound("File not found on disk");
            }

            var stream = System.IO.File.OpenRead(attachment.FilePath);
            return File(stream, "application/octet-stream", attachment.FileName);
        }

        /// <summary>
        /// View file contents (for text-based files)
        /// </summary>
        [HttpGet("view/{id}")]
        public async Task<ActionResult<object>> ViewAttachment(int id)
        {
            var attachment = await _context.Attachments.FindAsync(id);
            if (attachment == null)
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(attachment.FilePath))
            {
                return NotFound("File not found on disk");
            }

            try
            {
                var extension = Path.GetExtension(attachment.FileName).ToLowerInvariant();
                var isTextFile = new[] { ".txt", ".csv", ".json", ".xml", ".log" }.Contains(extension);

                if (!isTextFile)
                {
                    return BadRequest("This file type cannot be previewed. Please download instead.");
                }

                var content = await System.IO.File.ReadAllTextAsync(attachment.FilePath);
                return new { fileName = attachment.FileName, fileType = attachment.FileType, content };
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error reading file: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new attachment
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Attachment>> CreateAttachment(Attachment attachment)
        {
            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAttachment", new { id = attachment.AttachmentId }, attachment);
        }

        /// <summary>
        /// Update an existing attachment
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAttachment(int id, Attachment attachment)
        {
            if (id != attachment.AttachmentId)
            {
                return BadRequest();
            }

            _context.Entry(attachment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Attachments.Any(a => a.AttachmentId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Delete an attachment
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            var attachment = await _context.Attachments.FindAsync(id);
            if (attachment == null)
            {
                return NotFound();
            }

            // Delete file from disk if it exists
            if (System.IO.File.Exists(attachment.FilePath))
            {
                System.IO.File.Delete(attachment.FilePath);
            }

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
