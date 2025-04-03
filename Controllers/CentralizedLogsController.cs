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
    public class CentralizedLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CentralizedLogsController(AppDbContext context)
        {
            _context = context;
        }
        [Authorize]

        // GET: api/CentralizedLogs
        [HttpGet]
        public async Task<IActionResult> GetCentralizedLogs(
            int pageNumber = 1,
            int pageSize = 10,
            string sortOrder = "asc",
            string? searchTerm = null)
        {
            try
            {
                var query = _context.centralized_logs.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(log => log.details.Contains(searchTerm) || log.asset_barcode.Contains(searchTerm));
                }

                // Apply sorting
                query = sortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(log => log.timestamp)
                    : query.OrderBy(log => log.timestamp);

                // Apply pagination
                var totalRecords = await query.CountAsync();
                var logs = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

                if (!logs.Any()) return NotFound("No logs found.");

                return Ok(new { TotalRecords = totalRecords, PageNumber = pageNumber, PageSize = pageSize, Logs = logs });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving logs: {ex.Message}");
            }
        }
        [Authorize]

        // GET: api/CentralizedLogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CentralizedLogs>> GetCentralizedLogs(int id)
        {
            var centralizedLogs = await _context.centralized_logs.FindAsync(id);

            if (centralizedLogs == null)
            {
                return NotFound();
            }

            return centralizedLogs;
        }

        [Authorize]
        // GET: api/CentralizedLogs/type/{type}
        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<CentralizedLogs>>> GetCentralizedLogsByType(
            string type,
            int pageNumber = 1,
            int pageSize = 10,
            string sortOrder = "asc",
            string? searchTerm = null)
        {
            if (pageNumber < 1 || pageSize < 1) return BadRequest("Invalid pagination parameters.");

            var query = _context.centralized_logs.Where(log => log.type == type);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(log => log.details.Contains(searchTerm) || log.asset_barcode.Contains(searchTerm));
            }

            // Apply sorting
            query = sortOrder.ToLower() == "desc"
                ? query.OrderByDescending(log => log.timestamp)
                : query.OrderBy(log => log.timestamp);

            // Apply pagination
            var totalRecords = await query.CountAsync();
            var logs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!logs.Any()) return NotFound("No logs found for the given type and filters.");

            return Ok(new
            {
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Logs = logs
            });
        }

        private bool CentralizedLogsExists(int id)
        {
            return _context.centralized_logs.Any(e => e.id == id);
        }
    }
}
