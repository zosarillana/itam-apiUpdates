using ITAM.DataContext;
using ITAM.DTOs;
using ITAM.Models.Logs;
using ITAM.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static ITAM.DTOs.UpdateComputerDto;
using System.Security.Claims;

namespace ITAM.Services.ComputerService
{
    public class ComputerService
    {
        private readonly AppDbContext _context;

        public ComputerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResponse<object>> GetAllComputersAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            try
            {
                var query = _context.computers
                  .Where(c => !c.is_deleted) // Only include non-deleted computers
                  .AsQueryable();

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(c =>
                        EF.Functions.Like(c.serial_no, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.model, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.brand, $"%{searchTerm}%") ||
                        EF.Functions.Like(c.type, $"%{searchTerm}%"));
                }

                // Apply sorting
                query = sortOrder.ToLower() == "desc" ? query.OrderByDescending(c => c.id) : query.OrderBy(c => c.id);

                // Get total count for pagination
                var totalItems = await query.CountAsync();

                // Fetch paginated data
                var computers = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        c.id,
                        c.type,
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
                        c.asset_image,
                        c.owner_id,
                        c.is_deleted,
                        c.date_created,
                        c.date_modified,
                        c.date_acquired,
                        c.status,
                        c.assigned_assets
                    })
                    .ToListAsync();

                // Helper function to fetch component details
                async Task<List<object>> GetComponentDetailsAsync(string componentType, string uids)
                {
                    if (string.IsNullOrEmpty(uids))
                        return new List<object>();

                    var componentUids = uids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                             .Select(uid => uid.Trim())
                                             .ToList();

                    return await _context.computer_components
                        .Where(comp => comp.type == componentType && componentUids.Contains(comp.uid))
                        .Select(comp => new
                        {
                            IdProperty = comp.uid,
                            uid = comp.uid,
                            description = comp.description
                        })
                        .ToListAsync<object>();
                }

                var resultList = new List<object>();

                foreach (var computer in computers)
                {
                    // Fetch all component details
                    var ramDescriptions = await GetComponentDetailsAsync("RAM", computer.ram ?? "");
                    var ssdDescriptions = await GetComponentDetailsAsync("SSD", computer.ssd ?? "");
                    var hddDescriptions = await GetComponentDetailsAsync("HDD", computer.hdd ?? "");
                    var gpuDescriptions = await GetComponentDetailsAsync("GPU", computer.gpu ?? "");
                    var boardDescriptions = await GetComponentDetailsAsync("BOARD", computer.board ?? "");


                    // Fetch owner details
                    var ownerDetails = computer.owner_id != null
                        ? await _context.Users
                            .Where(u => u.id == computer.owner_id)
                            .Select(u => new
                            {
                                u.id,
                                u.name,
                                u.company,
                                u.department,
                                u.employee_id
                            })
                            .FirstOrDefaultAsync()
                        : null;

                    //FIXED: Handle `assigned_assets` correctly (string → List<int>)
                    List<object> assignedAssetsDetails = new List<object>();

                    if (computer.assigned_assets != null && computer.assigned_assets.Any())
                    {
                        var assignedAssetIds = computer.assigned_assets;



                        assignedAssetsDetails = await _context.Assets
                            .Where(a => assignedAssetIds.Contains(a.id))
                            .Select(a => new
                            {
                                a.id,
                                a.asset_barcode,
                                a.serial_no
                            })
                            .ToListAsync<object>();
                    }

                    var result = new
                    {
                        IdProperty = "1",
                        id = computer.id,
                        type = computer.type,
                        asset_barcode = computer.asset_barcode,
                        brand = computer.brand,
                        model = computer.model,
                        ram = ramDescriptions.Any() ? new { IdProperty = "2", Values = ramDescriptions } : null,
                        ssd = ssdDescriptions.Any() ? new { IdProperty = "5", Values = ssdDescriptions } : null,
                        hdd = hddDescriptions.Any() ? new { IdProperty = "6", Values = hddDescriptions } : null,
                        gpu = gpuDescriptions.Any() ? new { IdProperty = "7", Values = gpuDescriptions } : null,
                        board = boardDescriptions.Any() ? new { IdProperty = "11", Values = boardDescriptions } : null,
                        size = computer.size,
                        color = computer.color,
                        serial_no = computer.serial_no,
                        po = computer.po,
                        warranty = computer.warranty,
                        cost = computer.cost,
                        remarks = computer.remarks,
                        li_description = computer.li_description,
                        history = new { IdProperty = "8", Values = computer.history },
                        date_acquired = computer.date_acquired,
                        owner_id = computer.owner_id,
                        status = computer.status,
                        owner = ownerDetails != null ? new
                        {
                            IdProperty = "9",
                            id = ownerDetails.id,
                            name = ownerDetails.name,
                            company = ownerDetails.company,
                            department = ownerDetails.department,
                            employee_id = ownerDetails.employee_id
                        } : null,
                        assigned_assets = assignedAssetsDetails.Any() ? new { IdProperty = "10", Values = assignedAssetsDetails } : null
                    };

                    resultList.Add(result);
                }

                return new PaginatedResponse<object>
                {
                    Items = resultList,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving computers: {ex.Message}");
            }
        }








        public async Task<Computer> UpdateComputerAsync(int computerId, UpdateComputerDto computerDto, int? ownerId, ClaimsPrincipal user)
        {
            try
            {
                var computer = await _context.computers.FirstOrDefaultAsync(c => c.id == computerId);
                if (computer == null) return null;

                var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
                string originalData = JsonConvert.SerializeObject(computer, settings);

                // Only update owner if ownerId is provided and different from current owner
                if (ownerId.HasValue && computer.owner_id != ownerId)
                {
                    if (computer.history == null) computer.history = new List<string>();

                    if (computer.owner_id.HasValue)
                    {
                        var previousOwnerName = await _context.Users
                            .Where(u => u.id == computer.owner_id)
                            .Select(u => u.name)
                            .FirstOrDefaultAsync();
                        computer.history.Add(previousOwnerName ?? "Unknown");

                        var previousOwnerAccountability = await _context.user_accountability_lists
                            .FirstOrDefaultAsync(al => al.owner_id == computer.owner_id);
                        if (previousOwnerAccountability != null)
                        {
                            var computerIds = previousOwnerAccountability.computer_ids?
                                .Split(',')
                                .Where(id => id != computerId.ToString())
                                .ToList();
                            previousOwnerAccountability.computer_ids = computerIds != null && computerIds.Any()
                                ? string.Join(",", computerIds)
                                : null;

                            if (string.IsNullOrWhiteSpace(previousOwnerAccountability.asset_ids) &&
                                string.IsNullOrWhiteSpace(previousOwnerAccountability.computer_ids))
                            {
                                _context.user_accountability_lists.Remove(previousOwnerAccountability);
                            }
                            else
                            {
                                _context.user_accountability_lists.Update(previousOwnerAccountability);
                            }
                        }
                    }

                    var newOwnerAccountability = await _context.user_accountability_lists
                        .FirstOrDefaultAsync(al => al.owner_id == ownerId);

                    if (newOwnerAccountability == null)
                    {
                        var lastAccountability = await _context.user_accountability_lists
                            .OrderByDescending(al => al.id)
                            .FirstOrDefaultAsync();

                        int newAccountabilityNumber = (lastAccountability != null && lastAccountability.accountability_code.StartsWith("ACID-"))
                            ? int.Parse(lastAccountability.accountability_code.Substring(5)) + 1
                            : 1;
                        int newTrackingNumber = (lastAccountability != null && lastAccountability.tracking_code.StartsWith("TRID-"))
                            ? int.Parse(lastAccountability.tracking_code.Substring(5)) + 1
                            : 1;

                        var newAccountabilityList = new UserAccountabilityList
                        {
                            owner_id = ownerId.Value, // Use .Value because we know it has a value
                            accountability_code = $"ACID-{newAccountabilityNumber:D4}",
                            tracking_code = $"TRID-{newTrackingNumber:D4}",
                            computer_ids = computer.id.ToString(),
                        };
                        await _context.user_accountability_lists.AddAsync(newAccountabilityList);
                    }
                    else
                    {
                        newOwnerAccountability.computer_ids = string.IsNullOrWhiteSpace(newOwnerAccountability.computer_ids)
                            ? computer.id.ToString()
                            : $"{newOwnerAccountability.computer_ids},{computer.id}";
                        _context.user_accountability_lists.Update(newOwnerAccountability);
                    }

                    // Only update the owner_id if we're changing owners
                    computer.owner_id = ownerId;
                }

                // Update other properties only if they have valid values
                computer.type = string.IsNullOrEmpty(computerDto.type) ? computer.type : computerDto.type;
                computer.date_acquired = computerDto.date_acquired ?? computer.date_acquired;
                computer.asset_barcode = string.IsNullOrEmpty(computerDto.asset_barcode) ? computer.asset_barcode : computerDto.asset_barcode;
                computer.brand = string.IsNullOrEmpty(computerDto.brand) ? computer.brand : computerDto.brand;
                computer.model = string.IsNullOrEmpty(computerDto.model) ? computer.model : computerDto.model;
                computer.size = string.IsNullOrEmpty(computerDto.size) ? computer.size : computerDto.size;
                computer.color = string.IsNullOrEmpty(computerDto.color) ? computer.color : computerDto.color;
                computer.serial_no = string.IsNullOrEmpty(computerDto.serial_no) ? computer.serial_no : computerDto.serial_no;
                computer.po = string.IsNullOrEmpty(computerDto.po) ? computer.po : computerDto.po;
                computer.warranty = computerDto.warranty ?? computer.warranty;
                computer.cost = computerDto.cost != 0 ? computerDto.cost : computer.cost;
                computer.remarks = string.IsNullOrEmpty(computerDto.remarks) ? computer.remarks : computerDto.remarks;

                _context.computers.Update(computer);

                // Use the computer's owner_id (which may or may not have been updated)
                int ownerIdForComponents = computer.owner_id ?? 0;
                await UpdateComputerComponentsAsync(computer, ownerIdForComponents, computerDto);

                // Store computer ID in the history column of computer components
                var relatedComponents = await _context.computer_components
                    .Where(c => c.computer_id == computer.id)
                    .ToListAsync();

                string updatedData = JsonConvert.SerializeObject(computer, settings);
                var performedByUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM_USER";

                // Log the update action in centralized logs table
                var centralizedLogs = new CentralizedLogs
                {
                    type = computer.type,
                    asset_barcode = computer.asset_barcode,
                    action = "Computer Updated",
                    performed_by_user_id = performedByUserId,
                    timestamp = DateTime.UtcNow,
                    details = $"Computer ID {computer.id} was updated by User ID {performedByUserId}. " +
                              $"Original Data: {originalData}, Updated Data: {updatedData}"
                };

                _context.centralized_logs.Add(centralizedLogs);

                // Save changes
                await _context.SaveChangesAsync();
                return computer;
            }
            catch (DbUpdateException dbEx)
            {
                throw new Exception($"Database error: {dbEx.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error: {ex.Message}");
            }
        }


        private async Task UpdateComputerComponentsAsync(Computer computer, int ownerId, UpdateComputerDto computerDto)
        {
            var components = await _context.computer_components.Where(c => c.computer_id == computer.id).ToListAsync();
            var lastUid = await _context.computer_components.OrderByDescending(c => c.id).FirstOrDefaultAsync();
            int lastUidIndex = lastUid != null ? int.Parse(lastUid.uid.Split('-')[1]) : 0;
            string GenerateUID(int uidIndex) => $"UID-{uidIndex:D3}";

            var updatedTypes = new Dictionary<string, string>
            {
                { "RAM", computerDto.ram },
                { "SSD", computerDto.ssd },
                { "HDD", computerDto.hdd },
                { "GPU", computerDto.gpu },
                { "BOARD", computerDto.board }
            };

            foreach (var (type, newValue) in updatedTypes)
            {
                if (string.IsNullOrWhiteSpace(newValue)) continue;

                var existingComponent = components.FirstOrDefault(c => c.type == type);

                if (existingComponent != null && existingComponent.description == newValue) continue;

                // If an existing component is replaced, mark it inactive and remove owner/computer reference
                if (existingComponent != null)
                {
                    existingComponent.status = "INACTIVE";
                    existingComponent.owner_id = null;
                    existingComponent.computer_id = null;

                    _context.computer_components.Update(existingComponent);

                    // Log the component replacement
                    var componentLog = new Computer_components_logs
                    {
                        computer_components_id = existingComponent.id,
                        details = $"Component {type} was replaced. Old UID: {existingComponent.uid}, New UID: {GenerateUID(++lastUidIndex)}",
                        action = "Component Replaced",
                        performed_by_user_id = "0",
                        timestamp = DateTime.UtcNow
                    };
                    _context.computer_components_logs.Add(componentLog);
                }

                // ✅ FIX: Assign history as a List<string> instead of a string
                List<string> newHistory = new List<string> { computer.id.ToString() };

                var newComponent = new ComputerComponents
                {
                    type = type,
                    description = newValue,
                    asset_barcode = computer.asset_barcode,
                    status = "ACTIVE",
                    owner_id = ownerId,
                    computer_id = computer.id,
                    uid = GenerateUID(++lastUidIndex),
                    history = newHistory
                };
                _context.computer_components.Add(newComponent);

                // Update the computer record with the new UID for the component
                if (type == "RAM") computer.ram = newComponent.uid;
                if (type == "SSD") computer.ssd = newComponent.uid;
                if (type == "HDD") computer.hdd = newComponent.uid;
                if (type == "GPU") computer.gpu = newComponent.uid;
                if (type == "BOARD") computer.board = newComponent.uid;

            }

            await _context.SaveChangesAsync();
        }









        public async Task<object> GetComputerByIdAsync(int id)
        {
            try
            {
                var computer = await _context.computers
                    .Where(c => c.id == id)
                    .Select(c => new
                    {
                        c.id,
                        c.type,
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
                        c.asset_image,
                        c.owner_id,
                        c.is_deleted,
                        c.date_created,
                        c.date_modified,
                        c.date_acquired,
                        c.assigned_assets,
                        c.status
                    })
                    .FirstOrDefaultAsync();

                if (computer == null)
                    return null;

                // Helper method to fetch component details for multiple UIDs
                async Task<List<object>> GetComponentDetailsAsync(string componentType, string uids)
                {
                    if (string.IsNullOrEmpty(uids))
                        return new List<object>();

                    var componentUids = uids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                             .Select(uid => uid.Trim())
                                             .ToList();

                    return await _context.computer_components
                        .Where(comp => comp.type == componentType && componentUids.Contains(comp.uid))
                        .Select(comp => new
                        {
                            IdProperty = comp.uid,
                            id = comp.id,
                            uid = comp.uid,
                            description = comp.description,
                            date_acquired = comp.date_acquired,
                            cost = comp.cost,
                            asset_barcode = comp.asset_barcode,
                            status = comp.status

                        })
                        .ToListAsync<object>();
                }

                // Fetch all component details
                var ramDescriptions = await GetComponentDetailsAsync("RAM", computer.ram ?? "");
                var ssdDescriptions = await GetComponentDetailsAsync("SSD", computer.ssd ?? "");
                var hddDescriptions = await GetComponentDetailsAsync("HDD", computer.hdd ?? "");
                var gpuDescriptions = await GetComponentDetailsAsync("GPU", computer.gpu ?? "");
                var boardDescriptions = await GetComponentDetailsAsync("BOARD", computer.board ?? "");


                // Fetch owner details
                var ownerDetails = computer.owner_id != null
                    ? await _context.Users
                        .Where(u => u.id == computer.owner_id)
                        .Select(u => new
                        {
                            u.id,
                            u.name,
                            u.company,
                            u.department,
                            u.employee_id
                        })
                        .FirstOrDefaultAsync()
                    : null;

                // Fetch assigned asset details
                List<object> assignedAssetsDetails = new List<object>();

                if (computer.assigned_assets != null && computer.assigned_assets.Any())
                {
                    var assignedAssetIds = computer.assigned_assets;

                    assignedAssetsDetails = await _context.Assets
                       .Where(a => assignedAssetIds.Contains(a.id))
                       .Select(a => new
                       {
                           a.id,
                           a.type,
                           a.brand,
                           a.asset_barcode,
                           a.serial_no,
                           a.date_acquired,
                           a.model,
                           a.history,
                           a.status
                       })
                       .ToListAsync<object>();
                }

                // Extract owner IDs from history
                List<int> historyOwnerIds = computer.history?
                    .Where(id => int.TryParse(id, out _)) // Ensure all values are valid integers
                    .Select(int.Parse)
                    .Distinct()
                    .ToList() ?? new List<int>();

                // Fetch owner details for history
                var historyOwnerDetails = await _context.Users
                    .Where(u => historyOwnerIds.Contains(u.id))
                    .Select(u => new
                    {
                        u.id,
                        u.name,
                        u.company,
                        u.department,
                        u.employee_id
                    })
                    .ToListAsync();

                // Format the history data with owner details
                var formattedHistory = historyOwnerDetails.Any() ? new
                {
                    IdProperty = "8",
                    Values = historyOwnerDetails
                } : null;

                var result = new
                {
                    IdProperty = "1",
                    id = computer.id,
                    type = computer.type,
                    asset_barcode = computer.asset_barcode,
                    brand = computer.brand,
                    model = computer.model,
                    ram = ramDescriptions.Any() ? new { IdProperty = "2", Values = ramDescriptions } : null,
                    ssd = ssdDescriptions.Any() ? new { IdProperty = "5", Values = ssdDescriptions } : null,
                    hdd = hddDescriptions.Any() ? new { IdProperty = "6", Values = hddDescriptions } : null,
                    gpu = gpuDescriptions.Any() ? new { IdProperty = "7", Values = gpuDescriptions } : null,
                    board = boardDescriptions.Any() ? new { IdProperty = "11", Values = boardDescriptions } : null,
                    size = computer.size,
                    color = computer.color,
                    serial_no = computer.serial_no,
                    po = computer.po,
                    warranty = computer.warranty,
                    cost = computer.cost,
                    remarks = computer.remarks,
                    li_description = computer.li_description,
                    date_acquired = computer.date_acquired,
                    history = formattedHistory,
                    owner_id = computer.owner_id,
                    status = computer.status,
                    owner = ownerDetails != null ? new
                    {
                        IdProperty = "9",
                        id = ownerDetails.id,
                        name = ownerDetails.name,
                        company = ownerDetails.company,
                        department = ownerDetails.department,
                        employee_id = ownerDetails.employee_id
                    } : null,
                    assigned_assets = assignedAssetsDetails.Any() ? new { IdProperty = "10", Values = assignedAssetsDetails } : null
                };

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving computer with ID {id}: {ex.Message}");
            }
        }






        //for get by owner id endpoint 
        public async Task<List<ComputerWithOwnerDTO>> GetComputersByOwnerIdAsync(int ownerId)
        {
            return await _context.computers
                .Where(c => c.owner_id == ownerId)
                .Select(c => new ComputerWithOwnerDTO
                {
                    id = c.id,
                    type = c.type,
                    date_acquired = c.date_acquired,
                    asset_barcode = c.asset_barcode,
                    brand = c.brand,
                    model = c.model,
                    ram = c.ram,
                    ssd = c.ssd,
                    hdd = c.hdd,
                    gpu = c.gpu,
                    size = c.size,
                    color = c.color,
                    serial_no = c.serial_no,
                    po = c.po,
                    warranty = c.warranty,
                    cost = c.cost,
                    remarks = c.remarks,
                    li_description = c.li_description,
                    history = c.history,
                    asset_image = c.asset_image,
                    owner_id = c.owner_id,
                    is_deleted = c.is_deleted,
                    date_created = c.date_created,
                    date_modified = c.date_modified,
                    status = c.status,
                    owner = c.owner_id != null ? new OwnerDTO
                    {
                        id = c.id,
                        name = _context.Users.Where(u => u.id == c.owner_id).Select(u => u.name).FirstOrDefault(),
                        company = _context.Users.Where(u => u.id == c.owner_id).Select(u => u.company).FirstOrDefault(),
                        department = _context.Users.Where(u => u.id == c.owner_id).Select(u => u.department).FirstOrDefault(),
                        employee_id = _context.Users.Where(u => u.id == c.owner_id).Select(u => u.employee_id).FirstOrDefault()
                    } : null
                })
                .ToListAsync();
        }



        public async Task<ServiceResponse> DeleteComputerAsync(int id, ClaimsPrincipal user)
        {
            var computer = await _context.computers.FirstOrDefaultAsync(c => c.id == id);

            if (computer == null)
            {
                return new ServiceResponse { Success = false, StatusCode = 404, Message = "Computer not found." };
            }

            if (computer.is_deleted)
            {
                return new ServiceResponse { Success = false, StatusCode = 409, Message = "Computer is already deleted." };
            }

            // Update accountability list
            var assignedUser = await _context.user_accountability_lists
                .FirstOrDefaultAsync(ua => ua.computer_ids != null && ua.computer_ids.Contains(id.ToString()));

            if (assignedUser != null)
            {
                var updatedComputerIds = assignedUser.computer_ids
                    .Split(',')
                    .Where(cid => cid.Trim() != id.ToString())
                    .ToArray();

                assignedUser.computer_ids = updatedComputerIds.Length > 0
                    ? string.Join(",", updatedComputerIds)
                    : null;

                _context.user_accountability_lists.Update(assignedUser);
            }

            // Find and update related components (RAM, SSD, HDD, GPU)
            var componentUids = new List<string>
            {
                computer.ram, computer.ssd, computer.hdd, computer.gpu
            }.Where(uid => !string.IsNullOrEmpty(uid)).ToList();

            if (componentUids.Any())
            {
                var components = await _context.computer_components
                    .Where(c => componentUids.Contains(c.uid))
                    .ToListAsync();

                foreach (var component in components)
                {
                    component.status = "INACTIVE";
                    component.owner_id = null;
                    component.computer_id = null;
                }

                _context.computer_components.UpdateRange(components);
            }

            // Mark the computer as deleted
            computer.status = "INACTIVE";
            computer.is_deleted = true;
            computer.date_modified = DateTime.UtcNow;

            _context.computers.Update(computer);

            string performedByUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            // Log the delete action
            var centralizedLog = new CentralizedLogs
            {
                type = computer.type,
                asset_barcode = computer.asset_barcode,
                action = "DELETE",
                performed_by_user_id = performedByUserId,
                timestamp = DateTime.UtcNow,
                details = $"Computer ID {computer.id} and related components (RAM, SSD, HDD, GPU) were set to INACTIVE, owner_id and computer_id removed, and marked as deleted by User ID {performedByUserId}."
            };

            _context.centralized_logs.Add(centralizedLog);

            await _context.SaveChangesAsync();

            return new ServiceResponse { Success = true, StatusCode = 200, Message = "Computer and its components deleted successfully." };
        }


        // Pull Out Computer Endpoint
        public async Task<bool> PullOutComputerAsync(int computerId)
        {
            // Find the computer by ID
            var computer = await _context.computers.FindAsync(computerId);
            if (computer == null)
            {
                return false; // Computer not found
            }

            // Store previous owner_id in history before setting it to null
            if (computer.owner_id.HasValue)
            {
                if (computer.history == null)
                    computer.history = new List<string>();

                computer.history.Add(computer.owner_id.Value.ToString());
            }

            // Remove owner_id and update status
            computer.owner_id = null;
            computer.status = "INACTIVE";

            // Process assigned assets
            if (computer.assigned_assets != null && computer.assigned_assets.Any())
            {
                foreach (var assetId in computer.assigned_assets)
                {
                    var asset = await _context.Assets.FindAsync(assetId);
                    if (asset != null)
                    {
                        if (asset.owner_id.HasValue)
                        {
                            if (asset.history == null)
                                asset.history = new List<string>();

                            asset.history.Add(asset.owner_id.Value.ToString());
                        }
                        asset.owner_id = null;
                        asset.status = "INACTIVE";
                    }
                }
            }

            // Update components (ram, ssd, hdd, gpu, board)
            var components = await _context.computer_components
                .Where(c => c.computer_id == computerId)
                .ToListAsync();

            foreach (var component in components)
            {
                component.owner_id = null;
                component.asset_barcode = null;
                component.status = "INACTIVE";
            }

            // Save changes to database
            await _context.SaveChangesAsync();
            return true;
        }






        // Method to assign an owner to a vacant computer
        public async Task<Computer> AssignOwnerToComputerAsync(AssignOwnerforComputerDto assignOwnerforComputerDto, ClaimsPrincipal userClaims)
        {
            // Fetching a vacant computer (where owner_id is null)
            var computer = await _context.computers
                .FirstOrDefaultAsync(c => c.id == assignOwnerforComputerDto.computer_id && c.owner_id == null);

            if (computer == null)
            {
                throw new KeyNotFoundException("Vacant computer not found or already has an owner.");
            }

            // Fetching the user (owner) to assign to the computer
            var user = await _context.Users.FindAsync(assignOwnerforComputerDto.owner_id);
            if (user == null)
            {
                throw new KeyNotFoundException("Owner not found.");
            }

            // Assigning the owner to the computer
            computer.owner_id = assignOwnerforComputerDto.owner_id;

            // Update the computer entity in the database
            _context.computers.Update(computer);
            await _context.SaveChangesAsync();

            string performedByUserId = userClaims?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

            // Log the action in centralized_logs
            var centralizedLog = new CentralizedLogs
            {
                type = computer.type,
                asset_barcode = computer.asset_barcode,
                action = "Assigned to a user",
                performed_by_user_id = performedByUserId,
                timestamp = DateTime.UtcNow,
                details = $"Asset ID: {computer.id} is assigned to User ID: {user.id}."
            };

            _context.centralized_logs.Add(centralizedLog);
            await _context.SaveChangesAsync();
            await UpdateUserAccountabilityListAsync(user, computer);

            return computer;
        }



        // Helper method to update the user's accountability list
        private async Task UpdateUserAccountabilityListAsync(User user, Computer computer)
        {
            var userAccountabilityList = await _context.user_accountability_lists
                .FirstOrDefaultAsync(ual => ual.owner_id == user.id);

            if (userAccountabilityList == null)
            {
                var accountabilityCode = GenerateAccountabilityCode();
                var trackingCode = GenerateTrackingCode();

                userAccountabilityList = new UserAccountabilityList
                {
                    accountability_code = accountabilityCode,
                    tracking_code = trackingCode,
                    owner_id = user.id,
                    asset_ids = string.Empty,
                    computer_ids = string.Empty
                };

                _context.user_accountability_lists.Add(userAccountabilityList);
                await _context.SaveChangesAsync();
            }

            var existingComputerIds = string.IsNullOrEmpty(userAccountabilityList.computer_ids)
                ? new List<int>()
                : userAccountabilityList.computer_ids.Split(',').Where(id => !string.IsNullOrWhiteSpace(id)).Select(int.Parse).ToList();

            if (!existingComputerIds.Contains(computer.id))
            {
                existingComputerIds.Add(computer.id);
                userAccountabilityList.computer_ids = string.Join(",", existingComputerIds);
            }

            _context.user_accountability_lists.Update(userAccountabilityList);
            await _context.SaveChangesAsync();
        }


        private string GenerateAccountabilityCode()
        {
            var lastAccountabilityCode = _context.user_accountability_lists
                .OrderByDescending(ual => ual.accountability_code)
                .Select(ual => ual.accountability_code)
                .FirstOrDefault();

            int nextCode = 1;

            if (!string.IsNullOrEmpty(lastAccountabilityCode))
            {
                var lastNumber = lastAccountabilityCode.Substring(lastAccountabilityCode.LastIndexOf('-') + 1);
                if (int.TryParse(lastNumber, out int lastNumberParsed))
                {
                    nextCode = lastNumberParsed + 1;
                }
            }

            return $"ACID-{nextCode:D4}";
        }

        private string GenerateTrackingCode()
        {
            var lastTrackingCode = _context.user_accountability_lists
                .OrderByDescending(ual => ual.tracking_code)
                .Select(ual => ual.tracking_code)
                .FirstOrDefault();

            int nextCode = 1;

            if (!string.IsNullOrEmpty(lastTrackingCode))
            {
                var lastNumber = lastTrackingCode.Substring(lastTrackingCode.LastIndexOf('-') + 1);
                if (int.TryParse(lastNumber, out int lastNumberParsed))
                {
                    nextCode = lastNumberParsed + 1;
                }
            }

            return $"TRID-{nextCode:D4}";
        }


    }
}
