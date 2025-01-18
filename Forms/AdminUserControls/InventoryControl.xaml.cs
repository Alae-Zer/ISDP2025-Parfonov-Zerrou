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
            LoadLocationsToGlobal();
            LoadInitialData();
            btnClear.IsEnabled = false;
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

        // Function to show all inventory without any filters
        private void LoadInventory()
        {
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
        }

        // Function to show filtered inventory
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
            DefaultState();
        }

        private void DefaultState()
        {
            txtSearch.Clear();
            txtCaseSize.Clear();
            txtCostPrice.Clear();
            txtRetailPrice.Clear();
            txtQuantity.Clear();
            txtWeight.Clear();
            txtCategory.Clear();
            txtSKU.Clear();
            chkActive.IsChecked = false;
            cmbLocation.SelectedIndex = 0;
            txtItemName.Clear();
            cmbSearchCategory.SelectedIndex = 0;
            cmbSearchLocation.SelectedIndex = 0;
            btnClear.IsEnabled = false;

        }

        private void BtnRefresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadInventory();
        }

        private void DgInventory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgInventory.SelectedItem != null)
            {
                dynamic selectedItem = dgInventory.SelectedItem;

                try
                {
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

        private void BtnUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void BtnAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}