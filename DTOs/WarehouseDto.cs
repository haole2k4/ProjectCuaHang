namespace StoreManagementAPI.DTOs
{
    public class WarehouseDto
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string Status { get; set; } = "active";
        public int TotalItems { get; set; }
        public int TotalStock { get; set; }
    }

    // DTO để hiển thị lịch sử nhập/xuất kho
    public class WarehouseTransactionHistoryDto
    {
        public int TransactionId { get; set; }
        public string TransactionType { get; set; } = string.Empty; // "IN" (Nhập hàng) hoặc "OUT" (Bán hàng)
        public DateTime TransactionDate { get; set; }
        public int? ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalAmount { get; set; }
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string? SupplierName { get; set; } // Nếu là nhập hàng
        public string? CustomerName { get; set; } // Nếu là bán hàng
        public string? Username { get; set; } // Người thực hiện
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class WarehouseHistoryFilterDto
    {
        public int? WarehouseId { get; set; }
        public int? ProductId { get; set; }
        public string? TransactionType { get; set; } // "IN" hoặc "OUT"
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
