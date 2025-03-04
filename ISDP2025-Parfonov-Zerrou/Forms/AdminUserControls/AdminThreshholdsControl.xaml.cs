using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Mohammed on February 20,2025
namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class AdminThreshholdsControl : UserControl
    {
        //Database context and category list for dropdown
        BestContext context;
        List<string> categoriesList = new();

        //Initialize control and disable UI elements until data is loaded
        public AdminThreshholdsControl(Employee employee)
        {
            InitializeComponent();
            context = new BestContext();
            EnableControls(false);
            ClearFields();
            txtSearch.IsEnabled = false;
            cmbLocation.IsEnabled = false;
            cmbSearchCategory.IsEnabled = false;
        }

        //Loads active locations excluding system sites (9999, 10000)
        private void LoadLocations()
        {
            try
            {
                var excludedSites = new[] { 9999, 10000 };

                //Get active sites except excluded ones
                var query = context.Sites
                    .Where(s => s.Active == 1 && !excludedSites.Contains(s.SiteId))
                    .OrderBy(s => s.SiteName)
                    .ToList();

                //Add "All Locations" option
                var allSites = new Site { SiteId = 0, SiteName = "All Locations" };
                var sitesList = new List<Site> { allSites };
                sitesList.AddRange(query);

                cmbLocation.ItemsSource = sitesList;
                cmbLocation.SelectedIndex = 0;
                cmbLocation.IsEnabled = true;

                LoadInventory();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(
                    $"Error loading locations: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        //Loads active categories for filtering
        private void LoadCategories()
        {
            try
            {
                categoriesList.Clear();
                categoriesList.Add("All Categories");

                //Get active categories
                var categories = context.Categories
                    .Where(c => c.Active == 1)
                    .Select(c => c.CategoryName)
                    .OrderBy(c => c)
                    .ToList();

                categoriesList.AddRange(categories);
                cmbSearchCategory.ItemsSource = categoriesList;
                cmbSearchCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(
                    $"Error loading categories: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        //Loads inventory data based on selected filters
        private void LoadInventory()
        {
            try
            {
                //Base query with related data
                var query = context.Inventories
                    .Include(i => i.Item)
                    .Include(i => i.Site)
                    .AsQueryable();

                //Apply search filter
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    query = query.Where(i => i.Item.Name.ToLower().Contains(searchText));
                }

                //Apply category filter
                if (cmbSearchCategory.SelectedIndex > 0)
                {
                    string? category = cmbSearchCategory.SelectedItem.ToString();
                    query = query.Where(i => i.Item.Category == category);
                }

                //Apply location filter if specific location selected
                if (cmbLocation.SelectedItem is Site selectedSite && selectedSite.SiteId != 0)
                {
                    query = query.Where(i => i.SiteId == selectedSite.SiteId);
                }

                //Project and load filtered data
                var inventory = query
                    .Select(i => new
                    {
                        i.ItemId,
                        i.SiteId,
                        SiteName = i.Site.SiteName,
                        Name = i.Item.Name,
                        i.Quantity,
                        i.ReorderThreshold,
                        i.OptimumThreshold,
                        i.Notes
                    })
                    .ToList();

                dgvInventory.ItemsSource = inventory;
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(
                    $"Error loading inventory: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        //Enables or disables edit controls
        private void EnableControls(bool enabled)
        {
            nudReorderThreshold.IsEnabled = enabled;
            nudOptimumThreshold.IsEnabled = enabled;
            txtNotes.IsEnabled = enabled;
            btnSave.IsEnabled = enabled;
        }

        //Clears all input fields
        private void ClearFields()
        {
            lblItemId.Content = string.Empty;
            txtItemName.Clear();
            txtCurrentQuantity.Clear();
            nudReorderThreshold.Value = 0;
            nudOptimumThreshold.Value = 0;
            txtNotes.Clear();
            EnableControls(false);
        }

        //Populates form fields with selected inventory item data
        private void PopulateFields()
        {
            if (dgvInventory.SelectedItem != null)
            {
                var selected = dgvInventory.SelectedItem;
                try
                {
                    // Get and set properties from selected item
                    lblItemId.Content = selected.GetType().GetProperty("ItemId").GetValue(selected).ToString();
                    txtItemName.Text = selected.GetType().GetProperty("Name").GetValue(selected).ToString();
                    txtCurrentQuantity.Text = selected.GetType().GetProperty("Quantity").GetValue(selected).ToString();
                    nudReorderThreshold.Value = Convert.ToDouble(selected.GetType().GetProperty("ReorderThreshold").GetValue(selected));
                    nudOptimumThreshold.Value = Convert.ToDouble(selected.GetType().GetProperty("OptimumThreshold").GetValue(selected));
                    txtNotes.Text = selected.GetType().GetProperty("Notes").GetValue(selected)?.ToString() ?? "";
                    EnableControls(true);
                }
                catch (Exception ex)
                {
                    HandyControl.Controls.MessageBox.Show(
                        $"Error populating fields: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        //Validates threshold values
        private bool ValidateThresholds()
        {
            if (nudOptimumThreshold.Value <= nudReorderThreshold.Value)
            {
                HandyControl.Controls.MessageBox.Show(
                    "Optimum threshold must be greater than reorder threshold",
                    "Invalid Thresholds",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        //Saves changes to selected inventory item
        private void SaveChanges()
        {
            if (dgvInventory.SelectedItem == null)
            {
                HandyControl.Controls.MessageBox.Show(
                    "Please select an item to update",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!ValidateThresholds()) return;

            try
            {
                //Get selected item details
                int itemId = int.Parse(lblItemId.Content.ToString());
                var selected = dgvInventory.SelectedItem;
                int siteId = (int)selected.GetType().GetProperty("SiteId").GetValue(selected);

                //Update inventory record
                var inventory = context.Inventories.FirstOrDefault(i =>
                    i.ItemId == itemId && i.SiteId == siteId);

                if (inventory != null)
                {
                    inventory.ReorderThreshold = (int)nudReorderThreshold.Value;
                    inventory.OptimumThreshold = (int)nudOptimumThreshold.Value;
                    inventory.Notes = txtNotes.Text;

                    context.SaveChanges();
                    LoadInventory();
                    HandyControl.Controls.MessageBox.Show(
                        "Thresholds updated successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(
                    $"Error saving changes: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        //Event handlers for UI interactions
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadInventory();
        }

        private void CmbSearchCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadInventory();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCategories();
            LoadLocations();
            ClearFields();
        }

        private void DgvInventory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulateFields();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            context?.Dispose();
        }

        private void CmbLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadInventory();
            ClearFields();
        }
    }
}