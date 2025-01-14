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
        //static string ComputeSha256Hash(string str)
        //{
        //    SHA256 sha256 = SHA256Managed.Create();
        //    byte[] hashValue;
        //    UTF8Encoding objUtf8 = new UTF8Encoding();
        //    hashValue = sha256.ComputeHash(objUtf8.GetBytes(str));
        //    return Encoding.UTF8.GetString(hashValue);
            
        //}

        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    string result = ComputeSha256Hash("Hello");
        //    MessageBox.Show(result+" "+ result.Length.ToString());
        //}

        //private void Button_Click_1(object sender, RoutedEventArgs e)
        //{
        //    MessageBox.Show("Hello");
        //}
    }
}
