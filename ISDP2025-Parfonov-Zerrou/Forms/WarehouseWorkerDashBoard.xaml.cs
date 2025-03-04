using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using ISDP2025_Parfonov_Zerrou.Forms.FloorGuyUserControl;
using ISDP2025_Parfonov_Zerrou.Forms.UserControls;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace ISDP2025_Parfonov_Zerrou
{
    public partial class WarehouseWorkerDashBoard : Window
    {
        BestContext context = new BestContext();
        Employee employee;
        private LogoutManager logoutManager;
        public WarehouseWorkerDashBoard()
        {
            InitializeComponent();
        }

        public WarehouseWorkerDashBoard(Employee employee)
        {
            InitializeComponent();
            this.employee = employee;
            logoutManager = new LogoutManager(this, context);
            logoutManager.StartTimer();
            txtLoggedUser.Text = "Your Username is : " + employee.Username;
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

        private void btnPermissions_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new FloorGuyFulfil(employee);
        }
    }
}
