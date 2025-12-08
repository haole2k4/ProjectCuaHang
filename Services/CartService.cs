using StoreManagementAPI.Models;

namespace BlazorApp1.Services
{
    public class CartItem
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
        List<CartItem> GetCartItems();
        void AddToCart(Product product, int quantity = 1);
        void UpdateQuantity(int productId, int quantity);
        void RemoveFromCart(int productId);
        void ClearCart();
        decimal GetTotal();
        int GetItemCount();
    }

    public class CartService : ICartService
    {
        private readonly List<CartItem> _cartItems = new();

        public event Action? OnChange;

        public List<CartItem> GetCartItems() => _cartItems;

        public void AddToCart(Product product, int quantity = 1)
        {
            var existingItem = _cartItems.FirstOrDefault(x => x.ProductId == product.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                _cartItems.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    Quantity = quantity
                });
            }
            NotifyStateChanged();
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var item = _cartItems.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    _cartItems.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                NotifyStateChanged();
            }
        }

        public void RemoveFromCart(int productId)
        {
            var item = _cartItems.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                _cartItems.Remove(item);
                NotifyStateChanged();
            }
        }

        public void ClearCart()
        {
            _cartItems.Clear();
            NotifyStateChanged();
        }

        public decimal GetTotal() => _cartItems.Sum(x => x.Subtotal);

        public int GetItemCount() => _cartItems.Sum(x => x.Quantity);

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
