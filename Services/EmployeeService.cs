using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;
using StoreManagementAPI.Repositories;

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
        private readonly IRepository<User> _userRepository;
        private readonly StoreDbContext _context;

        public EmployeeService(
            IRepository<Employee> employeeRepository,
            IRepository<User> userRepository,
            StoreDbContext context)
        {
            _employeeRepository = employeeRepository;
            _userRepository = userRepository;
            _context = context;
        }

        public async Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync()
        {
            var employees = await _context.Employees
                .Include(e => e.User)
                .Where(e => e.Status != "deleted")
                .ToListAsync();

            return employees.Select(e => new EmployeeDto
            {
                EmployeeId = e.EmployeeId,
                FullName = e.FullName,
                Phone = e.Phone,
                Email = e.Email,
                EmployeeType = e.EmployeeType,
                UserId = e.UserId,
                Username = e.User?.Username,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            });
        }

        public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == id && e.Status != "deleted");

            if (employee == null)
                return null;

            return new EmployeeDto
            {
                EmployeeId = employee.EmployeeId,
                FullName = employee.FullName,
                Phone = employee.Phone,
                Email = employee.Email,
                EmployeeType = employee.EmployeeType,
                UserId = employee.UserId,
                Username = employee.User?.Username,
                PlaintextPassword = employee.PlaintextPassword,
                Status = employee.Status,
                CreatedAt = employee.CreatedAt,
                UpdatedAt = employee.UpdatedAt
            };
        }

        public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                User? user = null;

                // Tạo user nếu có username và password
                if (!string.IsNullOrWhiteSpace(dto.Username) && !string.IsNullOrWhiteSpace(dto.Password))
                {
                    // Xác định role dựa trên employee type
                    string role = dto.EmployeeType == "warehouse" ? "warehouse_staff" : "sales_staff";

                    user = new User
                    {
                        Username = dto.Username.Trim(),
                        Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                        FullName = dto.FullName,
                        Role = role,
                        Status = "active",
                        CreatedAt = DateTime.Now
                    };

                    await _userRepository.AddAsync(user);
                }

                // Tạo employee
                var employee = new Employee
                {
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    EmployeeType = dto.EmployeeType,
                    UserId = user?.UserId,                    PlaintextPassword = dto.Password,                    Status = "active",
                    CreatedAt = DateTime.Now
                };

                await _employeeRepository.AddAsync(employee);
                await transaction.CommitAsync();

                return new EmployeeDto
                {
                    EmployeeId = employee.EmployeeId,
                    FullName = employee.FullName,
                    Phone = employee.Phone,
                    Email = employee.Email,
                    EmployeeType = employee.EmployeeType,
                    UserId = employee.UserId,
                    Username = user?.Username,
                    Status = employee.Status,
                    CreatedAt = employee.CreatedAt
                };
            }
            catch
            {
                await transaction.RollbackAsync();
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
            return true;
        }
    }
}
