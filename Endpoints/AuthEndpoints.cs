using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Services;
using System.Security.Claims;

namespace BlazorApp1.Endpoints
{
    /// <summary>
    /// Authentication API Endpoints using Minimal API
    /// </summary>
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/employeeauth")
                .WithTags("Authentication");

            // POST: /api/employeeauth/login - Employee login
            group.MapPost("/login", Login)
                .WithName("EmployeeLogin")
                .WithSummary("Employee login")
                .WithDescription("Authenticate employee with username and password")
                .Produces<LoginResponse>(StatusCodes.Status200OK)
                .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
                .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

            // POST: /api/employeeauth/logout - Employee logout
            group.MapPost("/logout", Logout)
                .WithName("EmployeeLogout")
                .WithSummary("Employee logout")
                .WithDescription("Sign out the current employee");
        }

        private static async Task<IResult> Login(
            [FromBody] EmployeeLoginRequest request,
            IAuthService authService,
            HttpContext httpContext,
            ILogger<IAuthService> logger)
        {
            try
            {
                var loginDto = new LoginDto
                {
                    Username = request.Username,
                    Password = request.Password
                };

                var result = await authService.LoginAsync(loginDto);

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

                    await httpContext.SignInAsync(
                        IdentityConstants.ApplicationScheme,
                        claimsPrincipal,
                        authProperties);

                    return Results.Ok(new LoginResponse
                    {
                        Success = true,
                        Message = "Login successful",
                        User = result
                    });
                }

                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during employee login");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        private static async Task<IResult> Logout(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return Results.Ok(new SuccessResponse
            {
                Success = true,
                Message = "Logged out successfully"
            });
        }
    }

    // DTOs
    public class EmployeeLoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; } = false;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public LoginResponseDto? User { get; set; }
    }

    public class SuccessResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
