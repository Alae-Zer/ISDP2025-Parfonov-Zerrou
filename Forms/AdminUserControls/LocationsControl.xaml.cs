using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class LocationControl : UserControl
    {
        private readonly BestContext context;
        private bool isEditMode = false;
        private readonly string[] daysOfWeek = { "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY" };
        private readonly string[] countries = { "CANADA", "USA", "AUSTRALIA" };

        public LocationControl()
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
                cmbSearchProvince.SelectedValue = "NB";

                cmbProvince.ItemsSource = provinces;
                cmbDayOfWeek.ItemsSource = daysOfWeek;
                cmbDayOfWeek.SelectedIndex = -1;

                cmbCountry.ItemsSource = countries;
                cmbCountry.SelectedIndex = 0;

                EnableInputs(false);
                EnableSearchControls(false);
                btnAdd.IsEnabled = false;
                btnUpdate.IsEnabled = false;
                btnClear.IsEnabled = false;
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
                    .Select(s => new
                    {
                        s.SiteId,
                        s.SiteName,
                        s.Address,
                        s.Address2,
                        s.City,
                        s.ProvinceId,
                        ProvinceName = s.Province.ProvinceName,
                        s.Country,
                        PostalCode = ValidatorsFormatters.FormatPostalCode(s.PostalCode),
                        Phone = ValidatorsFormatters.FormatPhoneNumber(s.Phone),
                        s.DayOfWeek,
                        s.DistanceFromWh,
                        s.Notes,
                        Active = s.Active == 1 ? "Yes" : "No"
                    })
                    .OrderBy(s => s.SiteName)
                    .ToList();

                dgvLocations.ItemsSource = locations;
                UpdateRecordCount();
                EnableSearchControls(locations.Any());
                btnAdd.IsEnabled = true;
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
                        s.Address2,
                        s.City,
                        s.ProvinceId,
                        ProvinceName = s.Province.ProvinceName,
                        s.Country,
                        PostalCode = ValidatorsFormatters.FormatPostalCode(s.PostalCode),
                        Phone = ValidatorsFormatters.FormatPhoneNumber(s.Phone),
                        s.DayOfWeek,
                        s.DistanceFromWh,
                        s.Notes,
                        Active = s.Active == 1 ? "Yes" : "No"
                    })
                    .OrderBy(s => s.SiteName)
                    .ToList();

                dgvLocations.ItemsSource = result;
                UpdateRecordCount();
                UpdateAddButtonState();
                EnableSearchControls(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filters: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableInputs(bool enabled)
        {
            txtSiteName.IsEnabled = enabled;
            txtAddress.IsEnabled = enabled;
            txtAddress2.IsEnabled = enabled;
            txtCity.IsEnabled = enabled;
            cmbProvince.IsEnabled = enabled;
            cmbCountry.IsEnabled = enabled;
            txtPostalCode.IsEnabled = enabled;
            txtPhone.IsEnabled = enabled;
            cmbDayOfWeek.IsEnabled = enabled;
            txtDistanceFromWH.IsEnabled = enabled;
            txtNotes.IsEnabled = enabled;
            chkActive.IsEnabled = enabled;
        }

        private void UpdateAddButtonState()
        {
            var items = dgvLocations.ItemsSource as IEnumerable<object>;
            btnAdd.IsEnabled = items?.Any() == true;
        }

        private void UpdateRecordCount()
        {
            var items = dgvLocations.ItemsSource as IEnumerable<object>;
            int count = items?.Count() ?? 0;
            lblRecordCount.Text = $"Total Records: {count}";
        }

        private void ClearInputs()
        {
            lblSiteId.Content = string.Empty;
            txtSiteName.Clear();
            txtAddress.Clear();
            txtAddress2.Clear();
            txtCity.Clear();
            cmbProvince.SelectedIndex = -1;
            cmbCountry.SelectedIndex = 0;
            txtPostalCode.Clear();
            txtPhone.Clear();
            cmbDayOfWeek.SelectedIndex = -1;
            txtDistanceFromWH.Clear();
            txtNotes.Clear();
            chkActive.IsChecked = true;
        }

        private bool ValidateInputs()
        {
            var errors = new List<string>();

            if (ValidatorsFormatters.IsEmpty(txtSiteName.Text))
            {
                errors.Add("Site name cannot be empty");
            }
            else if (!ValidatorsFormatters.IsValidName(txtSiteName.Text))
            {
                errors.Add("Site name can only contain letters, spaces, hyphens and apostrophes");
            }

            if (ValidatorsFormatters.IsEmpty(txtAddress.Text))
            {
                errors.Add("Address cannot be empty");
            }

            if (ValidatorsFormatters.IsEmpty(txtCity.Text))
            {
                errors.Add("City cannot be empty");
            }

            if (cmbProvince.SelectedValue == null)
            {
                errors.Add("Please select a province");
            }

            if (cmbCountry.SelectedIndex == -1)
            {
                errors.Add("Please select a country");
            }

            if (ValidatorsFormatters.IsEmpty(txtPostalCode.Text))
            {
                errors.Add("Postal code cannot be empty");
            }
            else if (!ValidatorsFormatters.IsValidPostalCode(ValidatorsFormatters.CleanPostalCode(txtPostalCode.Text)))
            {
                errors.Add("Please enter a valid postal code (A1A 1A1 format)");
            }

            if (ValidatorsFormatters.IsEmpty(txtPhone.Text))
            {
                errors.Add("Phone number cannot be empty");
            }
            else if (!ValidatorsFormatters.IsValidPhoneNumber(ValidatorsFormatters.CleanPhoneNumber(txtPhone.Text)))
            {
                errors.Add("Please enter a valid 10-digit phone number");
            }

            if (ValidatorsFormatters.IsEmpty(txtDistanceFromWH.Text))
            {
                errors.Add("Distance cannot be empty");
            }
            else if (!ValidatorsFormatters.IsValidInteger(txtDistanceFromWH.Text, 0))
            {
                errors.Add("Distance must be a positive number");
            }

            if (cmbDayOfWeek.SelectedIndex == -1)
            {
                errors.Add("Please select a delivery day");
            }

            if (errors.Any())
            {
                MessageBox.Show(string.Join("\n", errors), "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void SaveChanges()
        {
            try
            {
                if (!ValidateInputs()) return;

                string cleanPostalCode = ValidatorsFormatters.CleanPostalCode(txtPostalCode.Text);
                string cleanPhone = ValidatorsFormatters.CleanPhoneNumber(txtPhone.Text);

                if (string.IsNullOrEmpty(lblSiteId.Content?.ToString()))
                {
                    var site = new Site
                    {
                        SiteName = txtSiteName.Text,
                        Address = txtAddress.Text,
                        Address2 = txtAddress2.Text,
                        City = txtCity.Text,
                        ProvinceId = cmbProvince.SelectedValue.ToString(),
                        Country = countries[cmbCountry.SelectedIndex],
                        PostalCode = cleanPostalCode,
                        Phone = cleanPhone,
                        DayOfWeek = daysOfWeek[cmbDayOfWeek.SelectedIndex],
                        DistanceFromWh = int.Parse(txtDistanceFromWH.Text),
                        Notes = txtNotes.Text,
                        Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0)
                    };
                    context.Sites.Add(site);
                }
                else
                {
                    int siteId = int.Parse(lblSiteId.Content.ToString());
                    var site = context.Sites.Find(siteId);
                    if (site != null)
                    {
                        site.SiteName = txtSiteName.Text;
                        site.Address = txtAddress.Text;
                        site.Address2 = txtAddress2.Text;
                        site.City = txtCity.Text;
                        site.ProvinceId = cmbProvince.SelectedValue.ToString();
                        site.Country = countries[cmbCountry.SelectedIndex];
                        site.PostalCode = cleanPostalCode;
                        site.Phone = cleanPhone;
                        site.DayOfWeek = daysOfWeek[cmbDayOfWeek.SelectedIndex];
                        site.DistanceFromWh = int.Parse(txtDistanceFromWH.Text);
                        site.Notes = txtNotes.Text;
                        site.Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0);
                    }
                    else
                    {
                        throw new Exception("Site not found");
                    }
                }

                context.SaveChanges();
                LoadLocations();
                ResetToDefaultState();
                EnableSearchControls(true);
                MessageBox.Show("Location saved successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving location: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetToDefaultState()
        {
            isEditMode = false;
            ClearInputs();
            EnableInputs(false);
            EnableSearchControls(true);
            btnSave.Visibility = Visibility.Collapsed;
            btnAdd.Visibility = Visibility.Visible;
            btnUpdate.Visibility = Visibility.Visible;
            btnClear.Visibility = Visibility.Visible;
            btnUpdate.IsEnabled = false;
            btnClear.IsEnabled = false;
            dgvLocations.IsEnabled = true;
            dgvLocations.UnselectAll();
            UpdateAddButtonState();
        }

        private void EnterEditMode()
        {
            isEditMode = true;
            EnableInputs(true);
            EnableSearchControls(false);
            btnAdd.Visibility = Visibility.Collapsed;
            btnUpdate.Visibility = Visibility.Collapsed;
            btnClear.Visibility = Visibility.Collapsed;
            btnSave.Visibility = Visibility.Visible;
            dgvLocations.IsEnabled = false;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnClear.IsEnabled = !string.IsNullOrWhiteSpace(txtSearch.Text);
            ApplyFilters();
        }

        private void CmbSearchProvince_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSearchProvince.SelectedIndex > 0)
            {
                btnClear.IsEnabled = true;
            }
            ApplyFilters();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadLocations();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            txtSearch.Clear();
            cmbSearchProvince.SelectedIndex = 0;
            btnClear.IsEnabled = false;
            btnUpdate.IsEnabled = false;
            dgvLocations.UnselectAll();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            EnterEditMode();
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dgvLocations.SelectedItem != null)
            {
                EnterEditMode();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Do you want to save the changes?",
                "Confirm Save",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SaveChanges();
            }
            else
            {
                ResetToDefaultState();
            }
        }

        private void DgLocations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvLocations.SelectedItem != null && !isEditMode)
            {
                dynamic selectedItem = dgvLocations.SelectedItem;

                lblSiteId.Content = selectedItem.SiteId.ToString();
                txtSiteName.Text = selectedItem.SiteName;
                txtAddress.Text = selectedItem.Address;
                txtAddress2.Text = selectedItem.Address2;
                txtCity.Text = selectedItem.City;
                cmbProvince.SelectedValue = selectedItem.ProvinceId;
                cmbCountry.SelectedItem = selectedItem.Country;
                txtPostalCode.Text = selectedItem.PostalCode;
                txtPhone.Text = selectedItem.Phone;
                cmbDayOfWeek.SelectedItem = selectedItem.DayOfWeek;
                txtDistanceFromWH.Text = selectedItem.DistanceFromWh.ToString();
                txtNotes.Text = selectedItem.Notes;
                chkActive.IsChecked = selectedItem.Active == "Yes";

                btnUpdate.IsEnabled = true;
                btnClear.IsEnabled = true;
            }
        }

        private void ChkActive_Click(object sender, RoutedEventArgs e)
        {
            if (chkActive.IsChecked == false)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Are you sure you want to deactivate this location?",
                    "Confirm Deactivation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    chkActive.IsChecked = true;
                }
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