namespace StoreManagementAPI.DTOs
{
    public class CreateOrderDto
    {
        public int? CustomerId { get; set; }
        public int? UserId { get; set; }
        public string? PromoCode { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? PaymentDate { get; set; }
        public int? PromoId { get; set; }
        public string? PromoCode { get; set; }
        public string? PromoType { get; set; }
        public string? PromoDescription { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }

    public class OrderItemResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public class PaymentDto
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "cash";
    }

    /// <summary>
    /// DTO for advanced order search with multiple criteria
    /// </summary>
    public class OrderSearchDto
    {
        /// <summary>
        /// Search by order ID or customer name
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by status (pending, completed, cancelled, etc.)
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Minimum total amount filter
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        /// Maximum total amount filter
        /// </summary>
        public decimal? MaxAmount { get; set; }

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
        /// Sort by field (OrderDate, TotalAmount, Status)
        /// </summary>
        public string? SortBy { get; set; } = "OrderDate";

        /// <summary>
        /// Sort direction (asc, desc)
        /// </summary>
        public string? SortDirection { get; set; } = "desc";
    }

    /// <summary>
    /// Response for paginated order search
    /// </summary>
    public class OrderSearchResultDto
    {
        public IEnumerable<OrderResponseDto> Orders { get; set; } = new List<OrderResponseDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
