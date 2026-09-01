using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;

namespace SmartIotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeploymentNodeController : ControllerBase
    {
        private readonly SmartIotContext _context;
        private readonly DeploymentValidator _validator;

        public DeploymentNodeController(SmartIotContext context)
        {
            _context = context;
            _validator = new DeploymentValidator();
        }

        /// <summary>
        /// Get all deployment nodes (entire hierarchy)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetDeploymentNodes()
        {
            var nodes = await _context.DeploymentNodes
                .Include(n => n.Children)
                .Include(n => n.Sensors)
                .ToListAsync();

            var result = nodes.Select(n => new
            {
                n.NodeId,
                n.Name,
                n.Description,
                n.IsActive,
                n.ParentNodeId,
                n.CreatedAt,
                ChildrenCount = n.Children?.Count ?? 0,
                SensorsCount = n.Sensors?.Count ?? 0,
                PathSafe = _validator.IsDeploymentPathSafe(n)
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Get a specific deployment node with full hierarchy details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetDeploymentNode(int id)
        {
            var node = await _context.DeploymentNodes
                .Include(n => n.Children)
                .Include(n => n.Parent)
                .Include(n => n.Sensors)
                .FirstOrDefaultAsync(n => n.NodeId == id);

            if (node == null)
            {
                return NotFound();
            }

            var deploymentPath = _validator.GetDeploymentPath(node);
            var isSafe = _validator.IsDeploymentPathSafe(node);

            var response = new
            {
                node.NodeId,
                node.Name,
                node.Description,
                node.IsActive,
                node.ParentNodeId,
                node.CreatedAt,
                Parent = node.Parent != null ? new { node.Parent.NodeId, node.Parent.Name } : null,
                Children = node.Children?.Select(c => new { c.NodeId, c.Name, c.IsActive }).ToList() ?? new(),
                AssociatedSensors = node.Sensors?.Select(s => new { s.Macaddress, s.NodeId, s.IsActive }).ToList() ?? new(),
                DeploymentPath = deploymentPath,
                PathSafe = isSafe
            };

            return Ok(response);
        }

        /// <summary>
        /// Create a new deployment node
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DeploymentNode>> CreateDeploymentNode(DeploymentNode node)
        {
            // Validate parent exists if specified
            if (node.ParentNodeId.HasValue)
            {
                var parent = await _context.DeploymentNodes.FindAsync(node.ParentNodeId);
                if (parent == null)
                {
                    return BadRequest("Specified parent node does not exist.");
                }
            }

            node.CreatedAt = DateTime.UtcNow;
            _context.DeploymentNodes.Add(node);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDeploymentNode", new { id = node.NodeId }, node);
        }

        /// <summary>
        /// Update a deployment node
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDeploymentNode(int id, DeploymentNode node)
        {
            if (id != node.NodeId)
            {
                return BadRequest();
            }

            // Validate parent exists if specified
            if (node.ParentNodeId.HasValue)
            {
                var parent = await _context.DeploymentNodes.FindAsync(node.ParentNodeId);
                if (parent == null)
                {
                    return BadRequest("Specified parent node does not exist.");
                }

                // Prevent circular references
                if (_validator.IsDeploymentPathSafe(parent) == false)
                {
                    return BadRequest("Setting this parent would create a circular reference.");
                }
            }

            _context.Entry(node).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.DeploymentNodes.Any(n => n.NodeId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a deployment node (only if it has no children or sensors)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeploymentNode(int id)
        {
            var node = await _context.DeploymentNodes
                .Include(n => n.Children)
                .Include(n => n.Sensors)
                .FirstOrDefaultAsync(n => n.NodeId == id);

            if (node == null)
            {
                return NotFound();
            }

            if (node.Children.Any() || node.Sensors.Any())
            {
                return BadRequest("Cannot delete a deployment node that has child nodes or associated sensors.");
            }

            _context.DeploymentNodes.Remove(node);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Validate the entire deployment tree for circular references and inactive nodes
        /// </summary>
        [HttpPost("{id}/validate")]
        public async Task<ActionResult<object>> ValidateDeploymentTree(int id)
        {
            var node = await _context.DeploymentNodes
                .Include(n => n.Children)
                .Include(n => n.Sensors)
                .FirstOrDefaultAsync(n => n.NodeId == id);

            if (node == null)
            {
                return NotFound();
            }

            var isSafe = _validator.ValidateDeploymentTreeSafety(node);
            var path = _validator.GetDeploymentPath(node);
            var pathSafe = _validator.IsDeploymentPathSafe(node);

            var response = new
            {
                NodeId = node.NodeId,
                NodeName = node.Name,
                TreeValidationPassed = isSafe,
                PathToRootValid = pathSafe,
                DeploymentPath = path,
                Message = isSafe 
                    ? "Entire deployment tree is safe. All nodes are active and path to root is valid." 
                    : "Deployment tree validation failed. Check for inactive nodes or circular references."
            };

            return Ok(response);
        }

        /// <summary>
        /// Get sensors assigned to a specific deployment node
        /// </summary>
        [HttpGet("{id}/sensors")]
        public async Task<ActionResult<IEnumerable<object>>> GetSensorsInNode(int id)
        {
            var node = await _context.DeploymentNodes
                .Include(n => n.Sensors)
                .FirstOrDefaultAsync(n => n.NodeId == id);

            if (node == null)
            {
                return NotFound();
            }

            var sensors = node.Sensors.Select(s => new
            {
                s.Macaddress,
                s.NodeId,
                s.Category,
                s.IsActive,
                s.RegisteredAt,
                DeploymentPathSafe = _validator.IsDeploymentPathSafe(node)
            }).ToList();

            return Ok(sensors);
        }
    }
}
