using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;

namespace SmartIotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentController : ControllerBase
    {
        private readonly SmartIotContext _context;

        public AttachmentController(SmartIotContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all attachments
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Attachment>>> GetAttachments()
        {
            // Note: SensorMacNavigation is JsonIgnored to prevent circular reference.
            // Use the SensorMac foreign key to fetch sensor details if needed.
            return await _context.Attachments.ToListAsync();
        }

        /// <summary>
        /// Get attachment details by attachment ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Attachment>> GetAttachment(int id)
        {
            // Note: SensorMacNavigation is JsonIgnored to prevent circular reference.
            var attachment = await _context.Attachments.FirstOrDefaultAsync(a => a.AttachmentId == id);

            if (attachment == null)
            {
                return NotFound();
            }

            return attachment;
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

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
