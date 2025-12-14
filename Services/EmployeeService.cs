using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;
using StoreManagementAPI.Repositories;
using Microsoft.AspNetCore.Identity;
using BlazorApp1.Data;

namespace StoreManagementAPI.Services
{
    public interface IEmployeeService
    {
        Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync();
        Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
        Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto);
        Task<EmployeeDto?> UpdateEmployeeAsync(int id, UpdateEmployeeDto dto);
        Task<bool> DeleteEmployeeAsync(int id);
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly IRepository<Employee> _employeeRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StoreDbContext _context;

        public EmployeeService(
            IRepository<Employee> employeeRepository,
            UserManager<ApplicationUser> userManager,
            StoreDbContext context)
        {
            _employeeRepository = employeeRepository;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync()
        {
            var employees = await _context.Employees
                .Where(e => e.Status != "deleted")
                .ToListAsync();

            var employeeDtos = new List<EmployeeDto>();
            foreach (var e in employees)
            {
                string? username = null;
                if (!string.IsNullOrEmpty(e.UserId))
                {
                    var user = await _userManager.FindByIdAsync(e.UserId);
                    username = user?.UserName;
                }

                employeeDtos.Add(new EmployeeDto
                {
                    EmployeeId = e.EmployeeId,
                    FullName = e.FullName,
                    Phone = e.Phone,
                    Email = e.Email,
                    EmployeeType = e.EmployeeType,
                    UserId = e.UserId,
                    Username = username,
                    Status = e.Status,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                });
            }
            return employeeDtos;
        }

        public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == id && e.Status != "deleted");

            if (employee == null)
                return null;

            string? username = null;
            if (!string.IsNullOrEmpty(employee.UserId))
            {
                var user = await _userManager.FindByIdAsync(employee.UserId);
                username = user?.UserName;
            }

            return new EmployeeDto
            {
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                Phone = employee.Phone,
                Email = employee.Email,
                EmployeeType = employee.EmployeeType,
                UserId = employee.UserId,
                Username = username,
                PlaintextPassword = employee.PlaintextPassword,
                Status = employee.Status,
                CreatedAt = employee.CreatedAt,
                UpdatedAt = employee.UpdatedAt
            };
        }

        public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto)
        {
            // Note: Transaction across contexts (Identity and Store) is tricky. 
            // Ideally we should use a distributed transaction or just accept eventual consistency.
            // For simplicity, we create User first, then Employee. If Employee fails, we might have an orphan user.
            
            ApplicationUser? user = null;

            // Create user if username and password provided
            if (!string.IsNullOrWhiteSpace(dto.Username) && !string.IsNullOrWhiteSpace(dto.Password))
            {
                // Determine role based on employee type
                string role = dto.EmployeeType == "warehouse" ? "WarehouseStaff" : "SalesStaff";

                user = new ApplicationUser
                {
                    UserName = dto.Username.Trim(),
                    Email = dto.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                await _userManager.AddToRoleAsync(user, role);
            }

            try
            {
                // Create employee
                var employee = new Employee
                {
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    EmployeeType = dto.EmployeeType,
                    UserId = user?.Id,
                    PlaintextPassword = dto.Password,
                    Status = "active",
                    CreatedAt = DateTime.Now
                };

                await _employeeRepository.AddAsync(employee);

                return new EmployeeDto
                {
                    EmployeeId = employee.EmployeeId,
                    FullName = employee.FullName,
                    Phone = employee.Phone,
                    Email = employee.Email,
                    EmployeeType = employee.EmployeeType,
                    UserId = employee.UserId,
                    Username = user?.UserName,
                    Status = employee.Status,
                    CreatedAt = employee.CreatedAt
                };
            }
            catch
            {
                // If employee creation fails, we should try to delete the user to rollback
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }
                throw;
            }
        }

        public async Task<EmployeeDto?> UpdateEmployeeAsync(int id, UpdateEmployeeDto dto)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null || employee.Status == "deleted")
                return null;

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                employee.FullName = dto.FullName;

            if (!string.IsNullOrWhiteSpace(dto.Phone))
                employee.Phone = dto.Phone;

            if (!string.IsNullOrWhiteSpace(dto.Email))
                employee.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.EmployeeType))
                employee.EmployeeType = dto.EmployeeType;

            if (!string.IsNullOrWhiteSpace(dto.Status))
                employee.Status = dto.Status;

            employee.UpdatedAt = DateTime.Now;

            await _employeeRepository.UpdateAsync(employee);

            return await GetEmployeeByIdAsync(id);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null || employee.Status == "deleted")
                return false;

            // Soft delete
            employee.Status = "deleted";
            employee.UpdatedAt = DateTime.Now;

            await _employeeRepository.UpdateAsync(employee);
            
            // Optionally disable the user account
            if (!string.IsNullOrEmpty(employee.UserId))
            {
                var user = await _userManager.FindByIdAsync(employee.UserId);
                if (user != null)
                {
                    // We could lock the user out
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                }
            }
            
            return true;
        }
    }
}
