using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;

namespace BlazorApp1.Services
{
    // DTO for order creation
    public class CartItemForOrder
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public interface ICustomerOrderService
    {
        Task<Order> CreateOrderAsync(int? customerId, List<CartItemForOrder> cartItems, string paymentMethod, decimal discountAmount = 0);
        Task<List<Order>> GetCustomerOrdersAsync(int customerId);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<Order?> GetOrderWithDetailsAsync(int orderId);
    }

    public class CustomerOrderService : ICustomerOrderService
    {
        private readonly StoreDbContext _context;

        public CustomerOrderService(StoreDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(int? customerId, List<CartItemForOrder> cartItems, string paymentMethod, decimal discountAmount = 0)
        {
            // Validate inventory before creating order
            foreach (var item in cartItems)
            {
                var totalInventory = await _context.Inventories
                    .Where(i => i.ProductId == item.ProductId)
                    .SumAsync(i => i.Quantity);

                if (item.Quantity > totalInventory)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    throw new InvalidOperationException($"Không đủ hàng cho sản phẩm {product?.ProductName}. Chỉ còn {totalInventory} sản phẩm trong kho.");
                }
            }

            // Calculate total from cart items
            decimal totalAmount = 0;
            foreach (var item in cartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    totalAmount += product.Price * item.Quantity;
                }
            }

            // Apply discount
            decimal finalAmount = totalAmount - discountAmount;
            if (finalAmount < 0) finalAmount = 0;

            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.Now,
                Status = "pending",
                TotalAmount = finalAmount,
                DiscountAmount = discountAmount
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Add order items and deduct inventory
            foreach (var item in cartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = product.Price,
                        Subtotal = product.Price * item.Quantity
                    };
                    _context.OrderItems.Add(orderItem);

                    // Deduct from inventory (FIFO - first warehouse first)
                    var remainingQuantity = item.Quantity;
                    var inventories = await _context.Inventories
                        .Where(i => i.ProductId == item.ProductId && i.Quantity > 0)
                        .OrderBy(i => i.InventoryId) // FIFO
                        .ToListAsync();

                    foreach (var inventory in inventories)
                    {
                        if (remainingQuantity <= 0) break;

                        var deductAmount = Math.Min(inventory.Quantity, remainingQuantity);
                        inventory.Quantity -= deductAmount;
                        inventory.UpdatedAt = DateTime.Now;
                        remainingQuantity -= deductAmount;
                    }
                }
            }

            // Add payment record
            var payment = new Payment
            {
                OrderId = order.OrderId,
                Amount = order.TotalAmount,
                PaymentMethod = paymentMethod,
                PaymentDate = DateTime.Now
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<List<Order>> GetCustomerOrdersAsync(int customerId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }
    }
}
