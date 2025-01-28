using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace ISDP2025_Parfonov_Zerrou
{
    public partial class WarehouseWorkerDashBoard : Window
    {
        BestContext context = new BestContext();
        Employee employee;

        public WarehouseWorkerDashBoard()
        {
            InitializeComponent();
        }

        public WarehouseWorkerDashBoard(Employee employee)
        {
            InitializeComponent();
            this.employee = employee;
        }

        private void InitializeWindow()
        {
            Site currentSite;
            try
            {
                context.Sites.Load();
                currentSite = context.Sites.FirstOrDefault(s => s.SiteId == employee.SiteId);
                txtLoggedUser.Text = "Logged in as: " + employee.Username;
                txtUserRole.Text = "Your Permission is: \nWarehouse Worker";
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
    }
}