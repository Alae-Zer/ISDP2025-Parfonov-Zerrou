using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Forms.UserControls
{
    /// <summary>
    /// Interaction logic for CreateStoreOrderManagers.xaml
    /// </summary>
    public partial class CreateStoreOrderManagers : UserControl
    {
        public CreateStoreOrderManagers()
        {
            InitializeComponent();
            context = new BestContext();
            LoadInitialData();
        }

        public CreateStoreOrderManagers(Employee emp)
        {
            InitializeComponent();
            context = new BestContext();
            LoadInitialData();
            employee = emp;
        }

        private readonly BestContext context;
        private readonly List<OrderLineItem> orderItems = new();
        private readonly List<Inventory> AllOfInventory = new();

        private const int ItemsPerPage = 12;
        private List<OrderItem> allItems = new();
        private string currentSearchText = "";


        Employee employee;

        public class OrderItem  // For inventory display
        {
            public int ItemId { get; set; }
            public string Name { get; set; }
            public int Quantity { get; set; }
            public int CaseSize { get; set; }
            public decimal Weight { get; set; }
            public int ReorderThreshold { get; set; }
            public string Description { get; set; }
        }

        public class OrderLineItem  // For order grid
        {
            public int ItemId { get; set; }
            public string Name { get; set; }
            public int OrderQuantity { get; set; }  // Quantity being ordered
            public int CaseSize { get; set; }
            public decimal Weight { get; set; }
        }

        private void LoadInitialData()
        {
            try
            {
                // Load stores (excluding warehouse, corporate, etc.)
                var stores = context.Sites
                    .Select(s => new
                    {
                        s.SiteId,
                        s.SiteName,
                        s.DayOfWeek
                    })
                    .OrderBy(s => s.SiteName)
                    .ToList();

                StoreComboBox.ItemsSource = stores;
                StoreComboBox.DisplayMemberPath = "SiteName";
                StoreComboBox.SelectedValuePath = "SiteId";

                // Load inventory items with current stock levels
                LoadInventoryItems();


                //PrePopulateOrder();
                // Set default delivery date based on selected store's delivery day
                DeliveryDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading initial data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PrePopulateOrder()
        {
            if (StoreComboBox.SelectedItem == null) return;
            orderItems.Clear(); // Clear existing pre-populated items

            int selectedSiteId = (int)StoreComboBox.SelectedValue;

            var inventoryData = context.Inventories
                .Include(i => i.Item)
                .Where(i => i.SiteId == selectedSiteId)
                .ToList();

            foreach (var inventory in inventoryData)
            {
                if (inventory.Quantity <= inventory.ReorderThreshold)
                {
                    int needed = inventory.OptimumThreshold - inventory.Quantity;
                    int cases = inventory.Item.CaseSize > 0 ? (int)Math.Ceiling((double)needed / inventory.Item.CaseSize) : needed;

                    orderItems.Add(new OrderLineItem
                    {
                        ItemId = inventory.ItemId,
                        Name = inventory.Item.Name,
                        OrderQuantity = cases * inventory.Item.CaseSize,
                        CaseSize = inventory.Item.CaseSize,
                        Weight = inventory.Item.Weight
                    });
                }
            }

            OrderGrid.ItemsSource = null;
            OrderGrid.ItemsSource = orderItems;
        }



        private void LoadInventoryItems()
        {
            try
            {
                if (StoreComboBox.SelectedItem == null)
                    return;

                var selectedSiteId = (int)StoreComboBox.SelectedValue;


                // Get store inventory with warehouse quantities
                allItems = context.Inventories
                    .Include(i => i.Item)
                    .Where(i => i.SiteId == selectedSiteId && i.Item.Active == 1)
                    .Select(i => new OrderItem
                    {
                        ItemId = i.ItemId,
                        Name = i.Item.Name,
                        Quantity = i.Quantity,
                        CaseSize = i.Item.CaseSize,
                        Weight = i.Item.Weight,
                        ReorderThreshold = i.ReorderThreshold ?? 0,
                        Description = i.Item.Description ?? i.Item.Name
                    })
                    .ToList();

                UpdatePaginationInfo();
                LoadCurrentPage();
                PrePopulateOrder();
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

            var existingItem = orderItems.FirstOrDefault(item => item.ItemId == selectedItem.ItemId);
            if (existingItem != null)
            {
                // Increment the quantity by case size
                existingItem.OrderQuantity += selectedItem.CaseSize;

                // Refresh the OrderGrid to show the updated quantity
                OrderGrid.ItemsSource = null;
                OrderGrid.ItemsSource = orderItems;
                return;
            }

            var newOrderItem = new OrderLineItem
            {
                ItemId = selectedItem.ItemId,
                Name = selectedItem.Name,
                OrderQuantity = selectedItem.CaseSize, // Default to one case
                CaseSize = selectedItem.CaseSize,
                Weight = selectedItem.Weight
            };

            orderItems.Add(newOrderItem);
            OrderGrid.ItemsSource = null;
            OrderGrid.ItemsSource = orderItems;
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = OrderGrid.SelectedItem as OrderLineItem;
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

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
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
                int selectedSite = int.Parse(StoreComboBox.SelectedValue.ToString());
                var txn = new Txn
                {
                    EmployeeId = employee.EmployeeID,
                    SiteIdto = selectedSite,
                    SiteIdfrom = 2, // Warehouse
                    TxnStatus = "NEW",
                    ShipDate = DeliveryDatePicker.SelectedDate ?? DateTime.Now.AddDays(1),
                    TxnType = "Store Order",
                    BarCode = GenerateBarcode(),
                    CreatedDate = DateTime.Now,
                    EmergencyDelivery = 0
                };

                context.Txns.Add(txn);
                context.SaveChanges();

                // Add order items
                foreach (var item in orderItems)
                {
                    var txnItem = new Txnitem
                    {
                        TxnId = txn.TxnId,
                        ItemId = item.ItemId,
                        Quantity = item.OrderQuantity
                    };
                    context.Txnitems.Add(txnItem);
                }

                context.SaveChanges();
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

