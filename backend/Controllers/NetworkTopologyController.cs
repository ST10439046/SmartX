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
    }
}
