using System.Windows;
using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Mohammed on January 26,2025
namespace ISDP2025_Parfonov_Zerrou
{
    public partial class StoreWorkerDashBoard : Window
    {
        BestContext context = new BestContext();
        Employee employee;
        private LogoutManager logoutManager;

        public StoreWorkerDashBoard()
        {
            InitializeComponent();
        }
        public StoreWorkerDashBoard(Employee employee)
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
                currentSite = context.Sites.FirstOrDefault(s => s.SiteId == employee.SiteId);
                txtLoggedUser.Text = "Logged in as: " + employee.Username;
                txtUserRole.Text = "Your Permission is: \nStore Worker";
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
            MainContent.Content = new AdminEmployeesControl();
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
    }
}