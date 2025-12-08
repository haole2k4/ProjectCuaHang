using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("promotion_products")]
    public class PromotionProduct
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("promo_id")]
        public int PromoId { get; set; }

        [Required]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("PromoId")]
        public virtual Promotion? Promotion { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
