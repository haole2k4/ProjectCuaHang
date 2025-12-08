namespace StoreManagementAPI.DTOs
{
    // Dashboard overview statistics
    public class DashboardStatisticsDto
    {
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal YearRevenue { get; set; }
        public int TodayOrders { get; set; }
        public int MonthOrders { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<TopProductDto> TopSellingProducts { get; set; } = new List<TopProductDto>();
        public List<RevenueByDateDto> RevenueByDate { get; set; } = new List<RevenueByDateDto>();
        public List<OrderStatusDto> OrdersByStatus { get; set; } = new List<OrderStatusDto>();
        public List<PaymentMethodDto> PaymentMethods { get; set; } = new List<PaymentMethodDto>();
    }

    // Top selling product
    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public string? ImageUrl { get; set; }
    }

    // Revenue by date for charts
    public class RevenueByDateDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    // Order count by status
    public class OrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // Payment method statistics
    public class PaymentMethodDto
    {
        public string Method { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // Sales report with filters
    public class SalesReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
        public int TotalOrders { get; set; }
        public int TotalItemsSold { get; set; }
        public List<RevenueByDateDto> DailyRevenue { get; set; } = new List<RevenueByDateDto>();
        public List<TopProductDto> TopProducts { get; set; } = new List<TopProductDto>();
        public List<CategorySalesDto> SalesByCategory { get; set; } = new List<CategorySalesDto>();
    }

    // Sales by category
    public class CategorySalesDto
    {
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalQuantity { get; set; }
    }

    // Inventory statistics
    public class InventoryStatisticsDto
    {
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public List<LowStockProductDto> LowStockItems { get; set; } = new List<LowStockProductDto>();
        public List<WarehouseStockStatisticsDto> StockByWarehouse { get; set; } = new List<WarehouseStockStatisticsDto>();
    }

    // Low stock product
    public class LowStockProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public string? WarehouseName { get; set; }
        public decimal Price { get; set; }
    }

    // Stock by warehouse for statistics
    public class WarehouseStockStatisticsDto
    {
        public int? WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
    }

    // Customer statistics
    public class CustomerStatisticsDto
    {
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int ActiveCustomers { get; set; }
        public List<TopCustomerDto> TopCustomers { get; set; } = new List<TopCustomerDto>();
    }

    // Top customer by revenue
    public class TopCustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
