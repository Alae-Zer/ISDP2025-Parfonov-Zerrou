using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Models;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Mohammed on January 26,2025
namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        BestContext context = new BestContext();
        public SettingsControl()
        {
            InitializeComponent();

        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            txtLogoutMinutes.Text = "";
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
