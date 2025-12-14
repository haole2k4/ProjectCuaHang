namespace StoreManagementAPI.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public string Unit { get; set; } = "pcs";
        public string Status { get; set; } = "active";
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? StockQuantity { get; set; }
        public bool HasOrders { get; set; } // True nếu sản phẩm đã được bán
    }

    public class ProductSimpleDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class CreateProductDto
    {
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; } = "pcs";
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateProductDto
    {
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public string? ProductName { get; set; }
        public string? Barcode { get; set; }
        public decimal? Price { get; set; }
        public decimal? CostPrice { get; set; }
        public string? Unit { get; set; }
        public string? Status { get; set; } // Thêm Status để có thể ẩn/hiện sản phẩm
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class UpdateStockDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ProductHistoryDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // "purchase" hoặc "sale"
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ReferenceNumber { get; set; } // Mã phiếu nhập/đơn hàng
        public string? UserName { get; set; }
        public string? SupplierName { get; set; } // Cho phiếu nhập
        public string? CustomerName { get; set; } // Cho đơn bán
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for advanced product search with multiple criteria
    /// </summary>
    public class ProductSearchDto
    {
        /// <summary>
        /// Search by product name (contains)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Filter by category ID
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Filter by status (active, inactive)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Minimum price filter
        /// </summary>
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Maximum price filter
        /// </summary>
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Page number for pagination (default: 1)
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size for pagination (default: 20, max: 100)
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Sort by field (ProductName, Price, CreatedAt)
        /// </summary>
        public string? SortBy { get; set; } = "ProductName";

        /// <summary>
        /// Sort direction (asc, desc)
        /// </summary>
        public string? SortDirection { get; set; } = "asc";
    }

    /// <summary>
    /// Response for paginated product search
    /// </summary>
    public class ProductSearchResultDto
    {
        public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
