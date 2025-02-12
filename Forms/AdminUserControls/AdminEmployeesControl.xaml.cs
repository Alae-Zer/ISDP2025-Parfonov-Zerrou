using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    //ISDP Project
    //Mohammed Alae-Zerrou, Serhii Parfonov
    //NBCC, Winter 2025
    //Completed By Mohammed with some changes from serhii
    //Last Modified by Mohammed on Feb 02,2025
    public partial class AdminEmployeesControl : UserControl
    {
        MainWindow mainWindow = new MainWindow();
        private readonly BestContext context;
        List<Employee> AllEmployees;
        Employee employee;

        public AdminEmployeesControl()
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
                btnUpdate.IsEnabled = true;
                btnDelete.IsEnabled = true;
                txtSearch.IsEnabled = true;
                cmbSearchCategory.IsEnabled = true;
                stcClear.IsEnabled = true;
            }
            else
            {
                employeeFormGroup.IsEnabled = false;
                btnAdd.IsEnabled = false;
                btnUpdate.IsEnabled = false;
                btnDelete.IsEnabled = false;
                txtSearch.IsEnabled = false;
                cmbSearchCategory.IsEnabled = false;
                stcClear.IsEnabled = false;
            }
        }
        private void LoadInitialData()
        {
            LoadPositions();
            LoadLocations();
            LoadEmployees();
            cmbSearchCategory.SelectedIndex = 1;
            cmbLocation.SelectedIndex = 1;
            cmbPosition.SelectedIndex = 1;
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
                IsActive = e.Active == 1 ? "Yes" : "No",
                e.Locked
            }).ToList();

            dgEmployees.ItemsSource = employees;
        }

        private void ClearForm()
        {
            txtFirstName.Clear();
            txtLastName.Clear();
            txtEmployeeID.Clear();
            txtEmail.Clear();
            txtUsername.Clear();
            txtEmail.Clear();
            cmbPosition.SelectedIndex = -1;
            cmbLocation.SelectedIndex = -1;
            chkActive.IsChecked = true;
            chkLocked.IsChecked = false;
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
                        var result = MessageBox.Show("Are you sure you want to add this employee?",
                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            var newEmployee = new Employee
                            {
                                FirstName = txtFirstName.Text,
                                LastName = txtLastName.Text,
                                Email = txtEmail.Text,
                                Username = txtUsername.Text,
                                Password = "P@ssw0rd-",
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
                            ClearForm();
                        }
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
            //if (txtFirstName.Text == "" || txtLastName.Text == "" || cmbLocation.SelectedIndex == -1 || cmbPosition.SelectedIndex == -1)
            //{
            //    return false;
            //}
            return true;
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrEmpty(txtEmployeeID.Text))
            {
                MessageBox.Show("Please select an employee to update.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var result = MessageBox.Show("Are you sure you want to update this employee?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (CheckAllInputs())
                {
                    try
                    {
                        int employeeId = int.Parse(txtEmployeeID.Text);
                        var selectedEmployee = context.Employees.Find(employeeId);

                        if (selectedEmployee == null)
                        {
                            MessageBox.Show("Employee not found.");
                            return;
                        }
                        selectedEmployee.FirstName = txtFirstName.Text;
                        selectedEmployee.LastName = txtLastName.Text;
                        selectedEmployee.Email = txtEmail.Text;
                        selectedEmployee.PositionId = (int)cmbPosition.SelectedValue;
                        selectedEmployee.SiteId = (int)cmbLocation.SelectedValue;
                        selectedEmployee.Active = chkActive.IsChecked == true ? (sbyte)1 : (sbyte)0;
                        selectedEmployee.Locked = chkLocked.IsChecked == true ? (sbyte)1 : (sbyte)0;

                        context.SaveChanges();
                        LoadEmployees();
                        MessageBox.Show("Employee updated successfully!");
                        ClearForm();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating employee: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Please Fill all the fields.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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
                int employeeId = ((dynamic)dgEmployees.SelectedItem).EmployeeID;
                Employee employee = AllEmployees.First(e => e.EmployeeID == employeeId);
                txtFirstName.Text = employee.FirstName;
                txtLastName.Text = employee.LastName;
                txtEmployeeID.Text = employee.EmployeeID.ToString();
                txtEmail.Text = employee.Email;
                txtUsername.Text = employee.Username;
                cmbPosition.SelectedValue = employee.PositionId;
                cmbLocation.SelectedValue = employee.SiteId;
                chkActive.IsChecked = employee.Active == 1;
                chkLocked.IsChecked = employee.Locked == 1;
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
            mainWindow.ComputeSha256Hash("P@ssw0rd-", "TheSalt");
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
                    case "Is Active":
                        return (emp.Active == 1 ? "Yes" : "No").ToLower().Contains(searchText);
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
                IsActive = e.Active == 1 ? "Yes" : "No",
                e.Locked
            });
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmployees.SelectedItem == null)
            {
                MessageBox.Show("Please select an employee to delete.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic selectedEmployee = dgEmployees.SelectedItem;
            int employeeId = selectedEmployee.EmployeeID;

            var result = MessageBox.Show("Are you sure you want to delete this employee?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var employee = context.Employees.Find(employeeId);
                    if (employee != null)
                    {
                        employee.Active = 0;
                        context.SaveChanges();
                        LoadEmployees();
                        ClearForm();
                        MessageBox.Show("Employee deleted successfully!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting employee: {ex.Message}");
                }
            }
        }

        private void btnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtEmployeeID.Text))
            {
                MessageBox.Show("Please select an employee to update.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to reset the password?", "Confirm reset", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    int employeeId = int.Parse(txtEmployeeID.Text);
                    var selectedEmployee = context.Employees.Find(employeeId);

                    if (selectedEmployee == null)
                    {
                        MessageBox.Show("Employee not found.");
                        return;
                    }
                    selectedEmployee.Password = "P@ssw0rd-";

                    context.SaveChanges();
                    LoadEmployees();
                    MessageBox.Show("Employee updated successfully!");
                    ClearForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating employee: {ex.Message}");
                }
            }
        }
    }
}
