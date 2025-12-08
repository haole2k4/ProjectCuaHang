using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("promotions")]
    public class Promotion
    {
        [Key]
        [Column("promo_id")]
        public int PromoId { get; set; }

        [Required]
        [Column("promo_code")]
        [StringLength(50)]
        public string PromoCode { get; set; } = string.Empty;

        [Column("description")]
        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        [Column("discount_type")]
        [StringLength(10)]
        public string DiscountType { get; set; } = string.Empty; // percent or fixed

        [Required]
        [Column("discount_value", TypeName = "decimal(10,2)")]
        public decimal DiscountValue { get; set; }

        [Required]
        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("min_order_amount", TypeName = "decimal(10,2)")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Column("usage_limit")]
        public int UsageLimit { get; set; } = 0;

        [Column("used_count")]
        public int UsedCount { get; set; } = 0;

        [Column("status")]
        [StringLength(10)]
        public string Status { get; set; } = "active";

        [Column("apply_type")]
        [StringLength(20)]
        public string ApplyType { get; set; } = "order"; // order, product, combo

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
    }
}
