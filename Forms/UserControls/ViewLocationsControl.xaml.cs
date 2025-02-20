using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Serhii on February 20,2025
namespace ISDP2025_Parfonov_Zerrou.Forms.UserControls
{
    //Displays Sites in DGV with Search Functionality
    public partial class ViewLocationsControl : UserControl
    {
        //Context Declaration - Global
        private BestContext context;

        //Initialize controls
        public ViewLocationsControl()
        {
            InitializeComponent();
            context = new BestContext();
            InitializeControls();
            dgvLocations.ItemsSource = null;
            UpdateRecordCount();
        }

        //Populates province dropdown with DB values
        private void InitializeControls()
        {
            //TRYCATCH
            try
            {
                //QUERY
                var provinces = context.Provinces
                    .OrderBy(p => p.ProvinceName)
                    .ToList();

                //Combine Default value with Values from DB
                var searchProvinces = new List<Province>
                {
                    new Province { ProvinceId = "-1", ProvinceName = "All Provinces" }
                };

                //LOOP
                foreach (var province in provinces)
                {
                    searchProvinces.Add(province);
                }

                //Bind values
                cmbSearchProvince.ItemsSource = searchProvinces;

                //Set Default - Obviously NB
                cmbSearchProvince.SelectedValue = "NB";

                //Search controls Disabling
                EnableSearchControls(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing controls: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Loads and displays active locations
        private void LoadLocations()
        {
            try
            {
                //QUERY
                var locations = context.Sites
                    .Include(s => s.Province)
                    .Where(s => s.Active == 1)
                    .Select(s => new
                    {
                        s.SiteId,
                        s.SiteName,
                        s.Address,
                        s.City,
                        ProvinceName = s.Province.ProvinceName,
                        // Format postal code XXX-XXX
                        PostalCode = string.Format("{0}-{1}",
                            s.PostalCode.Substring(0, 3),
                            s.PostalCode.Substring(3, 3)),
                        // Format phone (XXX) XXX-XXXX
                        Phone = string.Format("({0}) {1}-{2}",
                            s.Phone.Substring(0, 3),
                            s.Phone.Substring(3, 3),
                            s.Phone.Substring(6, 4)),
                        s.DayOfWeek,
                        s.DistanceFromWh
                    })
                    .OrderBy(s => s.SiteName)
                    .ToList();

                //Bind and Update count
                dgvLocations.ItemsSource = locations;
                UpdateRecordCount();
                //EnableSearchControls(locations.Any());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading locations: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Toggles search controls based on data availability
        private void EnableSearchControls(bool enabled)
        {
            txtSearch.IsEnabled = enabled;
            cmbSearchProvince.IsEnabled = enabled;
        }

        //Filters data based on search text and selected province
        private void ApplyFilters()
        {
            if (dgvLocations.ItemsSource != null)
            {
                try
                {
                    //Start Query
                    var query = context.Sites
                        .Include(s => s.Province)
                        .Where(s => s.Active == 1)
                        .AsQueryable();

                    //Text Search
                    if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                    {
                        //Add To QUERY
                        string searchText = txtSearch.Text.ToLower();
                        query = query.Where(s =>
                            s.SiteName.ToLower().Contains(searchText) ||
                            s.City.ToLower().Contains(searchText) ||
                            s.Address.ToLower().Contains(searchText));
                    }

                    //Province filter, was modified so still holds default index
                    if (cmbSearchProvince.SelectedValue != null && cmbSearchProvince.SelectedValue.ToString() != "-1")
                    {
                        //Add Again
                        string? provinceId = cmbSearchProvince.SelectedValue.ToString();
                        query = query.Where(s => s.ProvinceId == provinceId);
                    }

                    //And Finally assemble our list
                    var result = query
                        .Select(s => new
                        {
                            s.SiteId,
                            s.SiteName,
                            s.Address,
                            s.City,
                            s.ProvinceId,
                            ProvinceName = s.Province.ProvinceName,
                            s.PostalCode,
                            s.Phone,
                            s.DayOfWeek,
                            s.DistanceFromWh
                        })
                        .OrderBy(s => s.SiteName)
                        .ToList();

                    //Bind sources
                    dgvLocations.ItemsSource = result;
                    UpdateRecordCount();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error applying filters: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                dgvLocations.ItemsSource = null;
                UpdateRecordCount();
            }
        }

        //Total Count
        private void UpdateRecordCount()
        {
            int count = 0;
            if (dgvLocations.Items != null)
            {
                count = dgvLocations.Items.Count;
            }
            lblRecordCount.Text = "Total Records: " + count.ToString();
        }

        //Search text Changes
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbSearchProvince_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            //DEfault state 
            LoadLocations();
            txtSearch.Focus();
            txtSearch.Text = "";
            cmbSearchProvince.SelectedValue = "NB";
        }

        //Updates detail fields when location selection changes
        private void DgLocations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvLocations.SelectedItem != null)
            {
                //Only for displaying in controls
                dynamic selectedItem = dgvLocations.SelectedItem;

                txtSiteId.Text = selectedItem.SiteId.ToString();
                txtSiteName.Text = selectedItem.SiteName;
                txtAddress.Text = selectedItem.Address;
                txtCity.Text = selectedItem.City;
                txtProvince.Text = selectedItem.ProvinceName;
                txtPhone.Text = selectedItem.Phone;
                txtPostalCode.Text = selectedItem.PostalCode;
                txtDayOfWeek.Text = selectedItem.DayOfWeek;
                txtDistance.Text = selectedItem.DistanceFromWh.ToString();
            }
        }

        //Cleanup when control is unloaded
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (context != null)
            {
                context.Dispose();
            }
        }
    }
}