// Network-topology API controller: handles requests for the application's sensor-network topology data.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;

namespace SmartIotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NetworkTopologyController : ControllerBase
    {
        private readonly SmartIotContext _context;

        public NetworkTopologyController(SmartIotContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all network topologies
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NetworkTopology>>> GetNetworkTopologies()
        {
            // Note: Navigation properties are JsonIgnored to prevent circular references.
            // Use FromNodeMac and ToNodeMac foreign keys to fetch sensor details.
            return await _context.NetworkTopologies.ToListAsync();
        }

        /// <summary>
        /// Get network topology details by link ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<NetworkTopology>> GetNetworkTopology(int id)
        {
            // Note: Navigation properties are JsonIgnored to prevent circular references.
            var topology = await _context.NetworkTopologies.FirstOrDefaultAsync(n => n.LinkId == id);

            if (topology == null)
            {
                return NotFound();
            }

            return topology;
        }

        /// <summary>
        /// Create a new network topology link
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<NetworkTopology>> CreateNetworkTopology(NetworkTopology topology)
        {
            var validation = await ValidateLink(topology, null);
            if (validation != null) return BadRequest(new { message = validation });
            _context.NetworkTopologies.Add(topology);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetNetworkTopology", new { id = topology.LinkId }, topology);
        }

        /// <summary>
        /// Update an existing network topology link
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNetworkTopology(int id, NetworkTopology topology)
        {
            if (id != topology.LinkId)
            {
                return BadRequest();
            }
            var validation = await ValidateLink(topology, id);
            if (validation != null) return BadRequest(new { message = validation });

            _context.Entry(topology).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.NetworkTopologies.Any(n => n.LinkId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a network topology link
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNetworkTopology(int id)
        {
            var topology = await _context.NetworkTopologies.FindAsync(id);
            if (topology == null)
            {
                return NotFound();
            }

            _context.NetworkTopologies.Remove(topology);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        private async Task<string?> ValidateLink(NetworkTopology topology, int? excludedLinkId)
        {
            if (string.IsNullOrWhiteSpace(topology.FromNodeMac) || string.IsNullOrWhiteSpace(topology.ToNodeMac))
                return "Both endpoint sensor MAC addresses are required.";
            if (string.Equals(topology.FromNodeMac.Trim(), topology.ToNodeMac.Trim(), StringComparison.OrdinalIgnoreCase))
                return "A sensor cannot be linked to itself.";
            if (!double.IsFinite(topology.SignalStrength) || topology.SignalStrength is < 0 or > 100)
                return "SignalStrength must be a normalized percentage between 0 and 100.";

            var endpoints = new[] { topology.FromNodeMac.Trim(), topology.ToNodeMac.Trim() };
            var activeCount = await _context.Sensors.CountAsync(sensor =>
                endpoints.Contains(sensor.Macaddress) && sensor.IsActive);
            if (activeCount != 2)
                return "Both link endpoints must be registered active sensors.";

            var duplicate = await _context.NetworkTopologies.AnyAsync(link =>
                link.LinkId != excludedLinkId &&
                ((link.FromNodeMac == endpoints[0] && link.ToNodeMac == endpoints[1]) ||
                 (link.FromNodeMac == endpoints[1] && link.ToNodeMac == endpoints[0])));
            if (duplicate)
                return "A link already exists between these sensors.";
            return null;
        }

    }
}
