using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;

namespace StoreManagementAPI.Services
{
    public interface IInventoryService
    {
        Task<List<InventoryResponseDto>> GetAllInventory();
        Task<List<InventoryResponseDto>> GetInventoryByWarehouse(int warehouseId);
        Task<InventoryResponseDto> AddStock(StockReceiptDto dto);
        Task<ProductInventoryDetailDto> GetProductInventoryDetail(int productId);
        Task<RecalculateStockResponseDto> RecalculateAllStock();
    }

    public class InventoryService : IInventoryService
    {
        private readonly StoreDbContext _context;

        public InventoryService(StoreDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryResponseDto>> GetAllInventory()
        {
            var inventories = await _context.Inventories
                .Include(i => i.Product)
                    .ThenInclude(p => p.Category)
                .Include(i => i.Warehouse)
                .ToListAsync();

            return inventories.Select(i => new InventoryResponseDto
            {
                InventoryId = i.InventoryId,
                ProductId = i.ProductId,
                ProductName = i.Product.ProductName,
                CategoryName = i.Product.Category != null ? i.Product.Category.CategoryName : null,
                WarehouseId = i.WarehouseId,
                WarehouseName = i.Warehouse != null ? i.Warehouse.WarehouseName : null,
                Quantity = i.Quantity,
                Unit = i.Product.Unit ?? "Cái",
                CostPrice = i.Product.CostPrice,
                Price = i.Product.Price,
                UpdatedAt = i.UpdatedAt
            }).ToList();
        }

        public async Task<List<InventoryResponseDto>> GetInventoryByWarehouse(int warehouseId)
        {
            var inventories = await _context.Inventories
                .Include(i => i.Product)
                .Where(i => i.WarehouseId == warehouseId)
                .Select(i => new InventoryResponseDto
                {
                    InventoryId = i.InventoryId,
                    ProductId = i.ProductId,
                    ProductName = i.Product.ProductName,
                    Quantity = i.Quantity,
                    UpdatedAt = i.UpdatedAt
                })
                .ToListAsync();

            return inventories;
        }

        public async Task<InventoryResponseDto> AddStock(StockReceiptDto dto)
        {
            // Validate product exists
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                throw new Exception("Sản phẩm không tồn tại");
            }

            if (dto.Quantity <= 0)
            {
                throw new Exception("Số lượng phải lớn hơn 0");
            }

            // Check if inventory exists
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == dto.ProductId);

            if (inventory == null)
            {
                // Create new inventory
                inventory = new Inventory
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UpdatedAt = DateTime.Now
                };
                _context.Inventories.Add(inventory);
            }
            else
            {
                // Update existing inventory
                inventory.Quantity += dto.Quantity;
                inventory.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Return updated inventory with product info
            var updatedInventory = await _context.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.InventoryId == inventory.InventoryId);

            return new InventoryResponseDto
            {
                InventoryId = updatedInventory!.InventoryId,
                ProductId = updatedInventory.ProductId,
                ProductName = updatedInventory.Product.ProductName,
                Quantity = updatedInventory.Quantity,
                UpdatedAt = updatedInventory.UpdatedAt
            };
        }

        public async Task<ProductInventoryDetailDto> GetProductInventoryDetail(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
            {
                throw new Exception("Sản phẩm không tồn tại");
            }

            var inventories = await _context.Inventories
                .Include(i => i.Warehouse)
                .Where(i => i.ProductId == productId)
                .ToListAsync();

            var totalStock = inventories.Sum(i => i.Quantity);

            var warehouseStocks = inventories
                .Where(i => i.Warehouse != null)
                .Select(i => new WarehouseStockDto
                {
                    WarehouseId = i.WarehouseId ?? 0,
                    WarehouseName = i.Warehouse!.WarehouseName ?? "Không xác định",
                    Quantity = i.Quantity
                })
                .ToList();

            return new ProductInventoryDetailDto
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Barcode = product.Barcode,
                CategoryName = product.Category?.CategoryName ?? "Chưa phân loại",
                Unit = product.Unit ?? "Cái",
                TotalStock = totalStock,
                Warehouses = warehouseStocks
            };
        }

        public async Task<RecalculateStockResponseDto> RecalculateAllStock()
        {
            // ==========================================
            // TẠM THỜI TẮT CHỨC NĂNG TỰ ĐỘNG TÍNH TOÁN
            // Để debug và tìm điểm leak số lượng
            // ==========================================
            return new RecalculateStockResponseDto
            {
                Message = "⚠️ Chức năng tự động tính toán đã bị TẮT để debug. Vui lòng bật lại sau khi hoàn thành!",
                TotalProductsUpdated = 0,
                TotalWarehousesUpdated = 0,
                Details = new List<string> { "Chức năng đã bị vô hiệu hóa tạm thời" }
            };

            /* COMMENTED OUT - Uncomment để bật lại
            var details = new List<string>();
            var productsUpdated = 0;
            var warehousesUpdated = 0;

            // Lấy tất cả sản phẩm
            var products = await _context.Products.ToListAsync();

            foreach (var product in products)
            {
                // Tính tổng nhập từ PurchaseOrders
                var totalPurchased = await _context.PurchaseItems
                    .Where(pi => pi.ProductId == product.ProductId)
                    .SumAsync(pi => (int?)pi.Quantity) ?? 0;

                // Tính tổng xuất từ Orders (bán hàng)
                var totalSold = await _context.OrderItems
                    .Where(oi => oi.ProductId == product.ProductId)
                    .SumAsync(oi => (int?)oi.Quantity) ?? 0;

                // Tồn kho = Nhập - Xuất
                var calculatedStock = totalPurchased - totalSold;

                // Cập nhật hoặc tạo mới Inventory
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == product.ProductId && i.WarehouseId == null);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = product.ProductId,
                        Quantity = calculatedStock,
                        WarehouseId = null,
                        UpdatedAt = DateTime.Now
                    };
                    _context.Inventories.Add(inventory);
                }
                else
                {
                    inventory.Quantity = calculatedStock;
                    inventory.UpdatedAt = DateTime.Now;
                }

                productsUpdated++;
                details.Add($"SP #{product.ProductId} ({product.ProductName}): Nhập {totalPurchased}, Bán {totalSold}, Tồn {calculatedStock}");
            }

            await _context.SaveChangesAsync();

            return new RecalculateStockResponseDto
            {
                Message = "Tính toán lại tồn kho thành công!",
                TotalProductsUpdated = productsUpdated,
                TotalWarehousesUpdated = warehousesUpdated,
                Details = details
            };
            */
        }
    }
}
