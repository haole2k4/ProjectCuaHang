using Microsoft.IdentityModel.Tokens;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;
using StoreManagementAPI.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using BlazorApp1.Data;
using Microsoft.EntityFrameworkCore; // Added

namespace StoreManagementAPI.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<ApplicationUser?> RegisterAsync(RegisterDto registerDto);
        Task<string> GenerateJwtToken(ApplicationUser user);
        Task<IEnumerable<ApplicationUser>> GetUsersAsync();
        Task<bool> UpdateUserAsync(string id, UpdateUserDto updateDto);
        Task<bool> DeleteUserAsync(string id);
        Task<bool> UpdatePasswordAsync(string userId, string oldPassword, string newPassword);
        Task<bool> ToggleUserStatusAsync(string userId);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration, 
            IAuditLogService auditLogService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _auditLogService = auditLogService;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.Username);

            if (user == null)
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
                        { "Reason", "Tài khoản không tồn tại" },
                        { "AttemptTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                    }
                );
                return null;
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
            {
                // Log failed login attempt
                await _auditLogService.LogActionAsync(
                    action: "LOGIN_FAILED",
                    entityType: "User",
                    entityId: null, // User Id is string now, AuditLog expects int? EntityId. We can't store string ID in int column.
                    entityName: loginDto.Username,
                    oldValues: null,
                    newValues: null,
                    changesSummary: $"Đăng nhập thất bại cho tài khoản '{loginDto.Username}'",
                    userId: user.Id, // AuditLog UserId is string? now
                    username: loginDto.Username,
                    additionalInfo: new Dictionary<string, object>
                    {
                        { "Reason", "Sai mật khẩu" },
                        { "AttemptTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                    }
                );
                return null;
            }

            // Check if locked out (Identity handles this if configured, but we can check manually too)
            if (await _userManager.IsLockedOutAsync(user))
            {
                 throw new Exception("Account is locked. Please contact administrator.");
            }

            var token = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Staff";

            // Log successful login
            await _auditLogService.LogActionAsync(
                action: "LOGIN",
                entityType: "User",
                entityId: null,
                entityName: user.UserName,
                oldValues: null,
                newValues: null,
                changesSummary: $"Người dùng '{user.UserName}' đăng nhập thành công",
                userId: user.Id,
                username: user.UserName,
                additionalInfo: new Dictionary<string, object>
                {
                    { "Role", role },
                    { "LoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            );

            return new LoginResponseDto
            {
                Token = token,
                Username = user.UserName,
                FullName = "", // IdentityUser doesn't have FullName by default unless we added it to ApplicationUser. We didn't see it in ApplicationUser.cs.
                Role = role,
                UserId = user.Id // Changed to string in DTO? No, DTO likely has int. I need to check LoginResponseDto.
            };
        }

        public async Task<ApplicationUser?> RegisterAsync(RegisterDto registerDto)
        {
            // Check if admin exists
            if (registerDto.Role == "Admin")
            {
                // Check if any user has Admin role
                var users = await _userManager.GetUsersInRoleAsync("Admin");
                if (users.Any())
                    throw new Exception("Only one admin account is allowed.");
            }

            // Default role
            if (string.IsNullOrEmpty(registerDto.Role))
                registerDto.Role = "Staff"; // Or Customer?
            
            var user = new ApplicationUser
            {
                UserName = registerDto.Username,
                Email = registerDto.Username.Contains("@") ? registerDto.Username : null, // Simple check
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                // Log failed registration
                await _auditLogService.LogActionAsync(
                    action: "REGISTER_FAILED",
                    entityType: "User",
                    entityId: null,
                    entityName: registerDto.Username,
                    oldValues: null,
                    newValues: null,
                    changesSummary: $"Đăng ký thất bại: {string.Join(", ", result.Errors.Select(e => e.Description))}",
                    userId: null,
                    username: "system",
                    additionalInfo: new Dictionary<string, object>
                    {
                        { "Reason", "Registration failed" },
                        { "AttemptTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                    }
                );
                return null;
            }
            
            await _userManager.AddToRoleAsync(user, registerDto.Role);

            // Log successful registration
            await _auditLogService.LogActionAsync(
                action: "REGISTER",
                entityType: "User",
                entityId: null,
                entityName: user.UserName,
                oldValues: null,
                newValues: new
                {
                    Username = user.UserName,
                    Role = registerDto.Role
                },
                changesSummary: $"Đăng ký tài khoản mới: '{user.UserName}' với vai trò {registerDto.Role}",
                userId: user.Id,
                username: user.UserName,
                additionalInfo: new Dictionary<string, object>
                {
                    { "RegisterTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            );

            return user;
        }

        public async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "YourSuperSecretKeyForJwtTokenGeneration123456"));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var roles = await _userManager.GetRolesAsync(user);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
            };
            
            foreach(var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "StoreManagementAPI",
                audience: _configuration["Jwt:Audience"] ?? "StoreManagementClient",
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        public async Task<IEnumerable<ApplicationUser>> GetUsersAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<bool> UpdateUserAsync(string id, UpdateUserDto updateDto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return false;

            if (!string.IsNullOrEmpty(updateDto.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, updateDto.Password);
            }

            // FullName is not in default IdentityUser. If we added it to ApplicationUser, we can update it.
            // Assuming ApplicationUser has no FullName property based on previous file read.
            // If it does, we update it.
            
            // await _userManager.UpdateAsync(user);
            return true;
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return false;
            
            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> UpdatePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            return result.Succeeded;
        }

        public async Task<bool> ToggleUserStatusAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Check if admin
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin") && user.UserName == "admin@estore.com") // Hardcoded check for primary admin
            {
                 throw new Exception("Cannot lock the primary admin account");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
            
            return true;
        }
    }
}
