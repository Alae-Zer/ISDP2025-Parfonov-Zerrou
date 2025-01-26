using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Serhii on January 26,2025
namespace ISDP2025_Parfonov_Zerrou.Forms
{
    public partial class CustomMessageBox : Window
    {
        int defaultPositionId;
        int selectedPositionId;
        bool isCancelled;
        BestContext context;
        Employee employee;

        public CustomMessageBox(Employee emp)
        {
            InitializeComponent();
            context = new BestContext();
            employee = emp;
            LoadPermissions();
        }

        //Loads Permissions To DGV
        //Sends Nothing
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

                lblDefaultPermission.Text = defaultPermission.PermissionName;
                defaultPositionId = defaultPermission.PositionId;
                selectedPositionId = defaultPositionId;

                btnLogin.Content = $"Login as {lblDefaultPermission.Text}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading permissions: " + ex.Message);
            }
        }

        private void dgvPermissions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvPermissions.SelectedItem != null)
            {
                dynamic selected = dgvPermissions.SelectedItem;
                selectedPositionId = selected.PositionId;
                btnLogin.Content = $"Login as {selected.PermissionName}";
            }
            else
            {
                selectedPositionId = defaultPositionId;
                btnLogin.Content = $"Login as {lblDefaultPermission.Text}";
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            isCancelled = false;
            employee.PositionId = selectedPositionId;
            OpenNextForm();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            isCancelled = false;
            employee.PositionId = defaultPositionId;
            OpenNextForm();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!this.DialogResult.HasValue)
            {
                isCancelled = false;
                employee.PositionId = defaultPositionId;
                context.Dispose();
                OpenNextForm();
            }
        }

        private void OpenNextForm()
        {
            try
            {
                Window nextForm;

                switch (employee.PositionId)
                {
                    case 9999:
                        nextForm = new AdminDashBoard(employee);
                        break;
                    case 1:
                        nextForm = new RegionalManagerDashboard(employee);
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
                        return;
                    case 10000:
                        MessageBox.Show("Shopping Online");
                        return;
                    default:
                        MessageBox.Show("Unknown position type");
                        return;
                }

                nextForm.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening form: " + ex.Message);
            }
        }
    }
}