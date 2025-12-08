namespace StoreManagementAPI.DTOs
{
    public class CategoryDeleteRequestDto
    {
        // Map productId -> newCategoryId cho từng sản phẩm
        public Dictionary<int, int>? ProductCategoryMap { get; set; }
        public bool HideProducts { get; set; }
    }

    public class CategoryDeleteResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool SoftDeleted { get; set; }
        public int CategoryId { get; set; }
        public List<ProductDto>? AffectedProducts { get; set; }
        public int ProductCount { get; set; }
    }
}
