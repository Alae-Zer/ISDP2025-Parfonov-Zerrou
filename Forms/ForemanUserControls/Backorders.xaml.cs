using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class Backorders : UserControl
    {
        private readonly BestContext context;
        private readonly Employee currentUser;
        private Txn currentBackorder;
        private readonly BackorderManager backorderManager;
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

        public Backorders(Employee employee)
        {
            InitializeComponent();
            context = new BestContext();
            currentUser = employee;
            backorderManager = new BackorderManager(context, employee);

            SetControlsEnabled(false);
            LoadInitialData();
        }

        private void SetControlsEnabled(bool enabled)
        {
            StoreComboBox.IsEnabled = enabled;
            btnNewBackorder.IsEnabled = enabled;
            btnRefresh.IsEnabled = enabled;
        }

        private void UpdateAvailableStores()
        {
            var allStores = context.Sites
                .Where(s => s.Active == 1 && s.SiteId > 3)
                .OrderBy(s => s.SiteName)
                .ToList();

            var storesWithBackorders = context.Txns
                .Where(t => t.TxnType == "BACKORDER" && t.TxnStatus == "NEW")
                .Select(t => t.SiteIdto)
                .Distinct()
                .ToList();

            var availableStores = allStores
                .Where(s => !storesWithBackorders.Contains(s.SiteId))
                .ToList();

            StoreComboBox.ItemsSource = availableStores;
            StoreComboBox.DisplayMemberPath = "SiteName";
            StoreComboBox.SelectedValuePath = "SiteId";
        }

        private void LoadBackorders()
        {
            try
            {
                SetControlsEnabled(false);

                // Get all active backorders
                var backorders = context.Txns
                    .Include(t => t.SiteIdtoNavigation)  // Include store info
                    .Where(t => t.TxnType == "Back Order" &&
                               (t.TxnStatus == "NEW" || t.TxnStatus == "SUBMITTED"))
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();

                // Update the DataGrid
                BackordersGrid.ItemsSource = null; // Clear current items
                BackordersGrid.ItemsSource = backorders;

                if (!backorders.Any())
                {
                    BackorderIdText.Text = "No active backorders";
                    BackorderItemsGrid.ItemsSource = null;
                    currentBackorder = null;
                }

                SetControlsEnabled(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading backorders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetControlsEnabled(true);
            }
        }

        // Modify initial load to not show backorders
        private void LoadInitialData()
        {
            try
            {
                // Only update available stores initially
                UpdateAvailableStores();
                SetControlsEnabled(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading initial data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadBackorders();
            UpdateAvailableStores();
        }

        private void LoadInventoryItems()
        {
            try
            {
                var warehouseId = 2; // Warehouse siteID

                allItems = context.Inventories
                    .Include(i => i.Item)
                    .Where(i => i.SiteId == warehouseId && i.Item.Active == 1)
                    .Select(i => new OrderItem
                    {
                        ItemId = i.ItemId,
                        Name = i.Item.Name,
                        Quantity = i.Quantity,
                        CaseSize = i.Item.CaseSize,
                        Weight = i.Item.Weight,
                        ReorderThreshold = i.ReorderThreshold ?? 0
                    })
                    .ToList();

                UpdatePaginationInfo();
                LoadCurrentPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading warehouse inventory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePaginationInfo()
        {
            var filteredItems = FilterItems(allItems);
            InventoryGrid.ItemsSource = filteredItems;
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
            InventoryGrid.ItemsSource = filteredItems;
        }

        private void LoadBackorderItems(int txnId)
        {
            try
            {
                var items = context.Txnitems
                    .Where(ti => ti.TxnId == txnId)
                    .Include(ti => ti.Item)
                    .Select(ti => new OrderItem
                    {
                        ItemId = ti.ItemId,
                        Name = ti.Item.Name,
                        Quantity = ti.Quantity,
                        CaseSize = ti.Item.CaseSize,
                        Weight = ti.Item.Weight
                    })
                    .ToList();

                BackorderItemsGrid.ItemsSource = items;
                BackorderIdText.Text = $"Backorder #{txnId}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading backorder items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            currentSearchText = SearchBox.Text;
            UpdatePaginationInfo();
            LoadCurrentPage();
        }

        private void StoreComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StoreComboBox.SelectedValue is int siteId)
            {
                LoadInventoryItems();
            }
        }

        private void BackordersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BackordersGrid.SelectedItem is Txn selectedBackorder)
            {
                currentBackorder = selectedBackorder;
                LoadBackorderItems(selectedBackorder.TxnId);
                LoadInventoryItems();
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (currentBackorder == null && StoreComboBox.SelectedValue is int siteId)
            {
                var result = MessageBox.Show(
                    "No backorder selected. Would you like to create a new one?",
                    "Create New Backorder",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    CreateNewBackorder(siteId);
                }
                else
                {
                    return;
                }
            }

            var selectedItem = InventoryGrid.SelectedItem as OrderItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select an item to add.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var txnItem = new Txnitem
                {
                    TxnId = currentBackorder.TxnId,
                    ItemId = selectedItem.ItemId,
                    Quantity = selectedItem.CaseSize
                };

                context.Txnitems.Add(txnItem);
                context.SaveChanges();
                LoadBackorderItems(currentBackorder.TxnId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (currentBackorder == null)
            {
                MessageBox.Show("Please select a backorder first.", "No Backorder Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = BackorderItemsGrid.SelectedItem as OrderItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select an item to remove.", "No Item Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var txnItem = context.Txnitems
                    .FirstOrDefault(ti => ti.TxnId == currentBackorder.TxnId && ti.ItemId == selectedItem.ItemId);

                if (txnItem != null)
                {
                    context.Txnitems.Remove(txnItem);
                    context.SaveChanges();
                    LoadBackorderItems(currentBackorder.TxnId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnNewBackorder_Click(object sender, RoutedEventArgs e)
        {
            if (StoreComboBox.SelectedValue is not int siteId)
            {
                MessageBox.Show("Please select a store first.", "Store Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetControlsEnabled(false);
            CreateNewBackorder(siteId);
            LoadInventoryItems();
            SetControlsEnabled(true);
        }

        private void CreateNewBackorder(int siteId)
        {
            try
            {
                var newBackorder = new Txn
                {
                    EmployeeId = currentUser.EmployeeID,
                    SiteIdto = siteId,
                    SiteIdfrom = 2,
                    TxnStatus = "NEW",
                    TxnType = "BACKORDER",
                    ShipDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    BarCode = $"BO-{DateTime.Now:yyyyMMddHHmmss}"
                };

                context.Txns.Add(newBackorder);
                context.SaveChanges();

                LoadBackorders();
                currentBackorder = newBackorder;
                BackorderIdText.Text = $"Backorder #{newBackorder.TxnId}";
                BackorderItemsGrid.ItemsSource = new List<OrderItem>();

                UpdateAvailableStores();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating backorder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentBackorder == null)
            {
                MessageBox.Show("No backorder selected.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                context.SaveChanges();
                MessageBox.Show("Changes saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}