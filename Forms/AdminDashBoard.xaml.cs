using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ISDP2025_Parfonov_Zerrou
{
    /// <summary>
    /// Interaction logic for AdminDashBoard.xaml
    /// </summary>
    public partial class AdminDashBoard : Window
    {
        public AdminDashBoard()
        {
            InitializeComponent();
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
    }
}
