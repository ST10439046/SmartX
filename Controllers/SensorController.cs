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
        private readonly DeploymentValidator _deploymentValidator;

        public SensorController(SmartIotContext context)
        {
            _context = context;
            _deploymentValidator = new DeploymentValidator();
        }

        /// <summary>
        /// Get all active sensors with their deployment path validation status
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSensors()
        {
            var sensors = await _context.Sensors
                .Include(s => s.DeploymentNode)
                .ToListAsync();

            var result = sensors.Select(s => new
            {
                s.Macaddress,
                s.Room,
                s.Zone,
                s.NodeId,
                s.Category,
                s.RegisteredAt,
                s.IsActive,
                DeploymentNodeId = s.DeploymentNodeId,
                DeploymentPathSafe = s.DeploymentNode == null || _deploymentValidator.IsDeploymentPathSafe(s.DeploymentNode)
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Get sensor details by MAC address with deployment validation
        /// </summary>
        [HttpGet("{macaddress}")]
        public async Task<ActionResult<object>> GetSensor(string macaddress)
        {
            var sensor = await _context.Sensors
                .Include(s => s.ActiveAlerts)
                .Include(s => s.Attachments)
                .Include(s => s.CommandHistories)
                .Include(s => s.TelemetryData)
                .Include(s => s.DeploymentNode)
                .FirstOrDefaultAsync(s => s.Macaddress == macaddress);

            if (sensor == null)
            {
                return NotFound();
            }

            // Validate deployment path
            var isDeploymentSafe = sensor.DeploymentNode == null || 
                _deploymentValidator.IsDeploymentPathSafe(sensor.DeploymentNode);
            var deploymentPath = sensor.DeploymentNode != null ? 
                _deploymentValidator.GetDeploymentPath(sensor.DeploymentNode) : new List<string>();

            var response = new
            {
                sensor.Macaddress,
                sensor.Room,
                sensor.Zone,
                sensor.NodeId,
                sensor.Category,
                sensor.RegisteredAt,
                sensor.IsActive,
                DeploymentNodeId = sensor.DeploymentNodeId,
                DeploymentPathSafe = isDeploymentSafe,
                DeploymentPath = deploymentPath,
                ActiveAlertsCount = sensor.ActiveAlerts?.Count ?? 0,
                AttachmentsCount = sensor.Attachments?.Count ?? 0,
                TelemetryDataCount = sensor.TelemetryData?.Count ?? 0
            };

            return Ok(response);
        }

        /// <summary>
        /// Create a new sensor (active by default)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Sensor>> CreateSensor(Sensor sensor)
        {
            // Ensure sensor is active by default
            sensor.IsActive = true;

            // Validate deployment path if a deployment node is specified
            if (sensor.DeploymentNodeId.HasValue)
            {
                var deploymentNode = await _context.DeploymentNodes.FindAsync(sensor.DeploymentNodeId);
                if (deploymentNode == null)
                {
                    return BadRequest("Specified DeploymentNode does not exist.");
                }

                if (!_deploymentValidator.IsDeploymentPathSafe(deploymentNode))
                {
                    return BadRequest("Deployment path is not safe. One or more tiers are inactive or contain circular references.");
                }
            }

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

            // Validate deployment path if a deployment node is specified
            if (sensor.DeploymentNodeId.HasValue)
            {
                var deploymentNode = await _context.DeploymentNodes.FindAsync(sensor.DeploymentNodeId);
                if (deploymentNode == null)
                {
                    return BadRequest("Specified DeploymentNode does not exist.");
                }

                if (!_deploymentValidator.IsDeploymentPathSafe(deploymentNode))
                {
                    return BadRequest("Deployment path is not safe. One or more tiers are inactive or contain circular references.");
                }
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
