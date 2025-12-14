using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.DTOs;
using StoreManagementAPI.Models;
using StoreManagementAPI.Repositories;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using BlazorApp1.Data;

namespace StoreManagementAPI.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto?> CreateOrderAsync(CreateOrderDto dto, string? ipAddress = null);
        Task<OrderResponseDto?> GetOrderByIdAsync(int id);
        Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();
        Task<OrderSearchResultDto> SearchOrdersAdvancedAsync(OrderSearchDto searchDto);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        Task<bool> ProcessPaymentAsync(PaymentDto dto);
    }

    public class OrderService : IOrderService
    {
        private readonly StoreDbContext _context;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<Inventory> _inventoryRepository;
        private readonly IRepository<Promotion> _promotionRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderService(
            StoreDbContext context,
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<Payment> paymentRepository,
            IRepository<Inventory> inventoryRepository,
            IRepository<Promotion> promotionRepository,
            IAuditLogService auditLogService,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _paymentRepository = paymentRepository;
            _inventoryRepository = inventoryRepository;
            _promotionRepository = promotionRepository;
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

        private string GetApplyTypeDescription(string? applyType)
        {
            return applyType switch
            {
                "order" => "Giảm theo hóa đơn",
                "product" => "Giảm theo sản phẩm",
                "combo" => "Giảm nhiều sản phẩm cùng lúc",
                _ => "Không xác định"
            };
        }

        public async Task<OrderResponseDto?> CreateOrderAsync(CreateOrderDto dto, string? ipAddress = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string? userId = dto.UserId;

                // Create order
                var order = new Order
                {
                    CustomerId = dto.CustomerId,
                    UserId = userId,
                    Status = "pending",
                    OrderDate = DateTime.Now
                };

                decimal totalAmount = 0;

                // Add order items
                var orderItems = new List<OrderItem>();
                foreach (var item in dto.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null) continue;

                    // Check total inventory across all warehouses
                    var totalInventory = await _context.Inventories
                        .Where(i => i.ProductId == item.ProductId)
                        .SumAsync(i => i.Quantity);

                    if (totalInventory < item.Quantity)
                    {
                        throw new Exception($"Insufficient stock for product {product.ProductName}. Available: {totalInventory}, Requested: {item.Quantity}");
                    }

                    var subtotal = product.Price * item.Quantity;
                    totalAmount += subtotal;

                    var orderItem = new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = product.Price,
                        Subtotal = subtotal
                    };
                    orderItems.Add(orderItem);

                    // Update inventory - deduct from warehouses with stock (FIFO approach)
                    var inventories = await _context.Inventories
                        .Where(i => i.ProductId == item.ProductId && i.Quantity > 0)
                        .OrderBy(i => i.WarehouseId) // Ưu tiên kho theo thứ tự ID
                        .ToListAsync();

                    int remainingQuantity = item.Quantity;
                    foreach (var inv in inventories)
                    {
                        if (remainingQuantity <= 0) break;

                        int deductQuantity = Math.Min(inv.Quantity, remainingQuantity);
                        inv.Quantity -= deductQuantity;
                        inv.UpdatedAt = DateTime.Now;
                        remainingQuantity -= deductQuantity;
                    }
                }

                order.TotalAmount = totalAmount;

                // Apply promotion if provided
                if (!string.IsNullOrEmpty(dto.PromoCode))
                {
                    var promotion = await _context.Promotions
                        .Include(p => p.PromotionProducts)
                        .FirstOrDefaultAsync(p =>
                            p.PromoCode.ToLower() == dto.PromoCode.ToLower() &&
                            p.Status == "active" &&
                            p.StartDate <= DateTime.Now &&
                            p.EndDate >= DateTime.Now);

                    if (promotion != null && totalAmount >= promotion.MinOrderAmount)
                    {
                        if (promotion.UsageLimit == 0 || promotion.UsedCount < promotion.UsageLimit)
                        {
                            decimal discount = 0;

                            if (promotion.ApplyType == "order")
                            {
                                if (promotion.DiscountType == "percent")
                                {
                                    discount = totalAmount * (promotion.DiscountValue / 100);
                                }
                                else // fixed
                                {
                                    discount = promotion.DiscountValue;
                                }
                                order.DiscountAmount = discount;
                                order.TotalAmount = totalAmount - discount;
                            }
                            else if (promotion.ApplyType == "product")
                            {
                                var applicableProductIds = promotion.PromotionProducts.Select(pp => pp.ProductId).ToList();

                                foreach (var item in orderItems)
                                {
                                    if (item.ProductId.HasValue && applicableProductIds.Contains(item.ProductId.Value))
                                    {
                                        decimal itemDiscount = 0;
                                        if (promotion.DiscountType == "percent")
                                        {
                                            itemDiscount = item.Subtotal * (promotion.DiscountValue / 100);
                                        }
                                        else // fixed
                                        {
                                            itemDiscount = Math.Min(promotion.DiscountValue, item.Subtotal);
                                        }

                                        item.Subtotal -= itemDiscount;
                                        discount += itemDiscount;
                                    }
                                }

                                order.DiscountAmount = discount;
                                order.TotalAmount = orderItems.Sum(i => i.Subtotal);
                            }
                            else if (promotion.ApplyType == "combo")
                            {
                                var applicableProductIds = promotion.PromotionProducts.Select(pp => pp.ProductId).ToList();

                                bool hasApplicableProduct = orderItems.Any(item =>
                                    item.ProductId.HasValue && applicableProductIds.Contains(item.ProductId.Value));

                                if (hasApplicableProduct)
                                {
                                    decimal applicableSubtotal = 0;
                                    foreach (var item in orderItems)
                                    {
                                        if (item.ProductId.HasValue && applicableProductIds.Contains(item.ProductId.Value))
                                        {
                                            applicableSubtotal += item.Subtotal;
                                        }
                                    }

                                    if (promotion.DiscountType == "percent")
                                    {
                                        discount = applicableSubtotal * (promotion.DiscountValue / 100);
                                    }
                                    else // fixed
                                    {
                                        discount = promotion.DiscountValue;
                                    }

                                    discount = Math.Min(discount, applicableSubtotal);

                                    if (discount > 0)
                                    {
                                        foreach (var item in orderItems)
                                        {
                                            if (item.ProductId.HasValue && applicableProductIds.Contains(item.ProductId.Value))
                                            {
                                                decimal proportion = item.Subtotal / applicableSubtotal;
                                                decimal itemDiscount = discount * proportion;
                                                item.Subtotal -= itemDiscount;
                                            }
                                        }
                                    }
                                }

                                order.DiscountAmount = discount;
                                order.TotalAmount = orderItems.Sum(i => i.Subtotal);
                            }

                            order.PromoId = promotion.PromoId;
                            promotion.UsedCount++;
                        }
                    }
                }

                var createdOrder = await _orderRepository.AddAsync(order);

                foreach (var item in orderItems)
                {
                    item.OrderId = createdOrder.OrderId;
                    await _orderItemRepository.AddAsync(item);
                }

                await _context.SaveChangesAsync();

                var (auditUserId, auditUsername) = GetAuditInfo();
                var itemsInfo = orderItems.Select(oi => new
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    Subtotal = oi.Subtotal
                }).ToList();

                await _auditLogService.LogActionAsync(
                    action: "CREATE",
                    entityType: "Order",
                    entityId: createdOrder.OrderId,
                    entityName: $"DH{createdOrder.OrderId:D6}",
                    oldValues: null,
                    newValues: new
                    {
                        OrderId = createdOrder.OrderId,
                        CustomerId = createdOrder.CustomerId,
                        TotalAmount = createdOrder.TotalAmount,
                        DiscountAmount = createdOrder.DiscountAmount,
                        Status = createdOrder.Status,
                        ItemCount = orderItems.Count,
                        Items = itemsInfo
                    },
                    changesSummary: $"Tạo đơn hàng DH{createdOrder.OrderId:D6} - Khách hàng ID {createdOrder.CustomerId} - Tổng tiền: {createdOrder.TotalAmount:N0} VNĐ - {orderItems.Count} sản phẩm",
                    userId: auditUserId,
                    username: auditUsername,
                    additionalInfo: new Dictionary<string, object>
                    {
                        { "ItemCount", orderItems.Count },
                        { "HasPromotion", createdOrder.PromoId.HasValue }
                    }
                );

                await transaction.CommitAsync();

                return await GetOrderByIdAsync(createdOrder.OrderId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Include(o => o.Promotion)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return null;

            var payment = order.Payments.FirstOrDefault();
            
            string? userName = null;
            if (!string.IsNullOrEmpty(order.UserId))
            {
                var user = await _userManager.FindByIdAsync(order.UserId);
                userName = user?.UserName;
            }

            return new OrderResponseDto
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.Name,
                UserId = order.UserId,
                UserName = userName,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount + order.DiscountAmount,
                DiscountAmount = order.DiscountAmount,
                FinalAmount = order.TotalAmount,
                PaymentMethod = payment?.PaymentMethod,
                PaymentDate = payment?.PaymentDate,
                PromoId = order.PromoId,
                PromoCode = order.Promotion?.PromoCode,
                PromoType = order.Promotion?.ApplyType,
                PromoDescription = order.Promotion != null ? $"{order.Promotion.PromoCode} - {GetApplyTypeDescription(order.Promotion.ApplyType)}" : null,
                Items = order.OrderItems.Select(oi => {
                    decimal originalSubtotal = oi.Quantity * oi.Price;
                    decimal discountAmount = originalSubtotal - oi.Subtotal;
                    decimal discountPercent = discountAmount > 0 ? (discountAmount / originalSubtotal) * 100 : 0;
                    return new OrderItemResponseDto
                    {
                        ProductId = oi.ProductId ?? 0,
                        ProductName = oi.Product?.ProductName ?? "",
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        Subtotal = oi.Subtotal,
                        DiscountAmount = discountAmount,
                        DiscountPercent = discountPercent
                    };
                }).ToList()
            };
        }

        public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Include(o => o.Promotion)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Fetch users for all orders efficiently? 
            // For now, just fetch individually or skip username if not critical for list view.
            // Or fetch all users and map.
            
            // Let's fetch all users to a dictionary for mapping
            var userIds = orders.Where(o => !string.IsNullOrEmpty(o.UserId)).Select(o => o.UserId).Distinct().ToList();
            var userMap = new Dictionary<string, string>();
            
            foreach(var uid in userIds)
            {
                if (uid != null)
                {
                    var user = await _userManager.FindByIdAsync(uid);
                    if (user != null)
                    {
                        userMap[uid] = user.UserName ?? "Unknown";
                    }
                }
            }

            return orders.Select(order => new OrderResponseDto
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.Name,
                UserId = order.UserId,
                UserName = (order.UserId != null && userMap.ContainsKey(order.UserId)) ? userMap[order.UserId] : null,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount + order.DiscountAmount,
                DiscountAmount = order.DiscountAmount,
                FinalAmount = order.TotalAmount,
                PaymentMethod = order.Payments.FirstOrDefault()?.PaymentMethod,
                PaymentDate = order.Payments.FirstOrDefault()?.PaymentDate,
                PromoId = order.PromoId,
                PromoCode = order.Promotion?.PromoCode,
                PromoType = order.Promotion?.ApplyType,
                PromoDescription = order.Promotion != null ? $"{order.Promotion.PromoCode} - {GetApplyTypeDescription(order.Promotion.ApplyType)}" : null,
                Items = order.OrderItems.Select(oi => {
                    decimal originalSubtotal = oi.Quantity * oi.Price;
                    decimal discountAmount = originalSubtotal - oi.Subtotal;
                    decimal discountPercent = discountAmount > 0 ? (discountAmount / originalSubtotal) * 100 : 0;
                    return new OrderItemResponseDto
                    {
                        ProductId = oi.ProductId ?? 0,
                        ProductName = oi.Product?.ProductName ?? "",
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        Subtotal = oi.Subtotal,
                        DiscountAmount = discountAmount,
                        DiscountPercent = discountPercent
                    };
                }).ToList()
            });
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return false;

            var oldStatus = order.Status;
            order.Status = status;
            await _orderRepository.UpdateAsync(order);

            // Log audit
            var (userId, username) = GetAuditInfo();
            await _auditLogService.LogActionAsync(
                action: "UPDATE",
                entityType: "Order",
                entityId: orderId,
                entityName: $"DH{orderId:D6}",
                oldValues: new { Status = oldStatus },
                newValues: new { Status = status },
                changesSummary: $"Cập nhật trạng thái đơn hàng DH{orderId:D6}: {oldStatus} → {status}",
                userId: userId,
                username: username
            );

            return true;
        }

        public async Task<bool> ProcessPaymentAsync(PaymentDto dto)
        {
            var order = await _orderRepository.GetByIdAsync(dto.OrderId);
            if (order == null) return false;

            var payment = new Payment
            {
                OrderId = dto.OrderId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                PaymentDate = DateTime.Now
            };

            await _paymentRepository.AddAsync(payment);

            // Update order status
            var oldStatus = order.Status;
            order.Status = "paid";
            await _orderRepository.UpdateAsync(order);

            // Log audit
            var (userId, username) = GetAuditInfo();
            await _auditLogService.LogActionAsync(
                action: "PAYMENT",
                entityType: "Order",
                entityId: dto.OrderId,
                entityName: $"DH{dto.OrderId:D6}",
                oldValues: new { Status = oldStatus, PaidAmount = 0 },
                newValues: new { Status = "paid", PaidAmount = dto.Amount },
                changesSummary: $"Thanh toán đơn hàng DH{dto.OrderId:D6} - Số tiền: {dto.Amount:N0} VNĐ - Phương thức: {dto.PaymentMethod}",
                userId: userId,
                username: username,
                additionalInfo: new Dictionary<string, object>
                {
                    { "PaymentMethod", dto.PaymentMethod },
                    { "Amount", dto.Amount }
                }
            );

            return true;
        }

        public async Task<OrderSearchResultDto> SearchOrdersAdvancedAsync(OrderSearchDto searchDto)
        {
            // Validate and sanitize pagination parameters
            var pageNumber = searchDto.PageNumber < 1 ? 1 : searchDto.PageNumber;
            var pageSize = searchDto.PageSize < 1 ? 20 : searchDto.PageSize > 100 ? 100 : searchDto.PageSize;

            // Validate and swap date range if needed
            if (searchDto.StartDate.HasValue && searchDto.EndDate.HasValue && searchDto.StartDate.Value > searchDto.EndDate.Value)
            {
                var temp = searchDto.StartDate;
                searchDto.StartDate = searchDto.EndDate;
                searchDto.EndDate = temp;
            }

            // Validate and swap amount range if needed
            if (searchDto.MinAmount.HasValue && searchDto.MaxAmount.HasValue && searchDto.MinAmount.Value > searchDto.MaxAmount.Value)
            {
                var temp = searchDto.MinAmount;
                searchDto.MinAmount = searchDto.MaxAmount;
                searchDto.MaxAmount = temp;
            }

            // Start with base query
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Include(o => o.Promotion)
                .AsQueryable();

            // Apply filters
            // 1. Filter by search term (order ID or customer name)
            if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            {
                var searchTerm = searchDto.SearchTerm.ToLower().Trim();
                query = query.Where(o =>
                    o.OrderId.ToString().Contains(searchTerm) ||
                    (o.Customer != null && o.Customer.Name.ToLower().Contains(searchTerm)));
            }

            // 2. Filter by status (only if Status is not null or empty)
            if (!string.IsNullOrWhiteSpace(searchDto.Status))
            {
                var status = searchDto.Status.ToLower().Trim();
                query = query.Where(o => o.Status != null && o.Status.ToLower() == status);
            }

            // 3. Filter by amount range
            if (searchDto.MinAmount.HasValue && searchDto.MinAmount.Value > 0)
            {
                query = query.Where(o => o.TotalAmount >= searchDto.MinAmount.Value);
            }

            if (searchDto.MaxAmount.HasValue && searchDto.MaxAmount.Value > 0)
            {
                query = query.Where(o => o.TotalAmount <= searchDto.MaxAmount.Value);
            }

            // 4. Filter by date range
            if (searchDto.StartDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= searchDto.StartDate.Value);
            }

            if (searchDto.EndDate.HasValue)
            {
                // Add one day to include the end date
                var endDate = searchDto.EndDate.Value.AddDays(1);
                query = query.Where(o => o.OrderDate < endDate);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply sorting
            var sortBy = searchDto.SortBy?.ToLower() ?? "orderdate";
            var sortDirection = searchDto.SortDirection?.ToLower() ?? "desc";

            query = sortBy switch
            {
                "totalamount" => sortDirection == "desc"
                    ? query.OrderByDescending(o => o.TotalAmount)
                    : query.OrderBy(o => o.TotalAmount),
                "status" => sortDirection == "desc"
                    ? query.OrderByDescending(o => o.Status)
                    : query.OrderBy(o => o.Status),
                _ => sortDirection == "desc"
                    ? query.OrderByDescending(o => o.OrderDate)
                    : query.OrderBy(o => o.OrderDate)
            };

            // Apply pagination
            var orders = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            // Fetch users for mapping
            var userIds = orders.Where(o => !string.IsNullOrEmpty(o.UserId)).Select(o => o.UserId).Distinct().ToList();
            var userMap = new Dictionary<string, string>();
            
            foreach(var uid in userIds)
            {
                if (uid != null)
                {
                    var user = await _userManager.FindByIdAsync(uid);
                    if (user != null)
                    {
                        userMap[uid] = user.UserName ?? "Unknown";
                    }
                }
            }

            var orderDtos = orders.Select(order => {
                var payment = order.Payments.FirstOrDefault();

                return new OrderResponseDto
                {
                    OrderId = order.OrderId,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer?.Name,
                    UserId = order.UserId,
                    UserName = (order.UserId != null && userMap.ContainsKey(order.UserId)) ? userMap[order.UserId] : null,
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount + order.DiscountAmount,
                    DiscountAmount = order.DiscountAmount,
                    FinalAmount = order.TotalAmount,
                    PaymentMethod = payment?.PaymentMethod,
                    PaymentDate = payment?.PaymentDate,
                    PromoId = order.PromoId,
                    PromoCode = order.Promotion?.PromoCode,
                    PromoType = order.Promotion?.ApplyType,
                    PromoDescription = order.Promotion != null ? $"{order.Promotion.PromoCode} - {GetApplyTypeDescription(order.Promotion.ApplyType)}" : null,
                    Items = order.OrderItems.Select(oi => {
                        decimal originalSubtotal = oi.Quantity * oi.Price;
                        decimal discountAmount = originalSubtotal - oi.Subtotal;
                        decimal discountPercent = discountAmount > 0 ? (discountAmount / originalSubtotal) * 100 : 0;

                        return new OrderItemResponseDto
                        {
                            ProductId = oi.ProductId ?? 0,
                            ProductName = oi.Product?.ProductName ?? "Unknown",
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            Subtotal = oi.Subtotal,
                            DiscountAmount = discountAmount,
                            DiscountPercent = discountPercent
                        };
                    }).ToList()
                };
            }).ToList();

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new OrderSearchResultDto
            {
                Orders = orderDtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages
            };
        }
    }
}
