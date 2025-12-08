using Microsoft.IdentityModel.Tokens;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;
using StoreManagementAPI.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StoreManagementAPI.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<User?> RegisterAsync(RegisterDto registerDto);
        string GenerateJwtToken(User user);
        Task<IEnumerable<User>> GetUsersAsync();
        Task<bool> UpdateUserAsync(int id, UpdateUserDto updateDto);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> UpdatePasswordAsync(int userId, string OldPassword, string newPassword);
        Task<bool> ToggleUserStatusAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;

        public AuthService(IRepository<User> userRepository, IConfiguration configuration, IAuditLogService auditLogService)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _auditLogService = auditLogService;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var users = await _userRepository.FindAsync(u => u.Username == loginDto.Username);
            var user = users.FirstOrDefault();

            if (user == null || user.Password != loginDto.Password)
            {
                // Log failed login attempt
                await _auditLogService.LogActionAsync(
                    action: "LOGIN_FAILED",
                    entityType: "User",
                    entityId: null,
                    entityName: loginDto.Username,
                    oldValues: null,
                    newValues: null,
                    changesSummary: $"Đăng nhập thất bại cho tài khoản '{loginDto.Username}'",
                    userId: null,
                    username: loginDto.Username,
                    additionalInfo: new Dictionary<string, object>
                    {
                        { "Reason", user == null ? "Tài khoản không tồn tại" : "Sai mật khẩu" },
                        { "AttemptTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                    }
                );
                return null;
            }

            // Kiểm tra tài khoản có bị khóa không
            if (user.Status != "active")
            {
                throw new Exception("Account is locked. Please contact administrator.");
            }

            var token = GenerateJwtToken(user);

            // Log successful login
            await _auditLogService.LogActionAsync(
                action: "LOGIN",
                entityType: "User",
                entityId: user.UserId,
                entityName: user.FullName ?? user.Username,
                oldValues: null,
                newValues: null,
                changesSummary: $"Người dùng '{user.Username}' ({user.FullName}) đăng nhập thành công",
                userId: user.UserId,
                username: user.Username,
                additionalInfo: new Dictionary<string, object>
                {
                    { "Role", user.Role },
                    { "LoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            );

            return new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                FullName = user.FullName ?? "",
                Role = user.Role,
                UserId = user.UserId
            };
        }

        public async Task<User?> RegisterAsync(RegisterDto registerDto)
        {
            // Nếu user muốn tạo admin, kiểm tra có admin chưa
            if (registerDto.Role == "admin")
            {
                var hasAdmin = await _userRepository.ExistsAsync(u => u.Role == "admin");
                if (hasAdmin)
                    throw new Exception("Only one admin account is allowed.");
            }

            // Nếu không chỉ định role, mặc định là staff
            if (string.IsNullOrEmpty(registerDto.Role))
                registerDto.Role = "staff";
            
            var exists = await _userRepository.ExistsAsync(u => u.Username == registerDto.Username);
            if (exists)
            {
                // Log failed registration attempt
                await _auditLogService.LogActionAsync(
                    action: "REGISTER_FAILED",
                    entityType: "User",
                    entityId: null,
                    entityName: registerDto.Username,
                    oldValues: null,
                    newValues: null,
                    changesSummary: $"Đăng ký thất bại: Tài khoản '{registerDto.Username}' đã tồn tại",
                    userId: null,
                    username: "system",
                    additionalInfo: new Dictionary<string, object>
                    {
                        { "Reason", "Username already exists" },
                        { "AttemptTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                    }
                );
                return null;
            }

            var user = new User
            {
                Username = registerDto.Username,
                Password = registerDto.Password, // In production, hash this!
                FullName = registerDto.FullName,
                Role = registerDto.Role
            };

            var newUser = await _userRepository.AddAsync(user);

            // Log successful registration
            await _auditLogService.LogActionAsync(
                action: "REGISTER",
                entityType: "User",
                entityId: newUser.UserId,
                entityName: newUser.FullName ?? newUser.Username,
                oldValues: null,
                newValues: new
                {
                    Username = newUser.Username,
                    FullName = newUser.FullName,
                    Role = newUser.Role
                },
                changesSummary: $"Đăng ký tài khoản mới: '{newUser.Username}' ({newUser.FullName}) với vai trò {newUser.Role}",
                userId: newUser.UserId,
                username: newUser.Username,
                additionalInfo: new Dictionary<string, object>
                {
                    { "RegisterTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            );

            return newUser;
        }

        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJwtTokenGeneration123456"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "StoreManagementAPI",
                audience: _configuration["Jwt:Audience"] ?? "StoreManagementClient",
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
         public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto updateDto)
        {
            var users = await _userRepository.FindAsync(u => u.UserId == id);
            var user = users.FirstOrDefault();
            if (user == null) return false;

          
            if (!string.IsNullOrEmpty(updateDto.Password))
                user.Password = updateDto.Password; 

            if (!string.IsNullOrEmpty(updateDto.FullName))
                user.FullName = updateDto.FullName;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await _userRepository.DeleteAsync(id);
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var users = await _userRepository.FindAsync(u => u.UserId == userId);
            var user = users.FirstOrDefault();

            if (user == null)
                return false;
                
             // Kiểm tra mật khẩu cũ
            if (user.Password != oldPassword)
                throw new Exception("Old password is incorrect.");

            user.Password = newPassword; 
            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> ToggleUserStatusAsync(int userId)
        {
            var users = await _userRepository.FindAsync(u => u.UserId == userId);
            var user = users.FirstOrDefault();

            if (user == null)
                return false;

            // Không cho phép khóa admin ID = 1
            if (user.UserId == 1 && user.Role == "admin")
            {
                throw new Exception("Cannot lock the primary admin account (ID = 1)");
            }

            // Toggle status giữa active và inactive
            user.Status = user.Status == "active" ? "inactive" : "active";
            await _userRepository.UpdateAsync(user);
            return true;
        }
    }
}
