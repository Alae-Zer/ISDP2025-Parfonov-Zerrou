using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using ISDP2025_Parfonov_Zerrou.Forms.FloorGuyUserControl;
using ISDP2025_Parfonov_Zerrou.Forms.ForemanUserControls;
using ISDP2025_Parfonov_Zerrou.Forms.InventoryControls;
using ISDP2025_Parfonov_Zerrou.Forms.UserControls;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace ISDP2025_Parfonov_Zerrou
{
    /// <summary>
    /// Interaction logic for AdminDashBoard.xaml
    /// </summary>
    public partial class WarehouseFormanDashBoard : Window
    {
        BestContext context = new BestContext();
        Employee employee;
        private LogoutManager logoutManager;

        public WarehouseFormanDashBoard()
        {
            InitializeComponent();
            employee = context.Employees.Where(e => e.Username == "admin").FirstOrDefault();
        }

        public WarehouseFormanDashBoard(Employee employee)
        {
            InitializeComponent();
            this.employee = employee;
            logoutManager = new LogoutManager(this, context);
            logoutManager.StartTimer();
            txtLoggedUser.Text = "Logged in as: " + employee.Username;
        }

        private void InitializeWindow()
        {
            Site currentSite;
            try
            {
                context.Sites.Load();
                // Updated to match the exact column names from the database schema
                currentSite = context.Sites.FirstOrDefault(s => s.SiteId == employee.SiteId);


                txtLoggedUser.Text = "Your Username is : " + employee.Username;
                txtUserLocation.Text = "Current Location: " + (currentSite != null ? currentSite.SiteName : "Unknown");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEditItems_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ForemanInventoryControl();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeWindow();
        }

        private void btnEmployee_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ViewEmployeesControl();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //logoutManager.Cleanup();
            context.Dispose();
            new MainWindow().Show();
        }

        private void btnLocations_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ViewLocationsControl();
        }

        private void btnReorderThresholds_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new EditItemsControl(employee);
        }

        private void btnPermissions_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new Backorders(employee);
        }

        private void btnFulfil_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new FloorGuyFulfil(employee);
        }

        private void btnOrders_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ViewOrders(employee);
        }

        private void ScrollViewer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight || e.ExtentHeight <= e.ViewportHeight)
                scrollIndicator.Visibility = Visibility.Collapsed;
            else
                scrollIndicator.Visibility = Visibility.Visible;
        }

        private void btnSuppliers_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ForemanSupplierControl(employee);
        }

        private void btnLossReturn_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new InventoryAdjustmentControl(employee, " ");
        }
    }
}
