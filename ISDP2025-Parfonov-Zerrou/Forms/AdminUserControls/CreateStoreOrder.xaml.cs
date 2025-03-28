using HandyControl.Controls;
using HandyControl.Data;
using ISDP2025_Parfonov_Zerrou.Forms.UserControls;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class CreateStoreOrder : UserControl
    {
        private BestContext context;
        private List<OrderLineItem> orderItems = new();
        private List<OrderItem> allInventoryItems = new();

        private int? existingTxnId = null;
        private string searchText = "";
        Employee employee;

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

        public class OrderItem
        {
            public int ItemId { get; set; }
            public string Name { get; set; }
            public int Quantity { get; set; }
            public int CaseSize { get; set; }
            public decimal Weight { get; set; }
            public int ReorderThreshold { get; set; }
            public string Description { get; set; }
            public int? SupplierId { get; set; }
        }

        public class OrderLineItem
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
                if (employee != null)
                {
                    employee = context.Employees
                        .Include(e => e.Position)
                        .FirstOrDefault(e => e.EmployeeID == employee.EmployeeID);
                }
                return;
            }

            var permissionLevel = employee.Position.PermissionLevel;

            switch (permissionLevel)
            {
                case "Store Manager":
                    cmbStores.IsEnabled = false;
                    cmbStores.SelectedValue = employee.SiteId;
                    break;

                case "Warehouse Manager":
                    btnCreate.Visibility = Visibility.Collapsed;
                    break;

                case "Administrator":
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

                ConfigureUIForOrderType(existingOrder.TxnType);
            }
        }

        private void ConfigureUIForOrderType(string orderType)
        {
            if (orderType == "Supplier Order")
            {
                radio.Visibility = Visibility.Collapsed;
                cmbSuppliers.Visibility = Visibility.Visible;
                Alert.Visibility = Visibility.Collapsed;
                SupplierAlert.Visibility = Visibility.Visible;
            }
            else if (orderType == "Emergency Order")
            {
                radEmergency.IsChecked = true;
                Alert.Visibility = Visibility.Visible;
                SupplierAlert.Visibility = Visibility.Collapsed;
            }
            else if (orderType == "Store Order")
            {
                radNormal.IsChecked = true;
                Alert.Visibility = Visibility.Collapsed;
                SupplierAlert.Visibility = Visibility.Collapsed;
            }
        }

        private void loadStores()
        {
            try
            {
                var query = context.Sites
                .Where(s => s.SiteId != 1 && s.SiteId != 3 && s.SiteId != 10000 && s.SiteId != 9999)
                .OrderBy(s => s.SiteName);

                if (employee != null)
                {
                    if (employee.Position == null)
                    {
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

                if (employee?.Position?.PermissionLevel == "Store Manager")
                {
                    cmbStores.SelectedValue = employee.SiteId;
                }

                PrePopulateOrder();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading initial data", ex.Message);
            }
        }

        private void PrePopulateOrder()
        {
            if (cmbStores.SelectedItem == null || !existingTxnId.HasValue) return;

            orderItems.Clear();

            int selectedSiteId = (int)cmbStores.SelectedValue;
            try
            {
                var existingOrderItems = context.Txnitems
                    .Include(ti => ti.Item)
                    .Where(ti => ti.TxnId == existingTxnId)
                    .ToList();

                if (existingOrderItems.Any())
                {
                    LoadExistingOrderItems(existingOrderItems);
                }
                else
                {
                    LoadSuggestedItems(selectedSiteId);
                }

                dgvOrders.ItemsSource = null;
                dgvOrders.ItemsSource = orderItems;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error pre-populating order", ex.Message);
            }
        }

        private void LoadExistingOrderItems(List<Txnitem> existingItems)
        {
            foreach (var item in existingItems)
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

        private void LoadSuggestedItems(int siteId)
        {
            var inventoryData = context.Inventories
                .Include(i => i.Item)
                .Where(i => i.SiteId == siteId && i.Item.Active == 1)
                .ToList();

            foreach (var inventory in inventoryData)
            {
                if (inventory.Quantity <= inventory.ReorderThreshold)
                {
                    int needed = inventory.OptimumThreshold - inventory.Quantity;
                    int cases = inventory.Item.CaseSize > 0 ?
                        (int)Math.Ceiling((double)needed / inventory.Item.CaseSize) : needed;

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

            SaveSuggestedItems();
        }

        private void SaveSuggestedItems()
        {
            if (orderItems.Any() && existingTxnId.HasValue)
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

        private void LoadInventoryItems()
        {
            try
            {
                if (cmbStores.SelectedItem == null)
                    return;

                var selectedSiteId = (int)cmbStores.SelectedValue;
                bool isWarehouse = selectedSiteId == 2;

                var query = context.Inventories
                    .Include(i => i.Item)
                    .Where(i => i.SiteId == selectedSiteId && i.Item.Active == 1);

                if (isWarehouse && cmbSuppliers != null && cmbSuppliers.SelectedValue != null && (int)cmbSuppliers.SelectedValue != 0)
                {
                    int supplierId = (int)cmbSuppliers.SelectedValue;
                    query = query.Where(i => i.Item.SupplierId == supplierId);
                }

                if (isWarehouse)
                {
                    allInventoryItems = query
                        .OrderBy(i => i.Item.SupplierId)
                        .ThenBy(i => i.Item.Name)
                        .Select(i => new OrderItem
                        {
                            ItemId = i.ItemId,
                            Name = i.Item.Name,
                            Quantity = i.Quantity,
                            CaseSize = i.Item.CaseSize,
                            Weight = i.Item.Weight,
                            ReorderThreshold = i.ReorderThreshold ?? 0,
                            Description = i.Item.Description ?? i.Item.Name,
                            SupplierId = i.Item.SupplierId
                        })
                        .ToList();
                }
                else
                {
                    allInventoryItems = query
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
                }

                InventoryPagination.PageIndex = 1;
                UpdateDisplay();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading inventory", ex.Message);
            }
        }

        private void LoadSuppliers()
        {
            try
            {
                var suppliers = context.Suppliers
                    .Where(s => s.Active == 1)
                    .OrderBy(s => s.Name)
                    .Select(s => new { s.SupplierId, SupplierName = s.Name })
                    .ToList();

                var allSuppliersOption = new { SupplierId = 0, SupplierName = "All Suppliers" };
                var supplierList = new List<object> { allSuppliersOption };

                supplierList.AddRange(suppliers);

                cmbSuppliers.ItemsSource = supplierList;
                cmbSuppliers.DisplayMemberPath = "SupplierName";
                cmbSuppliers.SelectedValuePath = "SupplierId";
                cmbSuppliers.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading suppliers", ex.Message);
            }
        }

        private void CmbSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadInventoryItems();
            UpdateDisplay();
        }

        public DateTime GetNextDeliveryDate(string dayOfWeek)
        {
            DateTime today = DateTime.Now;
            DateTime nextDate = today;

            while (nextDate.DayOfWeek.ToString().ToUpper() != dayOfWeek.ToUpper())
            {
                nextDate = nextDate.AddDays(1);
            }

            if (nextDate.Date == today.Date && today.Hour >= 17)
            {
                nextDate = nextDate.AddDays(7);
            }

            return nextDate.Date;
        }

        private int CalculateItemsPerPage()
        {
            double gridHeight = InventoryGrid.ActualHeight;
            int rowHeight = 36;
            int headerHeight = 40;
            int availableRows = (int)((gridHeight - headerHeight) / rowHeight);
            return Math.Max(availableRows, 1);
        }

        private void UpdateDisplay()
        {
            var filteredItems = string.IsNullOrWhiteSpace(searchText)
                ? allInventoryItems
                : allInventoryItems.Where(i => i.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    i.ItemId.ToString().Contains(searchText)).ToList();

            int itemsPerPage = CalculateItemsPerPage();

            int totalPages = (int)Math.Ceiling(filteredItems.Count / (double)itemsPerPage);
            InventoryPagination.MaxPageCount = Math.Max(1, totalPages);

            if (InventoryPagination.PageIndex > totalPages && totalPages > 0)
                InventoryPagination.PageIndex = totalPages;

            int startIndex = (InventoryPagination.PageIndex - 1) * itemsPerPage;
            var pagedItems = filteredItems
                .Skip(startIndex)
                .Take(itemsPerPage)
                .ToList();

            InventoryGrid.ItemsSource = pagedItems;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchText = SearchBox.Text;
            InventoryPagination.PageIndex = 1;
            UpdateDisplay();
        }

        private void InventoryPagination_PageUpdated(object sender, HandyControl.Data.FunctionEventArgs<int> e)
        {
            UpdateDisplay();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (CheckEmergencyOrderItemLimit())
                return;

            var selectedItem = InventoryGrid.SelectedItem as OrderItem;
            if (selectedItem == null)
            {
                ShowWarningMessage("Please select an item to add.");
                return;
            }

            AddItemToOrder(selectedItem);
        }

        private bool CheckEmergencyOrderItemLimit()
        {
            if (radEmergency.IsChecked == true && orderItems != null && orderItems.Count >= 5)
            {
                ShowWarningMessage("Emergency orders cannot have more than 5 items.");
                return true;
            }
            return false;
        }

        private void AddItemToOrder(OrderItem selectedItem)
        {
            var existingItem = orderItems.FirstOrDefault(item => item.ItemId == selectedItem.ItemId);
            if (existingItem != null)
            {
                existingItem.OrderQuantity += selectedItem.CaseSize;
            }
            else
            {
                var newOrderItem = new OrderLineItem
                {
                    ItemId = selectedItem.ItemId,
                    Name = selectedItem.Name,
                    OrderQuantity = selectedItem.CaseSize,
                    CaseSize = selectedItem.CaseSize,
                    Weight = selectedItem.Weight
                };
                orderItems.Add(newOrderItem);
            }

            RefreshOrderItemsDisplay();
        }

        private void RefreshOrderItemsDisplay()
        {
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
            RefreshOrderItemsDisplay();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbStores.SelectedValue is null)
                {
                    ShowWarningMessage("Please select a store.");
                    return;
                }

                int selectedSite = (int)cmbStores.SelectedValue;
                bool isWarehouse = selectedSite == 2;

                if (isWarehouse)
                {
                    CreateSupplierOrder(selectedSite);
                    return;
                }

                if (!ValidateOrderType())
                    return;

                int emergencyFlag = radEmergency.IsChecked == true ? 1 : 0;

                if (CheckExistingOrder(selectedSite, emergencyFlag))
                    return;

                CreateNewOrder(selectedSite, emergencyFlag);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error creating order", ex.Message);
            }
        }

        private bool ValidateOrderType()
        {
            if (radEmergency.IsChecked == false && radNormal.IsChecked == false)
            {
                ShowWarningMessage("Please select an order type.");
                return false;
            }
            return true;
        }

        private bool CheckExistingOrder(int siteId, int emergencyFlag)
        {
            var existingOrder = context.Txns.FirstOrDefault(t =>
                t.SiteIdto == siteId &&
                t.TxnStatus == "NEW" &&
                t.EmergencyDelivery == emergencyFlag);

            if (existingOrder != null)
            {
                string orderType = emergencyFlag == 1 ? "emergency" : "normal";
                HandyControl.Controls.MessageBox.Show(
                    $"An open {orderType} order already exists for this store.",
                    "Duplicate Order",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                NavigateToOrderList();
                return true;
            }
            return false;
        }

        private void CreateSupplierOrder(int selectedSite)
        {
            var existingSupplierOrder = context.Txns.FirstOrDefault(t =>
                t.SiteIdto == selectedSite &&
                t.TxnStatus == "NEW" &&
                t.TxnType == "Supplier Order");

            if (existingSupplierOrder != null)
            {
                HandyControl.Controls.MessageBox.Show(
                    "An open supplier order already exists for the warehouse.",
                    "Duplicate Order",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                NavigateToOrderList();
                return;
            }

            disable(true);

            var supplierTxn = new Txn
            {
                EmployeeId = employee.EmployeeID,
                SiteIdto = selectedSite,
                SiteIdfrom = 2,
                TxnStatus = "NEW",
                ShipDate = DateTime.Today.AddDays(14),
                TxnType = "Supplier Order",
                BarCode = $"SUPPLIER_TXN-{DateTime.Now:yyyyMMddHHmmss}",
                CreatedDate = DateTime.Now,
                EmergencyDelivery = 0
            };

            CreateTransactionAndLog(supplierTxn, "Supplier order created successfully!");
            PrePopulateOrder();
        }

        private void CreateNewOrder(int selectedSite, int emergencyFlag)
        {
            disable(true);

            var site = context.Sites.FirstOrDefault(s => s.SiteId == selectedSite);

            var txn = new Txn
            {
                EmployeeId = employee.EmployeeID,
                SiteIdto = selectedSite,
                SiteIdfrom = 2,
                TxnStatus = "NEW",
                ShipDate = emergencyFlag != 1 ? GetNextDeliveryDate(site.DayOfWeek) : DateTime.Today.AddDays(1),
                TxnType = emergencyFlag == 1 ? "Emergency Order" : "Store Order",
                BarCode = GenerateBarcode(),
                CreatedDate = DateTime.Now,
                EmergencyDelivery = (sbyte)(emergencyFlag)
            };

            CreateTransactionAndLog(txn, "Order created successfully!");

            if (emergencyFlag == 0)
            {
                PrePopulateOrder();
            }
            else
            {
                Alert.Visibility = Visibility.Visible;
            }
        }

        private void CreateTransactionAndLog(Txn transaction, string successMessage)
        {
            try
            {
                context.Txns.Add(transaction);
                context.SaveChanges();

                existingTxnId = transaction.TxnId;

                LogTransactionActivity(
                    transaction.TxnId,
                    transaction.TxnType,
                    "NEW",
                    transaction.SiteIdto,
                    $"{transaction.TxnType} created by {employee.FirstName} {employee.LastName}"
                );

                ShowSuccessMessage(successMessage);
            }
            catch (Exception ex)
            {
                string fullError = ex.ToString();
                HandyControl.Controls.MessageBox.Show(
                    $"Error creating {transaction.TxnType}: {fullError}",
                    "Detailed Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LogTransactionActivity(int txnId, string txnType, string status, int siteId, string comment)
        {
            AuditTransactions.LogActivity(
                employee,
                txnId,
                txnType,
                status,
                siteId,
                null,
                comment
            );
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInventoryItems();
            UpdateDisplay();
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateBeforeSubmit())
                return;

            var confirmResult = HandyControl.Controls.MessageBox.Show(
                "Are you sure you want to submit this order?\n\n" +
                "Once submitted, the order will be sent to the warehouse for processing.",
                "Confirm Submission",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult == MessageBoxResult.No)
                return;

            try
            {
                SaveOrder();
                UpdateTransactionStatus("SUBMITTED");
                ShowSuccessMessage("Order submitted successfully!");
                NavigateToOrderList();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error submitting order", ex.Message);
            }
        }

        private bool ValidateBeforeSubmit()
        {
            if (!existingTxnId.HasValue)
            {
                ShowWarningMessage("No active order to submit.");
                return false;
            }

            if (orderItems.Count == 0)
            {
                ShowWarningMessage("Cannot submit an empty order.");
                return false;
            }

            var transaction = context.Txns.Find(existingTxnId);
            bool isEmergencyOrder = transaction.TxnType == "Emergency Order";

            if (isEmergencyOrder && orderItems.Count > 5)
            {
                HandyControl.Controls.MessageBox.Show(
                    "Emergency orders cannot have more than 5 items.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void UpdateTransactionStatus(string status)
        {
            var transaction = context.Txns.Find(existingTxnId);
            if (transaction != null)
            {
                transaction.TxnStatus = status;
                context.SaveChanges();

                LogTransactionActivity(
                    transaction.TxnId,
                    transaction.TxnType,
                    status,
                    transaction.SiteIdto,
                    $"Order {status.ToLower()} by {employee.FirstName} {employee.LastName}"
                );
            }
        }

        private string GenerateBarcode()
        {
            if (radEmergency.IsChecked == true)
                return $"EMERGENCY_TXN-{DateTime.Now:yyyyMMddHHmmss}";
            else
                return $"NORMAL_TXN-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private void StoreComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbStores.SelectedValue != null)
            {
                int selectedSiteId = (int)cmbStores.SelectedValue;
                if (selectedSiteId == 2)
                {
                    SetupWarehouseUI();
                }
                else
                {
                    SetupStoreUI();
                }
            }

            LoadInventoryItems();
            UpdateDisplay();
        }

        private void SetupWarehouseUI()
        {
            radio.Visibility = Visibility.Collapsed;
            cmbSuppliers.Visibility = Visibility.Visible;
            Alert.Visibility = Visibility.Collapsed;
            SupplierAlert.Visibility = Visibility.Visible;
            btnCreate.Content = "Create Supplier Order";
            LoadSuppliers();
        }

        private void SetupStoreUI()
        {
            radio.Visibility = Visibility.Visible;
            cmbSuppliers.Visibility = Visibility.Collapsed;
            SupplierAlert.Visibility = Visibility.Collapsed;
            btnCreate.Content = "Create";
        }

        private void nupQuantity_ValueChanged(object sender, HandyControl.Data.FunctionEventArgs<double> e)
        {
            NumericUpDown npm = (NumericUpDown)sender;
            OrderLineItem selectedItem = (OrderLineItem)dgvOrders.SelectedItem;

            if (selectedItem != null)
            {
                orderItems[dgvOrders.SelectedIndex].OrderQuantity = (int)npm.Value;
                RefreshOrderItemsDisplay();
            }
        }

        private void SaveOrder()
        {
            if (!existingTxnId.HasValue) return;

            try
            {
                var existingItems = context.Txnitems
                    .Where(t => t.TxnId == existingTxnId)
                    .ToList();

                List<Txnitem> itemsToUpdate = new List<Txnitem>();
                List<Txnitem> itemsToAdd = new List<Txnitem>();
                List<Txnitem> itemsToRemove = new List<Txnitem>();

                foreach (var item in orderItems)
                {
                    var existingItem = existingItems
                        .FirstOrDefault(e => e.ItemId == item.ItemId);

                    if (existingItem != null)
                    {
                        existingItem.Quantity = item.OrderQuantity;
                        itemsToUpdate.Add(existingItem);
                    }
                    else
                    {
                        var txnItem = new Txnitem
                        {
                            TxnId = existingTxnId.Value,
                            ItemId = item.ItemId,
                            Quantity = item.OrderQuantity
                        };
                        itemsToAdd.Add(txnItem);
                    }
                }

                itemsToRemove = existingItems
                    .Where(e => !orderItems.Any(o => o.ItemId == e.ItemId))
                    .ToList();

                context.Txnitems.AddRange(itemsToAdd);

                foreach (var item in itemsToUpdate)
                {
                    context.Txnitems.Update(item);
                }

                context.Txnitems.RemoveRange(itemsToRemove);

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
                ShowSuccessMessage("Order Saved successfully!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error saving order", ex.Message);
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
                        UpdateTransactionStatus("CANCELLED");
                        ShowSuccessMessage("Order cancelled successfully!");
                        NavigateToOrderList();
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessage("Error cancelling order", ex.Message);
                    }
                }
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigateToOrderList();
        }

        private void NavigateToOrderList()
        {
            var mainContent = this.Parent as ContentControl;
            mainContent.Content = new ViewOrders(employee);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (allInventoryItems.Count > 0)
            {
                UpdateDisplay();
            }
        }

        private void ShowSuccessMessage(string message)
        {
            Growl.Success(new GrowlInfo
            {
                Message = message,
                ShowDateTime = false,
                WaitTime = 2
            });
        }

        private void ShowWarningMessage(string message)
        {
            Growl.Warning(new GrowlInfo
            {
                Message = message,
                ShowDateTime = false,
                WaitTime = 2
            });
        }

        private void ShowErrorMessage(string title, string errorMessage)
        {
            HandyControl.Controls.MessageBox.Show(
                $"{title}: {errorMessage}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void InventoryGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedItem = InventoryGrid.SelectedItem as OrderItem;
            if (selectedItem != null)
            {
                var item = context.Items
                    .Include(i => i.Supplier)
                    .FirstOrDefault(i => i.ItemId == selectedItem.ItemId);

                if (item != null)
                {
                    var detailsPopup = new ItemDetailsPopup(item);
                    detailsPopup.ShowDialog();
                }
            }
        }

        private void dgvOrders_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedItem = dgvOrders.SelectedItem as OrderLineItem;
            if (selectedItem != null)
            {
                var item = context.Items
                    .Include(i => i.Supplier)
                    .FirstOrDefault(i => i.ItemId == selectedItem.ItemId);

                if (item != null)
                {
                    var detailsPopup = new ItemDetailsPopup(item);
                    detailsPopup.ShowDialog();
                }
            }
        }
    }
}