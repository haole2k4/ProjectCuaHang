using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("order_items")]
    public class OrderItem
    {
        [Key]
        [Column("order_item_id")]
        public int OrderItemId { get; set; }

        [Column("order_id")]
        public int? OrderId { get; set; }

        [Column("product_id")]
        public int? ProductId { get; set; }

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        [Column("subtotal", TypeName = "decimal(10,2)")]
        public decimal Subtotal { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
