using ITAM.DataContext;
using ITAM.DTOs;
using ITAM.Models;
using ITAM.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ITAM.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.password))
            {
                return BadRequest("User data is invalid.");
            }

            user.password = PasswordHasher.HashPassword(user.password);
            user.date_created = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            user.password = null;

            return CreatedAtAction(nameof(GetUser), new { id = user.id }, user);
        }

        [Authorize]
        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.password = null;
            return user;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers(
        int pageNumber = 1,
        int pageSize = 10,
        string sortOrder = "asc",
        string? searchTerm = null)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(u => u.name.Contains(searchTerm) || u.employee_id.Contains(searchTerm));
                }

                // Apply sorting
                query = sortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.name)
                    : query.OrderBy(u => u.name);

                // Apply pagination
                var users = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (users == null || users.Count == 0)
                {
                    return NotFound(new { message = "No Users found." });
                }

                // Nullify passwords for security reasons
                users.ForEach(user => user.password = null);

                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error retrieving users: {ex.Message}" });
            }
        }

        [Authorize]
        // Add this method to fetch the current logged-in user
        [HttpGet("current")]
        public async Task<ActionResult<User>> GetCurrentUser()
        {
            // Try to get user ID from session
            var userId = HttpContext.Session.GetInt32("UserId");

            // If session doesn't have user ID, check if you have it in claims/auth token
            if (userId == null && User.Identity?.IsAuthenticated == true)
            {
                // If using JWT or other auth, get the ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
                {
                    userId = id;
                }
            }

            if (userId == null)
            {
                return Unauthorized(new { message = "User is not logged in." });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            user.password = null;
            return user;
        }

        [Authorize]
        // PUT: api/Users/5/activate
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.is_active = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        // PUT: api/Users/5/deactivate
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.is_active = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        [HttpPut("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
        {
            // 1. Validate request
            if (request == null || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("New password is required.");
            }

            // 2. Find user
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // 3. Verify current password (if changing another user's password, add admin check)
            if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.password))
            {
                return BadRequest("Current password is incorrect.");
            }

            // 4. Validate new password strength
            if (!IsPasswordValid(request.NewPassword))
            {
                return BadRequest("Password must be at least 8 characters with uppercase, lowercase, number, and special character.");
            }

            // 5. Update password
            user.password = PasswordHasher.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully." });
        }

        // Password validation helper
        private bool IsPasswordValid(string password)
        {
            // Minimum 8 characters
            if (password.Length < 8)
                return false;

            // At least one uppercase
            if (!password.Any(char.IsUpper))
                return false;

            // At least one lowercase
            if (!password.Any(char.IsLower))
                return false;

            // At least one digit
            if (!password.Any(char.IsDigit))
                return false;

            // At least one special character
            var specialChars = "@$!%*?&";
            if (!password.Any(c => specialChars.Contains(c)))
                return false;

            return true;
        }

        [HttpPut("{id}/reset-password")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ResetPasswordToDefault(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Set to default password
            string defaultPassword = "@Temp1234!";
            user.password = PasswordHasher.HashPassword(defaultPassword);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset to default successfully." });
        }
    }
}
