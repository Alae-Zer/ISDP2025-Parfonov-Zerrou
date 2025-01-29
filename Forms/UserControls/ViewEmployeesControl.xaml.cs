using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    /// <summary>
    /// Interaction logic for EmployeesControl.xaml
    /// </summary>
    public partial class ViewEmployeesControl : UserControl
    {
        MainWindow mainWindow = new MainWindow();
        private readonly BestContext context;
        List<Employee> AllEmployees;
        Employee employee;

        public ViewEmployeesControl()
        {
            InitializeComponent();
            context = new BestContext();
            disableAll(false);

        }
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInitialData();
            disableAll(true);
        }

        private void disableAll(bool state)
        {
            if (state)
            {
                txtSearch.IsEnabled = true;
                cmbSearchCategory.IsEnabled = true;
            }
            else
            {
                txtSearch.IsEnabled = false;
                cmbSearchCategory.IsEnabled = false;
            }
        }
        private void LoadInitialData()
        {
            LoadEmployees();
            cmbSearchCategory.SelectedIndex = 1;
        }


        private void LoadEmployees()
        {
            AllEmployees = context.Employees.Include(e => e.Position).Include(e => e.Site).ToList();

            var employees = AllEmployees.Select(e => new
            {
                e.EmployeeID,
                e.FirstName,
                e.LastName,
                e.Email,
                e.Username,
                e.PositionId,
                e.SiteId,
                e.Active,
                e.Password,
                Position = e.Position,
                Site = e.Site,
            }).ToList();

            dgEmployees.ItemsSource = employees;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterEmployees();
        }

        private void CmbSearchCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterEmployees();
        }

        private void FilterEmployees()
        {

            var searchText = txtSearch.Text.ToLower();
            var searchCategory = cmbSearchCategory.SelectedItem as ComboBoxItem;

            var filteredEmployees = AllEmployees.Where(emp =>
            {
                switch (searchCategory.Content.ToString())
                {
                    case "Employee ID":
                        return emp.EmployeeID.ToString().Contains(searchText);
                    case "First Name":
                        return emp.FirstName.ToLower().Contains(searchText);
                    case "Last Name":
                        return emp.LastName.ToLower().Contains(searchText);
                    case "Email":
                        return emp.Email.ToLower().Contains(searchText);
                    case "Position":
                        return emp.Position.PermissionLevel.ToLower().Contains(searchText);
                    case "Location":
                        return emp.Site.SiteName.ToLower().Contains(searchText);
                    default:
                        return false;
                }
            });

            dgEmployees.ItemsSource = filteredEmployees.Select(e => new
            {
                e.EmployeeID,
                e.FirstName,
                e.LastName,
                e.Email,
                e.Username,
                e.PositionId,
                e.SiteId,
                e.Active,
                Position = e.Position,
                Site = e.Site,
            });
        }
    }
}
