using System.Windows;
using ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;

namespace ISDP2025_Parfonov_Zerrou
{
    /// <summary>
    /// Interaction logic for AdminDashBoard.xaml
    /// </summary>
    public partial class FinanceManagerDashBoard : Window
    {
        BestContext context = new BestContext();
        private Employee employee;
        private LogoutManager logoutManager;

        public FinanceManagerDashBoard(Employee employee)
        {
            InitializeComponent();
            this.employee = employee;
            logoutManager = new LogoutManager(this, context);
            logoutManager.StartTimer();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hello");

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hello");
        }

        private void btnEmployee_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ViewEmployeesControl();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            logoutManager.Cleanup();
            context.Dispose();
            new MainWindow().Show();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
