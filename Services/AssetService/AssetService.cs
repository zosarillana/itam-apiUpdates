using ITAM.DataContext;
using ITAM.DTOs;
using ITAM.Models.Logs;
using ITAM.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using ITAM.Services;

namespace ITAM.Services.AssetService
{
    public class AssetService
    {
        private readonly AppDbContext _context;
        private readonly UserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        //private readonly IHubContext<AssetsHub> _hubContext;



        public AssetService(AppDbContext context, UserService userService, IHttpContextAccessor httpContextAccessor /*,IHubContext<AssetsHub> hubContext*/)
        {
            _context = context;
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
            //_hubContext = hubContext;

        }

        public async Task<string> AddAssetAsync(AddAssetDto assetDto)
        {
            if (string.IsNullOrWhiteSpace(assetDto.type) || string.IsNullOrWhiteSpace(assetDto.asset_barcode))
            {
                throw new ArgumentException("Type and Asset Barcode are required.");
            }

            User user = null;
            if (!string.IsNullOrWhiteSpace(assetDto.user_name) &&
                !string.IsNullOrWhiteSpace(assetDto.company) &&
                !string.IsNullOrWhiteSpace(assetDto.department) &&
                !string.IsNullOrWhiteSpace(assetDto.employee_id))
            {
                user = await _userService.FindOrCreateUserAsync(assetDto);
                if (user == null) throw new Exception("User not found or could not be created.");
            }

            var liDescription = GenerateLiDescription(assetDto);

            // Define the types that should only be added to the 'Computer' table
            var computerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CPU", "CPU CORE i7 10th GEN", "CPU INTEL CORE i5", "Laptop", "Laptop Macbook AIR, NB 15S-DUI537TU"
            };

            try
            {
                // Determine the status based on user assignment
                string status = user != null ? "ACTIVE" : "INACTIVE";
                DateTime now = DateTime.UtcNow;

                if (computerTypes.Contains(assetDto.type))
                {
                    var assignedAssets = assetDto.assigned_assets?
                        .Select(value =>
                        {
                            int parsedValue;
                            return int.TryParse(value, out parsedValue) ? parsedValue : (int?)null;
                        })
                        .Where(x => x.HasValue)
                        .Select(x => x.Value)
                        .ToList();

                    // Create Computer object
                    var computer = new Computer
                    {
                        type = assetDto.type,
                        asset_barcode = assetDto.asset_barcode,
                        brand = assetDto.brand,
                        model = assetDto.model,
                        size = assetDto.size,
                        color = assetDto.color,
                        serial_no = assetDto.serial_no,
                        warranty = assetDto.warranty,
                        cost = assetDto.cost ?? 0m,
                        remarks = assetDto.remarks,
                        assigned_assets = assignedAssets,
                        owner_id = user?.id,
                        li_description = liDescription,
                        status = status,
                        date_acquired = assetDto.date_acquired,
                        date_created = now,
                        po = assetDto.po,
                    };

                    _context.computers.Add(computer);
                    await _context.SaveChangesAsync();
                    await StoreInComputerComponentsAsync(assetDto, user, computer);


                    if (assignedAssets != null && assignedAssets.Count > 0)
                    {
                        foreach (var assetId in assignedAssets)
                        {
                            var asset = await _context.Assets.FindAsync(assetId);
                            if (asset != null)
                            {
                                if (asset.root_history == null)
                                {
                                    asset.root_history = new List<int>();
                                }
                                asset.root_history.Add(computer.id);

                                asset.status = asset.root_history.Count > 0 ? "ACTIVE" : "INACTIVE"; // Status based on assignment

                                if (computer.owner_id.HasValue)
                                {
                                    asset.owner_id = computer.owner_id.Value;
                                }

                                asset.date_modified = now;
                                _context.Update(asset);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }


                    await LogToCentralizedLogsAsync("Computer Added", $"Computer with barcode {computer.asset_barcode} added.", assetDto.type, computer.asset_barcode);

                    if (user != null)
                    {
                        await HandleUserAccountabilityListAsync(user, computer);
                    }

                    return "Computer added successfully.";
                }
                else
                {

                    var asset = new Asset
                    {
                        type = assetDto.type,
                        asset_barcode = assetDto.asset_barcode,
                        brand = assetDto.brand,
                        model = assetDto.model,
                        size = assetDto.size,
                        color = assetDto.color,
                        serial_no = assetDto.serial_no,
                        po = assetDto.po,
                        warranty = assetDto.warranty,
                        cost = assetDto.cost ?? 0m,
                        remarks = assetDto.remarks,
                        owner_id = user?.id,
                        li_description = liDescription,
                        status = status,
                        date_acquired = assetDto.date_acquired,
                        date_created = now
                    };

                    _context.Assets.Add(asset);
                    await _context.SaveChangesAsync();

                    await LogToCentralizedLogsAsync("Asset Added", $"Asset with barcode {asset.asset_barcode} added.", assetDto.type, asset.asset_barcode);

                    if (user != null)
                    {
                        await HandleUserAccountabilityListAsync(user, asset);
                    }

                    return "Asset added successfully.";
                }
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database Update Exception: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }





        public async Task LogToCentralizedLogsAsync(string action, string details, string assetType, string assetBarcode)
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




        private async Task StoreInComputerComponentsAsync(AddAssetDto assetDto, User user, Computer computer, string performedByUserId = "SYSTEM")
        {
            var headers = new string[] { "RAM", "SSD", "HDD", "GPU", "BOARD" };
            var values = new string[] { assetDto.ram, assetDto.ssd, assetDto.hdd, assetDto.gpu, assetDto.board };

            var components = new List<ComputerComponents>();
            var centralizedLogs = new List<CentralizedLogs>();

            // Fetch the last UID only once before the loop
            var lastUid = await _context.computer_components
                .OrderByDescending(c => c.id)
                .FirstOrDefaultAsync();

            int lastUidIndex = lastUid != null ? int.Parse(lastUid.uid.Split('-')[1]) : 0;

            for (int i = 0; i < headers.Length; i++)
            {
                var description = values[i];
                if (string.IsNullOrWhiteSpace(description)) continue;

                // Check if an inactive component of the same type exists
                var existingComponent = await _context.computer_components
                    .Where(c => c.type == headers[i] && c.status == "INACTIVE")
                    .FirstOrDefaultAsync();

                if (existingComponent != null)
                {
                    // Update the existing component instead of creating a new one
                    existingComponent.status = "ACTIVE";
                    existingComponent.asset_barcode = assetDto.asset_barcode;
                    existingComponent.owner_id = user?.id;
                    existingComponent.computer_id = computer.id;
                    existingComponent.date_acquired = computer.date_acquired;
                    existingComponent.history ??= new List<string>();
                    existingComponent.history.Add(computer.id.ToString());

                    _context.computer_components.Update(existingComponent);

                    centralizedLogs.Add(new CentralizedLogs
                    {
                        type = existingComponent.type,
                        asset_barcode = existingComponent.uid,
                        action = "Component Reassigned",
                        performed_by_user_id = string.IsNullOrWhiteSpace(performedByUserId) ? "SYSTEM" : performedByUserId,
                        timestamp = DateTime.UtcNow,
                        details = $"Component {existingComponent.uid} reassigned to computer {computer.id}"
                    });
                }
                else
                {
                    // Increment UID for each new component
                    string newUID = $"UID-{(++lastUidIndex):D3}";

                    var newComponent = new ComputerComponents
                    {
                        type = headers[i],
                        description = description,
                        asset_barcode = assetDto.asset_barcode,
                        status = "ACTIVE",
                        owner_id = user?.id,
                        computer_id = computer.id,
                        uid = newUID, // Each component gets a different UID
                        history = new List<string> { computer.id.ToString() },
                        date_acquired = computer.date_acquired
                    };

                    components.Add(newComponent);
                    centralizedLogs.Add(new CentralizedLogs
                    {
                        type = newComponent.type,
                        asset_barcode = newComponent.uid,
                        action = "Computer Component Created",
                        performed_by_user_id = string.IsNullOrWhiteSpace(performedByUserId) ? "SYSTEM" : performedByUserId,
                        timestamp = DateTime.UtcNow,
                        details = $"Computer component added with UID: {newComponent.uid}"
                    });
                }
            }

            // Save changes to the database
            if (components.Any()) _context.computer_components.AddRange(components);
            if (centralizedLogs.Any()) _context.centralized_logs.AddRange(centralizedLogs);

            await _context.SaveChangesAsync();

            // Assign component UIDs to computer
            var savedComponents = await _context.computer_components
                .Where(c => c.computer_id == computer.id && headers.Contains(c.type))
                .ToListAsync();

            computer.ram = savedComponents.FirstOrDefault(c => c.type == "RAM")?.uid;
            computer.ssd = savedComponents.FirstOrDefault(c => c.type == "SSD")?.uid;
            computer.hdd = savedComponents.FirstOrDefault(c => c.type == "HDD")?.uid;
            computer.gpu = savedComponents.FirstOrDefault(c => c.type == "GPU")?.uid;
            computer.board = savedComponents.FirstOrDefault(c => c.type == "BOARD")?.uid;

            await _context.SaveChangesAsync();
        }





        private string GenerateLiDescription(AddAssetDto assetDto)
        {
            // Create an array of all the properties that make up the description
            var descriptionParts = new[] {
                assetDto.brand?.Trim(),    // Brand
                assetDto.type?.Trim(),     // Type
                assetDto.model?.Trim(),    // Model
                assetDto.ram?.Trim(),      // RAM
                assetDto.ssd?.Trim(),      // SSD
                assetDto.hdd?.Trim(),      // HDD
                assetDto.gpu?.Trim(),      // GPU
                assetDto.board?.Trim(),    // BOARD
                assetDto.size?.Trim(),     // Size
                assetDto.color?.Trim()     // Color
            };

            // Filter out null or empty values and join the remaining parts
            var liDescription = string.Join(" ", descriptionParts.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();

            // If the description is still empty after trimming, return a default message
            return string.IsNullOrWhiteSpace(liDescription) ? "No description available" : liDescription;
        }



        private async Task HandleUserAccountabilityListAsync(User user, object item)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "User cannot be null.");
            }
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "Item cannot be null.");
            }

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
                    computer_ids = string.Empty,
                    date_created = DateTime.UtcNow
                };
                _context.user_accountability_lists.Add(userAccountabilityList);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Created new UserAccountabilityList for User ID: {user.id}");
            }
            else
            {
                Console.WriteLine($"UserAccountabilityList found for User ID: {user.id}");
            }

            try
            {
                bool isModified = false;

                // Helper function to add IDs to a comma-separated string
                string AddIdsToList(string existingIds, List<int> newIds)
                {
                    var existingIdList = string.IsNullOrEmpty(existingIds)
                        ? new List<int>()
                        : existingIds.Split(',').Where(id => !string.IsNullOrWhiteSpace(id)).Select(int.Parse).ToList();

                    bool wasModified = false;
                    foreach (var id in newIds)
                    {
                        if (!existingIdList.Contains(id))
                        {
                            existingIdList.Add(id);
                            wasModified = true;
                        }
                    }

                    if (wasModified)
                    {
                        isModified = true;
                        return string.Join(",", existingIdList);
                    }

                    return existingIds;
                }

                if (item is Asset asset)
                {
                    Console.WriteLine($"Adding Asset ID: {asset.id} to User {user.id}");
                    userAccountabilityList.asset_ids = AddIdsToList(userAccountabilityList.asset_ids, new List<int> { asset.id });
                }
                else if (item is Computer computer)
                {
                    Console.WriteLine($"Adding Computer ID: {computer.id} to User {user.id}");
                    // Add the computer ID
                    userAccountabilityList.computer_ids = AddIdsToList(userAccountabilityList.computer_ids, new List<int> { computer.id });

                    // Add all assigned assets if they exist
                    if (computer.assigned_assets != null && computer.assigned_assets.Any())
                    {
                        Console.WriteLine($"Adding assigned asset IDs: {string.Join(", ", computer.assigned_assets)} to User {user.id}");
                        userAccountabilityList.asset_ids = AddIdsToList(userAccountabilityList.asset_ids, computer.assigned_assets);
                    }
                }

                if (isModified)
                {
                    userAccountabilityList.date_modified = DateTime.UtcNow;
                }

                Console.WriteLine($"Updated asset_ids: {userAccountabilityList.asset_ids}");
                Console.WriteLine($"Updated computer_ids: {userAccountabilityList.computer_ids}");

                _context.user_accountability_lists.Update(userAccountabilityList);
                await _context.SaveChangesAsync();
                Console.WriteLine("User Accountability List updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while updating User Accountability List: {ex.Message}");
                throw;
            }
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



        public async Task<object> CreateVacantAssetAsync(CreateAssetDto assetDto, ClaimsPrincipal userClaims)
        {
            var computerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CPU",
                "CPU CORE i7 10th GEN",
                "CPU INTEL CORE i5",
                "Laptop",
                "Laptop Macbook AIR, NB 15S-DUI537TU"
            };

            string typeToCheck = assetDto.type?.Trim().ToUpperInvariant();

            var liDescription = string.Join(" ",
                assetDto.brand?.Trim(),
                assetDto.type?.Trim(),
                assetDto.model?.Trim(),
                assetDto.ram?.Trim(),
                assetDto.ssd?.Trim(),
                assetDto.hdd?.Trim(),
                assetDto.gpu?.Trim(),
                assetDto.size?.Trim(),
                assetDto.color?.Trim()).Trim();

            if (string.IsNullOrWhiteSpace(liDescription))
            {
                liDescription = "No description available";
            }

            string performedByUserId = userClaims?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";
            string status = "AVAILABLE";

            if (computerTypes.Contains(typeToCheck))
            {
                var componentUIDs = await GenerateComponentUIDsAsync(assetDto);

                var computer = new Computer
                {
                    type = assetDto.type,
                    asset_barcode = assetDto.asset_barcode,
                    brand = assetDto.brand,
                    model = assetDto.model,
                    ram = componentUIDs["ram"],
                    ssd = componentUIDs["ssd"],
                    hdd = componentUIDs["hdd"],
                    gpu = componentUIDs["gpu"],
                    size = assetDto.size,
                    color = assetDto.color,
                    serial_no = assetDto.serial_no,
                    po = assetDto.po,
                    warranty = assetDto.warranty,
                    cost = assetDto.cost,
                    remarks = assetDto.remarks,
                    li_description = liDescription,
                    date_acquired = assetDto.date_acquired,
                    asset_image = assetDto.asset_image,
                    owner_id = null,
                    status = status,
                    history = new List<string>(),
                    date_created = DateTime.UtcNow
                };

                _context.computers.Add(computer);
                await _context.SaveChangesAsync();

                await LogToCentralizedSystem(
                    computer.type,
                    computer.asset_barcode,
                    "Vacant Computer Created",
                    performedByUserId,
                    $"Vacant computer created with barcode {computer.asset_barcode}, brand {computer.brand}, and model {computer.model}."
                );

                await StoreComputerComponentsAsync(computer, assetDto, userClaims);

                return computer;
            }
            else
            {
                var asset = new Asset
                {
                    type = assetDto.type,
                    asset_barcode = assetDto.asset_barcode,
                    brand = assetDto.brand,
                    model = assetDto.model,
                    size = assetDto.size,
                    color = assetDto.color,
                    serial_no = assetDto.serial_no,
                    po = assetDto.po,
                    warranty = assetDto.warranty,
                    cost = assetDto.cost,
                    remarks = assetDto.remarks,
                    li_description = liDescription,
                    date_acquired = assetDto.date_acquired,
                    asset_image = assetDto.asset_image,
                    owner_id = null,
                    status = status,
                    history = new List<string>(),
                    date_created = DateTime.UtcNow
                };

                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                await LogToCentralizedSystem(
                    asset.type,
                    asset.asset_barcode,
                    "Vacant Asset Created",
                    performedByUserId,
                    $"Vacant asset created with barcode {asset.asset_barcode}, brand {asset.brand}, and model {asset.model}."
                );

                return asset;
            }
        }


        private async Task LogToCentralizedSystem(
        string type,
        string? assetBarcode,
        string action,
        string performedByUserId,
        string details)
        {
            var centralizedLog = new CentralizedLogs
            {
                type = type,
                asset_barcode = assetBarcode,
                action = action,
                performed_by_user_id = performedByUserId,
                timestamp = DateTime.UtcNow,
                details = details
            };

            _context.centralized_logs.Add(centralizedLog);
            await _context.SaveChangesAsync();
        }



        private async Task<Dictionary<string, string>> GenerateComponentUIDsAsync(CreateAssetDto assetDto)
        {
            // Fetch the last used UID from the computer_components table
            var lastUid = await _context.computer_components
                .OrderByDescending(c => c.id)
                .FirstOrDefaultAsync();
            int lastUidIndex = lastUid != null ? int.Parse(lastUid.uid.Split('-')[1]) : 0;

            // Generate UIDs for RAM, SSD, HDD, and GPU
            var componentUIDs = new Dictionary<string, string>();

            // Generate UID for RAM if it's provided
            if (!string.IsNullOrWhiteSpace(assetDto.ram))
            {
                lastUidIndex++;
                componentUIDs["ram"] = $"UID-{lastUidIndex:D3}";
            }

            // Generate UID for SSD if it's provided
            if (!string.IsNullOrWhiteSpace(assetDto.ssd))
            {
                lastUidIndex++;
                componentUIDs["ssd"] = $"UID-{lastUidIndex:D3}";
            }

            // Generate UID for HDD if it's provided
            if (!string.IsNullOrWhiteSpace(assetDto.hdd))
            {
                lastUidIndex++;
                componentUIDs["hdd"] = $"UID-{lastUidIndex:D3}";
            }

            // Generate UID for GPU if it's provided
            if (!string.IsNullOrWhiteSpace(assetDto.gpu))
            {
                lastUidIndex++;
                componentUIDs["gpu"] = $"UID-{lastUidIndex:D3}";
            }

            return componentUIDs;
        }

        private async Task StoreComputerComponentsAsync(Computer computer, CreateAssetDto assetDto, ClaimsPrincipal userClaims)
        {
            var components = new List<ComputerComponents>();
            var centralizedLogs = new List<CentralizedLogs>();

            string performedByUserId = userClaims?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

            // Add RAM as a component if it exists
            if (!string.IsNullOrWhiteSpace(assetDto.ram))
            {
                var ramComponent = new ComputerComponents
                {
                    type = "RAM",
                    description = assetDto.ram, // Store the actual RAM value
                    asset_barcode = computer.asset_barcode,
                    status = "Available",
                    owner_id = null,
                    history = new List<string> { computer.id.ToString() },
                    computer_id = computer.id,
                    uid = computer.ram, // UID is still assigned
                    date_acquired = computer.date_acquired // Ensure same date_acquired value
                };
                components.Add(ramComponent);
            }

            // Add SSD as a component if it exists
            if (!string.IsNullOrWhiteSpace(assetDto.ssd))
            {
                var ssdComponent = new ComputerComponents
                {
                    type = "SSD",
                    description = assetDto.ssd, // Store the actual SSD value
                    asset_barcode = computer.asset_barcode,
                    status = "Available",
                    owner_id = null,
                    history = new List<string> { computer.id.ToString() },
                    computer_id = computer.id,
                    uid = computer.ssd,
                    date_acquired = computer.date_acquired
                };
                components.Add(ssdComponent);
            }

            // Add HDD as a component if it exists
            if (!string.IsNullOrWhiteSpace(assetDto.hdd))
            {
                var hddComponent = new ComputerComponents
                {
                    type = "HDD",
                    description = assetDto.hdd, // Store the actual HDD value
                    asset_barcode = computer.asset_barcode,
                    status = "Available",
                    owner_id = null,
                    history = new List<string> { computer.id.ToString() },
                    computer_id = computer.id,
                    uid = computer.hdd,
                    date_acquired = computer.date_acquired
                };
                components.Add(hddComponent);
            }

            // Add GPU as a component if it exists
            if (!string.IsNullOrWhiteSpace(assetDto.gpu))
            {
                var gpuComponent = new ComputerComponents
                {
                    type = "GPU",
                    description = assetDto.gpu, // Store the actual GPU value
                    asset_barcode = computer.asset_barcode,
                    status = "Available",
                    owner_id = null,
                    history = new List<string> { computer.id.ToString() },
                    computer_id = computer.id,
                    uid = computer.gpu,
                    date_acquired = computer.date_acquired
                };
                components.Add(gpuComponent);
            }

            // Save components to the database
            _context.computer_components.AddRange(components);
            await _context.SaveChangesAsync();

            // Create centralized log entries for each component
            foreach (var component in components)
            {
                centralizedLogs.Add(new CentralizedLogs
                {
                    type = component.type,
                    asset_barcode = component.uid,
                    action = "Computer Components Created",
                    performed_by_user_id = performedByUserId,
                    details = $"Component {component.type} created in root computer ID: {component.computer_id}, with asset barcode {component.asset_barcode}.",
                    timestamp = DateTime.UtcNow
                });
            }

            // Save logs to the centralized logs table
            _context.centralized_logs.AddRange(centralizedLogs);
            await _context.SaveChangesAsync();
        }



        public async Task<(bool success, string message)> CreateComputerAsync(CreateComputerRequest request)
        {
            if (request == null)
                return (false, "Invalid request data");

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    string BuildDescription(CreateComputerRequest req)
                    {
                        var descriptionParts = new[]
                        {
                    req.brand?.Trim(),
                    req.type?.Trim(),
                    req.model?.Trim(),
                    req.color?.Trim()
                };
                        return string.Join(" ", descriptionParts.Where(part => !string.IsNullOrWhiteSpace(part))).Trim() ?? "No description available";
                    }

                    // ✅ Create & Save the Computer First
                    var computer = new Computer
                    {
                        type = request.type,
                        date_acquired = request.date_acquired,
                        asset_barcode = request.asset_barcode,
                        brand = request.brand,
                        model = request.model,
                        size = request.size,
                        color = request.color,
                        serial_no = request.serial_no,
                        po = request.po,
                        warranty = request.warranty,
                        cost = request.cost,
                        remarks = request.remarks,
                        date_created = DateTime.UtcNow,
                        assigned_assets = new List<int>(),
                        status = "AVAILABLE",
                        li_description = BuildDescription(request)
                    };

                    _context.computers.Add(computer);
                    await _context.SaveChangesAsync(); // ✅ Save to get generated ID

                    // ✅ If components exist, save them
                    if (request.components != null && request.components.Any())
                    {
                        var lastUid = await _context.computer_components.OrderByDescending(c => c.id).FirstOrDefaultAsync();
                        int lastUidIndex = lastUid?.uid != null ? int.Parse(lastUid.uid.Split('-')[1]) : 0;
                        string GenerateUID(int uidIndex) => $"UID-{uidIndex:D3}";

                        var components = request.components.Select(comp => new ComputerComponents
                        {
                            date_acquired = comp.date_acquired,
                            cost = comp.cost,
                            type = comp.type,
                            description = comp.description,
                            computer_id = computer.id, // ✅ Assign computer_id
                            asset_barcode = request.asset_barcode,
                            status = "ACTIVE",
                            history = new List<string> { computer.id.ToString() },
                            uid = GenerateUID(++lastUidIndex)
                        }).ToList();

                        _context.computer_components.AddRange(components);
                        await _context.SaveChangesAsync();

                        var componentDict = components
                            .GroupBy(c => c.type)
                            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(c => c.uid)));

                        computer.ram = componentDict.GetValueOrDefault("RAM");
                        computer.ssd = componentDict.GetValueOrDefault("SSD");
                        computer.hdd = componentDict.GetValueOrDefault("HDD");
                        computer.gpu = componentDict.GetValueOrDefault("GPU");
                        computer.board = componentDict.GetValueOrDefault("BOARD");

                        await _context.SaveChangesAsync();
                    }

                    // ✅ If assets exist, save them & set computer_id
                    if (request.assets != null && request.assets.Any())
                    {
                        var assets = request.assets.Select(asset => new Asset
                        {
                            type = asset.type,
                            date_acquired = asset.date_acquired,
                            asset_barcode = asset.asset_barcode,
                            brand = asset.brand,
                            model = asset.model,
                            size = asset.size,
                            color = asset.color,
                            serial_no = asset.serial_no,
                            po = asset.po,
                            warranty = asset.warranty,
                            cost = asset.cost,
                            remarks = asset.remarks,
                            date_created = DateTime.UtcNow,
                            root_history = new List<int> { computer.id },
                            status = "ACTIVE",
                            computer_id = computer.id // ✅ Set the computer_id
                        }).ToList();

                        _context.Assets.AddRange(assets);
                        await _context.SaveChangesAsync();

                        // ✅ Add assigned asset IDs to the computer
                        computer.assigned_assets.AddRange(assets.Select(a => a.id));
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    return (true, "Computer, Components, and Assets saved successfully!");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Internal Server Error: {ex.Message}");
                }
            }
        }




        private bool IsEmptyAsset(AssetDTOs asset)
        {
            return string.IsNullOrWhiteSpace(asset.type) &&
                   string.IsNullOrWhiteSpace(asset.date_acquired) &&
                   string.IsNullOrWhiteSpace(asset.asset_barcode) &&
                   string.IsNullOrWhiteSpace(asset.brand) &&
                   string.IsNullOrWhiteSpace(asset.model) &&
                   string.IsNullOrWhiteSpace(asset.size) &&
                   string.IsNullOrWhiteSpace(asset.color) &&
                   string.IsNullOrWhiteSpace(asset.serial_no) &&
                   string.IsNullOrWhiteSpace(asset.po) &&
                   string.IsNullOrWhiteSpace(asset.warranty) &&
                   asset.cost == 0 &&
                   string.IsNullOrWhiteSpace(asset.remarks);
        }











        // For assigning user to vacant-asset items
        public async Task<Asset> AssignOwnerToAssetAsync(AssignOwnerDto assignOwnerDto, ClaimsPrincipal userClaims)
        {
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a => a.id == assignOwnerDto.asset_id && a.owner_id == null);

            if (asset == null)
            {
                throw new KeyNotFoundException("Vacant asset not found or already has an owner.");
            }

            var user = await _context.Users.FindAsync(assignOwnerDto.owner_id);
            if (user == null)
            {
                throw new KeyNotFoundException("Owner not found.");
            }

            asset.owner_id = assignOwnerDto.owner_id;

            _context.Assets.Update(asset);
            await _context.SaveChangesAsync();

            string performedByUserId = userClaims?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

            var centralizedLog = new CentralizedLogs
            {
                type = asset.type,
                asset_barcode = asset.asset_barcode,
                action = "Assigned to a user",
                performed_by_user_id = performedByUserId,
                timestamp = DateTime.UtcNow,
                details = $"Asset ID: {asset.id} is assigned to User ID: {user.id}."
            };

            _context.centralized_logs.Add(centralizedLog);
            await _context.SaveChangesAsync();
            await UpdateUserAccountabilityListAsync(user, asset);

            return asset;
        }




        private async Task UpdateUserAccountabilityListAsync(User user, Asset asset)
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
                    computer_ids = string.Empty,
                    date_modified = DateTime.UtcNow
                };

                _context.user_accountability_lists.Add(userAccountabilityList);
                await _context.SaveChangesAsync();
            }

            var existingAssetIds = string.IsNullOrEmpty(userAccountabilityList.asset_ids)
                ? new List<int>()
                : userAccountabilityList.asset_ids.Split(',').Where(id => !string.IsNullOrWhiteSpace(id)).Select(int.Parse).ToList();

            bool isModified = false;

            if (!existingAssetIds.Contains(asset.id))
            {
                existingAssetIds.Add(asset.id);
                userAccountabilityList.asset_ids = string.Join(",", existingAssetIds);
                isModified = true;
            }

            if (isModified)
            {
                userAccountabilityList.date_modified = DateTime.UtcNow;
            }

            _context.user_accountability_lists.Update(userAccountabilityList);
            await _context.SaveChangesAsync();
        }






        //for get by type endpoint 
        public async Task<PaginatedResponse<Asset>> GetAssetsByTypeAsync(
        string type,
        int pageNumber = 1,
        int pageSize = 10,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            var assetQuery = _context.Assets
                .Where(a => a.type.ToLower() == type.ToLower())
                .AsQueryable();

            var computerQuery = _context.computers
                .Where(c => c.type.ToLower() == type.ToLower())
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                assetQuery = assetQuery.Where(asset =>
                    asset.asset_barcode.Contains(searchTerm) ||
                    asset.type.Contains(searchTerm) ||
                    asset.brand.Contains(searchTerm));

                computerQuery = computerQuery.Where(computer =>
                    computer.asset_barcode.Contains(searchTerm) ||
                    computer.type.Contains(searchTerm) ||
                    computer.brand.Contains(searchTerm) ||
                    computer.serial_no.Contains(searchTerm) ||
                    computer.model.Contains(searchTerm));
            }

            var combinedQuery = assetQuery
                .Select(asset => new Asset
                {
                    id = asset.id,
                    type = asset.type,
                    asset_barcode = asset.asset_barcode,
                    brand = asset.brand,
                })
                .Union(computerQuery.Select(computer => new Asset
                {
                    id = computer.id,
                    type = computer.type,
                    asset_barcode = computer.asset_barcode,
                    brand = computer.brand,
                }))
                .AsQueryable();

            combinedQuery = sortOrder.ToLower() switch
            {
                "desc" => combinedQuery.OrderByDescending(a => a.id),
                "asc" or _ => combinedQuery.OrderBy(a => a.id),
            };

            var totalItems = await combinedQuery.CountAsync();

            var paginatedData = await combinedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResponse<Asset>
            {
                Items = paginatedData,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<PaginatedResponse<AssetWithOwnerDTO>> GetAllAssetsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            var query = _context.Assets
           .Where(asset => !asset.is_deleted)
           .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(asset =>
                    EF.Functions.Like(asset.asset_barcode, $"%{searchTerm}%") ||
                    EF.Functions.Like(asset.type, $"%{searchTerm}%") ||
                    EF.Functions.Like(asset.brand, $"%{searchTerm}%") ||
                    EF.Functions.Like(asset.status, $"%{searchTerm}%"));

            }

            query = sortOrder.ToLower() switch
            {
                "desc" => query.OrderByDescending(asset => asset.id),
                "asc" or _ => query.OrderBy(asset => asset.id),
            };

            var totalItems = await query.CountAsync();

            var paginatedData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(asset => new AssetWithOwnerDTO
                {
                    id = asset.id,
                    type = asset.type,
                    date_acquired = asset.date_acquired,
                    asset_barcode = asset.asset_barcode,
                    brand = asset.brand,
                    model = asset.model,
                    size = asset.size,
                    color = asset.color,
                    serial_no = asset.serial_no,
                    po = asset.po,
                    warranty = asset.warranty,
                    cost = asset.cost,
                    remarks = asset.remarks,
                    li_description = asset.li_description,
                    history = asset.history ?? new List<string>(),  // Initialize if null
                    asset_image = asset.asset_image,
                    owner_id = asset.owner_id,
                    is_deleted = asset.is_deleted,
                    date_created = asset.date_created,
                    date_modified = asset.date_modified,
                    status = asset.status,
                    owner = asset.owner_id != null ? new OwnerDTO
                    {
                        id = asset.owner_id.Value,
                        name = _context.Users
                            .Where(u => u.id == asset.owner_id)
                            .Select(u => u.name)
                            .FirstOrDefault(),
                        company = _context.Users
                            .Where(u => u.id == asset.owner_id)
                            .Select(u => u.company)
                            .FirstOrDefault(),
                        department = _context.Users
                            .Where(u => u.id == asset.owner_id)
                            .Select(u => u.department)
                            .FirstOrDefault(),
                        employee_id = _context.Users
                            .Where(u => u.id == asset.owner_id)
                            .Select(u => u.employee_id)
                            .FirstOrDefault()
                    } : null,
                    historyUsers = new List<OwnerDTO>()  // Initialize empty list
                })
                .ToListAsync();

            // Process history for each asset
            foreach (var asset in paginatedData)
            {
                if (asset.history != null && asset.history.Any())
                {
                    var historyUsers = new List<OwnerDTO>();

                    foreach (var ownerId in asset.history.Where(h => !string.IsNullOrEmpty(h)))
                    {
                        if (int.TryParse(ownerId, out int ownerIdInt))
                        {
                            var user = await _context.Users
                                .Where(u => u.id == ownerIdInt)
                                .Select(u => new OwnerDTO
                                {
                                    id = u.id,
                                    name = u.name ?? "",
                                    company = u.company ?? "",
                                    department = u.department ?? "",
                                    employee_id = u.employee_id ?? ""
                                })
                                .FirstOrDefaultAsync();

                            if (user != null)
                            {
                                historyUsers.Add(user);
                            }
                        }
                    }

                    // Add the history users inside the history data
                    asset.history = new List<string>
            {
                string.Join(", ", historyUsers.Select(h => h.name))
            };

                    // Include the historyUsers as well, for ease of access
                    asset.historyUsers = historyUsers;
                }
            }

            return new PaginatedResponse<AssetWithOwnerDTO>
            {
                Items = paginatedData,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }





        //for get by id assets endpoint
        public async Task<AssetWithOwnerDTO> GetAssetByIdAsync(
         int id,
         string sortOrder = "asc",
         string? searchTerm = null)
        {
            var query = _context.Assets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(asset =>
                    EF.Functions.Like(asset.asset_barcode, $"%{searchTerm}%") ||
                    EF.Functions.Like(asset.type, $"%{searchTerm}%") ||
                    EF.Functions.Like(asset.brand, $"%{searchTerm}%"));
            }

            // Determine sorting direction based on sortOrder
            if (sortOrder.ToLower() == "desc")
            {
                query = query.OrderByDescending(a => a.id); // Order by id in descending order
            }
            else
            {
                query = query.OrderBy(a => a.id); // Order by id in ascending order (default)
            }

            var asset = await query
                .Where(a => a.id == id)
                .Select(a => new AssetWithOwnerDTO
                {
                    id = a.id,
                    type = a.type,
                    date_acquired = a.date_acquired,
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
                    history = a.history ?? new List<string>(),  // Initialize if null
                    asset_image = a.asset_image,
                    owner_id = a.owner_id,
                    is_deleted = a.is_deleted,
                    date_created = a.date_created,
                    date_modified = a.date_modified,
                    status = a.status,
                    owner = a.owner_id != null ? new OwnerDTO
                    {
                        id = a.owner_id.Value,
                        name = _context.Users
                            .Where(u => u.id == a.owner_id)
                            .Select(u => u.name)
                            .FirstOrDefault(),
                        company = _context.Users
                            .Where(u => u.id == a.owner_id)
                            .Select(u => u.company)
                            .FirstOrDefault(),
                        department = _context.Users
                            .Where(u => u.id == a.owner_id)
                            .Select(u => u.department)
                            .FirstOrDefault(),
                        employee_id = _context.Users
                            .Where(u => u.id == a.owner_id)
                            .Select(u => u.employee_id)
                            .FirstOrDefault()
                    } : null,
                    historyUsers = new List<OwnerDTO>()  // Initialize empty list
                })
                .FirstOrDefaultAsync(); // Since we are fetching a single asset, use FirstOrDefaultAsync.

            // Process history for the asset if available
            if (asset?.history != null && asset.history.Any())
            {
                var historyUsers = new List<OwnerDTO>();

                foreach (var ownerId in asset.history.Where(h => !string.IsNullOrEmpty(h)))
                {
                    if (int.TryParse(ownerId, out int ownerIdInt))
                    {
                        var user = await _context.Users
                            .Where(u => u.id == ownerIdInt)
                            .Select(u => new OwnerDTO
                            {
                                id = u.id,
                                name = u.name ?? "",
                                company = u.company ?? "",
                                department = u.department ?? "",
                                employee_id = u.employee_id ?? ""
                            })
                            .FirstOrDefaultAsync();

                        if (user != null)
                        {
                            historyUsers.Add(user);
                        }
                    }
                }

                // Add the history users inside the history data
                asset.history = new List<string>
        {
            string.Join(", ", historyUsers.Select(h => h.name))
        };

                // Include the historyUsers as well, for ease of access
                asset.historyUsers = historyUsers;
            }

            return asset; // Return the single asset data
        }



        //for get by owner id endpoint
        // Asset service to get assets based on owner id
        public async Task<List<AssetWithOwnerDTO>> GetAssetsByOwnerIdAsync(int ownerId)
        {
            return await _context.Assets
                .Where(a => a.owner_id == ownerId)
                .Select(a => new AssetWithOwnerDTO
                {
                    id = a.id,
                    type = a.type,
                    date_acquired = a.date_acquired,
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
                    history = a.history,
                    asset_image = a.asset_image,
                    owner_id = a.owner_id,
                    is_deleted = a.is_deleted,
                    date_created = a.date_created,
                    date_modified = a.date_modified,
                    status = a.status,
                    owner = a.owner_id != null ? new OwnerDTO
                    {
                        id = a.id,
                        name = _context.Users.Where(u => u.id == a.owner_id).Select(u => u.name).FirstOrDefault(),
                        company = _context.Users.Where(u => u.id == a.owner_id).Select(u => u.company).FirstOrDefault(),
                        department = _context.Users.Where(u => u.id == a.owner_id).Select(u => u.department).FirstOrDefault(),
                        employee_id = _context.Users.Where(u => u.id == a.owner_id).Select(u => u.employee_id).FirstOrDefault()
                    } : null
                })
                .ToListAsync();
        }




        public async Task<Asset> UpdateAssetAsync(int assetId, UpdateAssetDto assetDto, int? ownerId, ClaimsPrincipal user)
        {
            try
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.id == assetId);

                if (asset == null)
                {
                    return null;
                }

                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                string originalData = JsonConvert.SerializeObject(asset, settings);

                // Check if the owner is changing
                if (ownerId.HasValue && asset.owner_id != ownerId)
                {
                    if (asset.history == null) asset.history = new List<string>();


                    // Only add to history if there was a previous owner
                    if (asset.owner_id.HasValue)
                    {
                        var previousOwnerName = await _context.Users
                            .Where(u => u.id == asset.owner_id)
                            .Select(u => u.name)
                            .FirstOrDefaultAsync();
                        asset.history.Add(previousOwnerName ?? "Unknown");

                        var previousOwnerAccountability = await _context.user_accountability_lists
                            .FirstOrDefaultAsync(al => al.owner_id == asset.owner_id);
                        if (previousOwnerAccountability != null)
                        {
                            var computerIds = previousOwnerAccountability.computer_ids?
                                .Split(',')
                                .Where(id => id != assetId.ToString())
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
                            computer_ids = asset.id.ToString(),
                        };
                        await _context.user_accountability_lists.AddAsync(newAccountabilityList);
                    }
                    else
                    {
                        newOwnerAccountability.computer_ids = string.IsNullOrWhiteSpace(newOwnerAccountability.computer_ids)
                            ? asset.id.ToString()
                            : $"{newOwnerAccountability.computer_ids},{asset.id}";
                        _context.user_accountability_lists.Update(newOwnerAccountability);
                    }

                    // Only update the owner_id if we're changing owners
                    asset.owner_id = ownerId;
                }


                //update other propertries only if they have valid values
                asset.type = string.IsNullOrEmpty(assetDto.type) ? asset.type : assetDto.type;
                asset.date_acquired = string.IsNullOrEmpty(assetDto.date_acquired) ? asset.date_acquired : assetDto.date_acquired;
                asset.asset_barcode = string.IsNullOrEmpty(assetDto.asset_barcode) ? asset.asset_barcode : assetDto.asset_barcode;
                asset.brand = string.IsNullOrEmpty(assetDto.brand) ? asset.brand : assetDto.brand;
                asset.model = string.IsNullOrEmpty(assetDto.model) ? asset.model : assetDto.model;
                asset.size = string.IsNullOrEmpty(assetDto.size) ? asset.size : assetDto.size;
                asset.color = string.IsNullOrEmpty(assetDto.color) ? asset.color : assetDto.color;
                asset.serial_no = string.IsNullOrEmpty(assetDto.serial_no) ? asset.serial_no : assetDto.serial_no;
                asset.po = string.IsNullOrEmpty(assetDto.po) ? asset.po : assetDto.po;
                asset.warranty = string.IsNullOrEmpty(assetDto.warranty) ? asset.warranty : assetDto.warranty;
                asset.cost = assetDto.cost != 0 ? asset.cost : asset.cost;
                asset.remarks = string.IsNullOrEmpty(assetDto.remarks) ? asset.remarks : assetDto.remarks;


                _context.Assets.Update(asset);


                string updatedData = JsonConvert.SerializeObject(asset, settings);

                var performedByUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

                // Log the update action in centralized logs table
                var centralizedLogs = new CentralizedLogs
                {
                    type = asset.type,
                    asset_barcode = asset.asset_barcode,
                    action = "Assets Updated",
                    performed_by_user_id = performedByUserId,
                    timestamp = DateTime.UtcNow,
                    details = $"Asset ID {asset.id} was updated by User ID {performedByUserId}. " +
                              $"Original Data: {originalData}, Updated Data: {updatedData}"
                };

                _context.centralized_logs.Add(centralizedLogs);

                // Save changes
                await _context.SaveChangesAsync();

                return asset;
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



        public async Task<ServiceResponse> DeleteAssetAsync(int id, ClaimsPrincipal user)
        {
            var asset = await _context.Assets.FirstOrDefaultAsync(a => a.id == id);

            if (asset == null)
            {
                return new ServiceResponse { Success = false, StatusCode = 404, Message = "Asset not found." };
            }

            if (asset.status == "INACTIVE" && asset.is_deleted)
            {
                return new ServiceResponse { Success = false, StatusCode = 409, Message = "Asset is already inactive and deleted." };
            }

            // Check if the asset is assigned in user_accountability_list
            var assignedUser = await _context.user_accountability_lists
                .FirstOrDefaultAsync(ua => ua.asset_ids != null && ua.asset_ids.Contains(id.ToString()));

            if (assignedUser != null)
            {
                var updatedAssetIds = assignedUser.asset_ids
                    .Split(',')
                    .Where(aid => aid.Trim() != id.ToString())
                    .ToArray();

                assignedUser.asset_ids = updatedAssetIds.Length > 0
                    ? string.Join(",", updatedAssetIds)
                    : null;

                _context.user_accountability_lists.Update(assignedUser);
            }

            // Set asset status to "INACTIVE" and mark it as deleted
            asset.status = "INACTIVE";
            asset.is_deleted = true;
            asset.date_modified = DateTime.UtcNow;

            _context.Assets.Update(asset);

            string performedByUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

            // Log the delete action in centralized_logs
            var centralizedLog = new CentralizedLogs
            {
                type = asset.type,
                asset_barcode = asset.asset_barcode,
                action = "DELETE",
                performed_by_user_id = performedByUserId,
                timestamp = DateTime.UtcNow,
                details = $"Asset ID {asset.id} was set to INACTIVE and marked as deleted by User ID {performedByUserId}."
            };

            _context.centralized_logs.Add(centralizedLog);

            await _context.SaveChangesAsync();

            return new ServiceResponse { Success = true, StatusCode = 200, Message = "Asset set to INACTIVE and marked as deleted successfully." };
        }


    }
}
