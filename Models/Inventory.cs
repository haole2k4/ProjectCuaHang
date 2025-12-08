using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("inventory")]
    public class Inventory
    {
        [Key]
        [Column("inventory_id")]
        public int InventoryId { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("warehouse_id")]
        public int? WarehouseId { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; } = 0;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("WarehouseId")]
        public virtual Warehouse? Warehouse { get; set; }
    }
}
