using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;
using Microsoft.AspNetCore.Identity;
using BlazorApp1.Data;

namespace StoreManagementAPI.Services
{
    public interface IPurchaseOrderService
    {
        Task<List<PurchaseOrderResponseDto>> GetAllPurchaseOrders();
        Task<PurchaseOrderResponseDto?> GetPurchaseOrderById(int purchaseId);
        Task<PurchaseOrderResponseDto> CreatePurchaseOrder(CreatePurchaseOrderDto dto, string userId);
        Task<PurchaseOrderResponseDto> UpdatePurchaseOrderStatus(int purchaseId, string status);
        Task<bool> DeletePurchaseOrder(int purchaseId);
    }

    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly StoreDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public PurchaseOrderService(
            StoreDbContext context, 
            IAuditLogService auditLogService, 
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        private (string? userId, string? username) GetAuditInfo()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return (null, "system");

            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var usernameClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var username = !string.IsNullOrEmpty(usernameClaim) ? usernameClaim : "system";

            return (userIdClaim, username);
        }

        public async Task<List<PurchaseOrderResponseDto>> GetAllPurchaseOrders()
        {
            var purchaseOrders = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.PurchaseItems)
                    .ThenInclude(pi => pi.Product)
                .OrderByDescending(po => po.PurchaseDate)
                .ToListAsync();

            // Fetch users
            var userIds = purchaseOrders.Select(po => po.UserId).Distinct().ToList();
            var userMap = new Dictionary<string, string>();
            foreach(var uid in userIds)
            {
                if (!string.IsNullOrEmpty(uid))
                {
                    var user = await _userManager.FindByIdAsync(uid);
                    if (user != null) userMap[uid] = user.UserName ?? "Unknown";
                }
            }

            return purchaseOrders.Select(po => {
                string? userName = null;
                if (!string.IsNullOrEmpty(po.UserId) && userMap.ContainsKey(po.UserId))
                {
                    userName = userMap[po.UserId];
                }
                return MapToResponseDto(po, userName);
            }).ToList();
        }

        public async Task<PurchaseOrderResponseDto?> GetPurchaseOrderById(int purchaseId)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.PurchaseItems)
                    .ThenInclude(pi => pi.Product)
                .FirstOrDefaultAsync(po => po.PurchaseId == purchaseId);

            if (purchaseOrder == null)
                return null;

            string? userName = null;
            if (!string.IsNullOrEmpty(purchaseOrder.UserId))
            {
                var user = await _userManager.FindByIdAsync(purchaseOrder.UserId);
                userName = user?.UserName;
            }

            return MapToResponseDto(purchaseOrder, userName);
        }

        public async Task<PurchaseOrderResponseDto> CreatePurchaseOrder(CreatePurchaseOrderDto dto, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create purchase order
                var purchaseOrder = new PurchaseOrder
                {
                    SupplierId = dto.SupplierId,
                    UserId = userId,
                    WarehouseId = dto.WarehouseId,
                    PurchaseDate = DateTime.Now,
                    Status = "pending",
                    TotalAmount = 0
                };

                _context.PurchaseOrders.Add(purchaseOrder);
                await _context.SaveChangesAsync();

                // Add purchase items
                decimal totalAmount = 0;
                foreach (var item in dto.Items)
                {
                    var subtotal = item.Quantity * item.CostPrice;
                    totalAmount += subtotal;

                    var purchaseItem = new PurchaseItem
                    {
                        PurchaseId = purchaseOrder.PurchaseId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        CostPrice = item.CostPrice,
                        Subtotal = subtotal
                    };

                    _context.PurchaseItems.Add(purchaseItem);

                    // Update product cost price
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.CostPrice = item.CostPrice;
                    }

                    // TỰ ĐỘNG CẬP NHẬT TỒN KHO NGAY KHI TẠO PHIẾU NHẬP
                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == dto.WarehouseId);

                    if (inventory != null)
                    {
                        // Update existing inventory
                        inventory.Quantity += item.Quantity;
                        inventory.UpdatedAt = DateTime.Now;
                    }
                    else
                    {
                        // Create new inventory record for this warehouse
                        var newInventory = new Inventory
                        {
                            ProductId = item.ProductId,
                            WarehouseId = dto.WarehouseId,
                            Quantity = item.Quantity,
                            UpdatedAt = DateTime.Now
                        };
                        _context.Inventories.Add(newInventory);
                    }
                }

                // Update total amount
                purchaseOrder.TotalAmount = totalAmount;
                
                // Set status to completed automatically
                purchaseOrder.Status = "completed";
                
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Log audit
                var (auditUserId, auditUsername) = GetAuditInfo();
                var itemsInfo = dto.Items.Select(i => new
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    CostPrice = i.CostPrice,
                    Subtotal = i.Quantity * i.CostPrice
                }).ToList();

                await _auditLogService.LogActionAsync(
                    action: "CREATE",
                    entityType: "PurchaseOrder",
                    entityId: purchaseOrder.PurchaseId,
                    entityName: $"PN{purchaseOrder.PurchaseId:D6}",
                    oldValues: null,
                    newValues: new
                    {
                        PurchaseId = purchaseOrder.PurchaseId,
                        SupplierId = purchaseOrder.SupplierId,
                        WarehouseId = purchaseOrder.WarehouseId,
                        TotalAmount = purchaseOrder.TotalAmount,
                        Status = purchaseOrder.Status,
                        ItemCount = dto.Items.Count,
                        Items = itemsInfo
                    },
                    changesSummary: $"Tạo phiếu nhập hàng PN{purchaseOrder.PurchaseId:D6} - Nhà cung cấp ID {purchaseOrder.SupplierId} - Tổng tiền: {totalAmount:N0} VNĐ - {dto.Items.Count} sản phẩm",
                    userId: auditUserId,
                    username: auditUsername,
                    additionalInfo: new Dictionary<string, object>
                    {
                        { "ItemCount", dto.Items.Count },
                        { "WarehouseId", dto.WarehouseId },
                        { "AutoCompleted", true }
                    }
                );

                // Return the created purchase order
                return (await GetPurchaseOrderById(purchaseOrder.PurchaseId))!;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PurchaseOrderResponseDto> UpdatePurchaseOrderStatus(int purchaseId, string status)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.PurchaseItems)
                .FirstOrDefaultAsync(po => po.PurchaseId == purchaseId);

            if (purchaseOrder == null)
                throw new Exception("Không tìm thấy phiếu nhập hàng");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var oldStatus = purchaseOrder.Status;
                purchaseOrder.Status = status;

                // If status is completed, update inventory in the specified warehouse
                if (status == "completed")
                {
                    foreach (var item in purchaseOrder.PurchaseItems)
                    {
                        // Find inventory for this product in the specified warehouse
                        var inventory = await _context.Inventories
                            .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.WarehouseId == purchaseOrder.WarehouseId);

                        if (inventory != null)
                        {
                            // Update existing inventory
                            inventory.Quantity += item.Quantity;
                            inventory.UpdatedAt = DateTime.Now;
                        }
                        else
                        {
                            // Create new inventory record for this warehouse
                            var newInventory = new Inventory
                            {
                                ProductId = item.ProductId,
                                WarehouseId = purchaseOrder.WarehouseId,
                                Quantity = item.Quantity,
                                UpdatedAt = DateTime.Now
                            };
                            _context.Inventories.Add(newInventory);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Log audit
                var (userId, username) = GetAuditInfo();
                await _auditLogService.LogActionAsync(
                    action: "UPDATE",
                    entityType: "PurchaseOrder",
                    entityId: purchaseId,
                    entityName: $"PN{purchaseId:D6}",
                    oldValues: new { Status = oldStatus },
                    newValues: new { Status = status },
                    changesSummary: $"Cập nhật trạng thái phiếu nhập hàng PN{purchaseId:D6}: {oldStatus} → {status}",
                    userId: userId,
                    username: username,
                    additionalInfo: status == "completed" 
                        ? new Dictionary<string, object> { { "InventoryUpdated", true }, { "ItemCount", purchaseOrder.PurchaseItems.Count } }
                        : null
                );

                await transaction.CommitAsync();

                return (await GetPurchaseOrderById(purchaseId))!;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeletePurchaseOrder(int purchaseId)
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.PurchaseItems)
                .FirstOrDefaultAsync(po => po.PurchaseId == purchaseId);

            if (purchaseOrder == null)
                return false;

            if (purchaseOrder.Status == "completed")
                throw new Exception("Không thể xóa phiếu nhập đã hoàn thành");

            // Save info for audit log
            var purchaseInfo = new
            {
                PurchaseId = purchaseOrder.PurchaseId,
                SupplierId = purchaseOrder.SupplierId,
                TotalAmount = purchaseOrder.TotalAmount,
                Status = purchaseOrder.Status,
                ItemCount = purchaseOrder.PurchaseItems.Count
            };

            _context.PurchaseOrders.Remove(purchaseOrder);
            await _context.SaveChangesAsync();

            // Log audit
            var (userId, username) = GetAuditInfo();
            await _auditLogService.LogActionAsync(
                action: "DELETE",
                entityType: "PurchaseOrder",
                entityId: purchaseId,
                entityName: $"PN{purchaseId:D6}",
                oldValues: purchaseInfo,
                newValues: null,
                changesSummary: $"Xóa phiếu nhập hàng PN{purchaseId:D6} - Tổng tiền: {purchaseOrder.TotalAmount:N0} VNĐ",
                userId: userId,
                username: username
            );

            return true;
        }

        private PurchaseOrderResponseDto MapToResponseDto(PurchaseOrder po, string? userName = null)
        {
            return new PurchaseOrderResponseDto
            {
                PurchaseId = po.PurchaseId,
                SupplierId = po.SupplierId,
                SupplierName = po.Supplier?.Name,
                WarehouseId = po.WarehouseId ?? 0,
                WarehouseName = po.Warehouse?.WarehouseName,
                UserId = po.UserId,
                UserName = userName,
                Username = userName,
                PurchaseDate = po.PurchaseDate,
                TotalAmount = po.TotalAmount,
                Status = po.Status,
                Items = po.PurchaseItems.Select(pi => new PurchaseItemResponseDto
                {
                    PurchaseItemId = pi.PurchaseItemId,
                    ProductId = pi.ProductId,
                    ProductName = pi.Product?.ProductName,
                    Barcode = pi.Product?.Barcode,
                    Quantity = pi.Quantity,
                    CostPrice = pi.CostPrice,
                    Subtotal = pi.Subtotal
                }).ToList()
            };
        }
    }
}
