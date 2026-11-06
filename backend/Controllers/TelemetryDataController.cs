// Telemetry API controller: exposes HTTP endpoints for retrieving and managing recorded sensor telemetry.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;
using SmartIotApi.Services.Mesh;

namespace SmartIotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelemetryDataController : ControllerBase
    {
        private readonly SmartIotContext _context;
        private readonly TelemetryAvlTreeService _telemetryIndex;

        public TelemetryDataController(SmartIotContext context, TelemetryAvlTreeService telemetryIndex)
        {
            _context = context;
            _telemetryIndex = telemetryIndex;
        }

        /// <summary>
        /// Get all telemetry data
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TelemetryDatum>>> GetTelemetryData()
        {
            return await _context.TelemetryData.Include(t => t.SensorMacNavigation).ToListAsync();
        }

        /// <summary>
        /// Get telemetry data details by telemetry ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TelemetryDatum>> GetTelemetryDatum(int id)
        {
            var telemetry = await _context.TelemetryData
                .Include(t => t.SensorMacNavigation)
                .FirstOrDefaultAsync(t => t.TelemetryId == id);

            if (telemetry == null)
            {
                return NotFound();
            }

            return telemetry;
        }

        /// <summary>
        /// Create a new telemetry data record
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TelemetryDatum>> CreateTelemetryDatum(TelemetryDatum telemetry)
        {
            _context.TelemetryData.Add(telemetry);
            await _context.SaveChangesAsync();
            _telemetryIndex.Add(telemetry);

            return CreatedAtAction("GetTelemetryDatum", new { id = telemetry.TelemetryId }, telemetry);
        }

        /// <summary>
        /// Update an existing telemetry data record
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTelemetryDatum(int id, TelemetryDatum telemetry)
        {
            if (id != telemetry.TelemetryId)
            {
                return BadRequest();
            }

            _context.Entry(telemetry).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _telemetryIndex.Rebuild(await _context.TelemetryData.AsNoTracking().ToListAsync());
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.TelemetryData.Any(t => t.TelemetryId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a telemetry data record
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTelemetryDatum(int id)
        {
            var telemetry = await _context.TelemetryData.FindAsync(id);
            if (telemetry == null)
            {
                return NotFound();
            }

            _context.TelemetryData.Remove(telemetry);
            await _context.SaveChangesAsync();
            _telemetryIndex.Rebuild(await _context.TelemetryData.AsNoTracking().ToListAsync());

            return NoContent();
        }
    }
}
