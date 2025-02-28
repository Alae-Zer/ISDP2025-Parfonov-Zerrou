using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using ISDP2025_Parfonov_Zerrou.Forms.UserControls;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Managers;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Forms.ForemanUserControls
{
    /// <summary>
    /// Interaction logic for ReceiveStoreOrder.xaml
    /// </summary>
    public partial class ReceiveStoreOrder : UserControl
    {
        private readonly BestContext context;
        private List<OrderItem> allInventoryItems = new();
        private List<OrderLineItem> orderItems;

        private int ItemsPerPage = 11;
        private int? existingTxnId = null;
        private string searchText = "";
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
            public int OrderQuantity { get; set; }
            public int CaseSize { get; set; }
            public decimal Weight { get; set; }
        }

        public ReceiveStoreOrder()
        {
            InitializeComponent();
        }

        public ReceiveStoreOrder(Employee emp, BestContext iNcontext)
        {
            InitializeComponent();
            context = iNcontext;
            employee = emp;
        }

        public ReceiveStoreOrder(Employee emp, int ID, BestContext iNcontext)
        {
            InitializeComponent();
            context = iNcontext;
            employee = emp;
            existingTxnId = ID;
            LoadInventoryItems();
            GetOrder(ID);
        }
        private void LoadInventoryItems()
        {
            try
            {
                // Get store inventory with warehouse quantities
                allInventoryItems = context.Inventories
                    .Include(i => i.Item)
                    .Where(i => i.SiteId == 2 && i.Item.Active == 1)
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

                // Reset to first page and update display
                InventoryPagination.PageIndex = 1;
                UpdateDisplay();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(
                    $"Error loading inventory: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private int CalculateItemsPerPage()
        {
            // Get the actual height of the grid
            double gridHeight = InventoryGrid.ActualHeight;
            // Row height is set to 36 in your XAML
            int rowHeight = 37;
            // Header height (from your style)
            int headerHeight = 53;
            // Calculate how many rows can fit
            int availableRows = (int)((gridHeight - headerHeight) / rowHeight);
            // Ensure at least 1 row
            return Math.Max(availableRows, 1);
        }

        private void UpdateDisplay()
        {
            // Apply search filter
            var filteredItems = string.IsNullOrWhiteSpace(searchText)
                ? allInventoryItems
                //to get the items that has the value in either the name or the ID
                : allInventoryItems.Where(i => i.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) || i.ItemId.ToString().Contains(searchText)).ToList();

            // Calculate page size based on current grid height
            int itemsPerPage = CalculateItemsPerPage();

            // Update pagination control
            int totalPages = (int)Math.Ceiling(filteredItems.Count / (double)itemsPerPage);
            InventoryPagination.MaxPageCount = Math.Max(1, totalPages);

            // Ensure current page is valid
            if (InventoryPagination.PageIndex > totalPages && totalPages > 0)
                InventoryPagination.PageIndex = totalPages;

            // Get current page of items
            int startIndex = (InventoryPagination.PageIndex - 1) * itemsPerPage;
            var pagedItems = filteredItems
                .Skip(startIndex)
                .Take(itemsPerPage)
                .ToList();

            // Update grid
            InventoryGrid.ItemsSource = pagedItems;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchText = SearchBox.Text;
            InventoryPagination.PageIndex = 1; // Reset to first page when search changes
            UpdateDisplay();
        }

        private void InventoryPagination_PageUpdated(object sender, HandyControl.Data.FunctionEventArgs<int> e)
        {
            UpdateDisplay();
        }

        private void GetOrder(int TxnId)
        {
            if (!existingTxnId.HasValue) return;

            // Initialize the list if it hasn't been done already
            orderItems = new List<OrderLineItem>();

            var existingOrder = context.Txns
                .Include(t => t.Txnitems)
                .ThenInclude(ti => ti.Item)
                .FirstOrDefault(t => t.TxnId == existingTxnId);

            if (existingOrder != null)
            {
                // Change status to RECEIVED
                existingOrder.TxnStatus = "RECEIVED";
                context.Txns.Update(existingOrder);
                context.SaveChanges();

                // Create audit record
                AuditTransactions.LogActivity(
                    employee,
                    existingOrder.TxnId,
                    existingOrder.TxnType,
                    "RECEIVED",
                    existingOrder.SiteIdfrom,
                    existingOrder.DeliveryId,
                    $"Order received by {employee.FirstName} {employee.LastName}"
                );

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
                dgvOrders.ItemsSource = null;
                dgvOrders.ItemsSource = orderItems;
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected item from the inventory grid
            var selectedItem = InventoryGrid.SelectedItem as OrderItem;

            // If nothing is selected, show a message and exit
            if (selectedItem == null)
            {
                HandyControl.Controls.MessageBox.Show(
                    "Please select an item to add.",
                    "No Item Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Check if this item is already in the order
            var existingItem = orderItems.FirstOrDefault(item => item.ItemId == selectedItem.ItemId);

            if (existingItem != null)
            {
                // Item is already in the order, just increase the quantity
                existingItem.OrderQuantity += selectedItem.CaseSize;
            }
            else
            {
                // Item is not in the order, add it as a new entry
                orderItems.Add(new OrderLineItem
                {
                    ItemId = selectedItem.ItemId,
                    Name = selectedItem.Name,
                    OrderQuantity = selectedItem.CaseSize,  // Start with one case
                    CaseSize = selectedItem.CaseSize,
                    Weight = selectedItem.Weight
                });
            }

            // Refresh the order grid to show the updated list
            dgvOrders.ItemsSource = null;
            dgvOrders.ItemsSource = orderItems;
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected item from the order grid
            var selectedItem = dgvOrders.SelectedItem as OrderLineItem;

            // If nothing is selected, show a message and exit
            if (selectedItem == null)
            {
                HandyControl.Controls.MessageBox.Show(
                    "Please select an item to remove.",
                    "No Item Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Remove the selected item from our order list
            orderItems.Remove(selectedItem);

            // Refresh the order grid to show the updated list
            dgvOrders.ItemsSource = null;
            dgvOrders.ItemsSource = orderItems;
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (!existingTxnId.HasValue)
            {
                HandyControl.Controls.MessageBox.Show(
                    "No order to process.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var result = HandyControl.Controls.MessageBox.Show(
                "Are you sure you want to send this order for assembly?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
                return;

            try
            {
                var transaction = context.Txns.Find(existingTxnId);

                // Create a backorder manager right away (we'll use it if needed)
                var backorderManager = new BackorderManager(employee);
                var existingBackorder = backorderManager.GetExistingBackorder(transaction.SiteIdto);

                // If no existing backorder and we'll need one, create it
                bool needBackorder = false;

                // Get all warehouse inventory items
                var warehouseInventories = context.Inventories
                    .Where(i => i.SiteId == 2)  // Warehouse ID
                    .ToList();

                int backorderedItemCount = 0;

                // Check each item in our order
                foreach (var item in orderItems)
                {
                    // Find this item in warehouse inventory
                    var inventory = warehouseInventories.FirstOrDefault(i => i.ItemId == item.ItemId);

                    if (inventory == null)
                    {
                        // Item not in warehouse at all - backorder the entire quantity
                        needBackorder = true;

                        if (existingBackorder == null)
                        {
                            backorderManager.CreateNewBackorder(transaction.SiteIdto);
                            existingBackorder = backorderManager.GetExistingBackorder(transaction.SiteIdto);
                        }

                        backorderManager.AddItemToBackorder(existingBackorder.TxnId, item.ItemId, item.OrderQuantity);
                        item.OrderQuantity = 0;
                        backorderedItemCount++;
                    }
                    else if (inventory.Quantity < item.OrderQuantity)
                    {
                        // Not enough in stock
                        needBackorder = true;

                        if (existingBackorder == null)
                        {
                            backorderManager.CreateNewBackorder(transaction.SiteIdto);
                            existingBackorder = backorderManager.GetExistingBackorder(transaction.SiteIdto);
                        }

                        int shortfall = item.OrderQuantity - inventory.Quantity;
                        backorderManager.AddItemToBackorder(existingBackorder.TxnId, item.ItemId, shortfall);
                        item.OrderQuantity = inventory.Quantity;
                        backorderedItemCount++;
                    }
                }

                // If we created any backorders, show a message
                if (needBackorder)
                {
                    HandyControl.Controls.Growl.Info(new HandyControl.Data.GrowlInfo
                    {
                        Message = $"{backorderedItemCount} item(s) added to backorder due to insufficient inventory",
                        WaitTime = 5
                    });
                }

                // Save the updated order items
                SaveOrder();

                // Update the transaction status to ASSEMBLING
                transaction.TxnStatus = "ASSEMBLING";
                context.SaveChanges();

                // Create a record in the audit log
                AuditTransactions.LogActivity(
                    employee,
                    transaction.TxnId,
                    transaction.TxnType,
                    "ASSEMBLING",
                    transaction.SiteIdto,
                    transaction.DeliveryId,
                    $"Order sent for assembly by {employee.FirstName} {employee.LastName}"
                );

                // Show a success message
                HandyControl.Controls.Growl.Success(new HandyControl.Data.GrowlInfo
                {
                    Message = "Order sent for assembly successfully!",
                    WaitTime = 2,
                    Token = "SuccessMsg"
                });

                // Navigate back to the view orders screen
                var mainContent = this.Parent as ContentControl;
                mainContent.Content = new ViewOrders(employee);
            }
            catch (Exception ex)
            {
                // If anything goes wrong, show an error message
                HandyControl.Controls.MessageBox.Show(
                    $"Error processing order: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveOrder()
        {
            if (!existingTxnId.HasValue)
            {
                HandyControl.Controls.MessageBox.Show(
                    "No order to save.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                // Get all items currently in this transaction from the database
                var existingItems = context.Txnitems
                .Where(t => t.TxnId == existingTxnId)
                .ToList();

                // Go through each item in our current order list
                foreach (var item in orderItems)
                {
                    // Look for this item in the existing database items
                    var existingItem = existingItems.FirstOrDefault(i => i.ItemId == item.ItemId);

                    if (existingItem != null)
                    {
                        // Item already exists, just update the quantity
                        existingItem.Quantity = item.OrderQuantity;
                        context.Txnitems.Update(existingItem);
                    }
                    else
                    {
                        // Item is new, add it to the database
                        var newItem = new Txnitem
                        {
                            TxnId = existingTxnId.Value,
                            ItemId = item.ItemId,
                            Quantity = item.OrderQuantity
                        };
                        context.Txnitems.Add(newItem);
                    }
                }

                // Find items that were in the database but are no longer in our list
                var itemsToRemove = existingItems
                    .Where(i => !orderItems.Any(o => o.ItemId == i.ItemId))
                    .ToList();

                // Remove those items from the database
                foreach (var item in itemsToRemove)
                {
                    context.Txnitems.Remove(item);
                }

                // Save all changes to the database
                context.SaveChanges();

                // Get the transaction to use in the audit log
                var transaction = context.Txns.Find(existingTxnId);

                // Create a record in the audit log
                AuditTransactions.LogActivity(
                    employee,
                    transaction.TxnId,
                    transaction.TxnType,
                    transaction.TxnStatus,
                    transaction.SiteIdto,
                    transaction.DeliveryId,
                    $"Order updated by {employee.FirstName} {employee.LastName}"
                );

                // Show a success message
                HandyControl.Controls.Growl.Success(new HandyControl.Data.GrowlInfo
                {
                    Message = "Order saved successfully!",
                    WaitTime = 2,
                    Token = "SuccessMsg"
                });
            }
            catch (Exception ex)
            {
                // If anything goes wrong, show an error message
                HandyControl.Controls.MessageBox.Show(
                    $"Error saving order: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveOrder();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Ask for confirmation before proceeding
            var result = HandyControl.Controls.MessageBox.Show(
                "Are you sure you want to cancel this order?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            // If the user clicks Yes
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Get the transaction from the database
                    var transaction = context.Txns.Find(existingTxnId);

                    // Change the status to CANCELLED
                    transaction.TxnStatus = "CANCELLED";

                    // Save the change to the database
                    context.SaveChanges();

                    // Create a record in the audit log
                    AuditTransactions.LogActivity(
                        employee,
                        transaction.TxnId,
                        transaction.TxnType,
                        "CANCELLED",
                        transaction.SiteIdto,
                        transaction.DeliveryId,
                        $"Order cancelled by {employee.FirstName} {employee.LastName}"
                    );

                    // Show a success message
                    HandyControl.Controls.Growl.Success(new HandyControl.Data.GrowlInfo
                    {
                        Message = "Order cancelled successfully!",
                        WaitTime = 2,
                        Token = "SuccessMsg"
                    });

                    // Navigate back to the view orders screen
                    var mainContent = this.Parent as ContentControl;
                    mainContent.Content = new ViewOrders(employee);
                }
                catch (Exception ex)
                {
                    // If anything goes wrong, show an error message
                    HandyControl.Controls.MessageBox.Show(
                        $"Error cancelling order: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            var mainContent = this.Parent as ContentControl;
            mainContent.Content = new ViewOrders(employee);
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

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Only update if we have items loaded
            if (allInventoryItems.Count > 0)
            {
                UpdateDisplay();
            }
        }
    }
}
