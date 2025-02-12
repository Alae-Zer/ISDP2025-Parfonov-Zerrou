using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Forms.UserControls
{
    public partial class ViewOrders : UserControl
    {
        private readonly BestContext context;

        public ViewOrders()
        {
            InitializeComponent();
            context = new BestContext();
            LoadTransactions(); // Load transactions (aka orders) when control is initialized
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
            var mainContent = this.Parent as ContentControl;
            if (mainContent != null)
            {
                mainContent.Content = new CreateStoreOrder();
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
            // have receive functionality
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
    }
}