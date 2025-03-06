using HandyControl.Controls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class AdminSupplierControl : UserControl
    {
        //DB context
        BestContext context;
        bool isEditMode = false;
        string[] countries = { "CANADA", "USA", "AUSTRALIA" };
        bool isAdminEnabled;

        public AdminSupplierControl(Employee employee)
        {
            InitializeComponent();
            context = new BestContext();
            InitializeControls();
            dgvSuppliers.ItemsSource = null;
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
                cmbSearchProvince.SelectedIndex = 0;

                cmbProvince.ItemsSource = provinces;
                cmbCountry.ItemsSource = countries;
                cmbCountry.SelectedIndex = 0;

                // Set default to Active (index 1)
                cmbIsActive.SelectedIndex = 1;

                //Disable controls initially
                EnableInputs(false);
                EnableSearchControls(false);
                btnAdd.IsEnabled = false;
                btnUpdate.IsEnabled = false;
                btnClear.IsEnabled = false;
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error initializing controls: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Loads suppliers from database and displays them in DGV
        private void LoadSuppliers()
        {
            try
            {
                //Query suppliers with province info
                var suppliers = context.Suppliers
                    .Include(s => s.ProvinceNavigation)
                    .Select(s => new
                    {
                        s.SupplierId,
                        s.Name,
                        Address = s.Address1,
                        s.Address2,
                        s.City,
                        s.Province,
                        ProvinceName = s.ProvinceNavigation.ProvinceName,
                        s.Country,
                        PostalCode = ValidatorsFormatters.FormatPostalCode(s.Postalcode),
                        Phone = ValidatorsFormatters.FormatPhoneNumber(s.Phone),
                        s.Contact,
                        s.Notes,
                        Active = s.Active == 1 ? "Yes" : "No",
                        ActiveValue = s.Active
                    })
                    .OrderBy(s => s.Name)
                    .ToList();

                dgvSuppliers.ItemsSource = suppliers;
                UpdateRecordCount();
                EnableSearchControls(suppliers.Any());
                btnAdd.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error loading suppliers: {ex.Message}");
            }
        }

        //Enables/disables search controls based on data availability
        private void EnableSearchControls(bool enabled)
        {
            txtSearch.IsEnabled = enabled;
            cmbSearchProvince.IsEnabled = enabled;
            cmbIsActive.IsEnabled = enabled;
        }

        //Filters grid data based on search text and selected province
        private void ApplyFilters()
        {
            if (dgvSuppliers.ItemsSource == null) return;

            try
            {
                //Start with base query
                var query = context.Suppliers
                    .Include(s => s.ProvinceNavigation)
                    .AsQueryable();

                //Add search text filter
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    query = query.Where(s =>
                        s.Name.ToLower().Contains(searchText) ||
                        s.City.ToLower().Contains(searchText) ||
                        s.Address1.ToLower().Contains(searchText) ||
                        s.Contact.ToLower().Contains(searchText));
                }

                //Add province filter
                if (cmbSearchProvince.SelectedValue != null && cmbSearchProvince.SelectedValue.ToString() != "-1")
                {
                    string? provinceId = cmbSearchProvince.SelectedValue.ToString();
                    query = query.Where(s => s.Province == provinceId);
                }

                //Add active filter if selected
                if (cmbIsActive.SelectedIndex > 0)
                {
                    sbyte isActive = (sbyte)(cmbIsActive.SelectedIndex == 1 ? 1 : 0);
                    query = query.Where(s => s.Active == isActive);
                }

                //Final query with formatting
                var result = query
                    .Select(s => new
                    {
                        s.SupplierId,
                        s.Name,
                        Address = s.Address1,
                        s.Address2,
                        s.City,
                        s.Province,
                        ProvinceName = s.ProvinceNavigation.ProvinceName,
                        s.Country,
                        PostalCode = ValidatorsFormatters.FormatPostalCode(s.Postalcode),
                        Phone = ValidatorsFormatters.FormatPhoneNumber(s.Phone),
                        s.Contact,
                        s.Notes,
                        Active = s.Active == 1 ? "Yes" : "No"
                    })
                    .OrderBy(s => s.Name)
                    .ToList();

                //Bind sources and update controls
                dgvSuppliers.ItemsSource = result;
                UpdateRecordCount();
                UpdateAddButtonState();
                EnableSearchControls(true);
            }
            catch (Exception ex)
            {
                Growl.Error($"Error applying filters: {ex.Message}");
            }
        }

        //Enables/disables input controls
        private void EnableInputs(bool enabled)
        {
            txtName.IsEnabled = enabled;
            txtAddress.IsEnabled = enabled;
            txtAddress2.IsEnabled = enabled;
            txtCity.IsEnabled = enabled;
            cmbProvince.IsEnabled = enabled;
            cmbCountry.IsEnabled = enabled;
            txtPostalCode.IsEnabled = enabled;
            txtPhone.IsEnabled = enabled;
            txtContact.IsEnabled = enabled;
            txtNotes.IsEnabled = enabled;
            chkActive.IsEnabled = enabled;
        }

        //Updates Add button state based on data availability
        private void UpdateAddButtonState()
        {
            btnAdd.IsEnabled = true;
        }

        //Updates record count display
        private void UpdateRecordCount()
        {
            int count = 0;
            if (dgvSuppliers.Items != null)
            {
                count = dgvSuppliers.Items.Count;
            }
            lblRecordCount.Text = "Total Records: " + count.ToString();
        }

        //Clears all input fields
        private void ClearInputs()
        {
            lblSupplierId.Content = string.Empty;
            txtName.Clear();
            txtAddress.Clear();
            txtAddress2.Clear();
            txtCity.Clear();
            cmbProvince.SelectedIndex = -1;
            cmbCountry.SelectedIndex = 0;
            txtPostalCode.Clear();
            txtPhone.Clear();
            txtContact.Clear();
            txtNotes.Clear();
            chkActive.IsChecked = true;
        }

        //Validates input fields before saving
        private bool ValidateInputs()
        {
            string errors = "";

            if (ValidatorsFormatters.IsEmpty(txtName.Text))
            {
                errors += "Supplier name cannot be empty\n";
            }
            else if (!ValidatorsFormatters.IsValidName(txtName.Text))
            {
                errors += "Supplier name can only contain letters, spaces, hyphens and apostrophes\n";
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

            if (ValidatorsFormatters.IsEmpty(txtContact.Text))
            {
                errors += "Contact person cannot be empty\n";
            }

            if (!string.IsNullOrEmpty(errors))
            {
                Growl.Warning(errors.TrimEnd('\n'));
                return false;
            }

            return true;
        }

        //Saves changes to database (new or updated record)
        private void SaveChanges()
        {
            try
            {
                // Get supplier ID and check if it exists
                if (!string.IsNullOrEmpty(lblSupplierId.Content?.ToString()))
                {
                    int supplierId = int.Parse(lblSupplierId.Content.ToString());
                    var supplier = context.Suppliers.Find(supplierId);

                    // Check if this is an inactive supplier being reactivated
                    if (supplier != null && supplier.Active == 0)
                    {
                        // Only update active status for inactive suppliers
                        supplier.Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0);
                        context.SaveChanges();
                        ApplyFilters(); // Refresh the grid with current filters
                        ResetToDefaultState();
                        EnableSearchControls(true);
                        Growl.Success("Supplier status updated successfully");
                        return;
                    }
                }

                if (!ValidateInputs())
                {
                    btnSave.IsEnabled = true; // Re-enable the button if validation fails
                    return;
                }

                // Clean up formatted input before saving
                string cleanPostalCode = ValidatorsFormatters.CleanPostalCode(txtPostalCode.Text);
                string cleanPhone = ValidatorsFormatters.CleanPhoneNumber(txtPhone.Text);

                // Add new record if no ID exists
                if (string.IsNullOrEmpty(lblSupplierId.Content?.ToString()))
                {
                    var supplier = new Supplier
                    {
                        Name = txtName.Text,
                        Address1 = txtAddress.Text,
                        Address2 = txtAddress2.Text,
                        City = txtCity.Text,
                        Province = cmbProvince.SelectedValue.ToString(),
                        Country = countries[cmbCountry.SelectedIndex],
                        Postalcode = cleanPostalCode,
                        Phone = cleanPhone,
                        Contact = txtContact.Text,
                        Notes = txtNotes.Text,
                        Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0)
                    };
                    context.Suppliers.Add(supplier);
                }
                // Update record if exists
                else
                {
                    int supplierId = int.Parse(lblSupplierId.Content.ToString());
                    var supplier = context.Suppliers.Find(supplierId);
                    if (supplier != null)
                    {
                        supplier.Name = txtName.Text;
                        supplier.Address1 = txtAddress.Text;
                        supplier.Address2 = txtAddress2.Text;
                        supplier.City = txtCity.Text;
                        supplier.Province = cmbProvince.SelectedValue.ToString();
                        supplier.Country = countries[cmbCountry.SelectedIndex];
                        supplier.Postalcode = cleanPostalCode;
                        supplier.Phone = cleanPhone;
                        supplier.Contact = txtContact.Text;
                        supplier.Notes = txtNotes.Text;
                        supplier.Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0);
                    }
                    else
                    {
                        throw new Exception("Supplier not found");
                    }
                }
                context.SaveChanges();
                ApplyFilters(); // Update to refresh with current filters
                ResetToDefaultState();
                EnableSearchControls(true);
                Growl.Success("Supplier saved successfully");
            }
            catch (Exception ex)
            {
                Growl.Error($"Error saving supplier: {ex.Message}");
                btnSave.IsEnabled = true;
            }
        }

        // Resets form to default state after operations
        private void ResetToDefaultState()
        {
            isEditMode = false;
            ClearInputs();
            EnableInputs(false);
            EnableSearchControls(true);
            btnSave.Visibility = Visibility.Collapsed;
            btnExit.Visibility = Visibility.Collapsed;
            btnAdd.Visibility = Visibility.Visible;
            btnUpdate.Visibility = Visibility.Visible;
            btnClear.Visibility = Visibility.Visible;
            btnUpdate.IsEnabled = false;
            btnClear.IsEnabled = false;
            dgvSuppliers.IsEnabled = true;
            dgvSuppliers.UnselectAll();
            UpdateAddButtonState();

            // Ensure buttons are re-enabled
            btnSave.IsEnabled = true;
            btnExit.IsEnabled = true;
        }

        // Prepares form for editing
        private void EnterEditMode()
        {
            isEditMode = true;
            EnableInputs(true);
            EnableSearchControls(false);
            btnAdd.Visibility = Visibility.Collapsed;
            btnUpdate.Visibility = Visibility.Collapsed;
            btnClear.Visibility = Visibility.Collapsed;
            btnSave.Visibility = Visibility.Visible;
            btnExit.Visibility = Visibility.Visible;
            dgvSuppliers.IsEnabled = false;

            // Ensure buttons are enabled
            btnSave.IsEnabled = true;
            btnExit.IsEnabled = true;
        }

        // Event Handlers
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

        private void CmbIsActive_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        // Refresh button click - reloads data
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadSuppliers();
            ApplyFilters();
        }

        // Clear button click - resets form and filters
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            txtSearch.Clear();
            cmbSearchProvince.SelectedIndex = 0;
            cmbIsActive.SelectedIndex = 0;
            btnClear.IsEnabled = false;
            btnUpdate.IsEnabled = false;
            dgvSuppliers.UnselectAll();
        }

        // Add button click - prepares form for new record
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            EnterEditMode();
        }

        // Update button click - prepares form for editing existing record
        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dgvSuppliers.SelectedItem != null)
            {
                dynamic selectedItem = dgvSuppliers.SelectedItem;
                bool isActive = selectedItem.Active == "Yes";

                if (!isActive)
                {
                    // For inactive suppliers, only allow changing the active status
                    Growl.Ask("This supplier is inactive. Do you want to change its status?", isConfirmed =>
                    {
                        if (isConfirmed)
                        {
                            // Enter a modified edit mode for inactive suppliers
                            isEditMode = true;
                            EnableInputs(false);
                            chkActive.IsEnabled = true;
                            EnableSearchControls(false);
                            btnAdd.Visibility = Visibility.Collapsed;
                            btnUpdate.Visibility = Visibility.Collapsed;
                            btnClear.Visibility = Visibility.Collapsed;
                            btnSave.Visibility = Visibility.Visible;
                            btnExit.Visibility = Visibility.Visible;
                            dgvSuppliers.IsEnabled = false;

                            // Toggle to active by default
                            chkActive.IsChecked = true;
                        }
                        return true;
                    });
                }
                else
                {
                    // Regular update for active suppliers
                    EnterEditMode();
                }
            }
        }

        // Save button click - validates and saves changes
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            btnSave.IsEnabled = false;

            // First check if this is an inactive supplier
            if (!string.IsNullOrEmpty(lblSupplierId.Content?.ToString()))
            {
                int supplierId = int.Parse(lblSupplierId.Content.ToString());
                var supplier = context.Suppliers.FirstOrDefault(s => s.SupplierId == supplierId);

                if (supplier != null && supplier.Active == 0 && !ValidateInputs())
                {
                    // If inactive, just confirm and save status changes
                    Growl.Ask("Do you want to save the status change?", isConfirmed =>
                    {
                        if (isConfirmed)
                        {
                            SaveChanges();
                        }
                        else
                        {
                            ResetToDefaultState();
                        }
                        return true;
                    });
                    return;
                }
            }

            // For active suppliers or new suppliers, validate first
            if (!ValidateInputs())
            {
                btnSave.IsEnabled = true;
                return;
            }

            // If validation passes, ask for confirmation
            Growl.Ask("Do you want to save the changes?", isConfirmed =>
            {
                if (isConfirmed)
                {
                    SaveChanges();
                }
                else
                {
                    ResetToDefaultState();
                }
                return true;
            });
        }

        // Grid selection changed - populates form with selected record
        private void DgSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvSuppliers.SelectedItem != null && !isEditMode)
            {
                dynamic selectedItem = dgvSuppliers.SelectedItem;

                // Populate form fields with selected item data
                lblSupplierId.Content = selectedItem.SupplierId.ToString();
                txtName.Text = selectedItem.Name;
                txtAddress.Text = selectedItem.Address;
                txtAddress2.Text = selectedItem.Address2;
                txtCity.Text = selectedItem.City;
                cmbProvince.SelectedValue = selectedItem.Province;
                cmbCountry.SelectedItem = selectedItem.Country;
                txtPostalCode.Text = selectedItem.PostalCode;
                txtPhone.Text = selectedItem.Phone;
                txtContact.Text = selectedItem.Contact;
                txtNotes.Text = selectedItem.Notes;
                chkActive.IsChecked = selectedItem.Active == "Yes";

                // Enable checkbox only for inactive suppliers
                bool isActive = selectedItem.Active == "Yes";

                btnUpdate.IsEnabled = true;
                btnClear.IsEnabled = true;
            }
        }

        // Active checkbox click - confirms deactivation
        private void ChkActive_Click(object sender, RoutedEventArgs e)
        {
            if (chkActive.IsChecked == false)
            {
                chkActive.IsEnabled = false;

                Growl.Ask("Are you sure you want to deactivate this supplier?", isConfirmed =>
                {
                    if (!isConfirmed)
                    {
                        chkActive.IsChecked = true;
                    }

                    chkActive.IsEnabled = true;
                    return true;
                });
            }
        }

        // Exit button click - confirms cancellation
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            btnExit.IsEnabled = false;

            Growl.Ask("Are you sure you want to cancel your changes?", isConfirmed =>
            {
                if (isConfirmed)
                {
                    ResetToDefaultState();
                }
                else
                {
                    btnExit.IsEnabled = true;
                }
                return true;
            });
        }

        // Cleanup when control is unloaded
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (context != null)
            {
                context.Dispose();
            }
        }
    }
}