using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;
using Microsoft.AspNetCore.Identity;
using BlazorApp1.Data;

namespace StoreManagementAPI.Services
{
    public interface IWarehouseService
    {
        Task<List<WarehouseDto>> GetAllWarehouses();
        Task<WarehouseDto?> GetWarehouseById(int warehouseId);
        Task<WarehouseDto> CreateWarehouse(WarehouseDto dto);
        Task<WarehouseDto> UpdateWarehouse(int warehouseId, WarehouseDto dto);
        Task<bool> DeleteWarehouse(int warehouseId);
        Task<List<WarehouseTransactionHistoryDto>> GetWarehouseTransactionHistory(WarehouseHistoryFilterDto? filter = null);
    }

    public class WarehouseService : IWarehouseService
    {
        private readonly StoreDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WarehouseService(StoreDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<WarehouseDto>> GetAllWarehouses()
        {
            var warehouses = await _context.Warehouses
                .Where(w => w.Status != "deleted")
                .ToListAsync();

            return warehouses.Select(w => MapToDto(w)).ToList();
        }

        public async Task<WarehouseDto?> GetWarehouseById(int warehouseId)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && w.Status != "deleted");

            if (warehouse == null)
                return null;

            return MapToDto(warehouse);
        }

        public async Task<WarehouseDto> CreateWarehouse(WarehouseDto dto)
        {
            var warehouse = new Warehouse
            {
                WarehouseName = dto.WarehouseName,
                Address = dto.Address,
                Status = "active"
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            return MapToDto(warehouse);
        }

        public async Task<WarehouseDto> UpdateWarehouse(int warehouseId, WarehouseDto dto)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && w.Status != "deleted");

            if (warehouse == null)
                throw new Exception("Không tìm thấy kho hàng");

            warehouse.WarehouseName = dto.WarehouseName;
            warehouse.Address = dto.Address;
            warehouse.Status = dto.Status;

            await _context.SaveChangesAsync();

            return MapToDto(warehouse);
        }

        public async Task<bool> DeleteWarehouse(int warehouseId)
        {
            var warehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId);

            if (warehouse == null)
                return false;

            // Soft delete
            warehouse.Status = "deleted";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<WarehouseTransactionHistoryDto>> GetWarehouseTransactionHistory(WarehouseHistoryFilterDto? filter = null)
        {
            var transactions = new List<WarehouseTransactionHistoryDto>();

            // Lấy lịch sử NHẬP HÀNG từ PurchaseOrder
            var purchaseQuery = _context.PurchaseOrders
                .Include(po => po.PurchaseItems)
                    .ThenInclude(pi => pi.Product)
                .Include(po => po.Warehouse)
                .Include(po => po.Supplier)
                .Where(po => po.Status == "completed")
                .AsQueryable();

            if (filter?.WarehouseId.HasValue == true)
                purchaseQuery = purchaseQuery.Where(po => po.WarehouseId == filter.WarehouseId.Value);

            if (filter?.FromDate.HasValue == true)
                purchaseQuery = purchaseQuery.Where(po => po.PurchaseDate >= filter.FromDate.Value);

            if (filter?.ToDate.HasValue == true)
                purchaseQuery = purchaseQuery.Where(po => po.PurchaseDate <= filter.ToDate.Value);

            var purchaseOrders = await purchaseQuery.ToListAsync();

            // Fetch users for purchase orders
            var purchaseUserIds = purchaseOrders.Select(po => po.UserId).Distinct().ToList();
            var purchaseUserMap = new Dictionary<string, string>();
            foreach(var uid in purchaseUserIds)
            {
                if (!string.IsNullOrEmpty(uid))
                {
                    var user = await _userManager.FindByIdAsync(uid);
                    if (user != null) purchaseUserMap[uid] = user.UserName ?? "Unknown";
                }
            }

            foreach (var po in purchaseOrders)
            {
                foreach (var item in po.PurchaseItems)
                {
                    if (filter?.ProductId.HasValue == true && item.ProductId != filter.ProductId.Value)
                        continue;

                    transactions.Add(new WarehouseTransactionHistoryDto
                    {
                        TransactionId = po.PurchaseId,
                        TransactionType = "IN",
                        TransactionDate = po.PurchaseDate,
                        ProductId = item.ProductId,
                        ProductName = item.Product?.ProductName ?? "N/A",
                        Quantity = item.Quantity,
                        Price = item.CostPrice,
                        TotalAmount = item.Subtotal,
                        WarehouseId = po.WarehouseId,
                        WarehouseName = po.Warehouse?.WarehouseName,
                        SupplierName = po.Supplier?.Name,
                        Username = (!string.IsNullOrEmpty(po.UserId) && purchaseUserMap.ContainsKey(po.UserId)) ? purchaseUserMap[po.UserId] : null,
                        Status = po.Status,
                        Notes = $"Nhập hàng từ {po.Supplier?.Name}"
                    });
                }
            }

            // Lấy lịch sử XUẤT HÀNG (BÁN) từ Order
            if (filter?.TransactionType == null || filter.TransactionType == "OUT")
            {
                var orderQuery = _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.Customer)
                    .Where(o => o.Status != "canceled")
                    .AsQueryable();

                if (filter?.FromDate.HasValue == true)
                    orderQuery = orderQuery.Where(o => o.OrderDate >= filter.FromDate.Value);

                if (filter?.ToDate.HasValue == true)
                    orderQuery = orderQuery.Where(o => o.OrderDate <= filter.ToDate.Value);

                var orders = await orderQuery.ToListAsync();

                // Fetch users for orders
                var orderUserIds = orders.Select(o => o.UserId).Distinct().ToList();
                var orderUserMap = new Dictionary<string, string>();
                foreach(var uid in orderUserIds)
                {
                    if (!string.IsNullOrEmpty(uid))
                    {
                        var user = await _userManager.FindByIdAsync(uid);
                        if (user != null) orderUserMap[uid] = user.UserName ?? "Unknown";
                    }
                }

                foreach (var order in orders)
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (filter?.ProductId.HasValue == true && item.ProductId != filter.ProductId.Value)
                            continue;

                        // Tìm warehouse của sản phẩm này (lấy warehouse đầu tiên có tồn kho)
                        var inventory = await _context.Inventories
                            .Include(i => i.Warehouse)
                            .FirstOrDefaultAsync(i => i.ProductId == item.ProductId);

                        if (filter?.WarehouseId.HasValue == true && inventory?.WarehouseId.GetValueOrDefault() != filter.WarehouseId.Value)
                            continue;

                        transactions.Add(new WarehouseTransactionHistoryDto
                        {
                            TransactionId = order.OrderId,
                            TransactionType = "OUT",
                            TransactionDate = order.OrderDate,
                            ProductId = item.ProductId,
                            ProductName = item.Product?.ProductName ?? "N/A",
                            Quantity = item.Quantity,
                            Price = item.Price,
                            TotalAmount = item.Subtotal,
                            WarehouseId = inventory?.WarehouseId,
                            WarehouseName = inventory?.Warehouse?.WarehouseName,
                            CustomerName = order.Customer?.Name,
                            Username = (!string.IsNullOrEmpty(order.UserId) && orderUserMap.ContainsKey(order.UserId)) ? orderUserMap[order.UserId] : null,
                            Status = order.Status,
                            Notes = $"Bán hàng cho {order.Customer?.Name}"
                        });
                    }
                }
            }

            // Sắp xếp theo thời gian mới nhất
            return transactions.OrderByDescending(t => t.TransactionDate).ToList();
        }

        private WarehouseDto MapToDto(Warehouse w)
        {
            return new WarehouseDto
            {
                WarehouseId = w.WarehouseId,
                WarehouseName = w.WarehouseName,
                Address = w.Address,
                Status = w.Status
            };
        }
    }
}
