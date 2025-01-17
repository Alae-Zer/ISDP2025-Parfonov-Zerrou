using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou
{
    /// <summary>
    /// Interaction logic for AdminDashBoard.xaml
    /// </summary>
    public partial class AdminDashBoard : Window
    {
        BestContext context = new BestContext();

        public AdminDashBoard()
        {
            InitializeComponent();
        }

        private void LoadDataToGrid<T>(DbSet<T> dbSet, DataGrid dataGrid) where T : class
        {
            try
            {
                //Ensure the DbSet and DataGrid are not null
                if (dbSet == null)
                {
                    throw new ArgumentNullException(nameof(dbSet), "DbSet cannot be null.");
                }
                if (dataGrid == null)
                {
                    throw new ArgumentNullException(nameof(dataGrid), "DataGrid cannot be null.");
                }

                //Load the data into the context
                dbSet.Load();

                //Bind the loaded data to the DataGrid
                dataGrid.ItemsSource = dbSet.Local.ToList();
            }
            catch (Exception ex)
            {
                //Display a user-friendly error message
                MessageBox.Show($"An error occurred while loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

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

        }
    }
}
