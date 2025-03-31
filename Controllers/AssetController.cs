using ITAM.DataContext;
using ITAM.DTOs;
using ITAM.Models.Logs;
using ITAM.Services;
using ITAM.Services.AssetImportService;
using ITAM.Services.AssetService;
using ITAM.Services.ComputerService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ITAM.Controllers
{
    [Route("api/api/[controller]")]
    [ApiController]
    public class AssetController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AssetImportService _assetImportService;
        private readonly AssetService _assetService;
        private readonly UserService _userService;
        private readonly ComputerService _computerService;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public AssetController(AppDbContext context, AssetImportService assetImportService, AssetService assetService, UserService userService, ComputerService computerService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _assetImportService = assetImportService;
            _assetService = assetService;
            _userService = userService;
            _computerService = computerService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportAssets(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            try
            {
                var result = await _assetImportService.ImportAssetsAsync(file, User);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error importing assets: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateComputer([FromBody] CreateComputerRequest request)
        {
            var result = await _assetService.CreateComputerAsync(request);

            if (result.success)
                return Ok(new { message = result.message });

            return BadRequest(new { message = result.message });
        }

        //assign owner for vacant asset items
        [HttpPost("assign-owner")]
        public async Task<IActionResult> AssignOwnerToAsset([FromBody] AssignOwnerDto assignOwnerDto)
        {
            try
            {
                var userClaims = User; // Get logged-in user's claims
                var asset = await _assetService.AssignOwnerToAssetAsync(assignOwnerDto, userClaims);
                return Ok(new { message = "Owner assigned successfully.", asset });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error: {ex.Message}" });
            }
        }

        //post endpoint for creating vacant item for asset and computer items store based on type
        [HttpPost("create-vacant-asset/computer-items")]
        public async Task<IActionResult> CreateVacantAsset([FromBody] CreateAssetDto assetDto)
        {
            if (assetDto == null)
            {
                return BadRequest("Invalid asset data.");
            }

            try
            {
                var userClaims = User; // Get logged-in user's claims
                var asset = await _assetService.CreateVacantAssetAsync(assetDto, userClaims);

                return Ok(new { message = "Asset created successfully.", asset });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating asset: {ex.Message}");
            }
        }

        [HttpPost("pull_in_assets")]
        public async Task<(bool success, string message)> PullInAssetsAsync(PullInAssetRequest request)
        {
            if (request == null || request.computer_id <= 0 || request.asset_ids == null || request.asset_ids.Count == 0)
                return (false, "Invalid request data");
            var userId = User?.FindFirst("sub")?.Value ?? "SYSTEM";
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var computer = await _context.computers.FindAsync(request.computer_id);
                    if (computer == null)
                        return (false, "Computer not found");
                    if (computer.assigned_assets == null)
                        computer.assigned_assets = new List<int>();

                    // Check if any assets are already assigned to another computer
                    foreach (var assetId in request.asset_ids)
                    {
                        var asset = await _context.Assets.FindAsync(assetId);
                        if (asset == null)
                            continue;

                        // If the asset is already assigned to another computer, unassign it first
                        if (asset.computer_id != null && asset.computer_id != computer.id)
                        {
                            // Find the computer that currently has this asset
                            var previousComputer = await _context.computers.FindAsync(asset.computer_id);
                            if (previousComputer != null && previousComputer.assigned_assets != null)
                            {
                                // Remove the asset from the previous computer's assigned_assets list
                                previousComputer.assigned_assets.Remove(asset.id);
                                _context.Entry(previousComputer).State = EntityState.Modified;
                            }
                        }

                        if (!computer.assigned_assets.Contains(asset.id))
                        {
                            computer.assigned_assets.Add(asset.id);
                        }

                        if (asset.history == null)
                            asset.history = new List<string>();
                        if (asset.owner_id != null && !asset.history.Contains(asset.owner_id.ToString()))
                        {
                            asset.history.Add(asset.owner_id.ToString());
                        }

                        if (asset.root_history == null)
                            asset.root_history = new List<int>();
                        asset.root_history.Add(computer.id);
                        asset.root_history = asset.root_history.Distinct().ToList();

                        asset.computer_id = computer.id;
                        asset.owner_id = computer.owner_id;
                        asset.status = "ACTIVE";

                        var repairLog = new Repair_logs
                        {
                            type = asset.type,
                            eaf_no = null,
                            inventory_code = asset.asset_barcode,
                            item_id = asset.id.ToString(),
                            computer_id = computer.id.ToString(),
                            action = "Pull in",
                            remarks = string.IsNullOrEmpty(request.remarks) ? null : request.remarks,
                            timestamp = DateTime.UtcNow,
                            performed_by_user_id = userId
                        };
                        _context.repair_logs.Add(repairLog);
                        _context.Entry(asset).State = EntityState.Modified;
                    }

                    // Ensure assigned_assets persists
                    _context.Entry(computer).State = EntityState.Modified;
                    Console.WriteLine("Final Assigned Assets: " + string.Join(",", computer.assigned_assets));
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return (true, "Assets successfully assigned to Computer!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Internal Server Error: {ex.Message}");
                }
            }
        }










        // Get endpoint to fetch only assets based on owner ID
        [HttpGet("assets/owner/{owner_id}")]
        public async Task<IActionResult> GetAssetsByOwnerId(int owner_id)
        {
            try
            {
                var assets = await _assetService.GetAssetsByOwnerIdAsync(owner_id);

                if (assets == null || !assets.Any())
                {
                    return NotFound(new { message = "No assets found for this owner." });
                }

                return Ok(assets);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving assets: {ex.Message}");
            }
        }



        [HttpGet("type-filter-asset-computers/{type}")]
        public async Task<IActionResult> GetAssetsByType(
        string type,
        int pageNumber = 1,
        int pageSize = 10,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            try
            {
                var response = await _assetService.GetAssetsByTypeAsync(
                    type, pageNumber, pageSize, sortOrder, searchTerm);

                if (response == null || !response.Items.Any())
                {
                    return NotFound(new { message = $"No assets found for type: {type}." });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving assets: {ex.Message}");
            }
        }


        [HttpGet("AssetItems")]
        public async Task<IActionResult> GetAllAssets(
            int pageNumber = 1,
            int pageSize = 10,
            string sortOrder = "asc",
            string? searchTerm = null)
        {
            try
            {
                var response = await _assetService.GetAllAssetsAsync(pageNumber, pageSize, sortOrder, searchTerm);

                if (response == null || !response.Items.Any())
                {
                    return Ok(new { Items = new List<object>(), TotalItems = 0, PageNumber = pageNumber, PageSize = pageSize });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving assets: {ex.Message}");
            }
        }



        [HttpGet("assets/vacant")]
        public async Task<IActionResult> GetVacantAssets()
        {
            var vacantAssets = await _context.Assets
                .Where(a => a.owner_id == null)
                .ToListAsync();

            if (vacantAssets == null || vacantAssets.Count == 0)
            {
                return NotFound(new { message = "No vacant assets available." });
            }

            return Ok(vacantAssets);
        }



        [HttpGet("AssetItems/{id}")]
        public async Task<IActionResult> GetAssetById(
        int id,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            try
            {
                var response = await _assetService.GetAssetByIdAsync(id, sortOrder, searchTerm);

                if (response == null)
                {
                    return NotFound(new { message = "Asset not found." });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving asset: {ex.Message}");
            }
        }




        [HttpPut("update-asset/{asset_id}")]
        public async Task<IActionResult> UpdateAsset(int asset_id, [FromBody] UpdateAssetDto assetDto)
        {
            if (assetDto == null)
            {
                return BadRequest("Invalid data.");
            }

            try
            {
                var existingAsset = await _context.Assets.FirstOrDefaultAsync(a => a.id == asset_id);
                if (existingAsset == null)
                {
                    return NotFound(new { message = "Asset not found." });
                }

                var newOwnerId = await _userService.GetOrCreateUserAsync(assetDto);

                int? ownerId = newOwnerId ?? existingAsset.owner_id;

                var updatedAsset = await _assetService.UpdateAssetAsync(asset_id, assetDto, ownerId, HttpContext.User);

                if (updatedAsset == null)
                {
                    return NotFound(new { message = "Asset not found." });
                }

                return Ok(new { message = "Asset updated successfully.", asset = updatedAsset });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating asset: {ex.Message}");
            }
        }


        [HttpPut("pullout/{asset_id}")]
        public async Task<IActionResult> PullOutAsset(int asset_id, [FromBody] PullOutRequest request)
        {
            var asset = await _context.Assets.FindAsync(asset_id);
            if (asset == null)
            {
                return NotFound(new { message = "Asset not found." });
            }

            var computers = await _context.computers
                .Where(c => c.assigned_assets != null && c.assigned_assets.Contains(asset_id))
                .ToListAsync();

            if (!computers.Any())
            {
                return NotFound(new { message = "No assigned computers found for this asset ID." });
            }

            string? pulledFromComputerId = null;
            string performedByUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM"; // ✅ Declared outside loop

            foreach (var computer in computers)
            {
                // ✅ Remove the asset ID from the computer's assigned assets list
                computer.assigned_assets = computer.assigned_assets
                    .Where(id => id != asset_id)
                    .ToList();

                _context.computers.Update(computer);
                pulledFromComputerId = computer.id.ToString();

                // ✅ Log the pull-out action
                string logDetails = $"Pulled out from computer ID: {computer.id}";
                await LogToCentralizedLogsAsync("Pull out", logDetails, asset.type, asset.asset_barcode, performedByUserId);
            }

            if (asset.owner_id.HasValue)
            {
                asset.history = (asset.history ?? new List<string>())
                    .Append(asset.owner_id.Value.ToString())
                    .ToList();
            }

            // ✅ Create a repair log entry
            var repairLog = new Repair_logs
            {
                type = asset.type,
                eaf_no = null,
                inventory_code = asset.asset_barcode,
                item_id = asset_id.ToString(),
                computer_id = pulledFromComputerId,
                action = "Pull out",
                remarks = string.IsNullOrEmpty(request.remarks) ? null : request.remarks,
                performed_by_user_id = performedByUserId,
                timestamp = DateTime.UtcNow
            };

            _context.repair_logs.Add(repairLog);

            // ✅ Clear the asset's `computer_id` (Important Fix)
            asset.computer_id = null;

            // ✅ Clear owner and update status
            asset.owner_id = null;
            asset.status = "AVAILABLE";

            _context.Assets.Update(asset);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Asset ID {asset_id} successfully pulled out, owner cleared, computer_id set to NULL, and status set to INACTIVE." });
        }


        // ✅ Updated method to accept `performedByUserId`
        private async Task LogToCentralizedLogsAsync(string action, string details, string assetType, string assetBarcode, string performedByUserId)
        {
            var logEntry = new CentralizedLogs
            {
                type = assetType,
                asset_barcode = assetBarcode,
                action = action,
                performed_by_user_id = performedByUserId, // ✅ Use provided user ID
                timestamp = DateTime.UtcNow,
                details = details
            };

            _context.centralized_logs.Add(logEntry);
            await _context.SaveChangesAsync();
        }






        [HttpDelete("delete-asset/{id}")]
        public async Task<IActionResult> DeleteAsset(int id)
        {
            var user = HttpContext.User; // Get the logged-in user details

            var result = await _assetService.DeleteAssetAsync(id, user);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, new { message = result.Message });
            }

            return Ok(new { message = "Asset deleted successfully.", assetId = id });
        }
    }

    
}
