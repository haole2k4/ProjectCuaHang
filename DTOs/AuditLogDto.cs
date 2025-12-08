namespace StoreManagementAPI.DTOs
{
    public class AuditLogDto
    {
        public int AuditId { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? ChangesSummary { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? AdditionalInfo { get; set; }
    }

    public class AuditLogFilterDto
    {
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AuditLogSummaryDto
    {
        public int TotalLogs { get; set; }
        public int TotalUsers { get; set; }
        public Dictionary<string, int> ActionCounts { get; set; } = new();
        public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
        public List<AuditLogDto> RecentLogs { get; set; } = new();
    }
}
