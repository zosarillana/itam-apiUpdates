using ITAM.DataContext;
using ITAM.Services.ComputerService;
using ITAM.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ITAM.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ITAM.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class ComputerController : ControllerBase
    {
        private readonly ComputerService _computerService;
        private readonly AppDbContext _context;
        private readonly UserService _userService;

        public ComputerController(ComputerService computerService, AppDbContext context, UserService userService)
        {
            _computerService = computerService;
            _context = context;
            _userService = userService;
        }

        [Authorize]
        // Endpoint for assigning owner to a computer
        [HttpPost("assign-owner-computer")]
        public async Task<IActionResult> AssignOwnerToComputer([FromBody] AssignOwnerforComputerDto assignOwnerforComputerDto)
        {
            if (assignOwnerforComputerDto == null || assignOwnerforComputerDto.computer_id == 0 || assignOwnerforComputerDto.owner_id == 0)
            {
                return BadRequest("Invalid data.");
            }

            try
            {
                var userClaims = User;
                var result = await _computerService.AssignOwnerToComputerAsync(assignOwnerforComputerDto, userClaims);

                return Ok(new { message = "Owner assigned successfully to the computer.", result });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error assigning owner: {ex.Message}");
            }
        }

        [Authorize]
        // Get all computers
        [HttpGet("ComputerItems")]
        public async Task<IActionResult> GetAllComputers(
            int pageNumber = 1,
            int pageSize = 10,
            string sortOrder = "asc",
            string? searchTerm = null)
        {
            try
            {
                var response = await _computerService.GetAllComputersAsync(pageNumber, pageSize, sortOrder, searchTerm);

                if (response == null || !response.Items.Any())
                {
                    return Ok(new { Items = new List<object>(), TotalItems = 0, PageNumber = pageNumber, PageSize = pageSize });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving computers: {ex.Message}");
            }
        }

        [Authorize]
        // Get all vacant computers
        [HttpGet("vacant")]
        public async Task<IActionResult> GetVacantComputers()
        {
            try
            {
                var componentDescriptionsQuery = _context.computer_components
                    .Where(cc => cc.computer_id != null)
                    .GroupBy(cc => cc.computer_id)
                    .Select(g => new
                    {
                        ComputerId = g.Key,
                        RamDescription = g.Where(cc => cc.type == "RAM").Select(cc => cc.description).FirstOrDefault(),
                        SsdDescriptions = g.Where(cc => cc.type == "SSD").Select(cc => cc.description).ToList(),
                        HddDescription = g.Where(cc => cc.type == "HDD").Select(cc => cc.description).FirstOrDefault(),
                        GpuDescription = g.Where(cc => cc.type == "GPU").Select(cc => cc.description).FirstOrDefault()
                    });

                var vacantComputers = await _context.computers
                    .Where(c => c.owner_id == null)
                    .Select(computer => new
                    {
                        computer.id,
                        computer.asset_barcode,
                        computer.type,
                        computer.date_acquired,
                        computer.brand,
                        computer.model,
                        ram = (from desc in componentDescriptionsQuery
                               where desc.ComputerId == computer.id
                               select desc.RamDescription).FirstOrDefault(),
                        ssd = string.Join(", ", (from desc in componentDescriptionsQuery
                                                 where desc.ComputerId == computer.id
                                                 select desc.SsdDescriptions).FirstOrDefault() ?? new List<string>()),
                        hdd = (from desc in componentDescriptionsQuery
                               where desc.ComputerId == computer.id
                               select desc.HddDescription).FirstOrDefault(),
                        gpu = (from desc in componentDescriptionsQuery
                               where desc.ComputerId == computer.id
                               select desc.GpuDescription).FirstOrDefault(),
                        computer.size,
                        computer.color,
                        computer.serial_no,
                        computer.po,
                        computer.warranty,
                        computer.cost,
                        computer.remarks,
                        computer.li_description,
                        computer.history,
                        computer.asset_image,
                        computer.is_deleted,
                        computer.date_created,
                        computer.date_modified,
                        computer.status
                    })
                    .ToListAsync();

                if (vacantComputers == null || vacantComputers.Count == 0)
                {
                    return NotFound(new { message = "No vacant computers available." });
                }

                return Ok(vacantComputers);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving vacant computers: {ex.Message}");
            }
        }

        [Authorize]
        // Get computer by ID
        [HttpGet("Computers/{id}")]
        public async Task<IActionResult> GetComputerById(int id)
        {
            try
            {
                var computer = await _computerService.GetComputerByIdAsync(id);

                if (computer == null)
                {
                    return NotFound(new { message = $"Computer with ID {id} not found." });
                }

                return Ok(computer);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving computer: {ex.Message}");
            }
        }

        [Authorize]
        // Get endpoint to fetch only computers based on owner ID
        [HttpGet("computers/owner/{owner_id}")]
        public async Task<IActionResult> GetComputersByOwnerId(int owner_id)
        {
            try
            {
                var computers = await _computerService.GetComputersByOwnerIdAsync(owner_id);

                if (computers == null || !computers.Any())
                {
                    return NotFound(new { message = "No computers found for this owner." });
                }

                return Ok(computers);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving computers: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPut("computers/pullout/{computerId}")]
        public async Task<IActionResult> PullOutComputer(int computerId)
        {
            try
            {
                var result = await _computerService.PullOutComputerAsync(computerId);

                if (!result)
                {
                    return NotFound(new { message = "Computer not found." });
                }

                return Ok(new { message = "Computer pulled out successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error pulling out computer: {ex.Message}" });
            }
        }

        [Authorize]
        // Update computer details
        [HttpPut("update-computer/{computer_id}")]
        public async Task<IActionResult> UpdateComputer(int computer_id, [FromBody] UpdateComputerDto computerDto)
        {
            if (computerDto == null)
            {
                return BadRequest("Invalid data.");
            }
            try
            {
                // Get existing computer to preserve owner if needed
                var existingComputer = await _context.computers.FirstOrDefaultAsync(c => c.id == computer_id);
                if (existingComputer == null)
                {
                    return NotFound(new { message = "Computer not found." });
                }

                // This will return null if all user fields are empty
                var newOwnerId = await _userService.GetOrCreateUserAsync(computerDto);

                // Use the existing owner_id if no new owner is specified
                int? ownerId = newOwnerId ?? existingComputer.owner_id;

                var updatedComputer = await _computerService.UpdateComputerAsync(computer_id, computerDto, ownerId, HttpContext.User);

                if (updatedComputer == null)
                {
                    return NotFound(new { message = "Computer not found." });
                }

                return Ok(new { message = "Computer updated successfully.", computer = updatedComputer });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating computer: {ex.Message}");
            }
        }

        [Authorize]
        // Delete a computer
        [HttpDelete("delete-computer/{id}")]
        public async Task<IActionResult> DeleteComputer(int id)
        {
            try
            {
                var user = HttpContext.User;
                var result = await _computerService.DeleteComputerAsync(id, user);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new { message = result.Message });
                }

                return Ok(new { message = "Computer deleted successfully.", computerId = id });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting computer: {ex.Message}");
            }
        }
    }
}
