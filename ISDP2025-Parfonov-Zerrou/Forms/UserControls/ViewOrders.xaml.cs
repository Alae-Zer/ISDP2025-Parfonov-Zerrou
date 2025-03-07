using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
            loadStores();
        }

        public ViewOrders(Employee employee)
        {
            InitializeComponent();
            Employee = employee;
            context = new BestContext();
            ConfigureUIForUserRole();
            LoadTransactions(); // Load transactions (aka orders) when control is initialized
            loadStores();
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

                if (cmbOrderType.SelectedItem != null)
                {
                    string selectedContent = ((ComboBoxItem)cmbOrderType.SelectedItem).Content.ToString();
                    if (selectedContent != "All")
                    {
                        string orderType = selectedContent;
                        query = query.Where(t => t.TxnType == orderType);
                    }
                }

                if (cmbStores.SelectedItem != null)
                {
                    int selectedContent = (int)cmbStores.SelectedValue;
                    if (selectedContent != -1)
                    {
                        query = query.Where(t => t.SiteIdto == selectedContent);
                    }
                }
                else if (permissionLevel == "Store Manager")
                {
                    query = query.Where(t => t.SiteIdto == Employee.SiteId);
                }

                if (cmbStatus.SelectedItem != null)
                {
                    string selectedContent = ((ComboBoxItem)cmbStatus.SelectedItem).Content.ToString();
                    if (selectedContent != "All")
                    {
                        string status = selectedContent;
                        query = query.Where(t => t.TxnStatus == status);
                    }
                    else
                    {
                        query = query.Where(t => t.TxnStatus != "CLOSED" && t.TxnStatus != "CANCELLED");
                    }
                }
                else
                {
                    // If no status is selected, apply the default filter
                    query = query.Where(t => t.TxnStatus != "CLOSED" && t.TxnStatus != "CANCELLED");
                }

                if (permissionLevel == "Store Manager")
                {
                    query = query.Where(t => t.TxnType == "Store Order" || t.TxnType == "Emergency Order");
                }

                // First, get the data from the database
                var rawResults = query.ToList();

                // Then, apply our custom transformations in memory
                var results = rawResults.Select(t => new OrderViewModel
                {
                    TxnId = t.TxnId,
                    Location = t.SiteIdtoNavigation.SiteName,
                    Status = t.TxnStatus,
                    Items = t.Txnitems.Count(),
                    Weight = t.Txnitems.Sum(ti => ti.Item.Weight * ti.Quantity) > 0 ?
                        t.Txnitems.Sum(ti => ti.Item.Weight * ti.Quantity).ToString("#.## KG") : "0 KG",
                    DeliveryDate = t.ShipDate,
                    OrderType = t.TxnType,
                    StatusGroup = GetStatusGroupName(t.TxnStatus)
                })
                .OrderBy(t => GetStatusGroupOrder(t.Status))
                .ThenByDescending(t => t.DeliveryDate)
                .ToList();

                // Set up grouping
                var view = CollectionViewSource.GetDefaultView(results);
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("StatusGroup"));

                dgOrders.ItemsSource = view;

                // Rest of the method remains the same
                if (permissionLevel != "Store Manager")
                {
                    Alert.Visibility = checkOrder() == true ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    Alert.Visibility = Visibility.Collapsed;
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

        public class OrderViewModel
        {
            public int TxnId { get; set; }
            public string Location { get; set; }
            public string Status { get; set; }
            public int Items { get; set; }
            public string Weight { get; set; }
            public DateTime DeliveryDate { get; set; }
            public string OrderType { get; set; }
            public string StatusGroup { get; set; }
        }

        // Helper method to get readable group names
        private string GetStatusGroupName(string status)
        {
            switch (status)
            {
                case "NEW":
                case "SUBMITTED":
                    return "New Orders";
                case "RECEIVED":
                    return "Received Orders";
                case "ASSEMBLING":
                case "ASSEMBLED":
                    return "Assembly";
                case "IN TRANSIT":
                    return "In Transit";
                case "DELIVERED":
                    return "Delivered";
                case "COMPLETE":
                    return "Completed";
                case "REJECTED":
                case "CANCELLED":
                    return "Rejected/Cancelled";
                default:
                    return "Other";
            }
        }

        // Helper method for ordering the groups
        private int GetStatusGroupOrder(string status)
        {
            switch (status)
            {
                case "NEW":
                case "SUBMITTED":
                    return 1;
                case "RECEIVED":
                    return 2;
                case "ASSEMBLING":
                case "ASSEMBLED":
                    return 3;
                case "IN TRANSIT":
                    return 4;
                case "DELIVERED":
                    return 5;
                case "COMPLETE":
                    return 6;
                case "REJECTED":
                case "CANCELLED":
                    return 7;
                default:
                    return 8;
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
                    WaitTime = 2,

                });
                return;
            }
            int txnID = (int)selectedOrder.GetType().GetProperty("TxnId").GetValue(selectedOrder);
            var transaction = context.Txns.FirstOrDefault(t => t.TxnId == txnID);
            if (transaction.TxnStatus != "IN TRANSIT") return;
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
            var query = context.Txns
                    .Where(t => t.TxnStatus == "NEW")
                    .Include(t => t.SiteIdtoNavigation)
                    .Include(t => t.Txnitems)
                        .ThenInclude(ti => ti.Item)

                    .AsQueryable();
            if (permissionLevel == "Store Manager")
            {
                query = query.Where(t => t.SiteIdto == Employee.SiteId);
            }
            var existingOrder = query.FirstOrDefault(); // &&


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

        private void loadStores()
        {
            try
            {
                var query = context.Sites
                .Where(s => s.SiteId != 1 && s.SiteId != 3 && s.SiteId != 10000 && s.SiteId != 9999)
                .OrderBy(s => s.SiteName);

                //// For store managers, filter to only show their store
                //if (Employee != null)
                //{
                //    // If employee.Position is null, load it from the database
                //    if (Employee.Position == null)
                //    {
                //        // Reload the employee with the Position included
                //        Employee = context.Employees
                //            .Include(e => e.Position)
                //            .FirstOrDefault(e => e.EmployeeID == Employee.EmployeeID);
                //    }
                //    if (Employee.Position.PermissionLevel == "Store Manager")
                //    {
                //        query = (IOrderedQueryable<Site>)query.Where(s => s.SiteId == Employee.SiteId);
                //    }
                //}
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
                if (Employee.Position.PermissionLevel == "Store Manager")
                {
                    cmbStores.SelectedValue = Employee.SiteId;
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading initial data: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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