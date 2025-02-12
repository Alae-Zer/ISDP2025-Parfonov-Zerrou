using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.ForemanUserControls
{
    public partial class EditItemsControl : UserControl
    {
        private readonly BestContext context;
        private readonly Employee _employee;
        private List<string> categoriesList = new();

        public EditItemsControl(Employee employee)
        {
            InitializeComponent();
            _employee = employee;
            context = new BestContext();
            lblEmployeeLocation.Content = GetEmployeeLocation();
            LoadInitialData();
        }

        private string GetEmployeeLocation()
        {
            return context.Sites
                .Where(s => s.SiteId == _employee.SiteId)
                .Select(s => s.SiteName)
                .FirstOrDefault() ?? "Unknown Location";
        }

        private void LoadInitialData()
        {
            try
            {
                LoadCategories();
                EnableControls(false);
                ClearFields();
                LoadInventory();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(
                    $"Error loading initial data: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadCategories()
        {
            try
            {
                categoriesList.Clear();
                categoriesList.Add("All Categories");

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

        private void LoadInventory()
        {
            try
            {
                var query = context.Inventories
                    .Include(i => i.Item)
                    .Where(i => i.SiteId == _employee.SiteId)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    query = query.Where(i => i.Item.Name.ToLower().Contains(searchText));
                }

                if (cmbSearchCategory.SelectedIndex > 0)
                {
                    string category = cmbSearchCategory.SelectedItem.ToString();
                    query = query.Where(i => i.Item.Category == category);
                }

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

        private void EnableControls(bool enabled)
        {
            nudReorderThreshold.IsEnabled = enabled;
            nudOptimumThreshold.IsEnabled = enabled;
            txtNotes.IsEnabled = enabled;
            btnSave.IsEnabled = enabled;
        }

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

        private void PopulateFields()
        {
            if (dgvInventory.SelectedItem != null)
            {
                var selected = dgvInventory.SelectedItem;
                try
                {
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
                int itemId = int.Parse(lblItemId.Content.ToString());
                var selected = dgvInventory.SelectedItem;
                int siteId = (int)selected.GetType().GetProperty("SiteId").GetValue(selected);

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
            LoadInventory();
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
    }
}