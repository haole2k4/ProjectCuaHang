using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("customer_id")]
        public int? CustomerId { get; set; }

        [Column("user_id")]
        public string? UserId { get; set; }

        [Column("promo_id")]
        public int? PromoId { get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column("status")]
        [StringLength(10)]
        public string Status { get; set; } = "pending";

        [Column("total_amount", TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Column("discount_amount", TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column("shipping_address")]
        [StringLength(500)]
        public string? ShippingAddress { get; set; }

        [Column("notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("PromoId")]
        public virtual Promotion? Promotion { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
