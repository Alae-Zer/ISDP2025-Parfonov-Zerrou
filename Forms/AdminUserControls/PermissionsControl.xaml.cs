using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class PermissionsControl : UserControl
    {
        BestContext context;
        bool isEditMode = false;
        List<Posn> permissionsList = new();
        List<Permission> permissions = new();

        public PermissionsControl()
        {
            InitializeComponent();
            context = new BestContext();
            InitializeControls();
        }

        private void InitializeControls()
        {
            //btnAddPermission.IsEnabled = false;
            //btnRemovePermission.IsEnabled = false;
            dgvEmployees.ItemsSource = null;
            txtSearch.IsEnabled = false;
            btnClear.IsEnabled = false;
            btnEdit.IsEnabled = false;
            cmbAvailablePermissions.IsEnabled = false;
            dgvCurrentPermissions.ItemsSource = null;
            dgvCurrentPermissions.IsEnabled = false;
            LoadPermissionsToList();
        }

        private void LoadPermissionsToList()
        {
            try
            {
                permissionsList.Clear();
                var dbPermissions = context.Posns
                    .Where(p => p.Active == 1)
                    .OrderBy(p => p.PermissionLevel)
                    .ToList();
                permissionsList.AddRange(dbPermissions);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading permissions: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEmployees()
        {
            try
            {
                var employees = context.Employees
                    .Include(e => e.Position)
                    .Include(e => e.Permissions)
                    .Select(e => new
                    {
                        e.EmployeeId,
                        e.FirstName,
                        e.LastName,
                        e.Username,
                        PositionName = e.Position.PermissionLevel,
                        DefaultPermission = e.Position.PermissionLevel,
                        AdditionalPermissions = string.Join(", ",
                            e.Permissions.Select(p => p.PermissionId)),
                        IsActive = e.Active == 1 ? "Yes" : "No"
                    })
                    .ToList();

                txtSearch.IsEnabled = (employees != null && employees.Count > 0);
                dgvEmployees.ItemsSource = employees;
                dgvEmployees.ItemsSource = employees;
                btnClear.IsEnabled = true;
                btnClear.IsEnabled = !string.IsNullOrEmpty(txtSearch.Text) ||
                    (dgvEmployees.Items != null && dgvEmployees.Items.Count > 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading employees: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgvEmployees.SelectedItem == null)
            {
                MessageBox.Show("Please select an employee first", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            isEditMode = true;
            ChangeEditMode(true);
        }

        private void ChangeEditMode(bool isEditing)
        {
            dgvEmployees.IsEnabled = !isEditing;
            btnEdit.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            btnSave.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            cmbAvailablePermissions.IsEnabled = isEditing;
            txtSearch.IsEnabled = !isEditing;
            btnClear.IsEnabled = !isEditing && !string.IsNullOrWhiteSpace(txtSearch.Text);
            btnRefresh.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            btnClear.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            dgvCurrentPermissions.IsEnabled = isEditing;

            if (!isEditing)
            {
                btnAddPermission.Visibility = Visibility.Collapsed;
                btnRemovePermission.Visibility = Visibility.Collapsed;
                cmbAvailablePermissions.SelectedIndex = -1;
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            InitializeControls();
            LoadPermissionsToList();
            LoadEmployees();
            txtSearch.Text = string.Empty;
            txtSearch.Focus();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            context?.Dispose();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            dgvEmployees.ItemsSource = null;
            txtSearch.Clear();
            InitializeControls();
        }


        private void dgvEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnEdit.IsEnabled = dgvEmployees.SelectedItem != null;
            if (dgvEmployees.SelectedItem != null)
            {
                var employee = (dynamic)dgvEmployees.SelectedItem;
                int selectedId = int.Parse(employee.EmployeeId.ToString());

                var permissions = context.Employees
                    .Include(e => e.Permissions)
                    .Where(e => e.EmployeeId == selectedId)
                    .SelectMany(e => e.Permissions)
                    .Select(p => new { Permission = p.PermissionName })
                    .ToList();

                lblEmployeeId.Text = employee.EmployeeId.ToString();
                lblEmployeeName.Text = employee.FirstName + " " + employee.LastName;
                lblPosition.Text = employee.PositionName;
                lblDefaultPermission.Text = employee.DefaultPermission;
                dgvCurrentPermissions.ItemsSource = permissions;
                cmbAvailablePermissions.ItemsSource = permissionsList;
                cmbAvailablePermissions.DisplayMemberPath = "PermissionLevel";
            }
            else
            {
                ClearLabels();
            }
        }

        private void SaveChanges()
        {
            MessageBoxResult result = MessageBox.Show("You're About to Live Edition Mode\nYES - Leave Edit Mode\n" +
                "NO - Keep Editing", "Would You like to Leave",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                LoadEmployees();
                ChangeEditMode(false);

            }
            else if (result == MessageBoxResult.Yes)
            {
                ChangeEditMode(false);
                LoadEmployees();
            }
            isEditMode = false;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.ToLower();

            var filteredEmployees = context.Employees
                .Include(e => e.Position)
                .Include(e => e.Permissions)
                .Where(e => e.FirstName.ToLower().Contains(searchText) ||
                            e.LastName.ToLower().Contains(searchText) ||
                            e.Username.ToLower().Contains(searchText))
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.Username,
                    PositionName = e.Position.PermissionLevel,
                    DefaultPermission = e.Position.PermissionLevel,
                    AdditionalPermissions = string.Join(", ", e.Permissions.Select(p => p.PermissionId)),
                    IsActive = e.Active == 1 ? "Yes" : "No"
                })
                .ToList();

            dgvEmployees.ItemsSource = filteredEmployees;
            btnClear.IsEnabled = !string.IsNullOrEmpty(searchText) || filteredEmployees.Any();
        }

        private void ClearLabels()
        {
            lblEmployeeId.Text = string.Empty;
            lblEmployeeName.Text = string.Empty;
            lblPosition.Text = string.Empty;
            lblDefaultPermission.Text = string.Empty;
            //lblCurrentPermissions.Text = string.Empty;
            cmbAvailablePermissions.ItemsSource = null;
            dgvCurrentPermissions.ItemsSource = null;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }

        private void btnAddPermission_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmbAvailablePermissions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isEditMode) return;

            if (cmbAvailablePermissions.SelectedItem is Posn selectedPermission)
            {
                var defaultPermission = lblDefaultPermission.Text;
                var currentPermissions = dgvCurrentPermissions.Items
                    .Cast<dynamic>()
                    .Select(p => p.Permission.ToString())
                    .ToList();

                btnAddPermission.Visibility =
                    (selectedPermission.PermissionLevel != defaultPermission &&
                    !currentPermissions.Contains(selectedPermission.PermissionLevel))
                    ? Visibility.Visible : Visibility.Collapsed;

                btnRemovePermission.Visibility = Visibility.Collapsed;
                dgvCurrentPermissions.SelectedIndex = -1;
            }
        }

        private void dgvCurrentPermissions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isEditMode) return;

            btnRemovePermission.Visibility = dgvCurrentPermissions.SelectedItem != null ?
                Visibility.Visible : Visibility.Collapsed;

            btnAddPermission.Visibility = Visibility.Collapsed;
            cmbAvailablePermissions.SelectedIndex = -1;
        }
    }
}