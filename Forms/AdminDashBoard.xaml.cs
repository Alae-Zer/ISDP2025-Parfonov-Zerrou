using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
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

        public AdminDashBoard()
        {
            InitializeComponent();
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
            MainContent.Content = new EmployeesControl();
        }

        private void btnLocations_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnInventory_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new InventoryControl();
        }

        private void btnSuppliers_Click(object sender, RoutedEventArgs e)
        {

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
            logoutManager.Cleanup();
            context.Dispose();
            new MainWindow().Show();
        }
    }
}