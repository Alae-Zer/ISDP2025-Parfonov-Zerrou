using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Serhii on February 2,2025
namespace ISDP2025_Parfonov_Zerrou.Forms
{
    public partial class CustomMessageBox : Window
    {
        //Class Level Variables Initializing
        int defaultPositionId;
        int selectedPositionId;
        BestContext context;
        Employee employee;

        public CustomMessageBox(Employee emp)
        {
            //Assigning Values, Load Employee Permissions
            InitializeComponent();
            context = new BestContext();
            employee = emp;
            LoadPermissions();
        }

        //Loads Employee Permissions
        //Sends nothing
        //Returns Nothing
        private void LoadPermissions()
        {
            try
            {
                //Get employee permissions
                var permissions = context.Employees
                    .Include(e => e.Permissions)
                    .Where(e => e.EmployeeID == employee.EmployeeID)
                    .SelectMany(e => e.Permissions)
                    .Select(p => new { PositionId = p.PermissionId, PermissionName = p.PermissionName })
                    .ToList();

                dgvPermissions.ItemsSource = permissions;

                //Get default permission
                var defaultPermission = context.Employees
                    .Include(e => e.Position)
                    .Where(e => e.EmployeeID == employee.EmployeeID)
                    .Select(e => new { PositionId = e.PositionId, PermissionName = e.Position.PermissionLevel })
                    .FirstOrDefault();

                if (defaultPermission != null)
                {
                    lblDefaultPermission.Text = defaultPermission.PermissionName;
                    defaultPositionId = defaultPermission.PositionId;
                    selectedPositionId = defaultPositionId;
                    btnLogin.Content = $"Login as {lblDefaultPermission.Text}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading permissions: " + ex.Message);
            }
        }

        private void dgvPermissions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dgvPermissions.SelectedItem != null)
                {
                    //When an item is selected in DataGrid
                    var selected = (dynamic)dgvPermissions.SelectedItem;
                    selectedPositionId = selected.PositionId;
                    btnLogin.Content = $"Login as {selected.PermissionName}";
                    btnCancel.Content = $"Login as {lblDefaultPermission.Text}";
                }
                else
                {
                    //When no item is selected (default state)
                    selectedPositionId = defaultPositionId;
                    btnLogin.Content = $"Login as {lblDefaultPermission.Text}";
                    btnCancel.Content = $"Login as {lblDefaultPermission.Text}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating button text: {ex.Message}");
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            //Login
            employee.PositionId = selectedPositionId;
            OpenNextForm();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            //Login as default
            employee.PositionId = defaultPositionId;
            OpenNextForm();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Close connection on closing
            context.Dispose();
        }

        //Opens next form
        //
        //
        private void OpenNextForm()
        {
            try
            {
                //Initialize new object and assign to null
                Window nextForm = null;

                //Check employees position ID and assign to the next form
                switch (employee.PositionId)
                {
                    case 9999:
                        nextForm = new AdminDashBoard(employee);
                        break;
                    case 1:
                        nextForm = new RegionalManagerDashBoard(employee);
                        break;
                    case 2:
                        nextForm = new FinanceManagerDashBoard(employee);
                        break;
                    case 3:
                        nextForm = new WarehouseFormanDashBoard(employee);
                        break;
                    case 4:
                        nextForm = new StoreManagerDashBoard(employee);
                        break;
                    case 5:
                        nextForm = new WarehouseWorkerDashBoard(employee);
                        break;
                    case 6:
                        MessageBox.Show("Delivery Is Online");
                        this.Close();
                        return;
                    case 10000:
                        MessageBox.Show("Shopping Online");
                        this.Close();
                        return;
                    default:
                        MessageBox.Show("Unknown position type");
                        this.Close();
                        return;
                }

                //Jump to the next form
                if (nextForm != null)
                {
                    nextForm.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                //If something is wrong- form closes
                MessageBox.Show("Error opening form: " + ex.Message);
                this.Close();
            }
        }
    }
}