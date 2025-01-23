using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    /// <summary>
    /// Interaction logic for EmployeesControl.xaml
    /// </summary>
    public partial class EmployeesControl : UserControl
    {
        private readonly BestContext context;
        List<Employee> AllEmployees;
        Employee employee;

        public EmployeesControl()
        {
            InitializeComponent();
            context = new BestContext();
            disableAll(false);
        }

        private void disableAll(bool state)
        {
            if (state)
            {
                employeeFormGroup.IsEnabled = true;
                btnAdd.IsEnabled = true;
                btnClear.IsEnabled = true;
                btnUpdate.IsEnabled = true;
            }
            else
            {
                employeeFormGroup.IsEnabled = false;
                btnAdd.IsEnabled = false;
                btnClear.IsEnabled = false;
                btnUpdate.IsEnabled = false;
            }
        }
        private void LoadInitialData()
        {
            LoadPositions();
            LoadLocations();
            LoadEmployees();
        }

        private void LoadPositions()
        {
            var positions = context.Posns.ToList();
            cmbPosition.ItemsSource = positions;
            cmbPosition.DisplayMemberPath = "PermissionLevel";
            cmbPosition.SelectedValuePath = "PositionId";
        }

        private void LoadLocations()
        {
            var locations = context.Sites.ToList();
            cmbLocation.ItemsSource = locations;
            cmbLocation.DisplayMemberPath = "SiteName";
            cmbLocation.SelectedValuePath = "SiteId";
        }

        private void LoadEmployees()
        {

            AllEmployees = context.Employees
        .Include(e => e.Position)
        .Include(e => e.Site)
        .ToList();

            var employees = context.Employees
                .Include(e => e.Position)
                .Include(e => e.Site)
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FirstName,
                    e.LastName,
                    e.Email,
                    e.Username,
                    e.PositionId,
                    e.SiteId,
                    e.Active,
                    Position = e.Position,
                    Site = e.Site,
                    IsActive = e.Active == 1 ? "Yes" : "No"
                })
                .ToList();
            dgEmployees.ItemsSource = employees;
        }

        private void ClearForm()
        {
            txtFirstName.Clear();
            txtLastName.Clear();
            txtEmployeeID.Clear();
            txtEmail.Clear();
            txtUsername.Clear();
            txtPassword.Clear();
            txtEmail.Clear();
            cmbPosition.SelectedIndex = -1;
            cmbLocation.SelectedIndex = -1;
            chkActive.IsChecked = false;
            dgEmployees.SelectedItem = null;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CheckAllInputs())
                {
                    if (txtEmployeeID.Text == "")
                    {
                        var newEmployee = new Employee
                        {
                            FirstName = txtFirstName.Text,
                            LastName = txtLastName.Text,
                            Email = txtEmail.Text,
                            Username = txtUsername.Text,
                            Password = pwdPassword.Visibility == 0 ? pwdPassword.Password : txtPassword.Text,
                            PositionId = (int)cmbPosition.SelectedValue,
                            SiteId = (int)cmbLocation.SelectedValue,
                            Active = chkActive.IsChecked == true ? (sbyte)1 : (sbyte)0,
                            Locked = 0
                        };

                        context.Employees.Add(newEmployee);
                        context.SaveChanges();
                        LoadEmployees();
                        ClearForm();
                        MessageBox.Show("Employee added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Employee already in exist in the Database! \n you can only modify this user", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Please Fill all the fields.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding employee: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool CheckAllInputs()
        {
            if (txtFirstName.Text == "" || txtLastName.Text == "" || cmbLocation.SelectedValue.ToString() == null || cmbPosition.SelectedValue.ToString() == null)
            {
                return false;
            }
            return true;
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected employee from our stored list
            var selectedEmployee = new Employee();

            if (txtEmployeeID.Text == "")
            {
                MessageBox.Show("Please select an employee to update.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Update employee info
                selectedEmployee.FirstName = txtFirstName.Text;
                selectedEmployee.LastName = txtLastName.Text;
                selectedEmployee.Email = txtEmail.Text;
                selectedEmployee.Username = txtUsername.Text;
                selectedEmployee.Password = pwdPassword.Visibility == Visibility.Visible ?
                    pwdPassword.Password : txtPassword.Text;
                selectedEmployee.PositionId = (int)cmbPosition.SelectedValue;
                selectedEmployee.SiteId = (int)cmbLocation.SelectedValue;
                selectedEmployee.Active = chkActive.IsChecked == true ? (sbyte)1 : (sbyte)0;

                // Save and refresh
                context.SaveChanges();
                LoadEmployees();
                MessageBox.Show("Employee updated successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating employee: {ex.Message}");
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInitialData();
            disableAll(true);
        }

        private void DgEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgEmployees.SelectedItem != null)
            {
                dynamic employee = dgEmployees.SelectedItem;
                txtFirstName.Text = employee.FirstName;
                txtLastName.Text = employee.LastName;
                txtEmployeeID.Text = employee.EmployeeId.ToString();
                txtEmail.Text = employee.Email;
                txtUsername.Text = employee.Username;
                cmbPosition.SelectedValue = employee.PositionId;
                cmbLocation.SelectedValue = employee.SiteId;
                chkActive.IsChecked = employee.Active == 1;
                txtPassword.Clear();
            }
        }

        private void TogglePasswordVisibility(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (pwdPassword.Visibility == Visibility.Visible)
            {
                txtPassword.Text = pwdPassword.Password;
                pwdPassword.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
            }
            else
            {
                pwdPassword.Password = txtPassword.Text;
                pwdPassword.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
            }
        }

        private string GenerateUsername(string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                return string.Empty;

            // Convert to lowercase and get base username (first initial + lastname)
            string baseUsername = (firstName[0] + lastName).ToLower();
            string username = baseUsername;

            // Check if username exists
            int counter = 1;
            while (context.Employees.Any(e => e.Username.ToLower() == username.ToLower()))
            {
                username = $"{baseUsername}{counter:D2}"; // Format counter as 2 digits
                counter++;
            }

            return username;
        }

        // Add this method to automatically update username when first or last name changes
        private void txtFirstName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtFirstName.Text) && !string.IsNullOrEmpty(txtLastName.Text))
            {
                txtUsername.Text = GenerateUsername(txtFirstName.Text, txtLastName.Text);
            }
        }

        private void txtLastName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtFirstName.Text) && !string.IsNullOrEmpty(txtLastName.Text))
            {
                txtUsername.Text = GenerateUsername(txtFirstName.Text, txtLastName.Text);
            }
        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtEmail.Text = txtUsername.Text + "@bullseye.ca";
            MainWindow.ComputeSha256Hash("P@ssw0rd-", "TheSalt");
        }
    }
}
