using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Forms.UserControls
{
    /// <summary>
    /// Interaction logic for CreateStoreOrderManagers.xaml
    /// </summary>
    public partial class CreateStoreOrderManagers : UserControl
    {
        private readonly BestContext context;
        private readonly List<OrderLineItem> orderItems = new();
        private readonly List<Inventory> AllOfInventory = new();
        private readonly List<Site> Stores = new();

        private const int ItemsPerPage = 11;
        private List<OrderItem> allItems = new();
        private string currentSearchText = "";
        private int? existingTxnId = null;


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


        private void disable(bool incase)
        {
            all1.IsEnabled = incase;
            all2.IsEnabled = incase;
        }
        public CreateStoreOrderManagers()
        {
            InitializeComponent();
            context = new BestContext();
            loadStores();
        }

        public CreateStoreOrderManagers(Employee emp)
        {
            InitializeComponent();
            context = new BestContext();
            employee = emp;
            loadStores();
            disable(false);
        }

        public CreateStoreOrderManagers(Employee emp, int ID)
        {
            InitializeComponent();
            employee = emp;
            existingTxnId = ID;
            context = new BestContext();
            loadStores();
            LoadExistingOrder();
            disable(false);
        }

        private void LoadExistingOrder()
        {
            if (!existingTxnId.HasValue) return;

            var existingOrder = context.Txns
                .Include(t => t.Txnitems)
                .ThenInclude(ti => ti.Item)
                .FirstOrDefault(t => t.TxnId == existingTxnId);

            if (existingOrder != null)
            {
                // Populate UI with existing order data
                StoreComboBox.SelectedValue = existingOrder.SiteIdto;
                DeliveryDatePicker.SelectedDate = existingOrder.ShipDate;

                orderItems.Clear();
                foreach (var item in existingOrder.Txnitems)
                {
                    orderItems.Add(new OrderLineItem
                    {
                        ItemId = item.ItemId,
                        Name = item.Item.Name,
                        OrderQuantity = item.Quantity,
                        CaseSize = item.Item.CaseSize,
                        Weight = item.Item.Weight
                    });
                }
                dgvOrders.ItemsSource = orderItems;
            }
        }

        private void loadStores()
        {
            try
            {
                var stores = context.Sites
                    .Select(s => new
                    {
                        s.SiteId,
                        s.SiteName,
                        s.DayOfWeek
                    })
                    .OrderBy(s => s.SiteName)
                    .ToList();
                stores.RemoveAt(0); // Corporate Headquarters
                stores.RemoveAt(4); // Online
                stores.RemoveAt(7); // Truck
                stores.RemoveAt(8); // Warehouse Bay

                StoreComboBox.ItemsSource = stores;
                StoreComboBox.DisplayMemberPath = "SiteName";
                StoreComboBox.SelectedValuePath = "SiteId";


                //PrePopulateOrder();
                // Set default delivery date based on selected store's delivery day
                DeliveryDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading initial data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PrePopulateOrder()
        {
            if (StoreComboBox.SelectedItem == null) return;
            orderItems.Clear(); // Clear existing pre-populated items

            int selectedSiteId = (int)StoreComboBox.SelectedValue;
            if (orderItems == null)
            {
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
            }

            dgvOrders.ItemsSource = null;
            dgvOrders.ItemsSource = orderItems;
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
                HandyControl.Controls.MessageBox.Show($"Error loading inventory: {ex.Message}",
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
                HandyControl.Controls.MessageBox.Show("Please select an item to add.",
                    "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existingItem = orderItems.FirstOrDefault(item => item.ItemId == selectedItem.ItemId);
            if (existingItem != null)
            {
                // Increment the quantity by case size
                existingItem.OrderQuantity += selectedItem.CaseSize;

                // Refresh the OrderGrid to show the updated quantity
                dgvOrders.ItemsSource = null;
                dgvOrders.ItemsSource = orderItems;
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
            dgvOrders.ItemsSource = null;
            dgvOrders.ItemsSource = orderItems;
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgvOrders.SelectedItem as OrderLineItem;
            if (selectedItem == null)
            {
                HandyControl.Controls.MessageBox.Show("Please select an item to remove.",
                    "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            orderItems.Remove(selectedItem);
            dgvOrders.ItemsSource = null;
            dgvOrders.ItemsSource = orderItems;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInventoryItems();
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (StoreComboBox.SelectedItem == null)
            {
                HandyControl.Controls.MessageBox.Show("Please select a store location.",
                    "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!orderItems.Any())
            {
                HandyControl.Controls.MessageBox.Show("Please add items to the order.",
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
                HandyControl.Controls.MessageBox.Show("Order created successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear the form
                orderItems.Clear();
                dgvOrders.ItemsSource = null;
                StoreComboBox.SelectedItem = null;
                DeliveryDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error creating order: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateBarcode()
        {
            return $"TXN-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private void StoreComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadInventoryItems();
        }

        private void nupQuantity_ValueChanged(object sender, HandyControl.Data.FunctionEventArgs<double> e)
        {

            NumericUpDown npm = (NumericUpDown)sender;
            OrderLineItem selectedItem = (OrderLineItem)dgvOrders.SelectedItem;
            if (selectedItem != null)
            {
                orderItems[dgvOrders.SelectedIndex].OrderQuantity = (int)npm.Value;
                dgvOrders.ItemsSource = orderItems;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (existingTxnId.GetValueOrDefault() != 0)
                    return;

                // Add order items
                foreach (var item in orderItems)
                {
                    var txnItem = new Txnitem
                    {
                        TxnId = (int)existingTxnId,
                        ItemId = item.ItemId,
                        Quantity = item.OrderQuantity
                    };
                    context.Txnitems.Add(txnItem);
                }

                context.SaveChanges();
                HandyControl.Controls.MessageBox.Show("Order created successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear the form
                orderItems.Clear();
                dgvOrders.ItemsSource = null;
                StoreComboBox.SelectedItem = null;
                DeliveryDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error creating order: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            disable(true);

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

                existingTxnId = txn.TxnId;

                context.SaveChanges();
                HandyControl.Controls.MessageBox.Show("Order created successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear the form
                orderItems.Clear();
                dgvOrders.ItemsSource = null;
                StoreComboBox.SelectedItem = null;
                DeliveryDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error creating order: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}

