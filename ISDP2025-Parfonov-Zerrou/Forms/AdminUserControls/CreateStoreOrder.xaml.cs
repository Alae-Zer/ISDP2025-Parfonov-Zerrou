using HandyControl.Controls;
using HandyControl.Data;
using ISDP2025_Parfonov_Zerrou.Forms.UserControls;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    //ISDP Project
    //Mohammed Alae-Zerrou, Serhii Parfonov
    //NBCC, Winter 2025
    //Completed By Mohammed
    //Last Modified by Mohammed on march 02,2025

    public partial class CreateStoreOrder : UserControl
    {
        private BestContext context;
        private List<OrderLineItem> orderItems = new();
        private List<OrderItem> allInventoryItems = new();
        private string currentSearchText = "";
        private int? existingTxnId = null;
        private string searchText = "";
        Employee employee;
        private bool isEmergencyOrder = false;

        public CreateStoreOrder()
        {
            InitializeComponent();
            context = new BestContext();
            loadStores();
        }

        public CreateStoreOrder(Employee emp)
        {
            InitializeComponent();
            context = new BestContext();
            employee = emp;
            loadStores();
            disable(false);
            ConfigureUIForUserRole();
        }

        public CreateStoreOrder(Employee emp, int ID)
        {
            InitializeComponent();
            employee = emp;
            existingTxnId = ID;
            context = new BestContext();
            loadStores();
            LoadExistingOrder();
            disable(true);
            ConfigureUIForUserRole();
        }

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
        private void ConfigureUIForUserRole()
        {
            if (employee == null || employee.Position == null)
            {
                // If employee or position is null, reload the employee with position included
                if (employee != null)
                {
                    employee = context.Employees
                        .Include(e => e.Position)
                        .FirstOrDefault(e => e.EmployeeID == employee.EmployeeID);
                }
                return;
            }

            // Get the permission level from employee
            var permissionLevel = employee.Position.PermissionLevel;

            // Configure UI based on role
            switch (permissionLevel)
            {
                case "Store Manager":
                    // Store managers can only see/edit their own store
                    cmbStores.IsEnabled = false;
                    cmbStores.SelectedValue = employee.SiteId;
                    break;

                case "Warehouse Manager":
                    // Warehouse managers can see all stores but not create new orders
                    btnCreate.Visibility = Visibility.Collapsed;
                    break;

                case "Administrator":
                    // Admins have full access - no restrictions
                    break;
            }
        }

        private void disable(bool incase)
        {
            all1.IsEnabled = incase;
            all2.IsEnabled = incase;
            cmbStores.IsEnabled = !incase;
            radio.IsEnabled = !incase;
            btnSave.IsEnabled = incase;
            btnSubmit.IsEnabled = incase;
            btnCreate.IsEnabled = !incase;
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
                cmbStores.SelectedValue = existingOrder.SiteIdto;
                //cmbDate.SelectedValue = existingOrder.ShipDate;

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
            if (existingOrder.TxnType == "Emergency Order")
            {
                radEmergency.IsChecked = true;
                Alert.Visibility = Visibility.Visible;
            }
            else if (existingOrder.TxnType == "Store Order")
            {
                radNormal.IsChecked = true;
                Alert.Visibility = Visibility.Collapsed;
            }
        }

        private void loadStores()
        {
            try
            {
                var query = context.Sites
                .Where(s => s.SiteId != 1 && s.SiteId != 3 && s.SiteId != 10000 && s.SiteId != 9999)
                .OrderBy(s => s.SiteName);

                // For store managers, filter to only show their store
                if (employee != null)
                {
                    // If employee.Position is null, load it from the database
                    if (employee.Position == null)
                    {
                        // Reload the employee with the Position included
                        employee = context.Employees
                            .Include(e => e.Position)
                            .FirstOrDefault(e => e.EmployeeID == employee.EmployeeID);
                    }
                    if (employee.Position.PermissionLevel == "Store Manager")
                    {
                        query = (IOrderedQueryable<Site>)query.Where(s => s.SiteId == employee.SiteId);
                    }
                }
                var stores = query.Select(s => new
                {
                    s.SiteId,
                    s.SiteName,
                    s.DayOfWeek
                }).ToList();

                cmbStores.ItemsSource = stores;
                cmbStores.DisplayMemberPath = "SiteName";
                cmbStores.SelectedValuePath = "SiteId";

                // Auto-select the store manager's store
                if (employee.Position.PermissionLevel == "Store Manager")
                {
                    cmbStores.SelectedValue = employee.SiteId;
                }


                PrePopulateOrder();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading initial data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PrePopulateOrder()
        {
            if (cmbStores.SelectedItem == null || !existingTxnId.HasValue) return;

            orderItems.Clear();

            int selectedSiteId = (int)cmbStores.SelectedValue;
            try
            {
                // First check if the order already has items
                var existingOrderItems = context.Txnitems
                    .Include(ti => ti.Item)
                    .Where(ti => ti.TxnId == existingTxnId)
                    .ToList();

                if (existingOrderItems.Any())
                {
                    // Order already has items, just load them
                    foreach (var item in existingOrderItems)
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
                }
                else
                {
                    // Order is new, needs pre-population based on thresholds
                    var inventoryData = context.Inventories
                        .Include(i => i.Item)
                        .Where(i => i.SiteId == selectedSiteId && i.Item.Active == 1)
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

                    // Only save new items if we're actually adding them
                    if (orderItems.Any())
                    {
                        foreach (var item in orderItems)
                        {
                            var txnItem = new Txnitem
                            {
                                TxnId = existingTxnId.Value,
                                ItemId = item.ItemId,
                                Quantity = item.OrderQuantity
                            };

                            context.Txnitems.Add(txnItem);
                        }

                        context.SaveChanges();
                    }
                }

                dgvOrders.ItemsSource = null;
                dgvOrders.ItemsSource = orderItems;
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(
                    $"Error pre-populating order: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadInventoryItems()
        {
            try
            {
                if (cmbStores.SelectedItem == null)
                    return;

                var selectedSiteId = (int)cmbStores.SelectedValue;

                // Get store inventory with warehouse quantities
                allInventoryItems = context.Inventories
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

        public DateTime GetNextDeliveryDate(string dayOfWeek)
        {
            DateTime today = DateTime.Now;
            DateTime nextDate = today;

            // Keep adding days until we hit the right day of week
            while (nextDate.DayOfWeek.ToString().ToUpper() != dayOfWeek.ToUpper())
            {
                nextDate = nextDate.AddDays(1);
            }

            // If it's today and past business hours, add a week
            if (nextDate.Date == today.Date && today.Hour >= 17)
            {
                nextDate = nextDate.AddDays(7);
            }

            return nextDate.Date;
        }

        private int CalculateItemsPerPage()
        {
            // Get the actual height of the grid
            double gridHeight = InventoryGrid.ActualHeight;
            // Row height is set to 36 in your XAML
            int rowHeight = 36;
            // Header height (from your style)
            int headerHeight = 40;
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

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (radEmergency.IsChecked == true)
            {
                if (orderItems != null && orderItems.Count >= 5)
                {
                    Growl.Warning(new GrowlInfo
                    {
                        Message = "Emergency orders cannot have more than 5 items.",
                        ShowDateTime = false,
                        WaitTime = 2
                    });
                    return;
                }
            }

            var selectedItem = InventoryGrid.SelectedItem as OrderItem;
            if (selectedItem == null)
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "Please select an item to add.",
                    ShowDateTime = false,
                    WaitTime = 2
                });
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
                OrderQuantity = selectedItem.CaseSize,
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

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbStores.SelectedValue is null)
                {
                    Growl.Warning(new GrowlInfo
                    {
                        Message = "Please select a store.",
                        ShowDateTime = false,
                        WaitTime = 2,
                    });

                    return;
                }
                if (radEmergency.IsChecked == false && radNormal.IsChecked == false)
                {
                    Growl.Warning(new GrowlInfo
                    {
                        Message = "Please select an order type.",
                        ShowDateTime = false,
                        WaitTime = 2
                    });
                    return;
                }

                int radioBtn = radEmergency.IsChecked == true ? 1 : 0;
                int selectedSite = (int)cmbStores.SelectedValue;

                var existingOrder = context.Txns.FirstOrDefault(t => t.SiteIdto == selectedSite && t.TxnStatus == "NEW" && t.EmergencyDelivery == radioBtn);

                if (existingOrder != null)
                {
                    HandyControl.Controls.MessageBox.Show($"An open {(radioBtn == 1 ? "emergency" : "normal")} order already exists for this store.",
                    "Duplicate Order", MessageBoxButton.OK, MessageBoxImage.Warning);

                    var mainContent = this.Parent as ContentControl;
                    mainContent.Content = new ViewOrders(employee);

                    return;
                }

                disable(true);

                var site = context.Sites.FirstOrDefault(s => s.SiteId == selectedSite);

                var txn = new Txn
                {
                    EmployeeId = employee.EmployeeID,
                    SiteIdto = selectedSite,
                    SiteIdfrom = 2,
                    TxnStatus = "NEW",
                    ShipDate = radioBtn != 1 ? GetNextDeliveryDate(site.DayOfWeek) : DateTime.Today.AddDays(1),
                    TxnType = radioBtn == 1 ? "Emergency Order" : "Store Order",
                    BarCode = GenerateBarcode(),
                    CreatedDate = DateTime.Now,
                    EmergencyDelivery = (sbyte)(radioBtn)
                };

                context.Txns.Add(txn);
                context.SaveChanges();

                existingTxnId = txn.TxnId;

                AuditTransactions.LogActivity(
                employee,
                txn.TxnId,
                txn.TxnType,
                "NEW",
                txn.SiteIdto,
                null,
                $"Order created by {employee.FirstName} {employee.LastName}"
            );

                if (radioBtn == 0)
                {
                    PrePopulateOrder();
                }
                else
                {
                    Alert.Visibility = Visibility.Visible;
                }

                Growl.Success(new GrowlInfo
                {
                    Message = "Order created successfully!",
                    ShowDateTime = false,
                    WaitTime = 2
                });
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error creating order: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }


        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInventoryItems();
            UpdateDisplay();
        }



        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (!existingTxnId.HasValue)
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "No active order to submit.",
                    ShowDateTime = false,
                    WaitTime = 2,
                });
                return;
            }

            if (orderItems.Count == 0)
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "Cannot submit an empty order.",
                    ShowDateTime = false,
                    WaitTime = 2,
                });
                return;
            }

            if (radEmergency.IsChecked == true && orderItems.Count > 5)
            {
                HandyControl.Controls.MessageBox.Show(
                    "Emergency orders cannot have more than 5 items.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var result = HandyControl.Controls.MessageBox.Show(
            "Are you sure you want to submit this order?\n\n" +
            "Once submitted, the order will be sent to the warehouse for processing.",
            "Confirm Submission",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                return;
            }

            try
            {
                SaveOrder();

                var transaction = context.Txns.Find(existingTxnId);
                transaction.TxnStatus = "SUBMITTED";

                //context.Txns.Update(transaction);
                context.SaveChanges();

                AuditTransactions.LogActivity(
                    employee,
                    transaction.TxnId,
                    transaction.TxnType,
                    "SUBMITTED",
                    transaction.SiteIdto,
                    null,
                    $"Order submitted to warehouse by {employee.FirstName} {employee.LastName}"
                );

                Growl.Success(new GrowlInfo
                {
                    Message = "Order submitted successfully!",
                    ShowDateTime = false,
                    WaitTime = 2,
                    Token = "SuccessMsg"
                });

                var mainContent = this.Parent as ContentControl;
                mainContent.Content = new ViewOrders(employee);
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error submitting order: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateBarcode()
        {
            if (radEmergency.IsChecked == true)
                return $"EMERGENCY_TXN-{DateTime.Now:yyyyMMddHHmmss}";
            else
                return $"NORAMAL_TXN-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private void StoreComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadInventoryItems();
            UpdateDisplay();
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

        private void SaveOrder()
        {
            if (!existingTxnId.HasValue) return;

            try
            {
                // First, get existing txnitems for this transaction
                var existingItems = context.Txnitems
                    .Where(t => t.TxnId == existingTxnId)
                    .ToList();

                // Track what we're going to update
                List<Txnitem> itemsToUpdate = new List<Txnitem>();
                List<Txnitem> itemsToAdd = new List<Txnitem>();
                List<Txnitem> itemsToRemove = new List<Txnitem>();

                // Process all items in our current order
                foreach (var item in orderItems)
                {
                    // Check if this item already exists in the transaction
                    var existingItem = existingItems
                        .FirstOrDefault(e => e.ItemId == item.ItemId);

                    if (existingItem != null)
                    {
                        // Update existing item
                        existingItem.Quantity = item.OrderQuantity;
                        itemsToUpdate.Add(existingItem);
                    }
                    else
                    {
                        // Add new item
                        var txnItem = new Txnitem
                        {
                            TxnId = (int)existingTxnId,
                            ItemId = item.ItemId,
                            Quantity = item.OrderQuantity
                        };
                        itemsToAdd.Add(txnItem);
                    }
                }

                // Find items that are no longer in the order
                itemsToRemove = existingItems
                    .Where(e => !orderItems.Any(o => o.ItemId == e.ItemId))
                    .ToList();

                // Now perform the actual database operations
                context.Txnitems.AddRange(itemsToAdd);

                foreach (var item in itemsToUpdate)
                {
                    context.Txnitems.Update(item);
                }

                context.Txnitems.RemoveRange(itemsToRemove);

                // Save all changes in one go
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving order: {ex.Message}", ex);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveOrder();
                Growl.Success(new GrowlInfo
                {
                    Message = "Order Saved successfully!",
                    ShowDateTime = false,
                    WaitTime = 2,
                });
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error creating order: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            context.Dispose();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (existingTxnId.HasValue)
            {
                var result = HandyControl.Controls.MessageBox.Show(
                    "Are you sure you want to cancel this order?",
                    "Confirm Cancel",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var transaction = context.Txns.Find(existingTxnId);
                        if (transaction != null)
                        {
                            transaction.TxnStatus = "CANCELLED";
                            context.SaveChanges();

                            Growl.Success(new GrowlInfo
                            {
                                Message = "Order cancelled successfully!",
                                ShowDateTime = false,
                                WaitTime = 2
                            });

                            var mainContent = this.Parent as ContentControl;
                            mainContent.Content = new ViewOrders(employee);
                        }
                    }
                    catch (Exception ex)
                    {
                        HandyControl.Controls.MessageBox.Show(
                            $"Error cancelling order: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            var mainContent = this.Parent as ContentControl;
            mainContent.Content = new ViewOrders(employee);
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
