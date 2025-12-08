using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("categories")]
    public class Category
    {
        [Key]
        [Column("category_id")]
        public int CategoryId { get; set; }

        [Required]
        [Column("category_name")]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "active"; // active, inactive, deleted

        // Navigation property
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
