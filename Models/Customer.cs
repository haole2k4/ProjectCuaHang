using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("customers")]
    public class Customer
    {
        [Key]
        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Required]
        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("phone")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Column("email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "active"; // active, inactive, deleted

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
