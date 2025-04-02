using ITAM.DataContext;
using ITAM.Models;
using ITAM.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ITAM.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

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
    }
}
