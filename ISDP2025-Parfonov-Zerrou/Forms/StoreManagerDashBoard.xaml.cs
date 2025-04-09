using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using ISDP2025_Parfonov_Zerrou.Forms.ForemanUserControls;
using ISDP2025_Parfonov_Zerrou.Forms.InventoryControls;
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
    public partial class StoreManagerDashBoard : Window
    {
        BestContext context = new BestContext();
        Employee employee;
        private LogoutManager logoutManager;
        public StoreManagerDashBoard()
        {
            InitializeComponent();
        }

        public StoreManagerDashBoard(Employee employee)
        {
            InitializeComponent();
            this.employee = employee;
            logoutManager = new LogoutManager(this, context);
            logoutManager.StartTimer();
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            Site currentSite;
            try
            {
                context.Sites.Load();
                currentSite = context.Sites.FirstOrDefault(s => s.SiteId == employee.SiteId);
                txtLoggedUser.Text = "Logged in as: " + employee.Username;
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
            logoutManager.Cleanup();
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

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new StoreManagerReceiveOrder(employee, " ");
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

        private void btnOnlineOrders_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new StroreManagerAcceptAndFulfilOnlineOrder(employee, " ");
        }

        private void BTNReports_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ReportsControl(employee, context);
        }

        private void btnLossReturn_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new InventoryAdjustmentControl(employee, " ");
        }
    }
}