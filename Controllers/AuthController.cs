using ITAM.DataContext;
using ITAM.DTOs;
using ITAM.Models;
using ITAM.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ITAM.Controllers
{
 
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            try
            {
                if (login == null || string.IsNullOrEmpty(login.employee_id) || string.IsNullOrEmpty(login.password))
                {
                    _logger.LogWarning("Invalid login request received");
                    return BadRequest("Employee ID and password are required.");
                }

                _logger.LogInformation($"Login attempt for employee ID: {login.employee_id}");

                // Find user by employee_id
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.employee_id == login.employee_id);

                if (user == null)
                {
                    _logger.LogWarning($"User not found for employee ID: {login.employee_id}");
                    return Unauthorized("Invalid credentials.");
                }

                if (!PasswordHasher.VerifyPassword(login.password, user.password))
                {
                    _logger.LogWarning($"Invalid password attempt for employee ID: {login.employee_id}");
                    return Unauthorized("Invalid credentials.");
                }

                var token = GenerateJwtToken(user);

                // Return proper JSON structure
                return Ok(new LoginResponse
                {
                    Success = true,
                    Token = token,
                    EmployeeId = user.employee_id,
                    Name = user.name,
                    Role = user.role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");

            // Enhanced configuration validation
            var jwtKey = jwtSettings["key"] ?? jwtSettings["Key"] ??
                        throw new ArgumentNullException("JWT Key is not configured");

            var issuer = jwtSettings["Issuer"] ??
                         throw new ArgumentNullException("JWT Issuer is not configured");

            var audience = jwtSettings["Audience"] ??
                          throw new ArgumentNullException("JWT Audience is not configured");

            var expirationMinutes = jwtSettings.GetValue("ExpirationMinutes", 720);

            // Key validation
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            {
                throw new ArgumentException("JWT Key must be at least 32 characters long");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.employee_id ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("designation", user.designation ?? string.Empty),
                new Claim("name", user.name ?? string.Empty),
                new Claim("role", user.role ?? string.Empty)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
