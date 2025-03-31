using ITAM.DataContext;
using ITAM.DTOs;
using ITAM.Models;
using Microsoft.EntityFrameworkCore;

namespace ITAM.Services // Change this from ITAM.Services.UserService
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User> FindOrCreateUserAsync(AddAssetDto assetDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.name == assetDto.user_name && u.company == assetDto.company && u.department == assetDto.department);

            if (user == null)
            {
                user = new User
                {
                    name = assetDto.user_name,
                    company = assetDto.company,
                    department = assetDto.department,
                    employee_id = assetDto.employee_id
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(assetDto.employee_id) && user.employee_id != assetDto.employee_id)
                {
                    user.employee_id = assetDto.employee_id;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
            }

            return user;
        }

        //for updating asset endpoint or creating new user for not existing user
        public async Task<int?> GetOrCreateUserAsync(UpdateAssetDto assetDto)
        {
            // Skip user creation if all fields are empty
            if (string.IsNullOrWhiteSpace(assetDto.user_name) &&
                string.IsNullOrWhiteSpace(assetDto.company) &&
                string.IsNullOrWhiteSpace(assetDto.department) &&
                string.IsNullOrWhiteSpace(assetDto.employee_id))
            {
                return null; // Return null to indicate no owner change
            }
            // Check if the user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.name == assetDto.user_name
                                       && u.company == assetDto.company
                                       && u.department == assetDto.department
                                       && u.employee_id == assetDto.employee_id);

            if (existingUser != null)
            {
                return existingUser.id; // Return the existing user's id
            }
            else
            {
                // Create a new user if not found
                var newUser = new User
                {
                    name = assetDto.user_name,
                    company = assetDto.company,
                    department = assetDto.department,
                    employee_id = assetDto.employee_id
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return newUser.id; // Return the new user's id
            }
        }

        public async Task<int?> GetOrCreateUserAsync(UpdateComputerDto computerDto)
        {
            // Skip user creation if all fields are empty
            if (string.IsNullOrWhiteSpace(computerDto.user_name) &&
                string.IsNullOrWhiteSpace(computerDto.company) &&
                string.IsNullOrWhiteSpace(computerDto.department) &&
                string.IsNullOrWhiteSpace(computerDto.employee_id))
            {
                return null; // Return null to indicate no owner change
            }

            // Check if the user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.name == computerDto.user_name
                                   && u.company == computerDto.company
                                   && u.department == computerDto.department
                                   && u.employee_id == computerDto.employee_id);
            if (existingUser != null)
            {
                return existingUser.id; // Return the existing user's id
            }
            else
            {
                // Create a new user if not found and fields aren't empty
                var newUser = new User
                {
                    name = computerDto.user_name,
                    company = computerDto.company,
                    department = computerDto.department,
                    employee_id = computerDto.employee_id,
                    date_created = DateTime.Now
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                return newUser.id; // Return the new user's id
            }
        }
    }
}
