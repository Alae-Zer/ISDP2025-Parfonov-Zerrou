using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Serhii on January 26,2025
namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class PermissionsControl : UserControl
    {
        //Declare Class-Level Variables, Lists, Context
        BestContext context;
        bool isEditMode = false;
        List<Posn> permissionsList = new();
        List<Permission> permissions = new();

        public PermissionsControl()
        {
            InitializeComponent();
            context = new BestContext();
            ActivateControls();
        }

        //Activates Default Control Set
        //Sends Nothing
        //Returns Nothing
        private void ActivateControls()
        {
            txtSearch.Clear();
            dgvEmployees.ItemsSource = null;
            txtSearch.IsEnabled = false;
            btnClear.IsEnabled = false;
            btnEdit.IsEnabled = false;
            cmbAvailablePermissions.IsEnabled = false;
            dgvCurrentPermissions.ItemsSource = null;
            dgvCurrentPermissions.IsEnabled = false;
            LoadPermissionsToList();
        }

        //Populates List With Permissions
        //Sends Nothing
        //Returns Nothing
        private void LoadPermissionsToList()
        {
            //Exception Handling
            try
            {
                //Clear List Prior To LOading
                permissionsList.Clear();

                //Query In Positions (Position === Permission)
                var dbPermissions = context.Posns
                    .Where(p => p.Active == 1)
                    .ToList();

                //LOOP and ADD
                foreach (var dbPermission in dbPermissions)
                {
                    permissionsList.Add(dbPermission);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to Load Permissions because: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Loads Employees To DGV
        //Sends Nothing
        //Returns Nothinfg
        private void LoadEmployees()
        {
            //Exception Handling
            try
            {
                //QUERY
                var employees = context.Employees
                    .Include(e => e.Position)
                    .Include(e => e.Permissions)
                    .Select(e => new
                    {
                        e.EmployeeID,
                        e.FirstName,
                        e.LastName,
                        e.Username,
                        PositionName = e.Position.PermissionLevel,
                        DefaultPermission = e.Position.PermissionLevel,
                        IsActive = e.Active == 1 ? "Yes" : "No"
                    })
                    .ToList();

                //Check If Employees Found and Convert response to BOOl
                bool hasEmployees = employees.Any();
                txtSearch.IsEnabled = hasEmployees;
                //Check If TextBox Contains Anything
                btnClear.IsEnabled = hasEmployees || !string.IsNullOrEmpty(txtSearch.Text);
                //Bind
                dgvEmployees.ItemsSource = employees;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading employees: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Change Button State Based On MODE
        //Sends BOOL
        //Returns Nothing
        private void ChangeEditMode(bool isEditing)
        {
            //Enable/Disable && Collapse/Show controls
            dgvEmployees.IsEnabled = !isEditing;
            btnEdit.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            btnSave.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            cmbAvailablePermissions.IsEnabled = isEditing;
            txtSearch.IsEnabled = !isEditing;
            btnClear.IsEnabled = !isEditing && !string.IsNullOrWhiteSpace(txtSearch.Text);
            btnRefresh.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            btnClear.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            dgvCurrentPermissions.IsEnabled = isEditing;

            //Single Conditions
            if (!isEditing)
            {
                btnAddPermission.Visibility = Visibility.Collapsed;
                btnRemovePermission.Visibility = Visibility.Collapsed;
                cmbAvailablePermissions.SelectedIndex = -1;
            }
        }

        //Displays Details For Selected Employee, Enable Editing and Populates DGV and CBO
        //Sends Nothing
        //Returns Notjing
        private void DisplayEmployeeDetails()
        {
            //Exception Handling
            try
            {
                //Display Control and View Based On DGV State
                bool isSelected = dgvEmployees.SelectedItem != null;
                btnEdit.IsEnabled = isSelected;

                if (isSelected)
                {
                    //Dynamic Object
                    var employee = (dynamic)dgvEmployees.SelectedItem;
                    int selectedId = employee.EmployeeID;

                    var permissions = context.Employees
                        .Include(e => e.Permissions)
                        .Where(e => e.EmployeeID == selectedId)
                        .SelectMany(e => e.Permissions)
                        .Select(p => new { Permission = p.PermissionName })
                        .ToList();

                    lblEmployeeId.Text = selectedId.ToString();
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying employee details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ClearLabels();
            }
        }

        //Removes Permission From The List
        //Sends Nothing
        //Returns Nothing
        private void RemovePermission()
        {
            //Exception Handling
            try
            {
                //If ID and Selection Exists - Continue
                if (dgvCurrentPermissions.SelectedItem != null &&
                    !string.IsNullOrEmpty(lblEmployeeId.Text))
                {
                    //QUERY
                    var selectedItem = dgvCurrentPermissions.SelectedItem;
                    var propertyInfo = selectedItem.GetType().GetProperty("Permission");
                    string permissionName = propertyInfo.GetValue(selectedItem).ToString();

                    //Confirm Removal Dialog
                    MessageBoxResult result = MessageBox.Show(
                        $"Remove permission '{permissionName}' from {lblEmployeeName.Text}?",
                        "Confirm Remove Permission",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    //If Result Positive - Procceed with Removal 
                    if (result == MessageBoxResult.Yes)
                    {
                        //Check If ID exists and Format Correct
                        if (int.TryParse(lblEmployeeId.Text, out int employeeID))
                        {
                            //TWO QUERIES
                            var employee = context.Employees
                                .Include(e => e.Permissions)
                                .FirstOrDefault(e => e.EmployeeID == employeeID);

                            var permission = employee.Permissions
                                .FirstOrDefault(p => p.PermissionName == permissionName);

                            //Remove Permission, Save Cahanges, Unselect DGV, Collapse Button, Update Employee Details
                            employee.Permissions.Remove(permission);
                            context.SaveChanges();
                            DisplayEmployeeDetails();
                            dgvCurrentPermissions.SelectedIndex = -1;
                            btnRemovePermission.Visibility = Visibility.Collapsed;
                        }
                        //Unable To Parse ID from The Label
                        else
                        {
                            MessageBox.Show("Invalid employee ID format.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                        }
                    }
                }
            }
            //DataBase Issues
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing permission: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        //Switches Modes displaying Correct GUI
        //Sends Nothign
        //Returns Nothing
        private void SwitchModes()
        {
            MessageBoxResult result = MessageBox.Show("You're About to Live Edition Mode\nYES - Leave Edit Mode\n" +
                "NO - Keep Editing", "Would You like to Leave",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            //Change Mode
            isEditMode = false;

            //This Functionality Was Inherited From Previous Forms, So I decided To Keep It As Is Despite Useless Prompt
            if (result == MessageBoxResult.No || result == MessageBoxResult.Yes)
            {
                LoadEmployees();
                ChangeEditMode(isEditMode);
                ClearLabels();
            }
        }

        //Unselects ComboBox and Dispalys RemoVe permission Button
        //Sends Nothing
        //Returns Nothing
        private void DeleteAddPermissionsState()
        {
            if (isEditMode)
            {
                if (dgvCurrentPermissions.SelectedItem != null)
                {
                    btnRemovePermission.Visibility = Visibility.Visible;
                }
                else
                {
                    btnRemovePermission.Visibility = Visibility.Collapsed;
                }

                btnAddPermission.Visibility = Visibility.Collapsed;
                cmbAvailablePermissions.SelectedIndex = -1;
            }
        }

        //Adds New Permission To The Database And Binds with DGV
        //Sends Nothing
        //Returns Nothing
        private void AddPermission()
        {
            //Exception Handling
            try
            {
                //Check If We Have Data
                if (cmbAvailablePermissions.SelectedItem != null && int.TryParse(lblEmployeeId.Text, out int employeeId))
                {
                    string permissionName = cmbAvailablePermissions.Text;
                    //Confirm Adding
                    MessageBoxResult result = MessageBox.Show(
                        $"Add permission '{permissionName}' to {lblEmployeeName.Text}?",
                        "Confirm Adding",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        var employee = context.Employees.Find(employeeId);
                        var permission = context.Permissions
                            .FirstOrDefault(p => p.PermissionName == permissionName);

                        if (employee == null || permission == null)
                        {
                            MessageBox.Show("Employee or permission not found", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        //Add And Save
                        employee.Permissions.Add(permission);
                        context.SaveChanges();

                        //Query Again And Update DataGrid With All Permissions
                        var permissions = context.Employees
                            .Include(e => e.Permissions)
                            .Where(e => e.EmployeeID == employeeId)
                            .SelectMany(e => e.Permissions)
                            .Select(p => new { Permission = p.PermissionName })
                            .ToList();
                        dgvCurrentPermissions.ItemsSource = permissions;
                        cmbAvailablePermissions.SelectedIndex = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding permission: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Unselects DGV, Enable ADD Button, Disable Remove Button
        //Sends Nothing
        //Returns Nothing
        private void HandleAvailablePermissions()
        {
            if (isEditMode)
            {
                if (cmbAvailablePermissions.SelectedItem is Posn selectedPermission)
                {
                    var defaultPermission = lblDefaultPermission.Text;
                    List<string> currentPermissions = new();
                    foreach (dynamic item in dgvCurrentPermissions.Items)
                    {
                        currentPermissions.Add(item.Permission.ToString());
                    }

                    if (selectedPermission.PermissionLevel != defaultPermission &&
                        !currentPermissions.Contains(selectedPermission.PermissionLevel))
                    {
                        btnAddPermission.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        btnAddPermission.Visibility = Visibility.Collapsed;
                    }

                    btnRemovePermission.Visibility = Visibility.Collapsed;
                    dgvCurrentPermissions.SelectedIndex = -1;
                }
            }
        }

        //Filter Results Based On TextBox State
        //Sends Nothing
        //Returns Nothing
        private void FilterDGV()
        {
            string searchText = txtSearch.Text.ToLower();

            //QUERY
            var filteredEmployees = context.Employees
                .Include(e => e.Position)
                .Include(e => e.Permissions)
                .Where(e => e.FirstName.ToLower().Contains(searchText) ||
                            e.LastName.ToLower().Contains(searchText) ||
                            e.Username.ToLower().Contains(searchText))
                .Select(e => new
                {
                    e.EmployeeID,
                    e.FirstName,
                    e.LastName,
                    e.Username,
                    PositionName = e.Position.PermissionLevel,
                    DefaultPermission = e.Position.PermissionLevel,
                    IsActive = e.Active == 1 ? "Yes" : "No"
                })
                .ToList();

            //Bind Sources
            dgvEmployees.ItemsSource = filteredEmployees;

            if (filteredEmployees.Count > 0 || searchText != "")
            {
                btnClear.IsEnabled = true;
            }
        }

        //Clears Labels, DGV and Combo
        //Sends Nothing
        //FReturns Nothing
        private void ClearLabels()
        {
            lblEmployeeId.Text = string.Empty;
            lblEmployeeName.Text = string.Empty;
            lblPosition.Text = string.Empty;
            lblDefaultPermission.Text = string.Empty;
            cmbAvailablePermissions.ItemsSource = null;
            dgvCurrentPermissions.ItemsSource = null;
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Event Handlers

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SwitchModes();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            //Edit Mode Activation
            if (dgvEmployees.SelectedItem == null)
            {
                MessageBox.Show("Please select an employee first", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                isEditMode = true;
                ChangeEditMode(true);
            }
        }

        private void btnAddPermission_Click(object sender, RoutedEventArgs e)
        {
            AddPermission();
        }

        private void cmbAvailablePermissions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HandleAvailablePermissions();
        }

        private void dgvCurrentPermissions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteAddPermissionsState();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterDGV();
        }

        private void btnRemovePermission_Click(object sender, RoutedEventArgs e)
        {
            RemovePermission();
        }

        private void dgvEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DisplayEmployeeDetails();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            //Refresh Controls and DataSourse
            ActivateControls();
            LoadPermissionsToList();
            LoadEmployees();
            //txtSearch.Text = string.Empty;
            txtSearch.Focus();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            context?.Dispose();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            //Clear Everything
            dgvEmployees.ItemsSource = null;
            //txtSearch.Clear();
            ActivateControls();
        }
    }
}