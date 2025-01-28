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

        public StoreManagerDashBoard()
        {
            InitializeComponent();
        }

        public StoreManagerDashBoard(Employee employee)
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
                txtUserRole.Text = "Your Permission is: \nStore Manager";
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