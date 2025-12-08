namespace StoreManagementAPI.DTOs
{
    // DTO for creating purchase order
    public class CreatePurchaseOrderDto
    {
        public int SupplierId { get; set; }
        public int WarehouseId { get; set; }
        public List<PurchaseItemDto> Items { get; set; } = new List<PurchaseItemDto>();
    }

    // DTO for purchase item
    public class PurchaseItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
    }

    // DTO for purchase order response
    public class PurchaseOrderResponseDto
    {
        public int PurchaseId { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<PurchaseItemResponseDto> Items { get; set; } = new List<PurchaseItemResponseDto>();
    }

    // DTO for purchase item response
    public class PurchaseItemResponseDto
    {
        public int PurchaseItemId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Barcode { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    // DTO for warehouse
    public class WarehouseDto
    {
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = "active";
    }

    // DTO for updating purchase order status
    public class UpdatePurchaseOrderStatusDto
    {
        public string Status { get; set; } = string.Empty; // pending, completed, canceled
    }
}
