using Microsoft.EntityFrameworkCore;
using StoreManagementAPI.Data;
using StoreManagementAPI.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BlazorApp1.Services
{
    // DTO for cart display
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => Price * Quantity;
    }

    public interface ICartService
    {
        event Action? OnChange;
        Task<List<CartItemDto>> GetCartItemsAsync();
        Task AddToCartAsync(Product product, int quantity = 1);
        Task UpdateQuantityAsync(int productId, int quantity);
        Task RemoveFromCartAsync(int productId);
        Task ClearCartAsync();
        Task<decimal> GetTotalAsync();
        Task<int> GetItemCountAsync();
        Task MergeGuestCartToCustomerAsync(int customerId);

        // Promo code properties
        string AppliedPromoCode { get; }
        int? AppliedPromoId { get; }
        decimal DiscountPercent { get; }
        decimal DiscountAmount { get; }
        void ApplyDiscount(string promoCode, int promoId, decimal percent, decimal amount);
        void RemoveDiscount();
    }

    public class CartService : ICartService
    {
        private readonly IDbContextFactory<StoreDbContext> _contextFactory;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public event Action? OnChange;

        public string AppliedPromoCode { get; private set; } = "";
        public int? AppliedPromoId { get; private set; }
        public decimal DiscountPercent { get; private set; }
        public decimal DiscountAmount { get; private set; }

        public CartService(
            IDbContextFactory<StoreDbContext> contextFactory,
            AuthenticationStateProvider authStateProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _contextFactory = contextFactory;
            _authStateProvider = authStateProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public void ApplyDiscount(string promoCode, int promoId, decimal percent, decimal amount)
        {
            AppliedPromoCode = promoCode;
            AppliedPromoId = promoId;
            DiscountPercent = percent;
            DiscountAmount = amount;
            NotifyStateChanged();
        }

        public void RemoveDiscount()
        {
            AppliedPromoCode = "";
            AppliedPromoId = null;
            DiscountPercent = 0;
            DiscountAmount = 0;
            NotifyStateChanged();
        }

        private async Task<Cart> GetOrCreateCartAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            Cart? cart = null;

            // Check if user is authenticated and has a customer record
            if (user.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId != null)
                {
                    // Find customer by ApplicationUserId
                    var customer = await context.Customers
                        .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

                    if (customer != null)
                    {
                        // Get cart by CustomerId
                        cart = await context.Carts
                            .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.Product)
                            .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);

                        if (cart == null)
                        {
                            cart = new Cart { CustomerId = customer.CustomerId };
                            context.Carts.Add(cart);
                            await context.SaveChangesAsync();
                        }
                    }
                }
            }

            // If no customer cart, use session-based cart
            if (cart == null)
            {
                var sessionId = GetOrCreateSessionId();
                cart = await context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);

                if (cart == null)
                {
                    cart = new Cart { SessionId = sessionId };
                    context.Carts.Add(cart);
                    await context.SaveChangesAsync();
                }
            }

            return cart;
        }

        private string GetOrCreateSessionId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return Guid.NewGuid().ToString();

            const string sessionKey = "CartSessionId";
            var sessionId = httpContext.Session.GetString(sessionKey);

            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                httpContext.Session.SetString(sessionKey, sessionId);
            }

            return sessionId;
        }

        public async Task<List<CartItemDto>> GetCartItemsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var cart = await GetOrCreateCartAsync();

            var cartItems = await context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartId == cart.CartId)
                .Select(ci => new CartItemDto
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.ProductName,
                    ImageUrl = ci.Product.ImageUrl,
                    Price = ci.Price,
                    Quantity = ci.Quantity
                })
                .ToListAsync();

            return cartItems;
        }

        public async Task AddToCartAsync(Product product, int quantity = 1)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var cart = await GetOrCreateCartAsync();

            var existingItem = await context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == product.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = product.ProductId,
                    Price = product.Price,
                    Quantity = quantity
                };
                context.CartItems.Add(cartItem);
            }

            cart.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();
            NotifyStateChanged();
        }

        public async Task UpdateQuantityAsync(int productId, int quantity)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var cart = await GetOrCreateCartAsync();

            var item = await context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    context.CartItems.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                cart.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
                NotifyStateChanged();
            }
        }

        public async Task RemoveFromCartAsync(int productId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var cart = await GetOrCreateCartAsync();

            var item = await context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == productId);

            if (item != null)
            {
                context.CartItems.Remove(item);
                cart.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
                NotifyStateChanged();
            }
        }

        public async Task ClearCartAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var cart = await GetOrCreateCartAsync();

            var items = await context.CartItems
                .Where(ci => ci.CartId == cart.CartId)
                .ToListAsync();

            context.CartItems.RemoveRange(items);
            await context.SaveChangesAsync();
            NotifyStateChanged();
        }

        public async Task<decimal> GetTotalAsync()
        {
            var items = await GetCartItemsAsync();
            return items.Sum(x => x.Subtotal);
        }

        public async Task<int> GetItemCountAsync()
        {
            var items = await GetCartItemsAsync();
            return items.Sum(x => x.Quantity);
        }

        public async Task MergeGuestCartToCustomerAsync(int customerId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var sessionId = GetOrCreateSessionId();

            // Find guest cart
            var guestCart = await context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.CustomerId == null);

            if (guestCart == null || !guestCart.CartItems.Any())
                return;

            // Find or create customer cart
            var customerCart = await context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customerCart == null)
            {
                // Convert guest cart to customer cart
                guestCart.CustomerId = customerId;
                guestCart.SessionId = null;
                guestCart.UpdatedAt = DateTime.Now;
            }
            else
            {
                // Merge items
                foreach (var guestItem in guestCart.CartItems)
                {
                    var existingItem = customerCart.CartItems
                        .FirstOrDefault(ci => ci.ProductId == guestItem.ProductId);

                    if (existingItem != null)
                    {
                        existingItem.Quantity += guestItem.Quantity;
                    }
                    else
                    {
                        var newItem = new CartItem
                        {
                            CartId = customerCart.CartId,
                            ProductId = guestItem.ProductId,
                            Price = guestItem.Price,
                            Quantity = guestItem.Quantity
                        };
                        context.CartItems.Add(newItem);
                    }
                }

                // Remove guest cart
                context.CartItems.RemoveRange(guestCart.CartItems);
                context.Carts.Remove(guestCart);
                customerCart.UpdatedAt = DateTime.Now;
            }

            await context.SaveChangesAsync();
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
