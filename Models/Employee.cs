using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("employees")]
    public class Employee
    {
        [Key]
        [Column("employee_id")]
        public int EmployeeId { get; set; }

        [Required]
        [Column("full_name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Column("phone")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [Column("email")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Column("employee_type")]
        [StringLength(20)]
        public string EmployeeType { get; set; } = "sales"; // sales (bán hàng), warehouse (nhập hàng)

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("plaintext_password")]
        [StringLength(255)]
        public string? PlaintextPassword { get; set; } // For admin to view employee passwords

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "active"; // active, inactive

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
