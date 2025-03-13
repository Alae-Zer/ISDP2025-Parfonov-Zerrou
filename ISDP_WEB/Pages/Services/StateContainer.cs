using ISDP2025_Parfonov_Zerrou.Models;

namespace ISDP_WEB.Pages.Services
{
    public class StateContainer
    {
        private Dictionary<int, StoreCart> _storeCarts = new Dictionary<int, StoreCart>();
        private Site _selectedStore;
        private bool _isCartOpen = false;
        public int TotalCartCount =>
        _storeCarts.Values.Sum(cart => cart.Items.Sum(item => item.Quantity));

        // Get all cart items across all stores
        public List<CartItem> CartItems =>
            _storeCarts.Values.SelectMany(cart => cart.Items).ToList();

        public Site SelectedStore
        {
            get => _selectedStore;
            set => _selectedStore = value;
        }

        public bool IsCartOpen
        {
            get => _isCartOpen;
            set
            {
                _isCartOpen = value;
                NotifyStateChanged();
            }
        }

        public event Action OnChange;

        // Get cart items for current store
        public List<CartItem> CurrentStoreItems =>
            _selectedStore != null && _storeCarts.ContainsKey(_selectedStore.SiteId)
                ? _storeCarts[_selectedStore.SiteId].Items
                : new List<CartItem>();

        // Check if a store has items in cart
        public bool CartHasItemsForStore(int storeId)
        {
            return _storeCarts.ContainsKey(storeId) && _storeCarts[storeId].Items.Any();
        }

        // Get all store carts
        public List<StoreCart> GetStoreCarts()
        {
            return _storeCarts.Values.ToList();
        }

        // Get total for a specific store
        public decimal GetStoreCartTotal(int storeId)
        {
            if (_storeCarts.ContainsKey(storeId))
            {
                return _storeCarts[storeId].Total;
            }
            return 0;
        }

        // Add item to store-specific cart
        public void AddToCart(CartItem item, int storeId, string storeName)
        {
            if (!_storeCarts.ContainsKey(storeId))
            {
                _storeCarts[storeId] = new StoreCart
                {
                    StoreId = storeId,
                    StoreName = storeName,
                    Items = new List<CartItem>()
                };
            }

            var storeCart = _storeCarts[storeId];
            var existingItem = storeCart.Items.FirstOrDefault(i => i.ItemId == item.ItemId);

            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                storeCart.Items.Add(item);
            }

            NotifyStateChanged();
        }

        // Update item quantity in store-specific cart
        public void UpdateCartItemQuantity(int storeId, int itemId, int quantity)
        {
            if (!_storeCarts.ContainsKey(storeId)) return;

            var storeCart = _storeCarts[storeId];
            var item = storeCart.Items.FirstOrDefault(i => i.ItemId == itemId);

            if (item != null)
            {
                item.Quantity = quantity;
                NotifyStateChanged();
            }
        }

        // Remove item from store-specific cart
        public void RemoveFromCart(int storeId, int itemId)
        {
            if (!_storeCarts.ContainsKey(storeId)) return;

            var storeCart = _storeCarts[storeId];
            var item = storeCart.Items.FirstOrDefault(i => i.ItemId == itemId);

            if (item != null)
            {
                storeCart.Items.Remove(item);

                // If this was the last item for this store, remove the store cart
                if (storeCart.Items.Count == 0)
                {
                    _storeCarts.Remove(storeId);
                }

                NotifyStateChanged();
            }
        }

        // Clear a specific store's cart
        public void ClearStoreCart(int storeId)
        {
            if (_storeCarts.ContainsKey(storeId))
            {
                _storeCarts.Remove(storeId);
                NotifyStateChanged();
            }
        }

        // Clear all carts
        public void ClearAllCarts()
        {
            _storeCarts.Clear();
            NotifyStateChanged();
        }

        // Toggle cart visibility
        public void ToggleCart()
        {
            IsCartOpen = !IsCartOpen;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }

    // Store cart class to group items by store
    public class StoreCart
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public decimal Total => Items.Sum(item => item.Price * item.Quantity);
    }

    // CartItem class (you already have this)
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