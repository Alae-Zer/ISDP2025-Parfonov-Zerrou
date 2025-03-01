using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using HandyControl.Data;
using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using ISDP2025_Parfonov_Zerrou.Forms.ForemanUserControls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Forms.UserControls
{
    public partial class ViewOrders : UserControl
    {
        private BestContext context;
        private Employee Employee;
        private int? selectedOrderTxnId = null;
        string permissionLevel;

        public ViewOrders()
        {
            InitializeComponent();
            context = new BestContext();
            LoadTransactions(); // Load transactions (aka orders) when control is initialized
        }

        public ViewOrders(Employee employee)
        {
            InitializeComponent();
            Employee = employee;
            context = new BestContext();
            ConfigureUIForUserRole();
            LoadTransactions(); // Load transactions (aka orders) when control is initialized


        }
        private void ConfigureUIForUserRole()
        {
            if (Employee == null || Employee.Position == null)
            {
                // If employee or position is null, reload the employee with position included
                if (Employee != null)
                {
                    Employee = context.Employees
                        .Include(e => e.Position)
                        .FirstOrDefault(e => e.EmployeeID == Employee.EmployeeID);
                }
            }

            // Get the permission level from employee
            permissionLevel = Employee.Position.PermissionLevel;


            switch (permissionLevel)
            {
                case "Store Manager":
                    Alert.Visibility = Visibility.Collapsed;
                    btnReceiveWarehouse.Visibility = Visibility.Collapsed;
                    break;

                case "Warehouse Manager":
                    btnCreate.Visibility = Visibility.Collapsed;
                    break;

                case "Administrator":
                    break;
            }
        }

        private bool checkOrder()
        {
            return context.Txns.Any(t => t.TxnStatus == "SUBMITTED" && (t.TxnType == "Store Order" || t.TxnType == "Emergency Order"));
        }

        private void LoadTransactions()
        {
            try
            {

                var query = context.Txns
                    .Include(t => t.SiteIdtoNavigation)
                    .Include(t => t.Txnitems)
                        .ThenInclude(ti => ti.Item)
                    .AsQueryable();

                // Apply filters if selected
                if (cmbOrderType.SelectedItem != null)
                {
                    string selectedContent = ((ComboBoxItem)cmbOrderType.SelectedItem).Content.ToString();
                    if (selectedContent != "All" && selectedContent != "ALL")
                    {
                        string orderType = selectedContent;
                        query = query.Where(t => t.TxnType == orderType);
                    }
                }

                if (cmbStatus.SelectedItem != null)
                {
                    string selectedContent = ((ComboBoxItem)cmbStatus.SelectedItem).Content.ToString();
                    if (selectedContent != "All" && selectedContent != "ALL")
                    {
                        string status = selectedContent;
                        query = query.Where(t => t.TxnStatus == status);
                    }
                }

                if (permissionLevel == "Store Manager")
                {
                    var orders = query
                    .Where(t => t.SiteIdto == Employee.SiteId)
                    .Select(t => new
                    {
                        TxnId = t.TxnId,
                        Location = t.SiteIdtoNavigation.SiteName,
                        Status = t.TxnStatus,
                        Items = t.Txnitems.Count(),
                        Weight = t.Txnitems.Sum(ti => ti.Item.Weight * ti.Quantity) > 0 ? t.Txnitems.Sum(ti => ti.Item.Weight * ti.Quantity).ToString("#.## KG") : "0 KG",
                        DeliveryDate = t.ShipDate,
                        OrderType = t.TxnType
                    })
                    .ToList();
                    dgOrders.ItemsSource = orders;
                }
                else
                {
                    var orders = query
                    .Select(t => new
                    {
                        TxnId = t.TxnId,
                        Location = t.SiteIdtoNavigation.SiteName,
                        Status = t.TxnStatus,
                        Items = t.Txnitems.Count(),
                        Weight = t.Txnitems.Sum(ti => ti.Item.Weight * ti.Quantity) > 0 ? t.Txnitems.Sum(ti => ti.Item.Weight * ti.Quantity).ToString("#.## KG") : "0 KG",
                        DeliveryDate = t.ShipDate,
                        OrderType = t.TxnType
                    })
                    .ToList();
                    dgOrders.ItemsSource = orders;
                    Alert.Visibility = checkOrder() == true ? Visibility.Visible : Visibility.Collapsed;
                }


            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading transactions: {ex.Message}",
                              "Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadTransactions();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            var mainContent = this.Parent as ContentControl;
            if (mainContent != null)
            {
                // Create brand new order
                mainContent.Content = new CreateStoreOrder(Employee);
            }
        }

        private void btnReceive_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = dgOrders.SelectedItem;
            if (selectedOrder == null)
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "Please select an order to receive.",
                    ShowDateTime = false,
                    WaitTime = 2
                });
                return;
            }
            int txnID = (int)selectedOrder.GetType().GetProperty("TxnId").GetValue(selectedOrder);
            var transaction = context.Txns.FirstOrDefault(t => t.TxnId == txnID);
            transaction.TxnStatus = "DELIVERED";
            context.Txns.Update(transaction);
            context.SaveChanges();
            LoadTransactions();
            selectedOrderTxnId = null;
            Growl.Success(new GrowlInfo
            {
                Message = "Order Received successfully!",
                ShowDateTime = false,
                WaitTime = 2
            });
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = dgOrders.SelectedItem;
            if (selectedOrder == null)
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "Please select an order to delete.",
                    ShowDateTime = false,
                    WaitTime = 2
                });
                return;
            }
            // write delete functionality
        }

        private void dgOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrders.SelectedItem != null)
            {
                dynamic selectedOrder = dgOrders.SelectedItem;
                if (selectedOrder.GetType().GetProperty("Status").GetValue(selectedOrder) == "NEW")
                {
                    selectedOrderTxnId = selectedOrder.TxnId;

                }
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var existingOrder = context.Txns.FirstOrDefault(t => t.TxnStatus == "NEW" && (t.TxnType == "Store Order" || t.TxnType == "Emergency Order")); //t.SiteIdto == Employee.SiteId &&

            var mainContent = this.Parent as ContentControl;
            if (mainContent != null)
            {
                if (selectedOrderTxnId.HasValue)
                {
                    // Open selected order for modification
                    mainContent.Content = new CreateStoreOrder(Employee, selectedOrderTxnId.Value);
                }
                else if (existingOrder != null)
                {
                    // Open existing order for modification
                    mainContent.Content = new CreateStoreOrder(Employee, existingOrder.TxnId);
                }
                else
                {
                    Growl.Warning(new GrowlInfo
                    {
                        Message = "Please select an open order.",
                        ShowDateTime = false,
                        WaitTime = 2
                    });
                }
            }
        }

        private void btnReceiveWarehouse_Click(object sender, RoutedEventArgs e)
        {
            var existingOrder = context.Txns.FirstOrDefault(t => t.TxnStatus == "SUBMITTED" && (t.TxnType == "Store Order" || t.TxnType == "Emergency Order")); //t.SiteIdto == Employee.SiteId &&

            var mainContent = this.Parent as ContentControl;
            if (mainContent != null)
            {
                if (selectedOrderTxnId.HasValue)
                {
                    // Open selected order for modification
                    mainContent.Content = new ReceiveStoreOrder(Employee, selectedOrderTxnId.Value, context);
                }
                else if (existingOrder != null)
                {
                    // Open existing order for modification
                    mainContent.Content = new ReceiveStoreOrder(Employee, existingOrder.TxnId, context);
                }
                else
                {
                    Growl.Warning(new GrowlInfo
                    {
                        Message = "Please select an open order.",
                        ShowDateTime = false,
                        WaitTime = 2
                    });
                }
            }
        }
    }
}