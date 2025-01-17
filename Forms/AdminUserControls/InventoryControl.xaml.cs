using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class InventoryControl : UserControl
    {
        private readonly BestContext context;

        public InventoryControl()
        {
            InitializeComponent();
            context = new BestContext();
            LoadInitialData();
            btnClear.IsEnabled = false;
        }

        private void LoadInitialData()
        {
            LoadCategories();
            LoadLocations();
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

        private void LoadLocations()
        {
            var locations = context.Sites.ToList();
            cmbSearchLocation.ItemsSource = locations;
            cmbSearchLocation.DisplayMemberPath = "SiteName";
            cmbSearchLocation.SelectedValuePath = "SiteId";

            var allLocations = new Site { SiteId = -1, SiteName = "All Locations" };
            locations.Insert(0, allLocations);
            cmbSearchLocation.SelectedIndex = 0;
        }

        private void ApplyFilters()
        {
            if (dgInventory.ItemsSource == null) return; // Don't apply filters if grid is empty

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
                    i.Item.CostPrice,
                    i.Item.RetailPrice,
                    Active = i.Item.Active == 1 ? "Yes" : "No"
                })
                .AsQueryable();

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
            txtSearch.Clear();
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
            // Check if an item is selected
            if (dgInventory.SelectedItem != null)
            {
                // Get the selected item using reflection since we're using an anonymous type
                dynamic selectedItem = dgInventory.SelectedItem;

                try
                {

                    //// Populate form fields
                    txtItemName.Text = selectedItem.ItemName;
                    txtSKU.Text = selectedItem.Sku;
                    txtCategory.Text = selectedItem.Category;
                    txtQuantity.Text = selectedItem.Quantity.ToString();
                    txtWeight.Text = selectedItem.Weight.ToString();
                    txtCostPrice.Text = selectedItem.CostPrice.ToString();
                    txtRetailPrice.Text = selectedItem.RetailPrice.ToString();
                    txtCaseSize.Text = selectedItem.CaseSize.ToString();

                    // Set Location combobox
                    cmbLocation.SelectedValue = selectedItem.SiteId;

                    //// Set Active checkbox
                    chkActive.IsChecked = selectedItem.Active == "Yes";

                    //// Enable/disable buttons as needed
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
                // Clear form if nothing is selected
                //ClearForm();
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