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
        public string PromoCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "percentage";
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal MinOrderAmount { get; set; }
        public int? UsageLimit { get; set; }
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
}
