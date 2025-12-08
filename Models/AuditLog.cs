using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoreManagementAPI.Models
{
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key]
        [Column("audit_id")]
        public int AuditId { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("username")]
        [StringLength(50)]
        public string? Username { get; set; }

        [Required]
        [Column("action")]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, VIEW, LOGIN, LOGOUT, etc.

        [Required]
        [Column("entity_type")]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty; // Product, Order, PurchaseOrder, Inventory, etc.

        [Column("entity_id")]
        public int? EntityId { get; set; }

        [Column("entity_name")]
        [StringLength(255)]
        public string? EntityName { get; set; }

        [Column("old_values", TypeName = "text")]
        public string? OldValues { get; set; } // JSON string of old values

        [Column("new_values", TypeName = "text")]
        public string? NewValues { get; set; } // JSON string of new values

        [Column("changes_summary", TypeName = "text")]
        public string? ChangesSummary { get; set; } // Human readable summary

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("additional_info", TypeName = "text")]
        public string? AdditionalInfo { get; set; } // JSON string for extra information

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
