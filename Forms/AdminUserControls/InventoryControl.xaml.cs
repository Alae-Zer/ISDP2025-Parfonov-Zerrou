using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class InventoryControl : UserControl
    {
        BestContext context;
        List<Site> locationsList = new List<Site>();

        public InventoryControl()
        {
            InitializeComponent();
            context = new BestContext();
            EnableSearchControls(false);
            LoadLocationsToGlobal();
            LoadInitialData();
            btnClear.IsEnabled = false;
            EnableInputs(false);
        }

        private void LoadLocationsToGlobal()
        {
            Site firstLocation = new Site();

            firstLocation.SiteId = -1;
            firstLocation.SiteName = "No Location Selected";

            // Add it to list
            locationsList.Add(firstLocation);

            // Get and add database locations
            var dbLocations = context.Sites.ToList();
            foreach (var location in dbLocations)
            {
                locationsList.Add(location);
            }
        }

        private void LoadInitialData()
        {
            LoadCategories();
            LoadSearchLocations();
            LoadChooseLocations();
        }

        private void LoadCategories()
        {
            var categories = context.Items
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            categories.Insert(0, "All Categories");
            cmbSearchCategory.ItemsSource = categories;
            cmbSearchCategory.SelectedIndex = 0;
        }

        private void LoadChooseLocations()
        {
            cmbLocation.ItemsSource = locationsList;
            cmbLocation.DisplayMemberPath = "SiteName";
            cmbLocation.SelectedValuePath = "SiteId";
            cmbLocation.SelectedIndex = 0;
        }

        private void LoadSearchLocations()
        {
            cmbSearchLocation.ItemsSource = locationsList;
            cmbSearchLocation.DisplayMemberPath = "SiteName";
            cmbSearchLocation.SelectedValuePath = "SiteId";
            cmbSearchLocation.SelectedIndex = 0;
        }

        private void LoadInventory()
        {
            EnableSearchControls(false);

            var inventory = context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Site)
                .Select(i => new
                {
                    i.ItemId,
                    i.SiteId,
                    SiteName = i.Site.SiteName,
                    ItemName = i.Item.Name,
                    i.Item.Sku,
                    i.Item.Category,
                    i.Quantity,
                    i.Item.Weight,
                    i.Item.CaseSize,
                    i.Item.CostPrice,
                    i.Item.RetailPrice,
                    Active = i.Item.Active == 1 ? "Yes" : "No"
                })
                .ToList();

            dgInventory.ItemsSource = inventory;
            EnableSearchControls(true);
        }

        private void EnableInputs(bool isEnabled)
        {
            txtItemName.IsEnabled = isEnabled;
            txtSKU.IsEnabled = isEnabled;
            txtCategory.IsEnabled = isEnabled;
            txtQuantity.IsEnabled = isEnabled;
            txtWeight.IsEnabled = isEnabled;
            txtCostPrice.IsEnabled = isEnabled;
            txtRetailPrice.IsEnabled = isEnabled;
            txtCaseSize.IsEnabled = isEnabled;
            cmbLocation.IsEnabled = isEnabled;
            chkActive.IsEnabled = isEnabled;
        }

        private void EnableSearchControls(bool isEnabled)
        {
            txtSearch.IsEnabled = isEnabled;
            cmbSearchCategory.IsEnabled = isEnabled;
            cmbSearchLocation.IsEnabled = isEnabled;
            dgInventory.IsEnabled = isEnabled;
        }

        private void ApplyFilters()
        {
            if (dgInventory.ItemsSource == null)
                return;

            var query = context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Site)
                .Select(i => new
                {
                    i.ItemId,
                    i.SiteId,
                    SiteName = i.Site.SiteName,
                    ItemName = i.Item.Name,
                    i.Item.Sku,
                    i.Item.Category,
                    i.Quantity,
                    i.Item.Weight,
                    i.Item.CaseSize,
                    i.Item.CostPrice,
                    i.Item.RetailPrice,
                    Active = i.Item.Active == 1 ? "Yes" : "No"
                });

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                string searchText = txtSearch.Text.ToLower();
                query = query.Where(i => i.ItemName.ToLower().Contains(searchText));
            }

            if (cmbSearchCategory.SelectedItem != null &&
                cmbSearchCategory.SelectedItem.ToString() != "All Categories")
            {
                string category = cmbSearchCategory.SelectedItem.ToString();
                query = query.Where(i => i.Category == category);
            }

            if (cmbSearchLocation.SelectedValue != null &&
                (int)cmbSearchLocation.SelectedValue != -1)
            {
                int siteId = (int)cmbSearchLocation.SelectedValue;
                query = query.Where(i => i.SiteId == siteId);
            }

            dgInventory.ItemsSource = query.ToList();
        }

        private void RestoreDefaultState()
        {
            // Clear and disable inputs
            ClearInputs();
            EnableInputs(false);

            // Enable search controls and grid
            EnableSearchControls(true);

            // Reset buttons to default visibility
            btnSave.Visibility = Visibility.Collapsed;
            btnAdd.Visibility = Visibility.Visible;
            btnUpdate.Visibility = Visibility.Visible;
            btnUpdate.IsEnabled = false;

            // Reset filters to default
            txtSearch.Clear();
            cmbSearchCategory.SelectedIndex = 0;
            cmbSearchLocation.SelectedIndex = 0;

            btnClear.IsEnabled = false;
            btnRefresh.Visibility = Visibility.Visible;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnClear.IsEnabled = !string.IsNullOrWhiteSpace(txtSearch.Text);
            ApplyFilters();
        }

        private void CmbSearchCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CmbSearchLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnClear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ClearInputs();
        }

        private void BtnRefresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadInventory();
        }

        private void ChangeIndex()
        {
            if (dgInventory.SelectedItem != null)
            {
                dynamic selectedItem = dgInventory.SelectedItem;

                try
                {
                    lblItemId.Content = selectedItem.ItemId.ToString();
                    txtItemName.Text = selectedItem.ItemName;
                    txtSKU.Text = selectedItem.Sku;
                    txtCategory.Text = selectedItem.Category;
                    txtQuantity.Text = selectedItem.Quantity.ToString();
                    txtWeight.Text = selectedItem.Weight.ToString();
                    txtCostPrice.Text = selectedItem.CostPrice.ToString();
                    txtRetailPrice.Text = selectedItem.RetailPrice.ToString();
                    txtCaseSize.Text = selectedItem.CaseSize.ToString();
                    cmbLocation.SelectedValue = selectedItem.SiteId;
                    chkActive.IsChecked = selectedItem.Active == "Yes";

                    btnUpdate.IsEnabled = true;
                    btnClear.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading item details: " + ex.Message);
                }
            }
            else
            {
                btnUpdate.IsEnabled = false;
            }
        }

        private void DgInventory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeIndex();
        }

        private void BtnUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            EnableInputs(true);
            EnableSearchControls(false);
            btnAdd.Visibility = Visibility.Collapsed;
            btnUpdate.Visibility = Visibility.Collapsed;
            btnRefresh.Visibility = Visibility.Collapsed;
            btnSave.Visibility = Visibility.Visible;
        }

        private void BtnAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ClearInputs();
            EnableInputs(true);
            EnableSearchControls(false);
            btnRefresh.Visibility = Visibility.Collapsed;
            btnAdd.Visibility = Visibility.Collapsed;
            btnUpdate.Visibility = Visibility.Collapsed;
            btnSave.Visibility = Visibility.Visible;
        }

        private void ClearInputs()
        {
            lblItemId.Content = string.Empty;
            txtItemName.Clear();
            txtSKU.Clear();
            txtCategory.Clear();
            txtQuantity.Clear();
            txtWeight.Clear();
            txtCostPrice.Clear();
            txtRetailPrice.Clear();
            txtCaseSize.Clear();
            cmbLocation.SelectedIndex = 0;
            chkActive.IsChecked = false;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result =
            MessageBox.Show("The Item Will Be Added/Updated", "Confirmation Needed", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Your save logic here
                RestoreDefaultState();
            }
            else if (result == MessageBoxResult.No)
            {
                RestoreDefaultState();
            }
        }
    }
}