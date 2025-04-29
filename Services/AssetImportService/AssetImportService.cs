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

            var computerTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DESKTOP", "LAPTOP" };
            int accountabilityCodeCounter = 1;
            int trackingCodeCounter = 1;
            string performedByUserId = userClaims?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

            using (var stream = file.OpenReadStream())
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;

                // First pass - process computers and create them if they don't exist
                for (int row = 2; row <= rowCount; row++)
                {
                    if (IsRowEmpty(worksheet, row)) continue;

                    var assetType = GetCellValue(worksheet.Cells[row, 4]);
                    if (string.IsNullOrWhiteSpace(assetType)) continue;

                    var assetBarcode = GetCellValue(worksheet.Cells[row, 6]);
                    if (string.IsNullOrWhiteSpace(assetBarcode)) continue;

                    if (computerTypes.Contains(assetType))
                    {
                        var user = await EnsureUserAsync(worksheet, row);
                        var computer = await _context.computers
                            .FirstOrDefaultAsync(c => c.asset_barcode == assetBarcode);

                        if (computer == null)
                        {
                            var dateAcquired = ParseDate(GetCellValue(worksheet.Cells[row, 5]));
                            var liDescription = BuildDescription(worksheet, row);

                            computer = new Computer
                            {
                                type = assetType,
                                date_acquired = dateAcquired,
                                asset_barcode = assetBarcode,
                                brand = GetCellValue(worksheet.Cells[row, 8]),
                                model = GetCellValue(worksheet.Cells[row, 9]),
                                color = GetCellValue(worksheet.Cells[row, 27]),
                                po = GetCellValue(worksheet.Cells[row, 28]),
                                warranty = GetCellValue(worksheet.Cells[row, 30]),
                                cost = TryParseDecimal(worksheet.Cells[row, 31]) ?? 0,
                                remarks = GetCellValue(worksheet.Cells[row, 39]),
                                size = GetCellValue(worksheet.Cells[row, 40]),
                                owner_id = user?.id,
                                date_created = DateTime.UtcNow,
                                li_description = liDescription,
                                status = user != null ? "ACTIVE" : "INACTIVE"
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
                        }
                    }
                }

                // Second pass - process components and assets
                for (int row = 2; row <= rowCount; row++)
                {
                    if (IsRowEmpty(worksheet, row)) continue;

                    var assetType = GetCellValue(worksheet.Cells[row, 4]);
                    if (string.IsNullOrWhiteSpace(assetType)) continue;

                    var assetBarcode = GetCellValue(worksheet.Cells[row, 6]);
                    var user = await EnsureUserAsync(worksheet, row);
                    var dateAcquired = ParseDate(GetCellValue(worksheet.Cells[row, 5]));
                    var liDescription = BuildDescription(worksheet, row);

                    var history = new List<string>
                    {
                        GetCellValue(worksheet.Cells[row, 32]),
                        GetCellValue(worksheet.Cells[row, 33]),
                        GetCellValue(worksheet.Cells[row, 34]),
                        GetCellValue(worksheet.Cells[row, 35]),
                        GetCellValue(worksheet.Cells[row, 36]),
                        GetCellValue(worksheet.Cells[row, 37]),
                        GetCellValue(worksheet.Cells[row, 38])
                    }.Where(h => !string.IsNullOrWhiteSpace(h)).ToList();

                    string status = user != null ? "ACTIVE" : "INACTIVE";

                    if (computerTypes.Contains(assetType))
                    {
                        var computer = await _context.computers
                            .FirstOrDefaultAsync(c => c.asset_barcode == assetBarcode);

                        if (computer != null)
                        {
                            await ProcessComputerComponents(worksheet, row, user, computer);
                            (accountabilityCodeCounter, trackingCodeCounter) = await UpdateUserAccountabilityListAsync(user, computer, accountabilityCodeCounter, trackingCodeCounter);
                        }
                    }
                    else
                    {
                        await ProcessNonComputerAsset(worksheet, row, user, dateAcquired, liDescription,
                            assetBarcode, assetType, history, status, accountabilityCodeCounter,
                            trackingCodeCounter, performedByUserId);
                    }
                }
            }

            await UpdateComputersAssignedAssets();
            return "Import completed successfully.";
        }

        private async Task ProcessNonComputerAsset(ExcelWorksheet worksheet, int row, User user,
            string dateAcquired, string liDescription, string assetBarcode, string assetType,
            List<string> history, string status, int accountabilityCodeCounter, int trackingCodeCounter,
            string performedByUserId)
        {
            var rootComputerIds = await GetRootComputerIdsAsync(user.id);
            var userComputers = await _context.computers
                .Where(c => c.owner_id == user.id)
                .ToListAsync();

            int? linkedComputerId = null;

            if (!string.IsNullOrWhiteSpace(assetBarcode) && int.TryParse(assetBarcode, out int assetId))
            {
                foreach (var computer in userComputers)
                {
                    var assignedAssetIds = computer.assigned_assets?
                        .Select(a => int.TryParse(a.ToString(), out int val) ? val : -1)
                        .ToList() ?? new List<int>();

                    if (assignedAssetIds.Contains(assetId))
                    {
                        linkedComputerId = computer.id;
                        break;
                    }
                }
            }

            var asset = new Asset
            {
                type = assetType,
                date_acquired = dateAcquired,
                asset_barcode = assetBarcode,
                brand = GetCellValue(worksheet.Cells[row, 8]),
                model = GetCellValue(worksheet.Cells[row, 9]),
                size = GetCellValue(worksheet.Cells[row, 40]),
                color = GetCellValue(worksheet.Cells[row, 27]),
                po = GetCellValue(worksheet.Cells[row, 28]),
                warranty = GetCellValue(worksheet.Cells[row, 30]),
                cost = TryParseDecimal(worksheet.Cells[row, 31]) ?? 0,
                remarks = GetCellValue(worksheet.Cells[row, 39]),
                owner_id = user.id,
                date_created = DateTime.UtcNow,
                li_description = liDescription,
                history = history,
                root_history = rootComputerIds,
                status = status,
                computer_id = linkedComputerId
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            await LogToCentralizedSystem(
                asset.type,
                asset.asset_barcode,
                "Created",
                performedByUserId,
                "Imported from file"
            );

            (accountabilityCodeCounter, trackingCodeCounter) = await UpdateUserAccountabilityListAsync(
                user, asset, accountabilityCodeCounter, trackingCodeCounter);
        }

        private async Task ProcessComputerComponents(ExcelWorksheet worksheet, int row, User user, Computer computer)
        {
            // FIXED: Corrected column mappings based on the Excel file structure
            // Format: Component Type -> (description column, inventory code column)
            var componentMappings = new Dictionary<string, (int descriptionCol, int invtCodeCol)>
            {
                {"RAM", (10, 11)},     // RAM:     descriptionCol = 10 (RAM), invtCodeCol = 11 (RAM INVT.CODE)
                {"SSD", (12, 13)},     // SSD:     descriptionCol = 12 (SSD), invtCodeCol = 13 (SSD INVT.CODE)
                {"HDD", (14, 15)},     // HDD:     descriptionCol = 14 (HDD), invtCodeCol = 15 (HDD INVT.CODE)
                {"GPU", (16, 17)},     // GPU:     descriptionCol = 16 (GPU), invtCodeCol = 17 (GPU INVT.CODE)
                {"BOARD", (18, 19)},   // BOARD:   descriptionCol = 18 (BOARD), invtCodeCol = 19 (BOARD INVT.CODE)
                {"PSU", (20, 21)},     // PSU:     descriptionCol = 20 (PSU), invtCodeCol = 21 (PSU INVT.CODE)
                {"CPU", (22, 23)},     // CPU:     descriptionCol = 22 (CPU), invtCodeCol = 23 (CPU INVT.CODE)
                {"CPU FAN", (24, 25)}, // CPU FAN: descriptionCol = 24 (CPU FAN), invtCodeCol = 25 (CPU FAN INVT.CODE)
                {"CD ROM", (26, 27)}   // CD ROM:  descriptionCol = 26 (CD ROM), invtCodeCol = 27 (CD ROM INVT.CODE)
            };

            foreach (var mapping in componentMappings)
            {
                var componentType = mapping.Key;
                var description = GetCellValue(worksheet.Cells[row, mapping.Value.descriptionCol]);
                var inventoryCode = GetCellValue(worksheet.Cells[row, mapping.Value.invtCodeCol]);

                if (!string.IsNullOrWhiteSpace(description) || !string.IsNullOrWhiteSpace(inventoryCode))
                {
                    // Store in computer_components table
                    var existingComponent = await _context.computer_components
                        .FirstOrDefaultAsync(cc => cc.computer_id == computer.id &&
                                                  cc.type == componentType);

                    if (existingComponent == null)
                    {
                        var component = new ComputerComponents
                        {
                            type = componentType,
                            description = description ?? string.Empty,     // e.g., "KINGSTON 8GB DDR4"
                            uid = inventoryCode,                           // e.g., "2025-A0103-00002"
                            asset_barcode = computer.asset_barcode,
                            status = user != null ? "ACTIVE" : "INACTIVE",
                            history = new List<string> { computer.id.ToString() },
                            owner_id = user?.id,
                            computer_id = computer.id,
                            date_acquired = computer.date_acquired
                        };

                        _context.computer_components.Add(component);
                    }
                    else
                    {
                        // Update existing component if needed
                        existingComponent.description = description ?? existingComponent.description;
                        existingComponent.uid = inventoryCode ?? existingComponent.uid;
                    }

                    // FIXED: Store in computers table with correct inventory codes
                    switch (componentType)
                    {
                        case "RAM":
                            computer.ram = inventoryCode;        // RAM description (from column 10)
                            break;
                        case "SSD":
                            computer.ssd = inventoryCode;        // SSD description (from column 12)
                            break;
                        case "HDD":
                            computer.hdd = inventoryCode;        // HDD description (from column 14)
                            break;
                        case "GPU":
                            computer.gpu = inventoryCode;        // GPU description (from column 16)
                            break;
                        case "BOARD":
                            computer.board = inventoryCode;      // BOARD description (from column 18)
                            break;
                        case "PSU":
                            computer.psu = inventoryCode;        // PSU description (from column 20)
                            break;
                        case "CPU":
                            computer.cpu = inventoryCode;        // CPU description (from column 22)
                            break;
                        case "CPU FAN":
                            computer.cpu_fan = inventoryCode;    // CPU FAN description (from column 24)
                            break;
                        case "CD ROM":
                            computer.cd_rom = inventoryCode;     // CD ROM description (from column 26)
                            break;
                    }
                }
            }

            await _context.SaveChangesAsync();
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
