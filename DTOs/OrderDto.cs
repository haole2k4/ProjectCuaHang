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
}
