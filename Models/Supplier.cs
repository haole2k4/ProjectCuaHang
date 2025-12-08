using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("suppliers")]
    public class Supplier
    {
        [Key]
        [Column("supplier_id")]
        public int SupplierId { get; set; }

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

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}
