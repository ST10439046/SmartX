using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;

namespace SmartIotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorController : ControllerBase
    {
        private readonly SmartIotContext _context;

        public SensorController(SmartIotContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all sensors
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sensor>>> GetSensors()
        {
            return await _context.Sensors.ToListAsync();
        }

        /// <summary>
        /// Get sensor details by MAC address
        /// </summary>
        [HttpGet("{macaddress}")]
        public async Task<ActionResult<Sensor>> GetSensor(string macaddress)
        {
            var sensor = await _context.Sensors
                .Include(s => s.ActiveAlerts)
                .Include(s => s.Attachments)
                .Include(s => s.CommandHistories)
                .Include(s => s.TelemetryData)
                .FirstOrDefaultAsync(s => s.Macaddress == macaddress);

            if (sensor == null)
            {
                return NotFound();
            }

            return sensor;
        }

        /// <summary>
        /// Create a new sensor
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Sensor>> CreateSensor(Sensor sensor)
        {
            _context.Sensors.Add(sensor);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSensor", new { macaddress = sensor.Macaddress }, sensor);
        }

        /// <summary>
        /// Update an existing sensor
        /// </summary>
        [HttpPut("{macaddress}")]
        public async Task<IActionResult> UpdateSensor(string macaddress, Sensor sensor)
        {
            if (macaddress != sensor.Macaddress)
            {
                return BadRequest();
            }

            _context.Entry(sensor).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Sensors.Any(s => s.Macaddress == macaddress))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a sensor
        /// </summary>
        [HttpDelete("{macaddress}")]
        public async Task<IActionResult> DeleteSensor(string macaddress)
        {
            var sensor = await _context.Sensors.FindAsync(macaddress);
            if (sensor == null)
            {
                return NotFound();
            }

            _context.Sensors.Remove(sensor);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
