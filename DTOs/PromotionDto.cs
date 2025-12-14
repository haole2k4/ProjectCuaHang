namespace StoreManagementAPI.DTOs
{
    public class PromotionDto
    {
        public int PromoId { get; set; }
        public string PromoCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "percentage";
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal MinOrderAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public string Status { get; set; } = "active";
        public string ApplyType { get; set; } = "order"; // order, product, combo
        public List<ProductSimpleDto> Products { get; set; } = new();
    }

    public class CreatePromotionDto
    {
        public int PromoId { get; set; }  // Th�m ?? support Edit mode
        public string PromoCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "percentage";
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal MinOrderAmount { get; set; }
        public int UsageLimit { get; set; }  // Changed from int? to int
        public string Status { get; set; } = "active";  // Th�m Status field
        public string ApplyType { get; set; } = "order"; // order, product, category
        public List<int> ProductIds { get; set; } = new();
    }

    public class UpdatePromotionDto
    {
        public string? Description { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public string? Status { get; set; }
        public string? ApplyType { get; set; }
        public List<int>? ProductIds { get; set; }
    }

    /// <summary>
    /// DTO for advanced promotion search with multiple criteria
    /// </summary>
    public class PromotionSearchDto
    {
        /// <summary>
        /// Search by promo code or description
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by status (active, inactive)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filter by discount type (percentage, fixed)
        /// </summary>
        public string? DiscountType { get; set; }

        /// <summary>
        /// Filter by apply type (order, product, combo)
        /// </summary>
        public string? ApplyType { get; set; }

        /// <summary>
        /// Start date filter
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date filter
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Page number for pagination (default: 1)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size for pagination (default: 20, max: 100)
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort by field (PromoCode, StartDate, EndDate, DiscountValue)
        /// </summary>
        public string? SortBy { get; set; } = "StartDate";

        /// <summary>
        /// Sort direction (asc, desc)
        /// </summary>
        public string? SortDirection { get; set; } = "desc";
    }

    /// <summary>
    /// Response for paginated promotion search
    /// </summary>
    public class PromotionSearchResultDto
    {
        public IEnumerable<PromotionDto> Promotions { get; set; } = new List<PromotionDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
