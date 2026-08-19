using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;

namespace SmartIotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActiveAlertController : ControllerBase
    {
        private readonly SmartIotContext _context;

        public ActiveAlertController(SmartIotContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all active alerts
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActiveAlert>>> GetActiveAlerts()
        {
            return await _context.ActiveAlerts.Include(a => a.SensorMacNavigation).ToListAsync();
        }

        /// <summary>
        /// Get active alert details by alert ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ActiveAlert>> GetActiveAlert(int id)
        {
            var alert = await _context.ActiveAlerts
                .Include(a => a.SensorMacNavigation)
                .FirstOrDefaultAsync(a => a.AlertId == id);

            if (alert == null)
            {
                return NotFound();
            }

            return alert;
        }

        /// <summary>
        /// Create a new active alert
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ActiveAlert>> CreateActiveAlert(ActiveAlert alert)
        {
            _context.ActiveAlerts.Add(alert);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetActiveAlert", new { id = alert.AlertId }, alert);
        }

        /// <summary>
        /// Update an existing active alert
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateActiveAlert(int id, ActiveAlert alert)
        {
            if (id != alert.AlertId)
            {
                return BadRequest();
            }

            _context.Entry(alert).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ActiveAlerts.Any(a => a.AlertId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Delete an active alert
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteActiveAlert(int id)
        {
            var alert = await _context.ActiveAlerts.FindAsync(id);
            if (alert == null)
            {
                return NotFound();
            }

            _context.ActiveAlerts.Remove(alert);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
