using ISDP2025_Parfonov_Zerrou.Models;

namespace ISDP_WEB.Pages.Services
{
    public class StateContainer
    {
        private List<CartItem> _cartItems = new List<CartItem>();
        private Site _selectedStore;

        public List<CartItem> CartItems => _cartItems;
        public Site SelectedStore { get => _selectedStore; set => _selectedStore = value; }

        public event Action OnChange;

        public void AddToCart(CartItem item)
        {
            var existingItem = _cartItems.FirstOrDefault(i => i.ItemId == item.ItemId);

            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                _cartItems.Add(item);
            }

            NotifyStateChanged();
        }

        public void UpdateCartItemQuantity(int itemId, int quantity)
        {
            var item = _cartItems.FirstOrDefault(i => i.ItemId == itemId);

            if (item != null)
            {
                item.Quantity = quantity;
                NotifyStateChanged();
            }
        }

        public void RemoveFromCart(int itemId)
        {
            var item = _cartItems.FirstOrDefault(i => i.ItemId == itemId);

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

        private void NotifyStateChanged() => OnChange?.Invoke();
    }

    public class CartItem
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
        public int StockQuantity { get; set; }
    }
}
