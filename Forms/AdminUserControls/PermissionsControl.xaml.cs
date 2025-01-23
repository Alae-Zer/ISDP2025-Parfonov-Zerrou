using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class PermissionsControl : UserControl
    {
        private readonly BestContext _context;
        private bool isEditMode = false;
        private readonly List<Posn> permissionsList = new();

        public PermissionsControl()
        {
            InitializeComponent();
            _context = new BestContext();
            InitializeControls();
        }

        private void InitializeControls()
        {
            btnAddPermission.IsEnabled = false;
            btnRemovePermission.IsEnabled = false;
            dgvEmployees.ItemsSource = null;
            txtSearch.IsEnabled = false;
            btnClear.IsEnabled = false;
            btnEdit.IsEnabled = false;
            cmbAvailablePermissions.IsEnabled = false;
            LoadPermissionsToList(); // Load permissions on initialization
        }

        private void LoadPermissionsToList()
        {
            try
            {
                permissionsList.Clear();
                var dbPermissions = _context.Posns
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
                var employees = _context.Employees
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
            _context?.Dispose();
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
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnClear.IsEnabled = !string.IsNullOrEmpty(txtSearch.Text) ||
                    (dgvEmployees.Items != null && dgvEmployees.Items.Count > 0);
        }
    }
}