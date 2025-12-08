using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;
using StoreManagementAPI.Repositories;

namespace StoreManagementAPI.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();
        Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm);
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto?> GetProductByBarcodeAsync(string barcode);
        Task<ProductDto> CreateProductAsync(CreateProductDto dto);
        Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto dto);
        Task<DeleteProductResult> DeleteProductAsync(int id);
        Task<bool> UpdateStockAsync(UpdateStockDto dto);
        Task<IEnumerable<ProductHistoryDto>> GetProductHistoryAsync(int productId);
    }

    public class DeleteProductResult
    {
        public bool Success { get; set; }
        public bool SoftDeleted { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ProductService : IProductService
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Inventory> _inventoryRepository;
        private readonly StoreDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductService(
            IRepository<Product> productRepository,
            IRepository<Inventory> inventoryRepository,
            StoreDbContext context,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _productRepository = productRepository;
            _inventoryRepository = inventoryRepository;
            _context = context;
            _auditLogService = auditLogService;
            _httpContextAccessor = httpContextAccessor;
        }

        private (int? userId, string? username) GetAuditInfo()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return (1, "admin"); // Default to admin user (id=1)

            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var usernameClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            int? userId = null;
            if (int.TryParse(userIdClaim, out int parsedUserId))
                userId = parsedUserId;

            // Nếu không có username từ authentication, dùng "admin" làm mặc định
            var username = !string.IsNullOrEmpty(usernameClaim) ? usernameClaim : "admin";
            var finalUserId = userId ?? 1; // Default to user id = 1 (admin)

            return (finalUserId, username);
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Inventories)
                .Include(p => p.OrderItems)
                .ToListAsync();

            return products.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.CategoryName,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier?.Name,
                ProductName = p.ProductName,
                Barcode = p.Barcode,
                Price = p.Price,
                CostPrice = p.CostPrice,
                Unit = p.Unit,
                Status = p.Status,
                StockQuantity = p.Inventories?.Sum(i => i.Quantity) ?? 0,
                HasOrders = p.OrderItems != null && p.OrderItems.Any()
            });
        }

        public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllProductsAsync();
            }

            searchTerm = searchTerm.ToLower().Trim();

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Inventories)
                .Include(p => p.OrderItems)
                .Where(p =>
                    p.ProductName.ToLower().Contains(searchTerm) ||
                    (p.Barcode != null && p.Barcode.ToLower().Contains(searchTerm)) ||
                    (p.Category != null && p.Category.CategoryName.ToLower().Contains(searchTerm)) ||
                    (p.Supplier != null && p.Supplier.Name.ToLower().Contains(searchTerm)))
                .ToListAsync();

            return products.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.CategoryName,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier?.Name,
                ProductName = p.ProductName,
                Barcode = p.Barcode,
                Price = p.Price,
                CostPrice = p.CostPrice,
                Unit = p.Unit,
                Status = p.Status,
                StockQuantity = p.Inventories?.Sum(i => i.Quantity) ?? 0,
                HasOrders = p.OrderItems != null && p.OrderItems.Any()
            });
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Inventories)
                .Include(p => p.OrderItems)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return null;

            return new ProductDto
            {
                ProductId = product.ProductId,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.CategoryName,
                SupplierId = product.SupplierId,
                SupplierName = product.Supplier?.Name,
                ProductName = product.ProductName,
                Barcode = product.Barcode,
                Price = product.Price,
                CostPrice = product.CostPrice,
                Unit = product.Unit,
                Status = product.Status,
                StockQuantity = product.Inventories?.Sum(i => i.Quantity) ?? 0,
                HasOrders = product.OrderItems != null && product.OrderItems.Any()
            };
        }

        public async Task<ProductDto?> GetProductByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return null;
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Inventories)
                .Include(p => p.OrderItems)
                .FirstOrDefaultAsync(p => p.Barcode == barcode.Trim());

            if (product == null) return null;

            return new ProductDto
            {
                ProductId = product.ProductId,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.CategoryName,
                SupplierId = product.SupplierId,
                SupplierName = product.Supplier?.Name,
                ProductName = product.ProductName,
                Barcode = product.Barcode,
                Price = product.Price,
                CostPrice = product.CostPrice,
                Unit = product.Unit,
                Status = product.Status,
                StockQuantity = product.Inventories?.Sum(i => i.Quantity) ?? 0,
                HasOrders = product.OrderItems != null && product.OrderItems.Any()
            };
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
        {
            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Tự động tạo barcode nếu không có
                if (string.IsNullOrWhiteSpace(dto.Barcode))
                {
                    dto.Barcode = await GenerateNextBarcodeAsync();
                }

                var product = new Product
                {
                    CategoryId = dto.CategoryId,
                    SupplierId = dto.SupplierId,
                    ProductName = dto.ProductName,
                    Barcode = dto.Barcode,
                    Price = dto.Price,
                    CostPrice = 0, // Giá nhập sẽ được cập nhật khi nhập hàng
                    Unit = dto.Unit,
                    CreatedAt = DateTime.Now
                };

                var createdProduct = await _productRepository.AddAsync(product);

                // Lấy warehouse mặc định (warehouse đầu tiên)
                var defaultWarehouse = await _context.Warehouses
                    .OrderBy(w => w.WarehouseId)
                    .FirstOrDefaultAsync();

                if (defaultWarehouse == null)
                {
                    throw new Exception("Không tìm thấy kho hàng. Vui lòng tạo kho hàng trước khi thêm sản phẩm.");
                }

                // Tạo inventory với số lượng ban đầu = 0 (sẽ nhập kho sau)
                var inventory = new Inventory
                {
                    ProductId = createdProduct.ProductId,
                    WarehouseId = defaultWarehouse.WarehouseId,
                    Quantity = 0 // Sản phẩm mới không có hàng, cần nhập kho
                };
                await _inventoryRepository.AddAsync(inventory);

                // Log audit
                var (userId, username) = GetAuditInfo();
                await _auditLogService.LogActionAsync(
                    action: "CREATE",
                    entityType: "Product",
                    entityId: createdProduct.ProductId,
                    entityName: createdProduct.ProductName,
                    oldValues: null,
                    newValues: new
                    {
                        ProductId = createdProduct.ProductId,
                        ProductName = createdProduct.ProductName,
                        Barcode = createdProduct.Barcode,
                        Price = createdProduct.Price,
                        CostPrice = createdProduct.CostPrice,
                        CategoryId = createdProduct.CategoryId,
                        SupplierId = createdProduct.SupplierId,
                        Unit = createdProduct.Unit
                    },
                    changesSummary: $"Tạo sản phẩm mới: {createdProduct.ProductName} (Barcode: {createdProduct.Barcode}, Giá: {createdProduct.Price:N0} VNĐ)",
                    userId: userId,
                    username: username
                );

                // Commit transaction nếu mọi thứ thành công
                await transaction.CommitAsync();

                return await GetProductByIdAsync(createdProduct.ProductId) ?? new ProductDto();
            }
            catch
            {
                // Rollback nếu có lỗi
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<string> GenerateNextBarcodeAsync()
        {
            // Tạo EAN13 ngẫu nhiên bắt đầu từ 890, không trùng
            string barcode;
            var random = new Random();
            int attempts = 0;
            const int maxAttempts = 1000; // Tránh vòng lặp vô hạn

            do
            {
                // Tạo 9 chữ số ngẫu nhiên sau 890
                string randomPart = "";
                for (int i = 0; i < 9; i++)
                {
                    randomPart += random.Next(0, 10).ToString();
                }

                string baseBarcode = "890" + randomPart; // 12 chữ số

                // Tính checksum EAN13
                int checksum = CalculateEAN13Checksum(baseBarcode);
                barcode = baseBarcode + checksum.ToString();

                attempts++;
                if (attempts >= maxAttempts)
                {
                    throw new Exception("Không thể tạo mã barcode duy nhất sau nhiều lần thử.");
                }
            }
            while (await _context.Products.AnyAsync(p => p.Barcode == barcode));

            return barcode;
        }

        private int CalculateEAN13Checksum(string barcode12)
        {
            if (barcode12.Length != 12)
                throw new ArgumentException("Barcode phải có 12 chữ số để tính checksum.");

            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = int.Parse(barcode12[i].ToString());
                sum += (i % 2 == 0) ? digit * 1 : digit * 3;
            }

            int checksum = (10 - (sum % 10)) % 10;
            return checksum;
        }

        public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _productRepository.GetByIdAsync(id);
                if (product == null) return null;

                // Lưu giá trị cũ để audit
                var oldValues = new
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Barcode = product.Barcode,
                    Price = product.Price,
                    CostPrice = product.CostPrice,
                    CategoryId = product.CategoryId,
                    SupplierId = product.SupplierId,
                    Unit = product.Unit,
                    Status = product.Status
                };

                var changes = new List<string>();

                if (dto.CategoryId.HasValue && product.CategoryId != dto.CategoryId)
                {
                    changes.Add($"Danh mục: {product.CategoryId} → {dto.CategoryId}");
                    product.CategoryId = dto.CategoryId;
                }
                if (dto.SupplierId.HasValue && product.SupplierId != dto.SupplierId)
                {
                    changes.Add($"Nhà cung cấp: {product.SupplierId} → {dto.SupplierId}");
                    product.SupplierId = dto.SupplierId;
                }
                if (!string.IsNullOrEmpty(dto.ProductName) && product.ProductName != dto.ProductName)
                {
                    changes.Add($"Tên: '{product.ProductName}' → '{dto.ProductName}'");
                    product.ProductName = dto.ProductName;
                }
                if (dto.Barcode != null && product.Barcode != dto.Barcode)
                {
                    changes.Add($"Barcode: {product.Barcode} → {dto.Barcode}");
                    product.Barcode = dto.Barcode;
                }
                if (dto.Price.HasValue && product.Price != dto.Price.Value)
                {
                    changes.Add($"Giá bán: {product.Price:N0} → {dto.Price.Value:N0} VNĐ");
                    product.Price = dto.Price.Value;
                }
                if (dto.CostPrice.HasValue && product.CostPrice != dto.CostPrice.Value)
                {
                    changes.Add($"Giá vốn: {product.CostPrice:N0} → {dto.CostPrice.Value:N0} VNĐ");
                    product.CostPrice = dto.CostPrice.Value;
                }
                if (!string.IsNullOrEmpty(dto.Unit) && product.Unit != dto.Unit)
                {
                    changes.Add($"Đơn vị: {product.Unit} → {dto.Unit}");
                    product.Unit = dto.Unit;
                }
                if (!string.IsNullOrEmpty(dto.Status) && product.Status != dto.Status)
                {
                    changes.Add($"Trạng thái: {product.Status} → {dto.Status}");
                    product.Status = dto.Status;
                }

                await _productRepository.UpdateAsync(product);

                // Log audit
                if (changes.Any())
                {
                    var (userId, username) = GetAuditInfo();
                    await _auditLogService.LogActionAsync(
                        action: "UPDATE",
                        entityType: "Product",
                        entityId: product.ProductId,
                        entityName: product.ProductName,
                        oldValues: oldValues,
                        newValues: new
                        {
                            ProductId = product.ProductId,
                            ProductName = product.ProductName,
                            Barcode = product.Barcode,
                            Price = product.Price,
                            CostPrice = product.CostPrice,
                            CategoryId = product.CategoryId,
                            SupplierId = product.SupplierId,
                            Unit = product.Unit,
                            Status = product.Status
                        },
                        changesSummary: $"Cập nhật sản phẩm '{product.ProductName}': {string.Join(", ", changes)}",
                        userId: userId,
                        username: username
                    );
                }

                await transaction.CommitAsync();
                return await GetProductByIdAsync(id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<DeleteProductResult> DeleteProductAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products
                    .Include(p => p.OrderItems)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return new DeleteProductResult
                    {
                        Success = false,
                        SoftDeleted = false,
                        Message = "Không tìm thấy sản phẩm"
                    };
                }

                var (userId, username) = GetAuditInfo();

                // Kiểm tra xem sản phẩm đã được bán chưa
                if (product.OrderItems != null && product.OrderItems.Any())
                {
                    // Đã bán => soft delete (chỉ ẩn đi)
                    var oldStatus = product.Status;
                    product.Status = "inactive";
                    await _productRepository.UpdateAsync(product);

                    // Log audit
                    await _auditLogService.LogActionAsync(
                        action: "SOFT_DELETE",
                        entityType: "Product",
                        entityId: product.ProductId,
                        entityName: product.ProductName,
                        oldValues: new { Status = oldStatus },
                        newValues: new { Status = "inactive" },
                        changesSummary: $"Ẩn sản phẩm '{product.ProductName}' (đã có đơn hàng, không thể xóa hẳn)",
                        userId: userId,
                        username: username
                    );

                    await transaction.CommitAsync();

                    return new DeleteProductResult
                    {
                        Success = true,
                        SoftDeleted = true,
                        Message = "Sản phẩm đã được bán nên đã được ẩn thay vì xóa"
                    };
                }

                // Chưa bán => xóa hẳn
                var productInfo = new
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Barcode = product.Barcode,
                    Price = product.Price,
                    CostPrice = product.CostPrice
                };

                var deleted = await _productRepository.DeleteAsync(id);

                if (deleted)
                {
                    // Log audit
                    await _auditLogService.LogActionAsync(
                        action: "DELETE",
                        entityType: "Product",
                        entityId: product.ProductId,
                        entityName: product.ProductName,
                        oldValues: productInfo,
                        newValues: null,
                        changesSummary: $"Xóa vĩnh viễn sản phẩm '{product.ProductName}' (Barcode: {product.Barcode})",
                        userId: userId,
                        username: username
                    );

                    await transaction.CommitAsync();

                    return new DeleteProductResult
                    {
                        Success = deleted,
                        SoftDeleted = false,
                        Message = deleted ? "Đã xóa sản phẩm thành công" : "Không thể xóa sản phẩm"
                    };
                }

                await transaction.RollbackAsync();
                return new DeleteProductResult
                {
                    Success = false,
                    SoftDeleted = false,
                    Message = "Không thể xóa sản phẩm"
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateStockAsync(UpdateStockDto dto)
        {
            var inventories = await _inventoryRepository.FindAsync(i => i.ProductId == dto.ProductId);
            var inventory = inventories.FirstOrDefault();

            if (inventory == null)
            {
                inventory = new Inventory
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                };
                await _inventoryRepository.AddAsync(inventory);
            }
            else
            {
                inventory.Quantity = dto.Quantity;
                inventory.UpdatedAt = DateTime.Now;
                await _inventoryRepository.UpdateAsync(inventory);
            }

            return true;
        }

        public async Task<IEnumerable<ProductHistoryDto>> GetProductHistoryAsync(int productId)
        {
            var history = new List<ProductHistoryDto>();

            // Lấy lịch sử nhập hàng từ PurchaseOrders
            var purchaseItems = await _context.PurchaseItems
                .Include(pi => pi.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Include(pi => pi.PurchaseOrder)
                    .ThenInclude(po => po.User)
                .Where(pi => pi.ProductId == productId)
                .OrderByDescending(pi => pi.PurchaseOrder.PurchaseDate)
                .ToListAsync();

            foreach (var item in purchaseItems)
            {
                history.Add(new ProductHistoryDto
                {
                    Id = item.PurchaseOrder.PurchaseId,
                    Type = "purchase",
                    Date = item.PurchaseOrder.PurchaseDate,
                    Quantity = item.Quantity,
                    UnitPrice = item.CostPrice,
                    TotalAmount = item.Subtotal,
                    ReferenceNumber = $"PN{item.PurchaseOrder.PurchaseId:D6}",
                    UserName = item.PurchaseOrder.User?.FullName,
                    SupplierName = item.PurchaseOrder.Supplier?.Name,
                    Notes = null
                });
            }

            // Lấy lịch sử bán hàng từ Orders
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.ProductId == productId && oi.Order != null)
                .ToListAsync();

            // Load related data
            var orderIds = orderItems.Where(oi => oi.Order != null).Select(oi => oi.Order!.OrderId).Distinct().ToList();
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Where(o => orderIds.Contains(o.OrderId))
                .ToDictionaryAsync(o => o.OrderId);

            foreach (var item in orderItems)
            {
                if (item.Order == null || !orders.ContainsKey(item.Order.OrderId)) continue;
                var order = orders[item.Order.OrderId];

                history.Add(new ProductHistoryDto
                {
                    Id = order.OrderId,
                    Type = "sale",
                    Date = order.OrderDate,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    TotalAmount = item.Subtotal,
                    ReferenceNumber = $"DH{order.OrderId:D6}",
                    UserName = order.User?.FullName,
                    CustomerName = order.Customer?.Name,
                    Notes = null
                });
            }

            // Sắp xếp theo ngày giảm dần
            return history.OrderByDescending(h => h.Date);
        }
    }
}
