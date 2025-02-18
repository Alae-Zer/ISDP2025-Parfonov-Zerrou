using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Forms.UserControls
{
    public partial class ViewOrders : UserControl
    {
        private BestContext context;
        private Employee Employee;
        private int? selectedTxnId = null;

        public ViewOrders()
        {
            InitializeComponent();
            context = new BestContext();
            LoadTransactions(); // Load transactions (aka orders) when control is initialized
        }

        public ViewOrders(Employee employee)
        {
            InitializeComponent();
            context = new BestContext();
            LoadTransactions(); // Load transactions (aka orders) when control is initialized
            Employee = employee;
        }

        private async void LoadTransactions()
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
                    string orderType = ((ComboBoxItem)cmbOrderType.SelectedItem).Content.ToString();
                    query = query.Where(t => t.TxnType == orderType);
                }

                if (cmbStatus.SelectedItem != null &&
                    ((ComboBoxItem)cmbStatus.SelectedItem).Content.ToString() != "ALL")
                {
                    string status = ((ComboBoxItem)cmbStatus.SelectedItem).Content.ToString();
                    query = query.Where(t => t.TxnStatus == status);
                }

                // Execute query and transform results
                var orders = await query
                    .Select(t => new
                    {
                        TxnId = t.TxnId,
                        Location = t.SiteIdtoNavigation.SiteName,
                        Status = t.TxnStatus,
                        Items = t.Txnitems.Count(),
                        Weight = t.Txnitems.Sum(ti => ti.Item.Weight * ti.Quantity),
                        DeliveryDate = t.ShipDate
                    })
                    .ToListAsync();

                dgOrders.ItemsSource = orders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transactions: {ex.Message}",
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
            var existingOrder = context.Txns.FirstOrDefault(t => t.TxnStatus == "NEW"); //t.SiteIdto == Employee.SiteId &&


            var mainContent = this.Parent as ContentControl;
            if (mainContent != null)
            {
                if (existingOrder != null)
                {
                    // Open existing order for modification
                    mainContent.Content = new CreateStoreOrder(Employee, existingOrder.TxnId);
                }
                else if (selectedTxnId.HasValue)
                {
                    // Open selected order for modification
                    mainContent.Content = new CreateStoreOrder(Employee, selectedTxnId.Value);
                }
                else
                {
                    // Create brand new order
                    mainContent.Content = new CreateStoreOrder(Employee);
                }
            }
        }

        private void btnReceive_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = dgOrders.SelectedItem;
            if (selectedOrder == null)
            {
                MessageBox.Show("Please select an order to receive.",
                              "No Order Selected",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }
            int txnID = (int)selectedOrder.GetType().GetProperty("TxnId").GetValue(selectedOrder);
            var transaction = context.Txns.FirstOrDefault(t => t.TxnId == txnID);
            transaction.TxnStatus = "DELIVERED";
            context.Txns.Update(transaction);
            context.SaveChanges();
            LoadTransactions();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = dgOrders.SelectedItem;
            if (selectedOrder == null)
            {
                MessageBox.Show("Please select an order to delete.",
                              "No Order Selected",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
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
                    selectedTxnId = selectedOrder.TxnId;

                }
                else
                    MessageBox.Show("No");
            }
        }
    }
}