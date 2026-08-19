using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;

namespace SmartIotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommandHistoryController : ControllerBase
    {
        private readonly SmartIotContext _context;

        public CommandHistoryController(SmartIotContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all command history records
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CommandHistory>>> GetCommandHistories()
        {
            return await _context.CommandHistories.Include(c => c.SensorMacNavigation).ToListAsync();
        }

        /// <summary>
        /// Get command history details by command ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CommandHistory>> GetCommandHistory(int id)
        {
            var commandHistory = await _context.CommandHistories
                .Include(c => c.SensorMacNavigation)
                .FirstOrDefaultAsync(c => c.CommandId == id);

            if (commandHistory == null)
            {
                return NotFound();
            }

            return commandHistory;
        }

        /// <summary>
        /// Create a new command history record
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CommandHistory>> CreateCommandHistory(CommandHistory commandHistory)
        {
            _context.CommandHistories.Add(commandHistory);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCommandHistory", new { id = commandHistory.CommandId }, commandHistory);
        }

        /// <summary>
        /// Update an existing command history record
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCommandHistory(int id, CommandHistory commandHistory)
        {
            if (id != commandHistory.CommandId)
            {
                return BadRequest();
            }

            _context.Entry(commandHistory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.CommandHistories.Any(c => c.CommandId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a command history record
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommandHistory(int id)
        {
            var commandHistory = await _context.CommandHistories.FindAsync(id);
            if (commandHistory == null)
            {
                return NotFound();
            }

            _context.CommandHistories.Remove(commandHistory);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
