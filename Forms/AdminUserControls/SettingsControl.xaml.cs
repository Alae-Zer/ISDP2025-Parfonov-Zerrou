using System.Windows;
using System.Windows.Controls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

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
            Setting setting;
            var result = MessageBox.Show("Are you sure you want to update the Auto Logout time?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    context.Sites.Load();
                    // Updated to match the exact column names from the database schema
                    setting = context.Settings.FirstOrDefault(s => s.SettingType == "global");

                    setting.LogoutTimeMinutes = int.Parse(txtLogoutMinutes.Text);
                    context.SaveChanges();
                    MessageBox.Show("The time got changed succesefuly", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error initializing window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
