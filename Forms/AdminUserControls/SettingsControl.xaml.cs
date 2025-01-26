using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Models;

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
