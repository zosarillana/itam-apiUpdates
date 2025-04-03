namespace ITAM.DTOs
{
    public class UserDTO
    {
    }

    // Request model
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class LoginRequest
    {
        public string employee_id { get; set; }
        public string password { get; set; }
    }

    public class OwnerDto
    {
        public int id { get; set; }
        public string name { get; set; }
        public string company { get; set; }
        public string department { get; set; }
        public string employee_id { get; set; }
    }

    // Add this DTO
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? EmployeeId { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? Message { get; set; }
    }

   
}
