using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using System.Windows.Controls;
using System.Windows.Controls;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Controls;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ISDP2025_Parfonov_Zerrou
{
    public partial class AdminDashBoard : Window
    {
        BestContext context = new BestContext();
        Employee employee;

        public AdminDashBoard(Employee employee)
        {
            InitializeComponent();
            this.employee = employee;
            InitializeWindow();
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
            // Load initial data if needed
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
    }
}