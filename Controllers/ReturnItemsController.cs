using ITAM.DataContext;
using ITAM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ReturnItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReturnItemsController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        // ✅ 1. Get all return records
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReturnItems>>> GetReturnItems()
        {
            return await _context.return_items
                .Include(r => r.user)
                .Include(r => r.asset)
                .Include(r => r.computer)
                .Include(r => r.user_accountability_list)
                .ToListAsync();
        }

        [Authorize]
        // ✅ 2. Get return item by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<ReturnItems>> GetReturnItemById(int id)
        {
            var returnItem = await _context.return_items
                .Include(r => r.user)
                .Include(r => r.asset)
                .Include(r => r.computer)
                .Include(r => r.user_accountability_list)
                .FirstOrDefaultAsync(r => r.id == id);

            if (returnItem == null)
            {
                return NotFound(new { message = "Return item not found" });
            }

            return returnItem;
        }

        [Authorize]
        // ✅ 3. Get return items by User ID
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ReturnItems>>> GetReturnItemsByUserId(int userId)
        {
            var returnItems = await _context.return_items
                .Where(r => r.user_id == userId)
                .Include(r => r.asset)
                .Include(r => r.computer)
                .Include(r => r.user_accountability_list)
                .ToListAsync();

            if (!returnItems.Any())
            {
                return NotFound(new { message = "No return items found for this user" });
            }

            return returnItems;
        }

        [Authorize]
        [HttpGet("accountability/{accountabilityId}")]
        public async Task<ActionResult<IEnumerable<ReturnItems>>> GetReturnItemsByAccountabilityId(int accountabilityId)
        {
            var returnItems = await _context.return_items
                .Where(r => r.accountability_id == accountabilityId)
                .Include(r => r.user_accountability_list)
                .Include(r => r.user)
                .Include(r => r.asset)
                .Include(r => r.computer)
                .Include(r => r.components)
                .ToListAsync();

            // Check if returnItems contains any data before accessing owner_id
            int? ownerId = returnItems.FirstOrDefault()?.user_accountability_list?.owner_id;

            var owner = ownerId != null
                ? await _context.Users
                    .Where(u => u.id == ownerId)
                    .Select(u => new
                    {
                        u.name,
                        u.employee_id,
                        u.designation,
                        u.company,
                        u.department
                    })
                    .FirstOrDefaultAsync()
                : null;

            return Ok(returnItems.Select(r => new
            {
                r.id,
                r.accountability_id,
                accountability = r.user_accountability_list != null ? new
                {
                    r.user_accountability_list.owner_id,
                    owner, // Use the fetched owner details
                    r.user_accountability_list.tracking_code,
                    r.user_accountability_list.accountability_code
                } : null,
                r.user_id,
                user = r.user != null ? new
                {
                    r.user.name,
                    r.user.employee_id,
                    r.user.designation,
                    r.user.company,
                    r.user.department
                } : null,
                r.asset_id,
                asset = r.asset != null ? new
                {
                    r.asset.type,
                    r.asset.date_acquired,
                    r.asset.asset_barcode,
                    r.asset.brand,
                    r.asset.model,
                    r.asset.history,
                    r.asset.status
                } : null,
                r.computer_id,
                computer = r.computer != null ? new
                {
                    r.computer.type,
                    r.computer.date_acquired,
                    r.computer.asset_barcode,
                    r.computer.model,
                    r.computer.history,
                    r.computer.status
                } : null,
                r.item_type,
                r.component_id,
                components = r.components != null ? new
                {
                    r.components.type,
                    r.components.date_acquired,
                    r.components.uid,
                    r.components.cost,
                } : null,
                r.status,
                r.remarks,
                r.return_date,
                r.validated_by
            }));
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ReturnItems>> CreateReturnItem(ReturnItems returnItem)
        {
            if (returnItem == null)
            {
                return BadRequest(new { message = "Invalid return item data" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                returnItem.return_date = DateTime.UtcNow; // Set timestamp

                // Retrieve the logged-in user ID from claims
                var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                returnItem.validated_by = int.TryParse(userIdClaim, out int userId) ? userId : 0; // Default to 0 for system actions

                _context.return_items.Add(returnItem);
                await _context.SaveChangesAsync();

                // Retrieve the corresponding UserAccountabilityList record
                var accountability = await _context.user_accountability_lists
                    .FirstOrDefaultAsync(ual => ual.id == returnItem.accountability_id);

                if (accountability == null)
                {
                    return BadRequest(new { message = "Accountability record not found." });
                }

                // Set accountability to inactive
                accountability.is_active = false;
                accountability.date_modified = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Fetch the computer being returned
                var computer = await _context.computers.FindAsync(returnItem.computer_id);

                if (computer != null)
                {
                    // Initialize history list if null
                    computer.history ??= new List<string>();

                    // Store previous owner_id in history list
                    if (computer.owner_id.HasValue)
                    {
                        computer.history.Add(computer.owner_id.Value.ToString());
                    }

                    // Remove the existing owner_id
                    computer.owner_id = null;

                    // Update computer status to AVAILABLE
                    computer.status = "AVAILABLE";

                    await _context.SaveChangesAsync();

                    // Find computers that have this computer in their assigned_assets column
                    var assignedComputers = await _context.computers
                        .Where(c => c.assigned_assets != null && c.assigned_assets.Contains(computer.id))
                        .ToListAsync();

                    foreach (var assignedComputer in assignedComputers)
                    {
                        // Initialize history list if null
                        assignedComputer.history ??= new List<string>();

                        // Store previous owner_id in history list
                        if (assignedComputer.owner_id.HasValue)
                        {
                            assignedComputer.history.Add(assignedComputer.owner_id.Value.ToString());
                        }

                        // Remove the existing owner_id
                        assignedComputer.owner_id = null;
                    }

                    await _context.SaveChangesAsync();

                    // 🔴 FIX: Fetch all assets assigned to BOTH returned computer and assigned computers
                    var affectedComputerIds = assignedComputers.Select(c => c.id).ToList();
                    affectedComputerIds.Add(computer.id); // Include the returned computer itself

                    var assignedAssets = await _context.Assets
                        .Where(a => a.computer_id.HasValue && affectedComputerIds.Contains(a.computer_id.Value))
                        .ToListAsync();

                    foreach (var asset in assignedAssets)
                    {
                        // Initialize history list if null
                        asset.history ??= new List<string>();

                        // Store previous owner_id in history list
                        if (asset.owner_id.HasValue)
                        {
                            asset.history.Add(asset.owner_id.Value.ToString());
                        }

                        // Remove the existing owner_id
                        asset.owner_id = null;
                    }

                    // 🔴 NEW: Remove owner_id from computer components (RAM, SSD, HDD, GPU, BOARD) WITHOUT modifying history
                    var components = await _context.computer_components
                        .Where(c => c.computer_id == computer.id)
                        .ToListAsync();

                    foreach (var component in components)
                    {
                        // ❌ Do NOT modify history
                        // Only remove owner_id
                        component.owner_id = null;
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return Ok(new { message = "Return item processed successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = $"Error processing return: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }

        [Authorize]
        // ✅ 5. Update return item status & remarks
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReturnItem(int id, ReturnItems updatedReturnItem)
        {
            var returnItem = await _context.return_items.FindAsync(id);
            if (returnItem == null)
            {
                return NotFound(new { message = "Return item not found" });
            }

            returnItem.status = updatedReturnItem.status;
            returnItem.remarks = updatedReturnItem.remarks;
            returnItem.validated_by = updatedReturnItem.validated_by;
            returnItem.return_date = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(returnItem);
        }

        [Authorize]
        // ✅ 6. Delete a return item entry (Optional: Change to soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReturnItem(int id)
        {
            var returnItem = await _context.return_items.FindAsync(id);
            if (returnItem == null)
            {
                return NotFound(new { message = "Return item not found" });
            }

            _context.return_items.Remove(returnItem);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Return item deleted successfully" });
        }
    }
}
