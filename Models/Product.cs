using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("supplier_id")]
        public int? SupplierId { get; set; }

        [Required]
        [Column("product_name")]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Column("barcode")]
        [StringLength(50)]
        public string? Barcode { get; set; }

        [Required]
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column("cost_price", TypeName = "decimal(10,2)")]
        public decimal CostPrice { get; set; } = 0;

        [Column("unit")]
        [StringLength(20)]
        public string Unit { get; set; } = "pcs";

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "active"; // active, inactive, deleted

        [Column("description")]
        public string? Description { get; set; } // Có sẵn trong DB

        [Column("image_url")]
        [StringLength(255)]
        public string? ImageUrl { get; set; } // Có sẵn trong DB

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; } // Có sẵn trong DB

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
    }
}
