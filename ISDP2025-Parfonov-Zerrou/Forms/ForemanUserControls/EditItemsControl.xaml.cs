using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

//ISDP Project - Inventory Management System
//Authors: Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Mohammed with some changes from Serhii
//Last Modified by Serhii on Feb 20,2025
namespace ISDP2025_Parfonov_Zerrou.Forms.ForemanUserControls
{
    public partial class EditItemsControl : UserControl
    {
        //Database context and state management
        BestContext context;
        Employee employee;
        List<string> categoriesList = new();

        //Initializes the control and sets up employee location
        public EditItemsControl(Employee inputEmployee)
        {
            InitializeComponent();
            employee = inputEmployee;
            context = new BestContext();
            lblEmployeeLocation.Content = GetEmployeeLocation();
        }

        //Gets the site name for the employee's location
        private string GetEmployeeLocation()
        {
            var siteName = context.Sites
                .Where(s => s.SiteId == employee.SiteId)
                .Select(s => s.SiteName)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(siteName))
            {
                return "Unknown Location";
            }
            return siteName;
        }

        //Loads and populates the category dropdown with active categories
        private void LoadCategories()
        {
            try
            {
                //Reset and initialize categories list
                categoriesList.Clear();
                categoriesList.Add("All Categories");

                //Get active categories from database
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

        //Loads inventory data based on search criteria and category filters
        private void LoadInventory()
        {
            try
            {
                //Base query for employee's site inventory
                var query = context.Inventories
                    .Include(i => i.Item)
                    .Where(i => i.SiteId == employee.SiteId)
                    .AsQueryable();

                //Apply search filter if text entered
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    query = query.Where(i => i.Item.Name.ToLower().Contains(searchText));
                }

                //Apply category filter if specific category selected
                if (cmbSearchCategory.SelectedIndex > 0)
                {
                    string category = cmbSearchCategory.SelectedItem.ToString();
                    query = query.Where(i => i.Item.Category == category);
                }

                //Project and load filtered inventory data
                var inventory = query
                    .Select(i => new
                    {
                        i.ItemId,
                        i.SiteId,
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

        //Enables or disables the edit controls based on selection state
        private void EnableControls(bool isEnabled)
        {
            nudReorderThreshold.IsEnabled = isEnabled;
            nudOptimumThreshold.IsEnabled = isEnabled;
            txtNotes.IsEnabled = isEnabled;
            btnSave.IsEnabled = isEnabled;
        }

        //Clears all input fields and disables controls
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
                    // Use reflection to get properties from anonymous type
                    lblItemId.Content = selected.GetType().GetProperty("ItemId")?.GetValue(selected)?.ToString() ?? "";
                    txtItemName.Text = selected.GetType().GetProperty("Name")?.GetValue(selected)?.ToString() ?? "";
                    txtCurrentQuantity.Text = selected.GetType().GetProperty("Quantity")?.GetValue(selected)?.ToString() ?? "0";
                    nudReorderThreshold.Value = Convert.ToDouble(selected.GetType().GetProperty("ReorderThreshold")?.GetValue(selected) ?? 0);
                    nudOptimumThreshold.Value = Convert.ToDouble(selected.GetType().GetProperty("OptimumThreshold")?.GetValue(selected) ?? 0);
                    txtNotes.Text = selected.GetType().GetProperty("Notes")?.GetValue(selected)?.ToString() ?? "";
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

        //Validates that optimum threshold is greater than reorder threshold
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

        //Saves changes to the selected inventory item
        private void SaveChanges()
        {
            // Validation checks
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
                // Get selected item details
                int itemId = int.Parse(lblItemId.Content.ToString());
                var selected = dgvInventory.SelectedItem;
                int siteId = (int)selected.GetType().GetProperty("SiteId").GetValue(selected);

                // Update inventory record
                var inventory = context.Inventories.FirstOrDefault(i =>
                    i.ItemId == itemId && i.SiteId == siteId);

                if (inventory != null)
                {
                    inventory.ReorderThreshold = (int)nudReorderThreshold.Value;
                    inventory.OptimumThreshold = (int)nudOptimumThreshold.Value;
                    inventory.Notes = txtNotes.Text;

                    context.SaveChanges();
                    LoadInventory();
                    EnableControls(false);
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
            LoadInventory();
            ClearFields();
            txtSearch.IsEnabled = true;
            cmbSearchCategory.IsEnabled = true;
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
    }
}