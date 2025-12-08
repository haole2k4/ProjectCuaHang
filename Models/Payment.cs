using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("payments")]
    public class Payment
    {
        [Key]
        [Column("payment_id")]
        public int PaymentId { get; set; }

        [Required]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Required]
        [Column("amount", TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Column("payment_method")]
        [StringLength(20)]
        public string PaymentMethod { get; set; } = "cash";

        [Column("payment_date")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
    }
}
