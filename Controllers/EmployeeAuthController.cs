using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Services;
using System.Security.Claims;

namespace BlazorApp1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeAuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<EmployeeAuthController> _logger;

        public EmployeeAuthController(IAuthService authService, ILogger<EmployeeAuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] EmployeeLoginRequest request)
        {
            try
            {
                var loginDto = new LoginDto
                {
                    Username = request.Username,
                    Password = request.Password
                };

                var result = await _authService.LoginAsync(loginDto);

                if (result != null)
                {
                    // Create claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                        new Claim(ClaimTypes.Name, result.Username),
                        new Claim(ClaimTypes.Role, result.Role),
                        new Claim("FullName", result.FullName ?? ""),
                        new Claim("EmployeeRole", result.Role)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = request.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24),
                        AllowRefresh = true
                    };

                    await HttpContext.SignInAsync(
                        IdentityConstants.ApplicationScheme,
                        claimsPrincipal,
                        authProperties);

                    return Ok(new { success = true, message = "Login successful", user = result });
                }

                return Unauthorized(new { success = false, message = "Invalid username or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during employee login");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return Ok(new { success = true, message = "Logged out successfully" });
        }
    }

    public class EmployeeLoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; } = false;
    }
}
