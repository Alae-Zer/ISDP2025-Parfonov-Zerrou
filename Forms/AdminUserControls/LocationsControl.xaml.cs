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
                cmbSearchProvince.SelectedIndex = 0;

                cmbProvince.ItemsSource = provinces;

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
                        s.PostalCode,
                        s.Phone,
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
                        s.PostalCode,
                        s.Phone,
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
                EnableSearchControls(result.Any());
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
            txtCountry.IsEnabled = enabled;
            txtPostalCode.IsEnabled = enabled;
            txtPhone.IsEnabled = enabled;
            txtDayOfWeek.IsEnabled = enabled;
            txtDistanceFromWH.IsEnabled = enabled;
            txtNotes.IsEnabled = enabled;
            chkActive.IsEnabled = enabled;
        }

        private void UpdateAddButtonState()
        {
            if (dgvLocations.ItemsSource != null)
            {
                int count = ((System.Collections.IEnumerable)dgvLocations.ItemsSource).Cast<object>().Count();
                btnAdd.IsEnabled = count > 0;
            }
            else
            {
                btnAdd.IsEnabled = false;
            }
        }

        private void UpdateRecordCount()
        {
            if (dgvLocations.ItemsSource != null)
            {
                int count = ((System.Collections.IEnumerable)dgvLocations.ItemsSource).Cast<object>().Count();
                lblRecordCount.Text = $"Total Records: {count}";
            }
            else
            {
                lblRecordCount.Text = "Total Records: 0";
            }
        }

        private void ClearInputs()
        {
            lblSiteId.Content = string.Empty;
            txtSiteName.Clear();
            txtAddress.Clear();
            chkActive.IsChecked = false;
            txtAddress2.Clear();
            txtCity.Clear();
            cmbProvince.SelectedIndex = -1;
            txtCountry.Clear();
            txtPostalCode.Clear();
            txtPhone.Clear();
            txtDayOfWeek.Clear();
            txtDistanceFromWH.Clear();
            txtNotes.Clear();
            chkActive.IsChecked = true;
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtSiteName.Text) ||
                string.IsNullOrWhiteSpace(txtAddress.Text) ||
                string.IsNullOrWhiteSpace(txtCity.Text) ||
                cmbProvince.SelectedValue == null ||
                string.IsNullOrWhiteSpace(txtCountry.Text) ||
                string.IsNullOrWhiteSpace(txtPostalCode.Text) ||
                string.IsNullOrWhiteSpace(txtPhone.Text) ||
                !int.TryParse(txtDistanceFromWH.Text, out int distance))
            {
                MessageBox.Show("Please fill in all required fields with valid values.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (distance < 0)
            {
                MessageBox.Show("Distance must be a positive number.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private Site previousSelection = null;

        private void SaveChanges()
        {
            try
            {
                if (!ValidateInputs()) return;

                int savedSiteId;
                Site savedSite;

                if (string.IsNullOrEmpty(lblSiteId.Content?.ToString()))
                {
                    var site = new Site
                    {
                        SiteName = txtSiteName.Text,
                        Address = txtAddress.Text,
                        Address2 = txtAddress2.Text,
                        City = txtCity.Text,
                        ProvinceId = cmbProvince.SelectedValue.ToString(),
                        Country = txtCountry.Text,
                        PostalCode = txtPostalCode.Text,
                        Phone = txtPhone.Text,
                        DayOfWeek = txtDayOfWeek.Text,
                        DistanceFromWh = int.Parse(txtDistanceFromWH.Text),
                        Notes = txtNotes.Text,
                        Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0)
                    };
                    context.Sites.Add(site);
                    context.SaveChanges();
                    savedSiteId = site.SiteId;
                    savedSite = site;
                }
                else
                {
                    savedSiteId = int.Parse(lblSiteId.Content.ToString());
                    var site = context.Sites.Find(savedSiteId);
                    if (site != null)
                    {
                        site.SiteName = txtSiteName.Text;
                        site.Address = txtAddress.Text;
                        site.Address2 = txtAddress2.Text;
                        site.City = txtCity.Text;
                        site.ProvinceId = cmbProvince.SelectedValue.ToString();
                        site.Country = txtCountry.Text;
                        site.PostalCode = txtPostalCode.Text;
                        site.Phone = txtPhone.Text;
                        site.DayOfWeek = txtDayOfWeek.Text;
                        site.DistanceFromWh = int.Parse(txtDistanceFromWH.Text);
                        site.Notes = txtNotes.Text;
                        site.Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0);
                        savedSite = site;
                    }
                    else
                    {
                        throw new Exception("Site not found");
                    }
                    context.SaveChanges();
                }

                // Store the current selection before reloading
                previousSelection = savedSite;

                LoadLocations();

                // Select the saved item
                foreach (var item in dgvLocations.Items)
                {
                    dynamic locationItem = item;
                    if (locationItem.SiteId == savedSiteId)
                    {
                        dgvLocations.SelectedItem = item;
                        dgvLocations.ScrollIntoView(item);
                        break;
                    }
                }

                ResetToDefaultState();
                MessageBox.Show("Location saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving location: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChoosePreviousChoice()
        {
            if (previousSelection != null)
            {
                foreach (var item in dgvLocations.Items)
                {
                    dynamic locationItem = item;
                    if (locationItem.SiteId == previousSelection.SiteId)
                    {
                        dgvLocations.SelectedItem = item;
                        dgvLocations.ScrollIntoView(item);
                        break;
                    }
                }
            }
        }

        private void ResetToDefaultState()
        {
            isEditMode = false;
            ClearInputs();
            EnableInputs(false);
            btnSave.Visibility = Visibility.Collapsed;
            btnAdd.Visibility = Visibility.Visible;
            btnUpdate.Visibility = Visibility.Visible;
            btnClear.Visibility = Visibility.Visible;
            btnUpdate.IsEnabled = false;
            btnClear.IsEnabled = false;
            dgvLocations.IsEnabled = true;
            dgvLocations.UnselectAll();
            UpdateAddButtonState();
            EnableSearchControls(false);
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
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SaveChanges();
            }
            else if (result == MessageBoxResult.No)
            {
                ResetToDefaultState();
                ChoosePreviousChoice();
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
                txtCountry.Text = selectedItem.Country;
                txtPostalCode.Text = selectedItem.PostalCode;
                txtPhone.Text = selectedItem.Phone;
                txtDayOfWeek.Text = selectedItem.DayOfWeek;
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