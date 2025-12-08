using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("username")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("password")]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Column("full_name")]
        [StringLength(100)]
        public string? FullName { get; set; }

        [Column("role")]
        [StringLength(10)]
        public string Role { get; set; } = "staff";

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "active"; // active, inactive, deleted

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}
