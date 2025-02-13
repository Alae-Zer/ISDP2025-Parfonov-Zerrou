using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    /// <summary>
    /// Interaction logic for CreateStoreOrder.xaml
    /// </summary>
    public partial class CreateStoreOrder : UserControl
    {
        private readonly BestContext context;
        private readonly List<OrderItem> orderItems = new();

        private const int ItemsPerPage = 10;
        private List<OrderItem> allItems = new();
        private string currentSearchText = "";

        public class OrderItem
        {
            public int ItemId { get; set; }
            public string Name { get; set; }
            public int Quantity { get; set; }
            public int CaseSize { get; set; }
            public decimal Weight { get; set; }

            public int ReorderThreshold { get; set; }
        }

        public CreateStoreOrder()
        {
            InitializeComponent();
            context = new BestContext();
            LoadInitialData();
        }

        private async void LoadInitialData()
        {
            try
            {
                // Load stores (excluding warehouse, corporate, etc.)
                var stores = await context.Sites
                .Where(s => s.Active == 1 && s.SiteId > 3) // Skip Corporate, Warehouse, and Bay
                .Select(s => new
                {
                    s.SiteId,
                    s.SiteName,
                    s.DayOfWeek
                })
                .OrderBy(s => s.SiteName)
                .ToListAsync();

                StoreComboBox.ItemsSource = stores;
                StoreComboBox.DisplayMemberPath = "SiteName";
                StoreComboBox.SelectedValuePath = "SiteId";

                // Load inventory items with current stock levels
                await LoadInventoryItems();

                // Set default delivery date based on selected store's delivery day
                DeliveryDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading initial data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async Task LoadInventoryItems()
        {
            try
            {
                if (StoreComboBox.SelectedItem == null)
                    return;

                var selectedSiteId = (int)StoreComboBox.SelectedValue;

                allItems = await context.Inventories
                    .Include(i => i.Item)
                    .Where(i => i.SiteId == selectedSiteId && i.Item.Active == 1)
                    .Select(i => new OrderItem
                    {
                        ItemId = i.ItemId,
                        Name = i.Item.Name,
                        Quantity = i.Quantity,
                        CaseSize = i.Item.CaseSize,
                        Weight = i.Item.Weight,
                        ReorderThreshold = i.ReorderThreshold ?? 0
                    })
                    .ToListAsync();

                UpdatePaginationInfo();
                LoadCurrentPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading inventory: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePaginationInfo()
        {
            var filteredItems = FilterItems(allItems);
            int totalPages = (int)Math.Ceiling(filteredItems.Count / (double)ItemsPerPage);
            InventoryPagination.MaxPageCount = Math.Max(1, totalPages);

            // Reset to first page when filter changes
            if (InventoryPagination.PageIndex > totalPages)
                InventoryPagination.PageIndex = 1;
        }

        private List<OrderItem> FilterItems(List<OrderItem> items)
        {
            if (string.IsNullOrWhiteSpace(currentSearchText))
                return items;

            return items.Where(i =>
                i.Name.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
                i.ItemId.ToString().Contains(currentSearchText)
            ).ToList();
        }

        private void LoadCurrentPage()
        {
            var filteredItems = FilterItems(allItems);
            var pagedItems = filteredItems
                .Skip((InventoryPagination.PageIndex - 1) * ItemsPerPage)
                .Take(ItemsPerPage)
                .ToList();

            InventoryGrid.ItemsSource = pagedItems;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            currentSearchText = SearchBox.Text;
            UpdatePaginationInfo();
            LoadCurrentPage();
        }

        private void InventoryPagination_PageUpdated(object sender, HandyControl.Data.FunctionEventArgs<int> e)
        {
            LoadCurrentPage();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = InventoryGrid.SelectedItem as OrderItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select an item to add.",
                    "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newOrderItem = new OrderItem
            {
                ItemId = selectedItem.ItemId,
                Name = selectedItem.Name,
                Quantity = selectedItem.CaseSize, // Default to one case
                CaseSize = selectedItem.CaseSize,
                Weight = selectedItem.Weight
            };

            orderItems.Add(newOrderItem);
            OrderGrid.ItemsSource = null;
            OrderGrid.ItemsSource = orderItems;
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = OrderGrid.SelectedItem as OrderItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select an item to remove.",
                    "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            orderItems.Remove(selectedItem);
            OrderGrid.ItemsSource = null;
            OrderGrid.ItemsSource = orderItems;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInventoryItems();
        }

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (StoreComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a store location.",
                    "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!orderItems.Any())
            {
                MessageBox.Show("Please add items to the order.",
                    "Empty Order", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var selectedSite = (Site)StoreComboBox.SelectedItem;
                var txn = new Txn
                {
                    EmployeeId = 1, // Replace with actual logged-in user ID
                    SiteIdto = selectedSite.SiteId,
                    SiteIdfrom = 2, // Warehouse
                    TxnStatus = "NEW",
                    ShipDate = DeliveryDatePicker.SelectedDate ?? DateTime.Now.AddDays(1),
                    TxnType = "Store Order",
                    BarCode = GenerateBarcode(),
                    CreatedDate = DateTime.Now,
                    EmergencyDelivery = 0
                };

                context.Txns.Add(txn);
                await context.SaveChangesAsync();

                // Add order items
                foreach (var item in orderItems)
                {
                    var txnItem = new Txnitem
                    {
                        TxnId = txn.TxnId,
                        ItemId = item.ItemId,
                        Quantity = item.Quantity
                    };
                    context.Txnitems.Add(txnItem);
                }

                await context.SaveChangesAsync();
                MessageBox.Show("Order created successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear the form
                orderItems.Clear();
                OrderGrid.ItemsSource = null;
                StoreComboBox.SelectedItem = null;
                DeliveryDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating order: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateBarcode()
        {
            return $"ORD-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private void StoreComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadInventoryItems();
        }
    }
}
