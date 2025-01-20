using ISDP2025_Parfonov_Zerrou.Forms.ForemanUserControls;
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

        public WarehouseFormanDashBoard()
        {
            InitializeComponent();
            employee = context.Employees.Where(e => e.Username == "admin").FirstOrDefault();
        }

        public WarehouseFormanDashBoard(Employee employee)
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
                // Updated to match the exact column names from the database schema
                currentSite = context.Sites.FirstOrDefault(s => s.SiteId == employee.SiteId);


                txtLoggedUser.Text = "Logged in as: " + employee.Username;
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
    }
}
