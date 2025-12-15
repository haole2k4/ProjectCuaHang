using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;

namespace StoreManagementAPI.Services
{
    public interface IStatisticsService
    {
        Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(int days = 30, List<string>? statusFilter = null);
        Task<SalesReportDto> GetSalesReportAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<InventoryStatisticsDto> GetInventoryStatisticsAsync(int lowStockThreshold = 10);
        Task<CustomerStatisticsDto> GetCustomerStatisticsAsync();
        Task<List<TransactionDto>> GetRecentTransactionsAsync(int limit);
    }

    public class StatisticsService : IStatisticsService
    {
        private readonly StoreDbContext _context;

        public StatisticsService(StoreDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(int days = 30, List<string>? statusFilter = null)
        {
            var now = DateTime.Now;
            var today = now.Date;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var yearStart = new DateTime(now.Year, 1, 1);
            var periodStart = today.AddDays(-days);

            // Get all orders with related data
            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .AsQueryable();

            // Apply status filter or default to successful orders for revenue/cashflow
            if (statusFilter != null && statusFilter.Any())
            {
                query = query.Where(o => statusFilter.Contains(o.Status));
            }
            else
            {
                // Default to successful orders for accurate cashflow/revenue if no filter selected
                // User requested: "orders c status thnh cng"
                var successStatuses = new[] { "completed", "paid" };
                query = query.Where(o => successStatuses.Contains(o.Status));
            }

            var allOrders = await query.ToListAsync();

            // Get Purchase Orders for Expenses (Spend)
            // User requested: "d?a vo purchase order"
            var purchaseOrders = await _context.PurchaseOrders
                .Where(p => p.Status == "completed")
                .ToListAsync();

            // Today's statistics
            var todayOrders = allOrders.Where(o => o.OrderDate.Date == today).ToList();
            var todayRevenue = todayOrders.Sum(o => o.TotalAmount);

            // Month statistics
            var monthOrders = allOrders.Where(o => o.OrderDate >= monthStart).ToList();
            var monthRevenue = monthOrders.Sum(o => o.TotalAmount);
            
            // Month Expenses from Purchase Orders
            var monthPurchases = purchaseOrders.Where(p => p.PurchaseDate >= monthStart).ToList();
            var monthExpenses = monthPurchases.Sum(p => p.TotalAmount);
            
            var monthProfit = monthRevenue - monthExpenses;

            // Calculate growth (comparing to previous month)
            var prevMonthStart = monthStart.AddMonths(-1);
            var prevMonthEnd = monthStart.AddDays(-1);
            
            var prevMonthOrders = allOrders
                .Where(o => o.OrderDate >= prevMonthStart && o.OrderDate <= prevMonthEnd)
                .ToList();
                
            var prevMonthRevenue = prevMonthOrders.Sum(o => o.TotalAmount);
            
            var prevMonthPurchases = purchaseOrders
                .Where(p => p.PurchaseDate >= prevMonthStart && p.PurchaseDate <= prevMonthEnd)
                .ToList();
            var prevMonthExpenses = prevMonthPurchases.Sum(p => p.TotalAmount);

            var incomeGrowth = prevMonthRevenue > 0 
                ? ((monthRevenue - prevMonthRevenue) / prevMonthRevenue) * 100 
                : (monthRevenue > 0 ? 100 : 0);
                
            var expenseGrowth = prevMonthExpenses > 0 
                ? ((monthExpenses - prevMonthExpenses) / prevMonthExpenses) * 100 
                : (monthExpenses > 0 ? 100 : 0);

            // Year statistics
            var yearOrders = allOrders.Where(o => o.OrderDate >= yearStart).ToList();
            var yearRevenue = yearOrders.Sum(o => o.TotalAmount);

            // Product statistics
            var totalProducts = await _context.Products
                .Where(p => p.Status == "active")
                .CountAsync();

            var lowStockProducts = await _context.Inventories
                .Where(i => i.Quantity < 10)
                .CountAsync();

            // Customer statistics
            var totalCustomers = await _context.Customers
                .Where(c => c.Status == "active")
                .CountAsync();

            var activeCustomers = await _context.Customers
                .Where(c => c.Status == "active" && c.Orders.Any(o => o.OrderDate >= monthStart))
                .CountAsync();

            // Average order value
            var avgOrderValue = monthOrders.Any() ? monthOrders.Average(o => o.TotalAmount) : 0;

            // Top selling products
            var topProducts = await _context.OrderItems
                .Where(oi => oi.Order!.OrderDate >= periodStart && oi.Order.Status != "cancelled")
                .GroupBy(oi => new { oi.ProductId, oi.Product!.ProductName, oi.Product.ImageUrl, oi.Product.Barcode, oi.Product.Category.CategoryName })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId ?? 0,
                    ProductName = g.Key.ProductName ?? string.Empty,
                    ProductCode = g.Key.Barcode ?? "N/A",
                    CategoryName = g.Key.CategoryName ?? "Uncategorized",
                    TotalQuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Subtotal),
                    ImageUrl = g.Key.ImageUrl
                })
                .OrderByDescending(p => p.TotalQuantitySold)
                .Take(10)
                .ToListAsync();

            // Revenue by date (last N days)
            // Group Orders
            var revenueByDateMap = allOrders
                .Where(o => o.OrderDate >= periodStart)
                .GroupBy(o => o.OrderDate.Date)
                .ToDictionary(g => g.Key, g => new 
                {
                    Revenue = g.Sum(o => o.TotalAmount),
                    Count = g.Count()
                });

            // Group Purchases
            var expensesByDateMap = purchaseOrders
                .Where(p => p.PurchaseDate >= periodStart)
                .GroupBy(p => p.PurchaseDate.Date)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalAmount));

            var revenueByDate = new List<RevenueByDateDto>();
            for (var d = periodStart; d <= today; d = d.AddDays(1))
            {
                var dateKey = d.Date;
                decimal revenue = 0;
                int count = 0;
                decimal expenses = 0;

                if (revenueByDateMap.TryGetValue(dateKey, out var revStats))
                {
                    revenue = revStats.Revenue;
                    count = revStats.Count;
                }

                if (expensesByDateMap.TryGetValue(dateKey, out var expAmount))
                {
                    expenses = expAmount;
                }

                revenueByDate.Add(new RevenueByDateDto
                {
                    Date = d.ToString("yyyy-MM-dd"),
                    Revenue = revenue,
                    Expenses = expenses,
                    OrderCount = count
                });
            }

            // Orders by status
            var ordersByStatus = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusDto
                {
                    Status = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            // Payment methods
            var paymentMethods = await _context.Payments
                .Where(p => p.PaymentDate >= periodStart)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new PaymentMethodDto
                {
                    Method = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToListAsync();

            return new DashboardStatisticsDto
            {
                TodayRevenue = todayRevenue,
                MonthRevenue = monthRevenue,
                MonthExpenses = monthExpenses,
                MonthProfit = monthProfit,
                YearRevenue = yearRevenue,
                IncomeGrowth = Math.Round(incomeGrowth, 1),
                ExpenseGrowth = Math.Round(expenseGrowth, 1),
                TodayOrders = todayOrders.Count,
                MonthOrders = monthOrders.Count,
                TotalProducts = totalProducts,
                LowStockProducts = lowStockProducts,
                TotalCustomers = totalCustomers,
                ActiveCustomers = activeCustomers,
                AverageOrderValue = avgOrderValue,
                TopSellingProducts = topProducts,
                RevenueByDate = revenueByDate,
                OrdersByStatus = ordersByStatus,
                PaymentMethods = paymentMethods
            };
        }

        public async Task<SalesReportDto> GetSalesReportAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.Now.AddMonths(-1);
            var end = endDate ?? DateTime.Now;

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p!.Category)
                .Where(o => o.OrderDate >= start && o.OrderDate <= end && o.Status != "cancelled")
                .ToListAsync();

            var totalRevenue = orders.Sum(o => o.TotalAmount);
            var totalOrders = orders.Count;
            var totalItemsSold = orders.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity);

            // Calculate cost and profit
            var totalCost = orders
                .SelectMany(o => o.OrderItems)
                .Sum(oi => (oi.Product?.CostPrice ?? 0) * oi.Quantity);

            var profit = totalRevenue - totalCost;
            var profitMargin = totalRevenue > 0 ? (profit / totalRevenue) * 100 : 0;

            // Daily revenue
            var dailyRevenue = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new RevenueByDateDto
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(r => r.Date)
                .ToList();

            // Top products
            var topProducts = orders
                .SelectMany(o => o.OrderItems)
                .GroupBy(oi => new { oi.ProductId, oi.Product!.ProductName, oi.Product.ImageUrl })
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key.ProductId ?? 0,
                    ProductName = g.Key.ProductName ?? string.Empty,
                    TotalQuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Subtotal),
                    ImageUrl = g.Key.ImageUrl
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(20)
                .ToList();

            // Sales by category
            var salesByCategory = orders
                .SelectMany(o => o.OrderItems)
                .Where(oi => oi.Product != null)
                .GroupBy(oi => new
                {
                    CategoryId = oi.Product!.CategoryId,
                    CategoryName = oi.Product.Category != null ? oi.Product.Category.CategoryName : "Uncategorized"
                })
                .Select(g => new CategorySalesDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    TotalRevenue = g.Sum(oi => oi.Subtotal),
                    TotalQuantity = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(c => c.TotalRevenue)
                .ToList();

            return new SalesReportDto
            {
                StartDate = start,
                EndDate = end,
                TotalRevenue = totalRevenue,
                TotalCost = totalCost,
                Profit = profit,
                ProfitMargin = profitMargin,
                TotalOrders = totalOrders,
                TotalItemsSold = totalItemsSold,
                DailyRevenue = dailyRevenue,
                TopProducts = topProducts,
                SalesByCategory = salesByCategory
            };
        }

        public async Task<InventoryStatisticsDto> GetInventoryStatisticsAsync(int lowStockThreshold = 10)
        {
            var inventories = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .ToListAsync();

            var totalProducts = inventories.Select(i => i.ProductId).Distinct().Count();
            var lowStockProducts = inventories.Where(i => i.Quantity < lowStockThreshold && i.Quantity > 0).Count();
            var outOfStockProducts = inventories.Where(i => i.Quantity == 0).Count();

            var totalInventoryValue = inventories.Sum(i => i.Quantity * (i.Product?.Price ?? 0));

            // Low stock items
            var lowStockItems = inventories
                .Where(i => i.Quantity < lowStockThreshold)
                .Select(i => new LowStockProductDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.ProductName ?? "Unknown",
                    CurrentStock = i.Quantity,
                    WarehouseName = i.Warehouse?.WarehouseName ?? "Main",
                    Price = i.Product?.Price ?? 0
                })
                .OrderBy(i => i.CurrentStock)
                .Take(50)
                .ToList();

            // Stock by warehouse
            var stockByWarehouse = inventories
                .GroupBy(i => new
                {
                    WarehouseId = i.WarehouseId,
                    WarehouseName = i.Warehouse != null ? i.Warehouse.WarehouseName : "Main Warehouse"
                })
                .Select(g => new WarehouseStockStatisticsDto
                {
                    WarehouseId = g.Key.WarehouseId,
                    WarehouseName = g.Key.WarehouseName,
                    TotalProducts = g.Count(),
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalValue = g.Sum(i => i.Quantity * (i.Product != null ? i.Product.Price : 0))
                })
                .ToList();

            return new InventoryStatisticsDto
            {
                TotalProducts = totalProducts,
                LowStockProducts = lowStockProducts,
                OutOfStockProducts = outOfStockProducts,
                TotalInventoryValue = totalInventoryValue,
                LowStockItems = lowStockItems,
                StockByWarehouse = stockByWarehouse
            };
        }

        public async Task<CustomerStatisticsDto> GetCustomerStatisticsAsync()
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var totalCustomers = await _context.Customers
                .Where(c => c.Status == "active")
                .CountAsync();

            var newCustomersThisMonth = await _context.Customers
                .Where(c => c.CreatedAt >= monthStart)
                .CountAsync();

            var activeCustomers = await _context.Customers
                .Where(c => c.Status == "active" && c.Orders.Any(o => o.OrderDate >= monthStart))
                .CountAsync();

            // Top customers by revenue
            var topCustomers = await _context.Customers
                .Where(c => c.Status == "active")
                .Select(c => new TopCustomerDto
                {
                    CustomerId = c.CustomerId,
                    CustomerName = c.Name,
                    TotalOrders = c.Orders.Count(o => o.Status != "cancelled"),
                    TotalSpent = c.Orders.Where(o => o.Status != "cancelled").Sum(o => o.TotalAmount),
                    Phone = c.Phone,
                    Email = c.Email
                })
                .Where(c => c.TotalOrders > 0)
                .OrderByDescending(c => c.TotalSpent)
                .Take(20)
                .ToListAsync();

            return new CustomerStatisticsDto
            {
                TotalCustomers = totalCustomers,
                NewCustomersThisMonth = newCustomersThisMonth,
                ActiveCustomers = activeCustomers,
                TopCustomers = topCustomers
            };
        }

        public async Task<List<TransactionDto>> GetRecentTransactionsAsync(int limit)
        {
            // Get Sales (Orders)
            var sales = await _context.Orders
                .Include(o => o.Customer)
                .OrderByDescending(o => o.OrderDate)
                .Take(limit)
                .Select(o => new TransactionDto
                {
                    Id = o.OrderId,
                    ReferenceCode = $"ORD-{o.OrderId}",
                    Type = "Sale",
                    PartnerName = o.Customer != null ? o.Customer.Name : "Guest",
                    Date = o.OrderDate,
                    Amount = o.TotalAmount,
                    Status = o.Status
                })
                .ToListAsync();

            // Get Imports (PurchaseOrders)
            var imports = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(limit)
                .Select(p => new TransactionDto
                {
                    Id = p.PurchaseId,
                    ReferenceCode = $"PO-{p.PurchaseId}",
                    Type = "Import",
                    PartnerName = p.Supplier != null ? p.Supplier.Name : "Unknown Supplier",
                    Date = p.PurchaseDate,
                    Amount = p.TotalAmount,
                    Status = p.Status
                })
                .ToListAsync();

            // Merge and Sort
            var transactions = sales.Concat(imports)
                .OrderByDescending(t => t.Date)
                .Take(limit)
                .ToList();

            return transactions;
        }
    }
}
