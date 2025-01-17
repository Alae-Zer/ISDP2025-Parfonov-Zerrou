using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou
{
    public partial class AdminDashBoard : Window
    {
        BestContext context = new BestContext();
        Employee employee;

        public AdminDashBoard() { }

        public AdminDashBoard(Employee employee)
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

        private void LoadDataToGrid<T>(DbSet<T> dbSet, DataGrid dataGrid) where T : class
        {
            try
            {
                if (dbSet == null)
                {
                    throw new ArgumentNullException(nameof(dbSet), "DbSet cannot be null.");
                }
                if (dataGrid == null)
                {
                    throw new ArgumentNullException(nameof(dataGrid), "DataGrid cannot be null.");
                }
                dbSet.Load();
                dataGrid.ItemsSource = dbSet.Local.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            LoadDataToGrid(context.Sites, dgvInformation);
        }

        private void btnInventory_Click(object sender, RoutedEventArgs e)
        {
            LoadDataToGrid(context.Inventories, dgvInformation);
        }

        private void btnSuppliers_Click(object sender, RoutedEventArgs e)
        {
            LoadDataToGrid(context.Suppliers, dgvInformation);
        }

        private void btnDashBoard_Click(object sender, RoutedEventArgs e)
        {
            // Handle dashboard view
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDataToGrid(context.Employees, dgvInformation);
        }
    }
}