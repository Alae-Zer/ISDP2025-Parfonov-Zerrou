using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using ISDP2025_Parfonov_Zerrou.Forms.FloorGuyUserControl;
using ISDP2025_Parfonov_Zerrou.Forms.ForemanUserControls;
using ISDP2025_Parfonov_Zerrou.Forms.StoreManagerUserControls;
using ISDP2025_Parfonov_Zerrou.Forms.UserControls;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Mohammed on January 26,2025
namespace ISDP2025_Parfonov_Zerrou
{
    public partial class AdminDashBoard : Window
    {
        BestContext context = new BestContext();
        Employee employee;
        private LogoutManager logoutManager;
        string userPermission = "Admin";

        public AdminDashBoard()
        {
            InitializeComponent();
            txtUserRole.Text = "Your permission is: " + userPermission;
            employee = context.Employees.Where(e => e.Username == "admin").FirstOrDefault();
        }

        public AdminDashBoard(Employee employee)
        {
            InitializeComponent();
            this.employee = employee;
            logoutManager = new LogoutManager(this, context);
            logoutManager.StartTimer();
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



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeWindow();
        }

        private void btnEmployees_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new AdminEmployeesControl();
        }

        private void btnLocations_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new LocationControl();
        }

        private void btnInventory_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new InventoryControl();
        }

        private void btnSuppliers_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new AdminSupplierControl(employee);
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnPermissions_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new PermissionsControl();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new SettingsControl();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //logoutManager.Cleanup();
            context.Dispose();
            new MainWindow().Show();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnItemNotes_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ForemanInventoryControl();
        }

        private void btnReorderThresholds_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new AdminThreshholdsControl(employee);
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

        private void btnFulfil_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new FloorGuyFulfil(employee);
        }

        private void btnFulfilOnline_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new StroreManagerAcceptAndFulfilOnlineOrder(employee, userPermission);
        }
    }
}