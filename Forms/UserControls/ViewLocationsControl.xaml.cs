using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.UserControls
{
    public partial class ViewLocationsControl : UserControl
    {
        private BestContext context;

        public ViewLocationsControl()
        {
            InitializeComponent();
            context = new BestContext();
            InitializeControls();
            dgvLocations.ItemsSource = null;
            UpdateRecordCount();
        }

        private void InitializeControls()
        {
            try
            {
                var provinces = context.Provinces
                    .OrderBy(p => p.ProvinceName)
                    .ToList();

                var searchProvinces = new List<Province>
        {
            new Province { ProvinceId = "-1", ProvinceName = "All Provinces" }
        };
                searchProvinces.AddRange(provinces);

                cmbSearchProvince.ItemsSource = searchProvinces;

                var newBrunswick = provinces.FirstOrDefault(p => p.ProvinceName == "New Brunswick");
                if (newBrunswick != null)
                    cmbSearchProvince.SelectedItem = newBrunswick;
                else
                    cmbSearchProvince.SelectedIndex = 0;

                EnableSearchControls(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing controls: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLocations()
        {
            try
            {
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
                        PostalCode = string.Format("{0}-{1}",
                            s.PostalCode.Substring(0, 3),
                            s.PostalCode.Substring(3, 3)),
                        Phone = string.Format("({0}) {1}-{2}",
                            s.Phone.Substring(0, 3),
                            s.Phone.Substring(3, 3),
                            s.Phone.Substring(6, 4)),
                        s.DayOfWeek,
                        s.DistanceFromWh
                    })
                    .OrderBy(s => s.SiteName)
                    .ToList();

                dgvLocations.ItemsSource = locations;
                UpdateRecordCount();
                EnableSearchControls(locations.Any());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading locations: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableSearchControls(bool enabled)
        {
            txtSearch.IsEnabled = enabled;
            cmbSearchProvince.IsEnabled = enabled;
        }

        private void ApplyFilters()
        {
            if (dgvLocations.ItemsSource == null) return;

            try
            {
                var query = context.Sites
                    .Include(s => s.Province)
                    .Where(s => s.Active == 1)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    query = query.Where(s =>
                        s.SiteName.ToLower().Contains(searchText) ||
                        s.City.ToLower().Contains(searchText) ||
                        s.Address.ToLower().Contains(searchText));
                }

                if (cmbSearchProvince.SelectedValue != null && cmbSearchProvince.SelectedValue.ToString() != "-1")
                {
                    string provinceId = cmbSearchProvince.SelectedValue.ToString();
                    query = query.Where(s => s.ProvinceId == provinceId);
                }

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

                dgvLocations.ItemsSource = result;
                UpdateRecordCount();
                EnableSearchControls(result.Any());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filters: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateRecordCount()
        {
            var items = dgvLocations.ItemsSource as IEnumerable<object>;
            int count = items?.Count() ?? 0;
            lblRecordCount.Text = $"Total Records: {count}";
        }

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
            LoadLocations();
        }

        private void DgLocations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvLocations.SelectedItem != null)
            {
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

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (context != null)
            {
                context.Dispose();
            }
        }
    }
}