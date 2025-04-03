using ITAM.DataContext;
using ITAM.DTOs;
using ITAM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountabilityListController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserAccountabilityListController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllUserAccountabilityLists(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.user_accountability_lists
                    .Include(u => u.owner) // Assuming navigation property "Owner"
                    .Include(u => u.assets) // Assuming navigation property "Assets"
                    .Include(u => u.computer) // Assuming navigation property "Computers"
                    .AsQueryable();

                var totalItems = await query.CountAsync();

                var userAccountabilityLists = await query
                    .OrderBy(u => u.id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        user_accountability_list = new
                        {
                            u.id,
                            u.accountability_code,
                            u.tracking_code,
                            u.date_created,
                            u.is_active
                        },
                        owner = u.owner != null ? new OwnerDetailsDto
                        {
                            id = u.owner.id,
                            name = u.owner.name,
                            company = u.owner.company,
                            department = u.owner.department,
                            employee_id = u.owner.employee_id,
                            designation = u.owner.designation
                        } : null,
                        assets = u.assets.Select(a => new AssetAccountabilityDto
                        {
                            id = a.id,
                            type = a.type,
                            asset_barcode = a.asset_barcode,
                            brand = a.brand,
                            model = a.model,
                            size = a.size,
                            color = a.color,
                            serial_no = a.serial_no,
                            po = a.po,
                            warranty = a.warranty,
                            cost = a.cost,
                            remarks = a.remarks,
                            li_description = a.li_description,
                            asset_image = a.asset_image,
                            owner_id = a.owner_id,
                            date_created = a.date_created,
                            date_modified = a.date_modified,
                            date_acquired = a.date_acquired,
                            status = a.status
                        }).ToList(),
                        computers = u.computer.Select(c => new ComputerAccountabilityDto
                        {
                            id = c.id,
                            type = c.type,
                            asset_barcode = c.asset_barcode,
                            brand = c.brand,
                            model = c.model,
                            size = c.size,
                            color = c.color,
                            serial_no = c.serial_no,
                            po = c.po,
                            warranty = c.warranty,
                            cost = c.cost,
                            remarks = c.remarks,
                            li_description = c.li_description,
                            asset_image = c.asset_image,
                            owner_id = c.owner_id,
                            is_deleted = c.is_deleted,
                            date_created = c.date_created,
                            date_modified = c.date_modified,
                            date_acquired = c.date_acquired,
                            status = c.status
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(new PaginatedResponse<object>
                {
                    Items = userAccountabilityLists.Cast<object>().ToList(), // Explicit casting
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });

            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving user accountability lists: {ex.Message}");
            }
        }



        //public class OwnerDetailsDto
        //{
        //    public int id { get; set; }
        //    public string name { get; set; }
        //    public string company { get; set; }
        //    public string department { get; set; }
        //    public string employee_id { get; set; }
        //}

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUserAccountabilityListById(int id)
        {
            try
            {
                // Get the user accountability list with owner details in a single query
                var userAccountabilityList = await _context.user_accountability_lists
            .Where(u => u.id == id)
            .Select(u => new
            {
                u.id,
                u.accountability_code,
                u.tracking_code,
                u.owner_id,
                u.computer_ids,
                u.date_created,
                owner = new OwnerDetailsDto
                {
                    id = u.owner.id,
                    name = u.owner.name,
                    company = u.owner.company,
                    department = u.owner.department,
                    employee_id = u.owner.employee_id,
                    designation = u.owner.designation
                }
            })
            .FirstOrDefaultAsync();

                if (userAccountabilityList == null)
                    return NotFound();

                // Parse computer IDs
                var computerIds = !string.IsNullOrEmpty(userAccountabilityList.computer_ids)
                    ? userAccountabilityList.computer_ids.Split(',').Select(int.Parse).ToList()
                    : new List<int>();

                if (computerIds.Count == 0)
                {
                    return Ok(new { userAccountabilityList, computers = new List<ComputerAccountabilityDto>() });
                }
                // Get all users that might be in history (for later use)
                var allUserIds = await _context.computers
                    .Where(c => computerIds.Contains(c.id) && c.history != null) // Ensure history is not null
                    .SelectMany(c => c.history) // Flatten the list directly in the query
                    .Distinct()
                    .ToListAsync(); // Convert to a flat list

                // Ensure we are checking user IDs correctly
                var userLookup = await _context.Users
                    .Where(u => allUserIds.Contains(u.id.ToString())) // Corrected: allUserIds is a List<string> now
                    .Select(u => new {
                        u.id,
                        Details = new OwnerDetailsDto
                        {
                            id = u.id,
                            name = u.name,
                            company = u.company,
                            department = u.department,
                            employee_id = u.employee_id
                        }
                    })
                    .ToDictionaryAsync(u => u.id.ToString(), u => u.Details);


                // Get all assigned asset IDs from computers
                var allAssignedAssetIds = await _context.computers
                    .Where(c => computerIds.Contains(c.id) && c.assigned_assets != null && c.assigned_assets.Any())
                    .SelectMany(c => c.assigned_assets)
                    .Distinct()
                    .ToListAsync();

                // Get all assets in a single query
                var assetLookup = new Dictionary<int, AssetAccountabilityDto>();
                if (allAssignedAssetIds.Any())
                {
                    var assets = await _context.Assets
                        .Where(a => allAssignedAssetIds.Contains(a.id))
                        .Select(a => new
                        {
                            Asset = new AssetAccountabilityDto
                            {
                                id = a.id,
                                type = a.type,
                                asset_barcode = a.asset_barcode,
                                brand = a.brand,
                                model = a.model,
                                size = a.size,
                                color = a.color,
                                serial_no = a.serial_no,
                                po = a.po,
                                warranty = a.warranty,
                                cost = a.cost,
                                remarks = a.remarks,
                                li_description = a.li_description,
                                asset_image = a.asset_image,
                                owner_id = a.owner_id,
                                date_created = a.date_created,
                                date_modified = a.date_modified,
                                date_acquired = a.date_acquired,
                                status = a.status
                            },
                            RootHistory = a.root_history
                        })
                        .ToListAsync();

                    foreach (var asset in assets)
                    {
                        // Set history details
                        if (asset.RootHistory != null)
                        {
                            asset.Asset.history_details = asset.RootHistory
                                .Where(id => userLookup.ContainsKey(id.ToString()))
                                .Select(id => userLookup[id.ToString()])
                                .ToList();
                        }
                        else
                        {
                            asset.Asset.history_details = new List<OwnerDetailsDto>();
                        }

                        assetLookup[asset.Asset.id] = asset.Asset;
                    }
                }

                // Get all computers with components in a single query (using a join)
                var computersWithComponents = await _context.computers
                    .Where(c => computerIds.Contains(c.id))
                    .Select(c => new
                    {
                        Computer = new ComputerAccountabilityDto
                        {
                            id = c.id,
                            type = c.type,
                            asset_barcode = c.asset_barcode,
                            brand = c.brand,
                            model = c.model,
                            size = c.size,
                            color = c.color,
                            serial_no = c.serial_no,
                            po = c.po,
                            warranty = c.warranty,
                            cost = c.cost,
                            remarks = c.remarks,
                            li_description = c.li_description,
                            asset_image = c.asset_image,
                            owner_id = c.owner_id,
                            is_deleted = c.is_deleted,
                            date_created = c.date_created,
                            date_modified = c.date_modified,
                            date_acquired = c.date_acquired,
                            status = c.status,
                            assigned_assets = c.assigned_assets != null ? string.Join(", ", c.assigned_assets.Select(a => a.ToString())) : "",
                            components = null,
                            history_details = new List<OwnerDetailsDto>()
                        },
                        History = c.history,
                        AssignedAssetsList = c.assigned_assets
                    })
                    .ToListAsync();

                // Get all components in a single query
                var components = await _context.computer_components
                    .Where(cc => cc.computer_id.HasValue && computerIds.Contains(cc.computer_id.Value))
                    .GroupBy(cc => cc.computer_id)
                    .Select(g => new
                    {
                        ComputerId = g.Key,
                        Components = g.GroupBy(cc => cc.type)
                            .Select(typeGroup => new
                            {
                                Type = typeGroup.Key,
                                Items = typeGroup.Select(cc => new
                                {
                                    id = cc.id,
                                    uid = cc.uid,
                                    description = cc.description,
                                    date_acquired = cc.date_acquired,
                                    asset_barcode = cc.asset_barcode,
                                    cost = cc.cost,
                                    status = cc.status
                                }).ToList()
                            })
                            .ToList()
                    })
                    .ToDictionaryAsync(x => x.ComputerId, x => x.Components);

                // Process and combine all data
                var computersResult = new List<ComputerAccountabilityDto>();
                foreach (var item in computersWithComponents)
                {
                    var computer = item.Computer;

                    // Set history details
                    if (item.History != null)
                    {
                        computer.history_details = item.History
                            .Where(id => userLookup.ContainsKey(id))
                            .Select(id => userLookup[id])
                            .ToList();
                    }

                    // Set components
                    if (components.TryGetValue(computer.id, out var computerComponents))
                    {
                        computer.components = computerComponents.ToDictionary(
                            g => g.Type,
                            g => g.Items.Cast<object>().ToList()
                        );
                    }
                    else
                    {
                        computer.components = new Dictionary<string, List<object>>();
                    }

                    // Set assigned assets
                    if (item.AssignedAssetsList != null && item.AssignedAssetsList.Any())
                    {
                        computer.AssignedAssetDetails = item.AssignedAssetsList
                            .Where(id => assetLookup.ContainsKey(id))
                            .Select(id => assetLookup[id])
                            .ToList();
                    }
                    else
                    {
                        computer.AssignedAssetDetails = new List<AssetAccountabilityDto>();
                    }

                    computersResult.Add(computer);
                }

                return Ok(new
                {
                    user_accountability_list = userAccountabilityList,
                    computers = computersResult
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving user accountability list: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPost("add-accountability")]
        public async Task<IActionResult> AddAccountability([FromBody] UserAccountabilityListDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Invalid data.");
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var assetIds = dto.asset_ids ?? new List<int>();
                var computerIds = dto.computer_ids ?? new List<int>();

                // Step 1: Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.employee_id == dto.employee_id.ToString());

                int userId;

                if (existingUser != null)
                {
                    userId = existingUser.id;
                }
                else
                {
                    var newUser = new User
                    {
                        name = dto.name,
                        company = dto.company,
                        department = dto.department,
                        employee_id = dto.employee_id.ToString(),
                        designation = dto.designation,
                        date_created = DateTime.UtcNow,
                        date_hired = dto.date_hired,
                        date_resignation = dto.date_resignation
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();
                    userId = newUser.id;
                }

                // Step 2: Check if accountability already exists for this owner_id
                var existingAccountability = await _context.user_accountability_lists
                    .FirstOrDefaultAsync(ual => ual.owner_id == userId);

                if (existingAccountability != null)
                {
                    // Step 3: Modify existing accountability record
                    var existingComputerIds = existingAccountability.computer_ids?.Split(',').Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList() ?? new List<int>();
                    var existingAssetIds = existingAccountability.asset_ids?.Split(',').Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList() ?? new List<int>();

                    existingComputerIds.AddRange(computerIds);
                    existingComputerIds = existingComputerIds.Distinct().ToList();

                    existingAssetIds.AddRange(assetIds);
                    existingAssetIds = existingAssetIds.Distinct().ToList();

                    existingAccountability.computer_ids = string.Join(",", existingComputerIds);
                    existingAccountability.asset_ids = string.Join(",", existingAssetIds);
                    existingAccountability.date_modified = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Step 4: Create a new accountability entry
                    var lastAccountability = await _context.user_accountability_lists
                        .OrderByDescending(ual => ual.id)
                        .FirstOrDefaultAsync();

                    int accountabilityCodeCounter = 1;
                    int trackingCodeCounter = 1;

                    if (lastAccountability != null)
                    {
                        accountabilityCodeCounter = int.TryParse(lastAccountability.accountability_code.Split('-').Last(), out int lastACID) ? lastACID + 1 : accountabilityCodeCounter;
                        trackingCodeCounter = int.TryParse(lastAccountability.tracking_code.Split('-').Last(), out int lastTRID) ? lastTRID + 1 : trackingCodeCounter;
                    }

                    var (newAccountabilityCode, newTrackingCode) = GenerateAccountabilityAndTrackingCode(accountabilityCodeCounter, trackingCodeCounter);

                    var newAccountability = new UserAccountabilityList
                    {
                        owner_id = userId,
                        computer_ids = string.Join(",", computerIds),
                        asset_ids = string.Join(",", assetIds),
                        accountability_code = newAccountabilityCode,
                        tracking_code = newTrackingCode,
                        date_created = DateTime.UtcNow,
                        date_modified = DateTime.UtcNow,
                        is_active = true,
                    };

                    _context.user_accountability_lists.Add(newAccountability);
                    await _context.SaveChangesAsync();
                }

                // Step 5: Update computers
                if (computerIds.Any())
                {
                    var computersToUpdate = await _context.computers
                        .Where(c => computerIds.Contains(c.id))
                        .ToListAsync();

                    foreach (var computer in computersToUpdate)
                    {
                        computer.status = "ACTIVE";
                        computer.owner_id = userId;

                        // Ensure history is initialized before adding new entry
                        if (computer.history == null)
                        {
                            computer.history = new List<string>();
                        }

                        computer.history.Add(userId.ToString());
                    }

                    await _context.SaveChangesAsync();

                    // Update Computer Components owner_id
                    var componentsToUpdate = await _context.computer_components
                        .Where(cc => cc.computer_id.HasValue && computerIds.Contains(cc.computer_id.Value)) // Fix Here
                        .ToListAsync();

                    foreach (var component in componentsToUpdate)
                    {
                        component.owner_id = userId;
                    }

                    await _context.SaveChangesAsync();

                    // Update Assigned Assets Owner ID
                    // Update Assigned Assets Owner ID
                    var assignedAssetIds = computersToUpdate
                        .SelectMany(c => c.assigned_assets)
                        .ToList();

                    if (assignedAssetIds.Any())
                    {
                        var assignedAssets = await _context.Assets
                            .Where(a => assignedAssetIds.Contains(a.id))
                            .ToListAsync();

                        foreach (var asset in assignedAssets)
                        {
                            asset.owner_id = userId;
                            asset.status = "ACTIVE";

                            // Ensure history is initialized before adding new entry
                            if (asset.history == null)
                            {
                                asset.history = new List<string>();
                            }

                            asset.history.Add(userId.ToString()); // Store new owner in history
                        }
                        await _context.SaveChangesAsync();
                    }

                }


                // Step 6: Update assets
                if (assetIds.Any())
                {
                    var assetsToUpdate = await _context.Assets
                        .Where(a => assetIds.Contains(a.id))
                        .ToListAsync();

                    foreach (var asset in assetsToUpdate)
                    {
                        asset.status = "ACTIVE";
                        asset.owner_id = userId;

                        // Ensure history is initialized before adding new entry
                        if (asset.history == null)
                        {
                            asset.history = new List<string>();
                        }

                        asset.history.Add(userId.ToString()); // Store new owner in history
                    }

                    await _context.SaveChangesAsync();
                }



                return Ok(new
                {
                    message = "Accountability updated successfully.",
                    userId = userId,
                    isExistingUser = existingUser != null
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating accountability list: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // Helper function remains unchanged
        private (string, string) GenerateAccountabilityAndTrackingCode(int accountabilityCodeCounter, int trackingCodeCounter)
        {
            string newAccountabilityCode = $"ACID-{accountabilityCodeCounter:D4}";
            string newTrackingCode = $"TRID-{trackingCodeCounter:D4}";
            return (newAccountabilityCode, newTrackingCode);
        }

        [Authorize]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUserAccountabilityList(int id)
        {
            var accountabilityList = await _context.user_accountability_lists.FindAsync(id);

            if (accountabilityList == null)
            {
                return NotFound(new { message = "User accountability list not found." });
            }

            // Perform a soft delete by setting is_deleted to true
            accountabilityList.is_deleted = true;
            accountabilityList.date_modified = DateTime.UtcNow;

            // Remove owner_id from associated assets and computers while storing the previous owner_id in history
            var assetIds = accountabilityList.asset_ids?.Split(',').Select(int.Parse).ToList() ?? new List<int>();
            var computerIds = accountabilityList.computer_ids?.Split(',').Select(int.Parse).ToList() ?? new List<int>();

            var assetsToUpdate = await _context.Assets.Where(a => assetIds.Contains(a.id)).ToListAsync();
            var computersToUpdate = await _context.computers.Where(c => computerIds.Contains(c.id)).ToListAsync();
            var componentsToUpdate = await _context.computer_components.Where(cc => cc.computer_id.HasValue && computerIds.Contains(cc.computer_id.Value)).ToListAsync();


            foreach (var asset in assetsToUpdate)
            {
                if (asset.owner_id.HasValue)
                {
                    // Initialize the list if it's null
                    if (asset.history == null)
                    {
                        asset.history = new List<string>();
                    }
                    // Add the owner_id to the history
                    asset.history.Add(asset.owner_id.Value.ToString());
                }
                asset.owner_id = null;
                asset.status = "INACTIVE";
            }

            foreach (var computer in computersToUpdate)
            {
                if (computer.owner_id.HasValue)
                {
                    // Initialize the list if it's null
                    if (computer.history == null)
                    {
                        computer.history = new List<string>();
                    }
                    // Add the owner_id to the history
                    computer.history.Add(computer.owner_id.Value.ToString());
                }
                computer.owner_id = null;
                computer.status = "INACTIVE";
            }

            foreach (var component in componentsToUpdate)
            {
                component.owner_id = null;
                component.asset_barcode = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "User accountability list marked as deleted, ownership removed, and associated computers set to INACTIVE with history updated." });
        }


    }
}
