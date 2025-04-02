using ITAM.DataContext;
using ITAM.DTOs;
using ITAM.Models.Logs;
using ITAM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static ITAM.DTOs.UpdateComputerDto;
using System.Security.Claims;

namespace ITAM.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ComputerComponentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public ComputerComponentsController(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Helper method to fetch the description based on uid
        private async Task<string> GetDescriptionByUidAsync(string uid)
        {
            var component = await _context.computer_components
                .Where(c => c.uid == uid)
                .FirstOrDefaultAsync();

            return component?.description ?? string.Empty;
        }


        [HttpGet("{uid}")]
        public async Task<IActionResult> GetComponentByUid(string uid)
        {
            var component = await _context.computer_components
                .Include(c => c.owner)
                .Include(c => c.computer)
                .Where(c => c.is_deleted == false)
                .FirstOrDefaultAsync(c => c.uid == uid);

            if (component == null)
            {
                return NotFound(new { message = "Component not found" });
            }

            var ramDescription = component.type == "RAM" ? component.description : await GetDescriptionByUidAsync(component.computer?.ram);
            var ssdDescription = component.type == "SSD" ? component.description : await GetDescriptionByUidAsync(component.computer?.ssd);
            var hddDescription = component.type == "HDD" ? component.description : await GetDescriptionByUidAsync(component.computer?.hdd);
            var gpuDescription = component.type == "GPU" ? component.description : await GetDescriptionByUidAsync(component.computer?.gpu);
            var boardDescription = component.type == "BOARD" ? component.description : await GetDescriptionByUidAsync(component.computer?.board);


            var componentDetails = new
            {
                component.id,
                component.type,
                component.date_acquired,
                component.description,
                component.asset_barcode,
                component.uid,
                component.status,
                component.history,
                component.owner_id,
                component.computer_id,
                component.cost,
                owner = component.owner != null ? new
                {
                    component.owner.id,
                    component.owner.name,
                    component.owner.company,
                    component.owner.department,
                    component.owner.employee_id
                } : null,
                computer = component.computer != null ? new
                {
                    component.computer.id,
                    component.computer.type,
                    component.computer.date_acquired,
                    component.computer.asset_barcode,
                    component.computer.brand,
                    component.computer.model,
                    ram = ramDescription,
                    ssd = ssdDescription,
                    hdd = hddDescription,
                    gpu = gpuDescription,
                    board = boardDescription,
                    component.computer.size,
                    component.computer.color,
                    component.computer.serial_no,
                    component.computer.po,
                    component.computer.warranty,
                    component.computer.cost,
                    component.computer.remarks,
                    component.computer.li_description,
                    component.computer.history,
                    component.computer.asset_image
                } : null
            };

            return Ok(componentDetails);
        }



        [HttpGet("Components")]
        public async Task<IActionResult> GetAllComponents(
         int pageNumber = 1,
         int pageSize = 10,
         string sortOrder = "asc",
         string? searchTerm = null)
        {
            try
            {
                var query = _context.computer_components
                    .Include(c => c.owner)
                    .Include(c => c.computer)
                    .Where(c => c.is_deleted == false)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(c => c.type.Contains(searchTerm) ||
                                             c.description.Contains(searchTerm) ||
                                             c.asset_barcode.Contains(searchTerm) ||
                                             c.status.Contains(searchTerm));
                }

                query = sortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(c => c.id)
                    : query.OrderBy(c => c.id);

                var totalItems = await query.CountAsync();
                var components = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (components == null || !components.Any())
                {
                    return Ok(new { Items = new List<object>(), TotalItems = 0, PageNumber = pageNumber, PageSize = pageSize });
                }

                var componentDetailsList = new List<object>();

                foreach (var component in components)
                {
                    var computerHistoryDetails = new List<object>();

                    if (component.history != null)
                    {
                        var computerIds = component.history != null
                            ? component.history.Select(id => int.Parse(id.ToString())).ToList()
                            : new List<int>();
                        var computers = await _context.computers
                            .Where(c => computerIds.Contains(c.id))
                            .ToListAsync();

                        computerHistoryDetails = computers.Select(c => new
                        {
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
                            c.li_description,
                            c.history,
                            c.asset_image
                        }).ToList<object>();
                    }

                    var componentDetails = new
                    {
                        component.id,
                        component.type,
                        component.description,
                        component.asset_barcode,
                        component.uid,
                        component.status,
                        component.cost,
                        history = computerHistoryDetails,  // Updated history field
                        component.owner_id,
                        component.computer_id,
                        component.date_acquired,
                        owner = component.owner != null ? new
                        {
                            component.owner.id,
                            component.owner.name,
                            component.owner.company,
                            component.owner.department,
                            component.owner.employee_id
                        } : null,
                        computer = component.computer != null ? new
                        {
                            component.computer.id,
                            component.computer.type,
                            component.computer.date_acquired,
                            component.computer.asset_barcode,
                            component.computer.brand,
                            component.computer.model,
                            component.computer.ram,
                            component.computer.ssd,
                            component.computer.hdd,
                            component.computer.gpu,
                            component.computer.board,
                            component.computer.size,
                            component.computer.color,
                            component.computer.serial_no,
                            component.computer.po,
                            component.computer.warranty,
                            component.computer.cost,
                            component.computer.remarks,
                            component.computer.li_description,
                            component.computer.history,
                            component.computer.asset_image
                        } : null
                    };

                    componentDetailsList.Add(componentDetails);
                }

                var response = new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                    Items = componentDetailsList
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error retrieving components: {ex.Message}" });
            }
        }



        [HttpGet("Components/{id}")]
        public async Task<IActionResult> GetComponentById(int id)
        {
            var component = await _context.computer_components.FindAsync(id);

            if (component == null)
            {
                return NotFound(new { message = "Component not found" });
            }

            return Ok(component);
        }



        [HttpPost]
        public async Task<IActionResult> CreateComponent([FromBody] CreateComputerComponentsDTO componentDTO)
        {
            try
            {
                if (string.IsNullOrEmpty(componentDTO.type) || string.IsNullOrEmpty(componentDTO.description))
                {
                    return BadRequest(new { message = "Type and description are required." });
                }

                // Validate and format date_acquired
                string formattedDateAcquired = null;
                if (!string.IsNullOrEmpty(componentDTO.date_acquired))
                {
                    if (DateTime.TryParse(componentDTO.date_acquired, out DateTime parsedDate))
                    {
                        formattedDateAcquired = parsedDate.ToString("MM/dd/yyyy"); // Format as MM/dd/yyyy
                    }
                    else
                    {
                        return BadRequest(new { message = "Invalid date format. Please use MM/dd/yyyy." });
                    }
                }

                // Generate new UID based on the last component's ID
                var lastComponent = await _context.computer_components.OrderByDescending(c => c.id).FirstOrDefaultAsync();
                string newUid = lastComponent != null ? $"UID-{(lastComponent.id + 1):D3}" : "UID-001";

                var component = new ComputerComponents
                {
                    type = componentDTO.type,
                    description = componentDTO.description,
                    asset_barcode = null,
                    status = "AVAILABLE",
                    history = null,
                    owner_id = null,
                    computer_id = null,
                    uid = newUid,
                    is_deleted = false,
                    date_acquired = formattedDateAcquired,
                    cost = componentDTO.cost ?? 0
                };

                _context.computer_components.Add(component);
                await _context.SaveChangesAsync();

                // Log to Centralized Logs
                await LogToCentralizedLogsAsync(
                    "Component Added",
                    $"Component with UID {component.uid} and type {component.type} added.",
                    component.type,
                    component.uid
                );

                // Ensure GetComponentById exists before using CreatedAtAction
                return CreatedAtAction(nameof(GetComponentById), new { id = component.id }, component);
            }
            catch (DbUpdateException dbEx)
            {
                return BadRequest(new { message = "Database error: " + dbEx.InnerException?.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error creating component: " + ex.Message });
            }
        }


        private async Task LogToCentralizedLogsAsync(string action, string details, string assetType, string assetBarcode)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

            var logEntry = new CentralizedLogs
            {
                type = assetType,
                asset_barcode = assetBarcode,
                action = action,
                performed_by_user_id = userId,
                timestamp = DateTime.UtcNow,
                details = details
            };

            _context.centralized_logs.Add(logEntry);
            await _context.SaveChangesAsync();
        }

        [Authorize]
        [HttpPost("pull_in_component")]
        public async Task<(bool success, string message)> PullInComponentAsync(PullInComponentRequest request)
        {
            if (request == null || request.computer_id <= 0 || string.IsNullOrWhiteSpace(request.component_uid))
                return (false, "Invalid request data");

            // Retrieve the user ID
            var userId = User?.FindFirst("sub")?.Value ?? "SYSTEM"; // Default to "SYSTEM" if user is null

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Find the computer
                    var computer = await _context.computers.FindAsync(request.computer_id);
                    if (computer == null)
                        return (false, "Computer not found");

                    // Find the existing component by UID
                    var component = await _context.computer_components.FirstOrDefaultAsync(c => c.uid == request.component_uid);
                    if (component == null)
                        return (false, "Component not found");

                    // Check if the component is already assigned to another computer
                    if (component.computer_id != null && component.computer_id != computer.id)
                        return (false, "Component is already assigned to another computer");

                    // Assign the component to the target computer
                    component.computer_id = computer.id;
                    component.status = "ACTIVE"; // Update status to ACTIVE
                    component.asset_barcode = computer.asset_barcode; // Update asset barcode
                    component.owner_id = computer.owner_id; // Update owner_id to match the computer

                    // Update history column
                    if (component.history == null)
                    {
                        component.history = new List<string>();
                    }
                    component.history.Add(computer.id.ToString());

                    await _context.SaveChangesAsync();

                    // Update the computer's component list
                    var existing_uids = new List<string>();
                    switch (component.type.ToUpper())
                    {
                        case "RAM":
                            existing_uids = (computer.ram ?? "").Split(", ").ToList();
                            existing_uids.Add(component.uid);
                            computer.ram = string.Join(", ", existing_uids.Where(u => !string.IsNullOrEmpty(u)));
                            break;
                        case "SSD":
                            existing_uids = (computer.ssd ?? "").Split(", ").ToList();
                            existing_uids.Add(component.uid);
                            computer.ssd = string.Join(", ", existing_uids.Where(u => !string.IsNullOrEmpty(u)));
                            break;
                        case "HDD":
                            existing_uids = (computer.hdd ?? "").Split(", ").ToList();
                            existing_uids.Add(component.uid);
                            computer.hdd = string.Join(", ", existing_uids.Where(u => !string.IsNullOrEmpty(u)));
                            break;
                        case "GPU":
                            existing_uids = (computer.gpu ?? "").Split(", ").ToList();
                            existing_uids.Add(component.uid);
                            computer.gpu = string.Join(", ", existing_uids.Where(u => !string.IsNullOrEmpty(u)));
                            break;
                        case "BOARD":
                            existing_uids = (computer.board ?? "").Split(", ").ToList();
                            existing_uids.Add(component.uid);
                            computer.board = string.Join(", ", existing_uids.Where(u => !string.IsNullOrEmpty(u)));
                            break;
                        default:
                            return (false, "Invalid component type");
                    }

                    await _context.SaveChangesAsync();

                    // Log the pull-in action
                    var repairLog = new Repair_logs
                    {
                        type = component.type,
                        eaf_no = null,
                        inventory_code = component.uid,
                        item_id = component.id.ToString(),
                        computer_id = computer.id.ToString(),
                        action = "Pull in",
                        remarks = string.IsNullOrEmpty(request.remarks) ? null : request.remarks,
                        timestamp = DateTime.UtcNow,
                        performed_by_user_id = userId // ✅ Store user ID or "SYSTEM"
                    };

                    _context.repair_logs.Add(repairLog);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Log the pull-in action to centralized logs
                    await LogToCentralizedLogsAsync(
                        "Pull in",
                        $"Component {component.uid} successfully assigned to computer {computer.id}",
                        component.type,
                        component.uid
                    );

                    return (true, $"Component {component.uid} successfully assigned to computer {computer.id}!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Internal Server Error: {ex.Message}");
                }
            }
        }






        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateComponent(int id, [FromBody] ComputerComponents component)
        //{
        //    if (id != component.id)
        //    {
        //        return BadRequest(new { message = "ID mismatch" });
        //    }

        //    try
        //    {
        //        _context.ChangeTracker.Clear();

        //        var existingComponent = await _context.computer_components
        //            .FirstOrDefaultAsync(c => c.id == id);

        //        if (existingComponent == null)
        //        {
        //            return NotFound(new { message = "Component not found" });
        //        }

        //        string performedByUserId = HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

        //        string originalData = $"Component ID: {existingComponent.id}, Type: {existingComponent.type}, " +
        //                           $"Asset Barcode: {existingComponent.asset_barcode}, Status: {existingComponent.status}, " +
        //                           $"Owner ID: {existingComponent.owner_id}, Computer ID: {existingComponent.computer_id}";

        //        // Only process computer reassignment if computer_id is greater than 0
        //        // If computer_id is 0 or null, we're not assigning it to a computer
        //        if (component.computer_id.HasValue && component.computer_id.Value > 0 && component.computer_id != existingComponent.computer_id)
        //        {
        //            var newComputer = await _context.computers
        //                .FirstOrDefaultAsync(c => c.id == component.computer_id.Value);

        //            if (newComputer == null)
        //            {
        //                return NotFound(new { message = "Destination computer not found" });
        //            }

        //            if (newComputer.owner_id.HasValue)
        //            {
        //                component.owner_id = newComputer.owner_id;
        //            }

        //            component.asset_barcode = newComputer.asset_barcode;

        //            // Helper functions
        //            string RemoveComponentUid(string existingValue, string uidToRemove)
        //            {
        //                if (string.IsNullOrEmpty(existingValue)) return existingValue;

        //                var components = existingValue.Split(new[] { ", " }, StringSplitOptions.None)
        //                    .Where(uid => uid != uidToRemove)
        //                    .ToList();

        //                return components.Any() ? string.Join(", ", components) : null;
        //            }

        //            string AddComponentUid(string existingValue, string newUid)
        //            {
        //                if (string.IsNullOrEmpty(existingValue))
        //                {
        //                    return newUid;
        //                }

        //                var components = existingValue.Split(new[] { ", " }, StringSplitOptions.None).ToList();
        //                if (!components.Contains(newUid))
        //                {
        //                    components.Add(newUid);
        //                }

        //                return string.Join(", ", components);
        //            }

        //            // Remove UID from old computer if applicable
        //            if (existingComponent.computer_id.HasValue && existingComponent.computer_id.Value > 0)
        //            {
        //                var oldComputer = await _context.computers
        //                    .FirstOrDefaultAsync(c => c.id == existingComponent.computer_id.Value);

        //                if (oldComputer != null && !string.IsNullOrEmpty(existingComponent.uid))
        //                {
        //                    // Remove UID from old computer
        //                    switch (existingComponent.type?.ToUpper())
        //                    {
        //                        case "RAM":
        //                            oldComputer.ram = RemoveComponentUid(oldComputer.ram, existingComponent.uid);
        //                            break;
        //                        case "SSD":
        //                            oldComputer.ssd = RemoveComponentUid(oldComputer.ssd, existingComponent.uid);
        //                            break;
        //                        case "HDD":
        //                            oldComputer.hdd = RemoveComponentUid(oldComputer.hdd, existingComponent.uid);
        //                            break;
        //                        case "GPU":
        //                            oldComputer.gpu = RemoveComponentUid(oldComputer.gpu, existingComponent.uid);
        //                            break;
        //                    }
        //                }
        //            }

        //            // Add UID to new computer
        //            if (!string.IsNullOrEmpty(existingComponent.uid))
        //            {
        //                // Use existing component type if available, otherwise use the updated type
        //                string componentType = string.IsNullOrEmpty(component.type)
        //                    ? existingComponent.type?.ToUpper()
        //                    : component.type.ToUpper();

        //                switch (componentType)
        //                {
        //                    case "RAM":
        //                        newComputer.ram = AddComponentUid(newComputer.ram, existingComponent.uid);
        //                        break;
        //                    case "SSD":
        //                        newComputer.ssd = AddComponentUid(newComputer.ssd, existingComponent.uid);
        //                        break;
        //                    case "HDD":
        //                        newComputer.hdd = AddComponentUid(newComputer.hdd, existingComponent.uid);
        //                        break;
        //                    case "GPU":
        //                        newComputer.gpu = AddComponentUid(newComputer.gpu, existingComponent.uid);
        //                        break;
        //                }
        //            }

        //            // Update history
        //            List<string> updatedHistory = existingComponent.history?.ToList() ?? new List<string>();

        //            if (existingComponent.computer_id.HasValue && existingComponent.computer_id.Value > 0)
        //            {
        //                string previousComputerId = existingComponent.computer_id.Value.ToString();
        //                if (!updatedHistory.Contains(previousComputerId))
        //                {
        //                    updatedHistory.Add(previousComputerId);
        //                }
        //            }

        //            string newComputerId = component.computer_id.Value.ToString();
        //            if (!updatedHistory.Contains(newComputerId))
        //            {
        //                updatedHistory.Add(newComputerId);
        //            }

        //            existingComponent.history = updatedHistory;
        //            component.status = "ACTIVE";
        //        }
        //        // Handle case where component is being detached from a computer
        //        else if (component.computer_id.HasValue && component.computer_id.Value == 0 && existingComponent.computer_id.HasValue && existingComponent.computer_id.Value > 0)
        //        {
        //            // Remove UID from old computer
        //            var oldComputer = await _context.computers
        //                .FirstOrDefaultAsync(c => c.id == existingComponent.computer_id.Value);

        //            if (oldComputer != null && !string.IsNullOrEmpty(existingComponent.uid))
        //            {
        //                string RemoveComponentUid(string existingValue, string uidToRemove)
        //                {
        //                    if (string.IsNullOrEmpty(existingValue)) return existingValue;

        //                    var components = existingValue.Split(new[] { ", " }, StringSplitOptions.None)
        //                        .Where(uid => uid != uidToRemove)
        //                        .ToList();

        //                    return components.Any() ? string.Join(", ", components) : null;
        //                }

        //                switch (existingComponent.type?.ToUpper())
        //                {
        //                    case "RAM":
        //                        oldComputer.ram = RemoveComponentUid(oldComputer.ram, existingComponent.uid);
        //                        break;
        //                    case "SSD":
        //                        oldComputer.ssd = RemoveComponentUid(oldComputer.ssd, existingComponent.uid);
        //                        break;
        //                    case "HDD":
        //                        oldComputer.hdd = RemoveComponentUid(oldComputer.hdd, existingComponent.uid);
        //                        break;
        //                    case "GPU":
        //                        oldComputer.gpu = RemoveComponentUid(oldComputer.gpu, existingComponent.uid);
        //                        break;
        //                }
        //            }

        //            // Update history
        //            List<string> updatedHistory = existingComponent.history?.ToList() ?? new List<string>();

        //            // Add the old computer ID to history if not already there
        //            string previousComputerId = existingComponent.computer_id.Value.ToString();
        //            if (!updatedHistory.Contains(previousComputerId))
        //            {
        //                updatedHistory.Add(previousComputerId);
        //            }

        //            existingComponent.history = updatedHistory;

        //            // Set status to INACTIVE when detaching from a computer
        //            component.status = component.status ?? "INACTIVE";
        //        }

        //        // Update the component properties - use null coalescing to keep existing values when fields are null
        //        existingComponent.computer_id = component.computer_id;
        //        existingComponent.owner_id = component.owner_id;
        //        existingComponent.asset_barcode = component.asset_barcode ?? existingComponent.asset_barcode;
        //        existingComponent.status = component.status ?? existingComponent.status;
        //        existingComponent.description = component.description ?? existingComponent.description;
        //        existingComponent.date_acquired = component.date_acquired ?? existingComponent.date_acquired;
        //        existingComponent.is_deleted = component.is_deleted ?? existingComponent.is_deleted;
        //        existingComponent.component_image = component.component_image ?? existingComponent.component_image;
        //        existingComponent.uid = component.uid ?? existingComponent.uid;

        //        // Only update type if provided and non-empty
        //        if (!string.IsNullOrEmpty(component.type))
        //        {
        //            existingComponent.type = component.type;
        //        }

        //        await _context.SaveChangesAsync();

        //        string updatedData = $"Component ID: {existingComponent.id}, Type: {existingComponent.type}, " +
        //                          $"Asset Barcode: {existingComponent.asset_barcode}, Status: {existingComponent.status}, " +
        //                          $"Owner ID: {existingComponent.owner_id}, Computer ID: {existingComponent.computer_id}";

        //        var logEntry = new CentralizedLogs
        //        {
        //            type = existingComponent.type,
        //            asset_barcode = existingComponent.asset_barcode,
        //            action = "Component Updated",
        //            performed_by_user_id = performedByUserId,
        //            details = $"Original Data: {originalData} | Updated Data: {updatedData}",
        //            timestamp = DateTime.UtcNow
        //        };

        //        _context.centralized_logs.Add(logEntry);
        //        await _context.SaveChangesAsync();

        //        return NoContent();
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "An error occurred while updating the component", details = ex.Message });
        //    }
        //}


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComponent(int id, [FromBody] ComputerComponentUpdateDto component)
        {
            try
            {
                _context.ChangeTracker.Clear();

                var existingComponent = await _context.computer_components
                    .FirstOrDefaultAsync(c => c.id == id);

                if (existingComponent == null)
                {
                    return NotFound(new { message = "component_not_found" });
                }

                string performed_by_user_id = HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

                // Preserve original data for logging
                string original_data = JsonConvert.SerializeObject(existingComponent, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

                // Update only the allowed fields if they are not null or non-empty
                existingComponent.asset_barcode = string.IsNullOrWhiteSpace(component.asset_barcode) ? existingComponent.asset_barcode : component.asset_barcode;
                existingComponent.status = string.IsNullOrWhiteSpace(component.status) ? existingComponent.status : component.status;
                existingComponent.description = string.IsNullOrWhiteSpace(component.description) ? existingComponent.description : component.description;
                existingComponent.date_acquired = string.IsNullOrWhiteSpace(component.date_acquired) ? existingComponent.date_acquired : component.date_acquired;
                existingComponent.uid = string.IsNullOrWhiteSpace(component.uid) ? existingComponent.uid : component.uid;
                existingComponent.type = string.IsNullOrWhiteSpace(component.type) ? existingComponent.type : component.type;
                existingComponent.cost = component.cost ?? existingComponent.cost;

                await _context.SaveChangesAsync();

                // Log the update
                string updated_data = JsonConvert.SerializeObject(existingComponent, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

                var log_entry = new CentralizedLogs
                {
                    type = existingComponent.type,
                    asset_barcode = existingComponent.asset_barcode,
                    action = "component_updated",
                    performed_by_user_id = performed_by_user_id,
                    details = $"original_data: {original_data} | updated_data: {updated_data}",
                    timestamp = DateTime.UtcNow
                };

                _context.centralized_logs.Add(log_entry);
                await _context.SaveChangesAsync();

                // Return response using DTO with snake_case
                return Ok(new ComputerComponentUpdateDto
                {
                    date_acquired = existingComponent.date_acquired,
                    type = existingComponent.type,
                    description = existingComponent.description,
                    asset_barcode = existingComponent.asset_barcode,
                    uid = existingComponent.uid,
                    status = existingComponent.status,
                    history = existingComponent.history
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "server_error", details = ex.Message });
            }
        }





        [HttpPut("pullout/{id}")]
        public async Task<IActionResult> PullOutComponent(int id, [FromBody] PullOutRequest request)
        {
            var component = await _context.computer_components.FindAsync(id);
            if (component == null)
            {
                return NotFound(new { message = "Component not found" });
            }

            string? pulledFromComputerId = component.computer_id?.ToString();
            string computerAssetBarcode = "N/A";

            if (component.computer_id != null)
            {
                var computer = await _context.computers.FindAsync(component.computer_id);
                if (computer != null)
                {
                    // Helper function to remove UID
                    void RemoveComponentUid(ref string componentField)
                    {
                        if (!string.IsNullOrEmpty(componentField))
                        {
                            var componentList = componentField.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            componentList.Remove(component.uid);
                            componentField = componentList.Any() ? string.Join(", ", componentList) : null;
                        }
                    }

                    // **Fix CS0206: Pass a local variable instead of a property**
                    string updatedRam = computer.ram;
                    RemoveComponentUid(ref updatedRam);
                    computer.ram = updatedRam;

                    string updatedSsd = computer.ssd;
                    RemoveComponentUid(ref updatedSsd);
                    computer.ssd = updatedSsd;

                    string updatedHdd = computer.hdd;
                    RemoveComponentUid(ref updatedHdd);
                    computer.hdd = updatedHdd;

                    string updatedGpu = computer.gpu;
                    RemoveComponentUid(ref updatedGpu);
                    computer.gpu = updatedGpu;

                    string updatedBoard = computer.board;
                    RemoveComponentUid(ref updatedBoard);
                    computer.board = updatedBoard;

                    computer.date_modified = DateTime.UtcNow;
                    computerAssetBarcode = computer.asset_barcode;

                    await _context.SaveChangesAsync();  // ✅ Ensure computer table is updated
                }
            }

            // **Retrieve the user ID**
            var userId = User?.FindFirst("sub")?.Value ?? "SYSTEM"; // Default to "SYSTEM" if user is null

            // Log the pull-out action
            var repairLog = new Repair_logs
            {
                type = component.type,
                eaf_no = null,
                inventory_code = component.uid,
                item_id = component.id.ToString(),
                computer_id = pulledFromComputerId,
                action = "Pull out",
                remarks = string.IsNullOrEmpty(request.remarks) ? null : request.remarks,
                timestamp = DateTime.UtcNow,
                performed_by_user_id = userId // ✅ Store user ID or "SYSTEM"
            };

            _context.repair_logs.Add(repairLog);
            await _context.SaveChangesAsync();

            await LogToCentralizedLogsAsync(
                "Pull out",
                $"Pull out from computer id: {pulledFromComputerId} with an asset_barcode of: {computerAssetBarcode}",
                component.type,
                component.uid // ❌ Removed the extra userId argument
            );


            // Update component status
            component.owner_id = null;
            component.computer_id = null;
            component.asset_barcode = null;
            component.status = "AVAILABLE";

            await _context.SaveChangesAsync(); // ✅ Ensure component table is updated

            return Ok(new { message = "Component pulled out successfully", component });
        }










        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComponent(int id)
        {
            var component = await _context.computer_components.FindAsync(id);
            if (component == null)
            {
                return NotFound(new { message = "Component not found" });
            }

            // Step 1: Store the computer_id before nullifying it
            int? computerId = component.computer_id;

            // Step 2: Soft delete the component
            component.is_deleted = true;
            component.computer_id = null;
            component.owner_id = null;
            component.status = "DELETED";

            // Step 3: Update the computer's corresponding column if needed
            if (computerId != null)
            {
                var computer = await _context.computers.FindAsync(computerId);
                if (computer != null)
                {
                    switch (component.type.ToUpper())
                    {
                        case "RAM":
                            computer.ram = null;
                            break;
                        case "SSD":
                            computer.ssd = null;
                            break;
                        case "HDD":
                            computer.hdd = null;
                            break;
                        case "GPU":
                            computer.gpu = null;
                            break;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
