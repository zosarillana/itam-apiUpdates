using ITAM.DataContext;
using ITAM.Models.Logs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class RepairLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RepairLogsController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        // GET: api/RepairLogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetRepairLogs(
         [FromQuery] string? type,
         [FromQuery] string? item_id)
        {
            var query = _context.repair_logs.AsQueryable();
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(log => log.type == type);
            }
            if (!string.IsNullOrEmpty(item_id))
            {
                query = query.Where(log => log.item_id == item_id);
            }
            var logs = await query.OrderByDescending(log => log.timestamp).ToListAsync();

            // Fetch item details for relevant logs
            var enrichedLogs = new List<object>();
            foreach (var log in logs)
            {
                object? itemDetails = null;

                // Check if item_id is valid
                if (!string.IsNullOrEmpty(log.item_id) && int.TryParse(log.item_id, out int itemId))
                {
                    // Check if type is a computer component
                    if (new[] { "RAM", "SSD", "HDD", "GPU", "BOARD" }.Contains(log.type))
                    {
                        // Handle computer component
                        var component = await _context.computer_components
                            .Where(c => c.id == itemId)
                            .FirstOrDefaultAsync();

                        if (component != null)
                        {
                            // Fetch computer details directly if the component has history
                            var historyDetails = new List<object>();
                            if (component.history != null && component.history.Count > 0)
                            {
                                foreach (var historyId in component.history)
                                {
                                    if (int.TryParse(historyId.ToString(), out int computerId))
                                    {
                                        // Fetch the computer details with the specific fields
                                        var computerDetails = await _context.computers
                                            .Where(c => c.id == computerId)
                                            .Select(c => new {
                                                c.id,
                                                c.type,
                                                c.date_acquired,
                                                c.asset_barcode,
                                                c.brand,
                                                c.model,
                                                c.ram,
                                                c.ssd,
                                                c.hdd,
                                                c.gpu,
                                                c.board,
                                                c.size,
                                                c.color,
                                                c.serial_no,
                                                c.po,
                                                c.warranty,
                                                c.cost,
                                                c.remarks,
                                                c.li_description
                                            })
                                            .FirstOrDefaultAsync();

                                        if (computerDetails != null)
                                        {
                                            historyDetails.Add(computerDetails);
                                        }
                                    }
                                }
                            }

                            // Create a new anonymous object with component details and history details
                            itemDetails = new
                            {
                                component.id,
                                component.date_acquired,
                                component.type,
                                component.description,
                                component.asset_barcode,
                                component.uid,
                                component.cost,
                                component.status,
                                history_details = historyDetails,
                                component.owner_id,
                                component.owner,
                                component.is_deleted,
                                component.component_image,
                                component.computer_id
                            };
                        }
                    }
                    else
                    {
                        // Handle non-computer components (assets)
                        var asset = await _context.Assets
                            .Where(a => a.id == itemId)
                            .FirstOrDefaultAsync();

                        if (asset != null)
                        {
                            // Create object with asset details
                            itemDetails = new
                            {
                                asset.id,
                                asset.date_acquired,
                                asset.type,
                                asset.asset_barcode,
                                asset.brand,
                                asset.model,
                                asset.size,
                                asset.color,
                                asset.serial_no,
                                asset.po,
                                asset.warranty,
                                asset.cost,
                                asset.remarks,
                                asset.root_history,
                                // Include other relevant asset properties here
                                asset_details = true // Flag to indicate this is an asset, not a component
                            };
                        }
                    }
                }

                enrichedLogs.Add(new
                {
                    log.id,
                    log.type,
                    log.eaf_no,
                    log.action,
                    log.timestamp,
                    log.computer_id,
                    log.remarks,
                    item_id = itemDetails, // Now includes component or asset details

                });
            }

            return Ok(enrichedLogs);
        }

        [Authorize]
        // GET: api/RepairLogs/byComputer/{computer_id}
        [HttpGet("byComputer/{computer_id}")]
        public async Task<ActionResult<IEnumerable<object>>> GetRepairLogsByComputer(string computer_id)
        {
            var logs = await _context.repair_logs
                .Where(log => log.computer_id == computer_id)
                .OrderByDescending(log => log.timestamp)
                .ToListAsync();

            // Return an empty array if no logs are found
            if (!logs.Any())
            {
                return Ok(new List<object>());
            }

            // Fetch item details for each log
            var enrichedLogs = new List<object>();
            foreach (var log in logs)
            {
                object? itemDetails = null;

                // Check if item_id is valid
                if (!string.IsNullOrEmpty(log.item_id) && int.TryParse(log.item_id, out int itemId))
                {
                    if (new[] { "RAM", "SSD", "HDD", "GPU", "BOARD" }.Contains(log.type))
                    {
                        // Handle computer components
                        var component = await _context.computer_components
                            .Where(c => c.id == itemId)
                            .FirstOrDefaultAsync();

                        if (component != null)
                        {
                            itemDetails = new
                            {
                                component.id,
                                component.date_acquired,
                                component.type,
                                component.description,
                                component.asset_barcode,
                                component.uid,
                                component.cost,
                                component.status,
                                component.owner_id,
                                component.owner,
                                component.is_deleted,
                                component.component_image,
                                component.computer_id
                            };
                        }
                    }
                    else
                    {
                        // Handle assets
                        var asset = await _context.Assets
                            .Where(a => a.id == itemId)
                            .FirstOrDefaultAsync();

                        if (asset != null)
                        {
                            itemDetails = new
                            {
                                asset.id,
                                asset.date_acquired,
                                asset.type,
                                asset.asset_barcode,
                                asset.brand,
                                asset.model,
                                asset.size,
                                asset.color,
                                asset.serial_no,
                                asset.po,
                                asset.warranty,
                                asset.cost,
                                asset.remarks,
                                asset.root_history
                            };
                        }
                    }
                }

                enrichedLogs.Add(new
                {
                    log.id,
                    log.type,
                    log.eaf_no,
                    log.action,
                    log.timestamp,
                    log.computer_id,
                    log.remarks,
                    log.inventory_code,
                    item_id = itemDetails // Now includes component or asset details
                });
            }

            return Ok(enrichedLogs);
        }

        [Authorize]
        // GET: api/RepairLogs/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Repair_logs>> GetRepairLogById(int id)
        {
            var log = await _context.repair_logs.FindAsync(id);

            if (log == null)
            {
                return NotFound(new { message = "Repair log not found" });
            }

            return Ok(log);
        }
    }
}
