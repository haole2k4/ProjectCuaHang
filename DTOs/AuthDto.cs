namespace StoreManagementAPI.DTOs
{
    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "staff";
    }
    public class UpdateUserDto
    {
        public string? Password { get; set; }
        public string? FullName { get; set; }
        public string? Role { get; set; }
    }

    public class UpdatePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;

    }
    // public class UpdatePasswordDto
    // {
    //     public string OldPassword { get; set; } = string.Empty;
    //     public string NewPassword { get; set; } = string.Empty;
    // }
}
