namespace StoreManagementAPI.DTOs
{
    public class EmployeeDto
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeType { get; set; } = "sales"; // sales, warehouse
        public string? UserId { get; set; }
        public string? Username { get; set; }        public string? PlaintextPassword { get; set; }        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateEmployeeDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeType { get; set; } = "sales";
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class UpdateEmployeeDto
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? EmployeeType { get; set; }
        public string? Status { get; set; }
    }
}
