using System.Security.Cryptography;
using System.Text;
using System.Windows;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou
{
    /// <summary>
    /// Interaction logic for AdminDashBoard.xaml
    /// </summary>
    public partial class AdminDashBoard : Window
    {
        BestContext context = new BestContext();

        private void GetAllEmployees()
        {
            try
            {
                // Load the employees into the context
                context.Employees.Load();

                // Bind the loaded employees to the DataGrid
                dgvEmployees.ItemsSource = context.Employees.Local.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading employees: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public AdminDashBoard()
        {
            InitializeComponent();
            GetAllEmployees();
        }

        static string ComputeSha256Hash(string str)
        {
            //create an Object
            using (SHA256 sha256 = SHA256Managed.Create())
            {
                //Compute the hash value from the input string
                byte[] hashValue = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));

                //Convert the byte array into a hexadecimal string
                StringBuilder hexString = new StringBuilder(64);
                foreach (byte b in hashValue)
                {
                    hexString.Append(b.ToString("x2"));
                }

                //Return the hexadecimal string (64 characters long)
                return hexString.ToString();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            string result = ComputeSha256Hash("Hello");
            MessageBox.Show(result + " " + result.Length.ToString());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetAllEmployees();
        }
    }
}
