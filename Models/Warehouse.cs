using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("warehouses")]
    public class Warehouse
    {
        [Key]
        [Column("warehouse_id")]
        public int WarehouseId { get; set; }

        [Column("warehouse_name")]
        [StringLength(100)]
        public string? WarehouseName { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "active"; // active, inactive, deleted

        // Navigation properties
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}
