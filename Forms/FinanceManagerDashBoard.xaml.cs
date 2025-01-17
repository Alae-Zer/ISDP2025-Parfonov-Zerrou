using ISDP2025_Parfonov_Zerrou.Models;
using System.Windows;

namespace ISDP2025_Parfonov_Zerrou
{
    /// <summary>
    /// Interaction logic for AdminDashBoard.xaml
    /// </summary>
    public partial class FinanceManagerDashBoard : Window
    {
        BestContext context = new BestContext();
        private Employee employee;

        public FinanceManagerDashBoard(Employee employee)
        {
            InitializeComponent();
            this.employee = employee;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hello");

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hello");
        }
    }
}
