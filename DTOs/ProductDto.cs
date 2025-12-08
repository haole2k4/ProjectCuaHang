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
        // CostPrice được cập nhật khi nhập hàng, không cần thiết khi tạo sản phẩm
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
}
