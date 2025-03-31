using ITAM.DataContext;
using ITAM.Models;
using ITAM.Models.Logs;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using System.Security.Claims;

namespace ITAM.Services.AssetImportService
{
    public class AssetImportService
    {
        private readonly AppDbContext _context;

        public AssetImportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> ImportAssetsAsync(IFormFile file, ClaimsPrincipal userClaims)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded.");

            var computerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CPU", "CPU CORE i7 10th GEN", "CPU INTEL CORE i5",
                "Laptop", "Laptop Macbook AIR, NB 15S-DUI537TU"
            };

            int accountabilityCodeCounter = 1;
            int trackingCodeCounter = 1;

            string performedByUserId = userClaims?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    if (IsRowEmpty(worksheet, row))
                        continue;

                    var assetType = GetCellValue(worksheet.Cells[row, 4]);
                    if (string.IsNullOrWhiteSpace(assetType))
                        continue;

                    var user = await EnsureUserAsync(worksheet, row);
                    var dateAcquired = ParseDate(GetCellValue(worksheet.Cells[row, 5]));
                    var liDescription = BuildDescription(worksheet, row);
                    var assetBarcode = GetCellValue(worksheet.Cells[row, 6]);

                    var history = new List<string>
            {
                GetCellValue(worksheet.Cells[row, 19]),
                GetCellValue(worksheet.Cells[row, 20]),
                GetCellValue(worksheet.Cells[row, 21]),
                GetCellValue(worksheet.Cells[row, 22]),
                GetCellValue(worksheet.Cells[row, 23]),
                GetCellValue(worksheet.Cells[row, 24]),
                GetCellValue(worksheet.Cells[row, 25])
            }.Where(h => !string.IsNullOrWhiteSpace(h)).ToList();

                    string status = user != null ? "ACTIVE" : "INACTIVE";

                    if (computerTypes.Contains(assetType))
                    {
                        var computer = new Computer
                        {
                            type = assetType,
                            date_acquired = dateAcquired,
                            asset_barcode = assetBarcode,
                            brand = GetCellValue(worksheet.Cells[row, 7]),
                            model = GetCellValue(worksheet.Cells[row, 8]),
                            ram = GetCellValue(worksheet.Cells[row, 9]),
                            ssd = GetCellValue(worksheet.Cells[row, 10]),
                            hdd = GetCellValue(worksheet.Cells[row, 11]),
                            gpu = GetCellValue(worksheet.Cells[row, 12]),
                            board = GetCellValue(worksheet.Cells[row, 13]),
                            color = GetCellValue(worksheet.Cells[row, 14]),
                            serial_no = GetCellValue(worksheet.Cells[row, 15]),
                            po = GetCellValue(worksheet.Cells[row, 16]),
                            warranty = GetCellValue(worksheet.Cells[row, 17]),
                            cost = TryParseDecimal(worksheet.Cells[row, 18]) ?? 0,
                            remarks = GetCellValue(worksheet.Cells[row, 26]),
                            size = GetCellValue(worksheet.Cells[row, 27]),
                            owner_id = user?.id,
                            date_created = DateTime.UtcNow,
                            li_description = liDescription,
                            history = history,
                            status = status
                        };

                        _context.computers.Add(computer);
                        await _context.SaveChangesAsync();

                        await LogToCentralizedSystem(
                            computer.type,
                            computer.asset_barcode,
                            "Created",
                            performedByUserId,
                            "Imported from file"
                        );

                        var computerComponents = await StoreInComputerComponentsAsync(worksheet, row, assetType, user, computer);

                        foreach (var component in computerComponents)
                        {
                            await LogToCentralizedSystem(
                                component.type,
                                component.uid,
                                "Created",
                                performedByUserId,
                                "Imported from file"
                            );
                        }

                        (accountabilityCodeCounter, trackingCodeCounter) = await UpdateUserAccountabilityListAsync(user, computer, accountabilityCodeCounter, trackingCodeCounter);
                    }
                    else
                    {
                        var rootComputerIds = await GetRootComputerIdsAsync(user.id);

                        // Fetch computers owned by the user
                        var userComputers = await _context.computers
                            .Where(c => c.owner_id == user.id)
                            .ToListAsync();

                        int? linkedComputerId = null;

                        Console.WriteLine($"🔍 Checking assigned assets for asset barcode: {assetBarcode}");

                        // Convert assetBarcode to integer
                        if (!string.IsNullOrWhiteSpace(assetBarcode) && int.TryParse(assetBarcode, out int assetId))
                        {
                            foreach (var computer in userComputers)
                            {
                                // Ensure assigned assets are integers
                                var assignedAssetIds = computer.assigned_assets?
                                    .Select(a => int.TryParse(a.ToString(), out int val) ? val : -1)
                                    .ToList() ?? new List<int>();

                                Console.WriteLine($"💻 Computer ID: {computer.id}, Assigned Assets: {string.Join(",", assignedAssetIds)}");

                                if (assignedAssetIds.Contains(assetId))
                                {
                                    linkedComputerId = computer.id;
                                    Console.WriteLine($"✅ Linked asset {assetId} to computer ID {linkedComputerId}");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Warning: Asset barcode '{assetBarcode}' is not a valid integer.");
                        }

                        var asset = new Asset
                        {
                            type = assetType,
                            date_acquired = dateAcquired,
                            asset_barcode = assetBarcode,
                            brand = GetCellValue(worksheet.Cells[row, 7]),
                            model = GetCellValue(worksheet.Cells[row, 8]),
                            size = GetCellValue(worksheet.Cells[row, 13]),
                            color = GetCellValue(worksheet.Cells[row, 14]),
                            serial_no = GetCellValue(worksheet.Cells[row, 15]),
                            po = GetCellValue(worksheet.Cells[row, 16]),
                            warranty = GetCellValue(worksheet.Cells[row, 17]),
                            cost = TryParseDecimal(worksheet.Cells[row, 18]) ?? 0,
                            remarks = GetCellValue(worksheet.Cells[row, 26]),
                            owner_id = user.id,
                            date_created = DateTime.UtcNow,
                            li_description = liDescription,
                            history = history,
                            root_history = rootComputerIds,
                            status = status,
                            computer_id = linkedComputerId // Assigning computer ID based on assigned_assets
                        };

                        Console.WriteLine($"🔄 Saving asset {assetBarcode} with computer_id: {linkedComputerId}");

                        _context.Assets.Add(asset);
                        await _context.SaveChangesAsync();

                        await LogToCentralizedSystem(
                            asset.type,
                            asset.asset_barcode,
                            "Created",
                            performedByUserId,
                            "Imported from file"
                        );

                        (accountabilityCodeCounter, trackingCodeCounter) = await UpdateUserAccountabilityListAsync(user, asset, accountabilityCodeCounter, trackingCodeCounter);
                    }
                }
            }

            await UpdateComputersAssignedAssets();
            return "Import completed successfully.";
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
                performed_by_user_id = string.IsNullOrEmpty(performedByUserId) ? "SYSTEM" : performedByUserId,
                timestamp = DateTime.UtcNow,
                details = details
            };

            _context.centralized_logs.Add(centralizedLog);
            await _context.SaveChangesAsync();
        }


        // Helper method to get component type
        private string GetComponentType(string? ram, string? ssd, string? hdd, string? gpu)
        {
            if (!string.IsNullOrWhiteSpace(ram)) return "RAM";
            if (!string.IsNullOrWhiteSpace(ssd)) return "SSD";
            if (!string.IsNullOrWhiteSpace(hdd)) return "HDD";
            if (!string.IsNullOrWhiteSpace(gpu)) return "GPU";
            return "Unknown Component";
        }

        //Helper method to get root computer id
        private async Task<List<int>> GetRootComputerIdsAsync(int userId)
        {
            // Fetch computer_ids as strings from the database
            var computerIdStrings = await _context.user_accountability_lists
                .Where(ua => ua.owner_id == userId && ua.computer_ids != null)
                .Select(ua => ua.computer_ids)
                .ToListAsync();

            // Process in memory: split, filter valid numbers, convert to integers
            return computerIdStrings
                .SelectMany(ids => ids.Split(',')
                                      .Select(id => int.TryParse(id, out var parsedId) ? parsedId : (int?)null))
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();
        }


        //helper to update accountability list
        private async Task UpdateComputersAssignedAssets()
        {
            var accountabilityList = await _context.user_accountability_lists.ToListAsync();

            // Dictionary to track which computer has which assets
            Dictionary<int, List<int>> computerToAssets = new Dictionary<int, List<int>>();

            // First loop: Update the assigned_assets in computers
            foreach (var entry in accountabilityList)
            {
                var assetIds = entry.asset_ids.Split(',')
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(int.Parse)
                    .ToList();

                foreach (var computerId in entry.computer_ids.Split(',')
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(int.Parse))
                {
                    var computer = await _context.computers.FindAsync(computerId);
                    if (computer != null)
                    {
                        computer.assigned_assets = assetIds;
                        _context.computers.Update(computer);

                        // Track which computer has which assets
                        computerToAssets[computerId] = assetIds;
                    }
                }
            }

            // Second loop: Update the computer_id in each asset based on assigned_assets
            foreach (var computerEntry in computerToAssets)
            {
                int computerId = computerEntry.Key;
                List<int> assetIds = computerEntry.Value;

                foreach (var assetId in assetIds)
                {
                    var asset = await _context.Assets.FindAsync(assetId);
                    if (asset != null)
                    {
                        asset.computer_id = computerId;
                        _context.Assets.Update(asset);
                        Console.WriteLine($"✅ Updated asset ID {assetId} with computer_id: {computerId}");
                    }
                }
            }

            await _context.SaveChangesAsync();
        }



        // Helper to check if a row is empty
        private bool IsRowEmpty(ExcelWorksheet worksheet, int row)
        {
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Text))
                    return false;
            }
            return true;
        }

        // Helper to safely get a cell value or return null
        private string GetCellValue(ExcelRange cell)
        {
            var value = cell.Text.Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        // Helper to safely parse a decimal or return null
        private decimal? TryParseDecimal(ExcelRange cell)
        {
            return decimal.TryParse(cell.Text.Trim(), out var result) ? result : (decimal?)null;
        }



        // Method to store components like RAM, SSD, etc., in the ComputerComponents table
        private async Task<List<ComputerComponents>> StoreInComputerComponentsAsync(ExcelWorksheet worksheet, int row, string assetType, User user, Computer computer)
        {
            var assetBarcode = worksheet.Cells[row, 6].Text.Trim();
            var ownerId = user.id;
            var storedComponents = new List<ComputerComponents>();

            // Define headers that you want to store as 'type'
            var headers = new string[] { "RAM", "SSD", "HDD", "GPU", "BOARD" };
            var values = new string[]
            {
            worksheet.Cells[row, 9].Text.Trim(),  // 'RAM'
            worksheet.Cells[row, 10].Text.Trim(), // 'SSD'
            worksheet.Cells[row, 11].Text.Trim(), // 'HDD'
            worksheet.Cells[row, 12].Text.Trim(),  // 'GPU'
            worksheet.Cells[row, 13].Text.Trim()  // 'BOARD'

            };

            // Fetch the last used UID from the computer_components table
            var lastUid = await _context.computer_components
                .OrderByDescending(c => c.id)
                .FirstOrDefaultAsync();

            int lastUidIndex = lastUid != null ? int.Parse(lastUid.uid.Split('-')[1]) : 0;

            string GenerateUID(int uidIndex) => $"UID-{uidIndex:D3}";

            // Loop through the headers and values to create components
            for (int i = 0; i < headers.Length; i++)
            {
                var description = values[i];

                // Only create a component if the description is not empty
                if (!string.IsNullOrWhiteSpace(description))
                {
                    var component = new ComputerComponents
                    {
                        type = headers[i],
                        description = description,
                        asset_barcode = assetBarcode,
                        status = ownerId != null ? "ACTIVE" : "INACTIVE",
                        history = new List<string> { computer.id.ToString() },
                        owner_id = ownerId,
                        computer_id = computer.id,
                        uid = GenerateUID(++lastUidIndex),
                        date_acquired = computer.date_acquired // ✅ Store same date_acquired as computer
                    };

                    _context.computer_components.Add(component);
                    storedComponents.Add(component);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine("Data saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while saving: {ex.Message}");
            }

            // Update the computer entity with the UIDs of components (RAM, SSD, HDD, GPU, BOARD)
            var components = await _context.computer_components
                .Where(c => c.computer_id == computer.id && headers.Contains(c.type))
                .ToListAsync();

            var componentDict = components.ToDictionary(c => c.type, c => c.uid);

            computer.ram = componentDict.ContainsKey("RAM") ? componentDict["RAM"] : null;
            computer.ssd = componentDict.ContainsKey("SSD") ? componentDict["SSD"] : null;
            computer.hdd = componentDict.ContainsKey("HDD") ? componentDict["HDD"] : null;
            computer.gpu = componentDict.ContainsKey("GPU") ? componentDict["GPU"] : null;
            computer.board = componentDict.ContainsKey("BOARD") ? componentDict["BOARD"] : null;


            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine("Computer updated with UIDs successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while updating computer with UIDs: {ex.Message}");
            }

            return storedComponents;
        }



        private async Task<User> EnsureUserAsync(ExcelWorksheet worksheet, int row)
        {
            var userName = worksheet.Cells[row, 1].Text.Trim();
            var company = worksheet.Cells[row, 2].Text.Trim();
            var department = worksheet.Cells[row, 3].Text.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.name == userName && u.company == company && u.department == department);

            if (user == null)
            {
                user = new User
                {
                    name = userName,
                    company = company,
                    department = department,
                    date_created = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            return user;
        }


        private string ParseDate(string dateCellValue)
        {
            if (double.TryParse(dateCellValue, out var serialDate))
            {
                var date = DateTime.FromOADate(serialDate);
                return date.ToString("MM/dd/yyyy");
            }
            else if (!string.IsNullOrWhiteSpace(dateCellValue))
            {
                if (DateTime.TryParseExact(dateCellValue, "MM/dd/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    return parsedDate.ToString("MM/dd/yyyy");
                }
            }

            return "Invalid Date";
        }

        private string BuildDescription(ExcelWorksheet worksheet, int row)
        {
            var descriptionParts = new[] {
                worksheet.Cells[row, 7].Text?.Trim(),
                worksheet.Cells[row, 4].Text?.Trim(),
                worksheet.Cells[row, 8].Text?.Trim(),
                worksheet.Cells[row, 9].Text?.Trim(),
                worksheet.Cells[row, 10].Text?.Trim(),
                worksheet.Cells[row, 11].Text?.Trim(),
                worksheet.Cells[row, 12].Text?.Trim(),
                worksheet.Cells[row, 13].Text?.Trim(),
                worksheet.Cells[row, 14].Text?.Trim()
            };

            return string.Join(" ", descriptionParts.Where(part => !string.IsNullOrWhiteSpace(part))).Trim() ?? "No description available";
        }



        private async Task<(int AccountabilityCodeCounter, int TrackingCodeCounter)> UpdateUserAccountabilityListAsync(
     User user, object assetOrComputer, int accountabilityCodeCounter, int trackingCodeCounter)
        {
            int assetId = 0;
            int computerId = 0;

            if (assetOrComputer is Asset asset)
            {
                assetId = asset.id;
            }
            else if (assetOrComputer is Computer computer)
            {
                computerId = computer.id;
            }
            else
            {
                throw new ArgumentException("Invalid asset or computer type.");
            }

            var userAccountabilityList = await _context.user_accountability_lists
                .FirstOrDefaultAsync(ual => ual.owner_id == user.id);

            bool isNewRecord = userAccountabilityList == null;

            if (isNewRecord)
            {
                userAccountabilityList = new UserAccountabilityList
                {
                    accountability_code = $"ACID-{accountabilityCodeCounter:D4}",
                    tracking_code = $"TRID-{trackingCodeCounter:D4}",
                    owner_id = user.id,
                    asset_ids = assetId > 0 ? assetId.ToString() : "",
                    computer_ids = computerId > 0 ? computerId.ToString() : "",
                    date_created = DateTime.UtcNow, // Ensure this is always set
                    date_modified = null,          // Ensure this is null initially
                    is_active = true // This will be stored as 1 in the database
                };
                _context.user_accountability_lists.Add(userAccountabilityList);
                accountabilityCodeCounter++;
                trackingCodeCounter++;
            }
            else
            {
                if (!string.IsNullOrEmpty(userAccountabilityList.asset_ids))
                {
                    var existingAssetIds = userAccountabilityList.asset_ids
                        .Split(',')
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Select(id => int.TryParse(id, out var parsedId) ? parsedId : 0)
                        .Where(id => id > 0)
                        .ToList();

                    if (assetId > 0 && !existingAssetIds.Contains(assetId))
                    {
                        existingAssetIds.Add(assetId);
                    }

                    userAccountabilityList.asset_ids = string.Join(",", existingAssetIds);
                }
                else if (assetId > 0)
                {
                    userAccountabilityList.asset_ids = assetId.ToString();
                }

                if (!string.IsNullOrEmpty(userAccountabilityList.computer_ids))
                {
                    var existingComputerIds = userAccountabilityList.computer_ids
                        .Split(',')
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Select(id => int.TryParse(id, out var parsedId) ? parsedId : 0)
                        .Where(id => id > 0)
                        .ToList();

                    if (computerId > 0 && !existingComputerIds.Contains(computerId))
                    {
                        existingComputerIds.Add(computerId);
                    }

                    userAccountabilityList.computer_ids = string.Join(",", existingComputerIds);
                }
                else if (computerId > 0)
                {
                    userAccountabilityList.computer_ids = computerId.ToString();
                }

                userAccountabilityList.is_active = true; // Ensure it's set to true when updated
            }

            await _context.SaveChangesAsync();
            return (accountabilityCodeCounter, trackingCodeCounter);
        }

    }
}
