namespace StoreManagementAPI.DTOs
{
    public class StockReceiptDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class InventoryResponseDto
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WarehouseStockDto
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class ProductInventoryDetailDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int TotalStock { get; set; }
        public List<WarehouseStockDto> Warehouses { get; set; } = new();
    }

    public class RecalculateStockResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int TotalProductsUpdated { get; set; }
        public int TotalWarehousesUpdated { get; set; }
        public List<string> Details { get; set; } = new();
    }
}
