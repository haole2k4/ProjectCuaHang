using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("purchase_orders")]
    public class PurchaseOrder
    {
        [Key]
        [Column("purchase_id")]
        public int PurchaseId { get; set; }

        [Required]
        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("warehouse_id")]
        public int? WarehouseId { get; set; }

        [Column("purchase_date")]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        [Column("total_amount", TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "pending"; // pending, completed, canceled

        // Navigation properties
        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("WarehouseId")]
        public virtual Warehouse? Warehouse { get; set; }

        public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
    }
}
