using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Serhii on February 20,2025
namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class LocationControl : UserControl
    {
        //DB context
        BestContext context;
        bool isEditMode = false;
        string[] daysOfWeek = { "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY" };
        string[] countries = { "CANADA", "USA", "AUSTRALIA" };

        //Constructor
        public LocationControl()
        {
            InitializeComponent();
            context = new BestContext();
            InitializeControls();
            dgvLocations.ItemsSource = null;
            UpdateRecordCount();
        }

        //Initialize controls
        private void InitializeControls()
        {
            try
            {
                //Load provinces and Sort
                var provinces = context.Provinces
                    .OrderBy(p => p.ProvinceName)
                    .ToList();

                //Create list with default "All Provinces" option
                var searchProvinces = new List<Province>
                {
                   new Province { ProvinceId = "-1", ProvinceName = "All Provinces" }
                };

                //Add database provinces to list
                foreach (var province in provinces)
                {
                    searchProvinces.Add(province);
                }

                //Set up dropdown sources and defaults
                cmbSearchProvince.ItemsSource = searchProvinces;
                //New Brunswick default
                cmbSearchProvince.SelectedValue = "NB";

                cmbProvince.ItemsSource = provinces;
                cmbDayOfWeek.ItemsSource = daysOfWeek;
                cmbDayOfWeek.SelectedIndex = -1;

                cmbCountry.ItemsSource = countries;
                cmbCountry.SelectedIndex = 0;

                //Disable controls initially
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

        //Loads locations from database and displays them in DGV
        private void LoadLocations()
        {
            try
            {
                //Query locations with province info
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

        //Enables/disables search controls based on data availability
        private void EnableSearchControls(bool enabled)
        {
            txtSearch.IsEnabled = enabled;
            cmbSearchProvince.IsEnabled = enabled;
        }

        //Filters grid data based on search text and selected province
        private void ApplyFilters()
        {
            if (dgvLocations.ItemsSource == null) return;

            try
            {
                //Start with base query
                var query = context.Sites
                    .Include(s => s.Province)
                    .AsQueryable();

                //Add to Query
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    query = query.Where(s =>
                        s.SiteName.ToLower().Contains(searchText) ||
                        s.City.ToLower().Contains(searchText) ||
                        s.Address.ToLower().Contains(searchText));
                }

                //Add to Query Again if needed
                if (cmbSearchProvince.SelectedValue != null && cmbSearchProvince.SelectedValue.ToString() != "-1")
                {
                    string? provinceId = cmbSearchProvince.SelectedValue.ToString();
                    query = query.Where(s => s.ProvinceId == provinceId);
                }

                //Final query with fomrmatting
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

                //Bind sources and update controls
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

        //Enables/disables input controls
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

        //Updates Add button state based on data availability
        private void UpdateAddButtonState()
        {
            if (dgvLocations.Items != null && dgvLocations.Items.Count > 0)
            {
                btnAdd.IsEnabled = true;
            }
            else
            {
                btnAdd.IsEnabled = false;
            }
        }

        //Updates record count display
        private void UpdateRecordCount()
        {
            int count = 0;
            if (dgvLocations.Items != null)
            {
                count = dgvLocations.Items.Count;
            }
            lblRecordCount.Text = "Total Records: " + count.ToString();
        }

        //Clears all input fields
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

        //Validates input fields before saving
        private bool ValidateInputs()
        {
            string errors = "";

            if (ValidatorsFormatters.IsEmpty(txtSiteName.Text))
            {
                errors += "Site name cannot be empty\n";
            }
            else if (!ValidatorsFormatters.IsValidName(txtSiteName.Text))
            {
                errors += "Site name can only contain letters, spaces, hyphens and apostrophes\n";
            }

            if (ValidatorsFormatters.IsEmpty(txtAddress.Text))
            {
                errors += "Address cannot be empty\n";
            }

            if (ValidatorsFormatters.IsEmpty(txtCity.Text))
            {
                errors += "City cannot be empty\n";
            }

            if (cmbProvince.SelectedValue == null)
            {
                errors += "Please select a province\n";
            }

            if (cmbCountry.SelectedIndex == -1)
            {
                errors += "Please select a country\n";
            }

            if (ValidatorsFormatters.IsEmpty(txtPostalCode.Text))
            {
                errors += "Postal code cannot be empty\n";
            }
            else if (!ValidatorsFormatters.IsValidPostalCode(ValidatorsFormatters.CleanPostalCode(txtPostalCode.Text)))
            {
                errors += "Please enter a valid postal code (A1A 1A1 format)\n";
            }

            if (ValidatorsFormatters.IsEmpty(txtPhone.Text))
            {
                errors += "Phone number cannot be empty\n";
            }
            else if (!ValidatorsFormatters.IsValidPhoneNumber(ValidatorsFormatters.CleanPhoneNumber(txtPhone.Text)))
            {
                errors += "Please enter a valid 10-digit phone number\n";
            }

            if (ValidatorsFormatters.IsEmpty(txtDistanceFromWH.Text))
            {
                errors += "Distance cannot be empty\n";
            }
            else if (!ValidatorsFormatters.IsValidInteger(txtDistanceFromWH.Text, 0))
            {
                errors += "Distance must be a positive number\n";
            }

            if (cmbDayOfWeek.SelectedIndex == -1)
            {
                errors += "Please select a delivery day\n";
            }

            if (!string.IsNullOrEmpty(errors))
            {
                MessageBox.Show(errors.TrimEnd('\n'), "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        //Saves changes to database (new or updated record)
        private void SaveChanges()
        {
            try
            {
                if (ValidateInputs())
                {
                    //Clean up formatted input before querying
                    string cleanPostalCode = ValidatorsFormatters.CleanPostalCode(txtPostalCode.Text);
                    string cleanPhone = ValidatorsFormatters.CleanPhoneNumber(txtPhone.Text);

                    //Add new record if no ID exists
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
                    //Update record if exists
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving location: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Resets form to default state after operations
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

        //Prepares form for editing
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

        //Event Handlers
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnClear.IsEnabled = !string.IsNullOrWhiteSpace(txtSearch.Text);
            ApplyFilters();
        }

        //Event Handlers continued...
        private void CmbSearchProvince_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSearchProvince.SelectedIndex > 0)
            {
                btnClear.IsEnabled = true;
            }
            ApplyFilters();
        }

        //Refresh button click - reloads data
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadLocations();
        }

        //Clear button click - resets form and filters
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            txtSearch.Clear();
            cmbSearchProvince.SelectedIndex = 0;
            btnClear.IsEnabled = false;
            btnUpdate.IsEnabled = false;
            dgvLocations.UnselectAll();
        }

        //Add button click - prepares form for new record
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            EnterEditMode();
        }

        //Update button click - prepares form for editing existing record
        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dgvLocations.SelectedItem != null)
            {
                EnterEditMode();
            }
        }

        //Save button click - validates and saves changes
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

        //Grid selection changed - populates form with selected record
        private void DgLocations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvLocations.SelectedItem != null && !isEditMode)
            {
                dynamic selectedItem = dgvLocations.SelectedItem;

                //Populate form fields with selected item data
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

        //Active checkbox click - confirms deactivation
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