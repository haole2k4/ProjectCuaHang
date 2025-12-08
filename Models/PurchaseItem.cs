using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("purchase_items")]
    public class PurchaseItem
    {
        [Key]
        [Column("purchase_item_id")]
        public int PurchaseItemId { get; set; }

        [Required]
        [Column("purchase_id")]
        public int PurchaseId { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Required]
        [Column("cost_price", TypeName = "decimal(10,2)")]
        public decimal CostPrice { get; set; }

        [Required]
        [Column("subtotal", TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        // Navigation properties
        [ForeignKey("PurchaseId")]
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}
