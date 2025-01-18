using ISDP2025_Parfonov_Zerrou.Models;
using System.Windows.Controls;

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
            // Load employees into DataGrid
            var employees = context.Employees
                .ToList();
            dgEmployees.ItemsSource = employees;
        }

        private void DgEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgEmployees.SelectedItem is Employee employee)
            {
                // Populate form with selected employee data
                txtFirstName.Text = employee.FirstName;
                txtLastName.Text = employee.LastName;
                txtEmployeeID.Text = employee.EmployeeId.ToString();
                txtEmail.Text = employee.Email;
                txtUsername.Text = employee.Username;
                cmbPosition.SelectedValue = employee.PositionId;
                cmbLocation.SelectedValue = employee.SiteId;
                chkActive.IsChecked = employee.Active == 1;
            }
        }

        private void BtnRefresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadInitialData();
        }
    }
}
