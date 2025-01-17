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

        public EmployeesControl()
        {
            InitializeComponent();
            context = new BestContext();
            LoadInitialData();  // Load data when control initializes
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
            var employees = context.Employees
                .Include(e => e.Position)    // Include Position relationship
                .Include(e => e.Site)    // Include Site relationship
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
                var newEmployee = new Employee
                {
                    FirstName = txtFirstName.Text,
                    LastName = txtLastName.Text,
                    Email = txtEmail.Text,
                    Username = txtUsername.Text,
                    Password = pwdPassword.Visibility == 0 ? pwdPassword.Password : txtPassword.Text,
                    PositionId = (int)cmbPosition.SelectedValue,
                    SiteId = (int)cmbLocation.SelectedValue,
                    Active = chkActive.IsChecked == true ? (sbyte)1 : (sbyte)0
                };

                context.Employees.Add(newEmployee);
                context.SaveChanges();
                LoadEmployees();
                ClearForm();
                MessageBox.Show("Employee added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding employee: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmployees.SelectedItem is Employee selectedEmployee)
            {
                try
                {
                    selectedEmployee.FirstName = txtFirstName.Text;
                    selectedEmployee.LastName = txtLastName.Text;
                    selectedEmployee.Email = txtEmail.Text;
                    selectedEmployee.Username = txtUsername.Text;
                    selectedEmployee.Password = pwdPassword.Visibility == 0 ? pwdPassword.Password : txtPassword.Text;
                    selectedEmployee.PositionId = (int)cmbPosition.SelectedValue;
                    selectedEmployee.SiteId = (int)cmbLocation.SelectedValue;
                    selectedEmployee.Active = chkActive.IsChecked == true ? (sbyte)1 : (sbyte)0;

                    context.SaveChanges();
                    LoadEmployees();
                    MessageBox.Show("Employee updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating employee: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select an employee to update.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInitialData();
        }

        private void DgEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgEmployees.SelectedItem is Employee employee)
            {
                txtFirstName.Text = employee.FirstName;
                txtLastName.Text = employee.LastName;
                txtEmployeeID.Text = employee.EmployeeId.ToString();
                txtEmail.Text = employee.Email;
                txtUsername.Text = employee.Username;
                cmbPosition.SelectedValue = employee.PositionId;
                cmbLocation.SelectedValue = employee.SiteId;
                chkActive.IsChecked = employee.Active == 1;
                txtPassword.Clear(); // Clear password for security
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
    }
}
