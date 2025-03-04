using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class InventoryControl : UserControl
    {
        BestContext context;
        List<dynamic> suppliersList = new List<dynamic>();
        List<string> categoriesList = new List<string>();

        public InventoryControl()
        {
            InitializeComponent();
            context = new BestContext();
            EnableSearchControls(false);
            LoadSuppliersToList();
            LoadCategoriesToList();
            btnClear.IsEnabled = false;
            EnableInputs(false);
            ClearInputs();
            dgvItems.ItemsSource = null;
        }

        private void ClearInputs()
        {
            lblItemId.Content = string.Empty;
            txtItemName.Clear();
            txtSKU.Clear();
            cmbEditCategory.SelectedIndex = -1;
            cmbEditCategory.Text = string.Empty;
            txtQuantity.Text = "0";
            txtWeight.Clear();
            txtCostPrice.Clear();
            txtRetailPrice.Clear();
            txtCaseSize.Clear();
            cmbSupplier.SelectedIndex = 0;
            chkActive.IsChecked = true;
            cmbSearchCategory.SelectedIndex = 0;
            txtSearch.Clear();
        }

        private void LoadCategoriesToList()
        {
            var dbCategories = context.Items
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            categoriesList.Clear();
            categoriesList.Add("All Categories");

            foreach (var category in dbCategories)
            {
                categoriesList.Add(category);
            }

            cmbSearchCategory.ItemsSource = categoriesList;
            cmbSearchCategory.SelectedIndex = 0;
            cmbEditCategory.ItemsSource = categoriesList.Skip(1).ToList();
            cmbEditCategory.SelectedIndex = 0;
        }

        private void LoadSuppliersToList()
        {
            suppliersList.Add(new Supplier
            {
                SupplierId = -1,
                Name = "No Supplier Selected",
                Active = 1
            });

            var dbSuppliers = context.Suppliers
                .Where(s => s.Active == 1)
                .OrderBy(s => s.Name)
                .ToList();

            foreach (var supplier in dbSuppliers)
            {
                suppliersList.Add(supplier);
            }

            cmbSupplier.ItemsSource = suppliersList;
            cmbSupplier.DisplayMemberPath = "Name";
            cmbSupplier.SelectedValuePath = "SupplierId";
            cmbSupplier.SelectedIndex = 0;
        }

        private void LoadItems()
        {
            var inventory = context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                .Select(i => new
                {
                    i.ItemId,
                    ItemName = i.Name,
                    i.Sku,
                    i.Category,
                    TotalQuantity = i.Inventories.Sum(inv => inv.Quantity),
                    i.Weight,
                    i.CaseSize,
                    i.CostPrice,
                    i.RetailPrice,
                    i.SupplierId,
                    SupplierName = i.Supplier.Name,
                    Active = i.Active == 1 ? "Yes" : "No"
                })
                .ToList();

            dgvItems.ItemsSource = inventory;
        }

        private void EnableInputs(bool isEnabled)
        {
            txtItemName.IsEnabled = isEnabled;
            txtSKU.IsEnabled = isEnabled;
            cmbEditCategory.IsEnabled = isEnabled;
            txtQuantity.IsEnabled = false;
            txtWeight.IsEnabled = isEnabled;
            txtCostPrice.IsEnabled = isEnabled;
            txtRetailPrice.IsEnabled = isEnabled;
            txtCaseSize.IsEnabled = isEnabled;
            chkActive.IsEnabled = isEnabled;
            cmbSupplier.IsEnabled = isEnabled;
        }

        private void EnableSearchControls(bool isEnabled)
        {
            txtSearch.IsEnabled = isEnabled;
            cmbSearchCategory.IsEnabled = isEnabled;
            dgvItems.IsEnabled = isEnabled;
        }

        private bool CheckInputs()
        {
            var errors = new List<string>();

            if (!ValidatorsFormatters.ValidateItemName(txtItemName.Text))
            {
                errors.Add(ValidatorsFormatters.GetItemNameError(txtItemName.Text));
            }

            if (!ValidatorsFormatters.ValidateSKU(txtSKU.Text))
            {
                errors.Add(ValidatorsFormatters.GetSKUError(txtSKU.Text));
            }

            if (!ValidatorsFormatters.ValidateCategory(cmbEditCategory.Text))
            {
                errors.Add(ValidatorsFormatters.GetCategoryError(cmbEditCategory.Text));
            }

            if (!ValidatorsFormatters.ValidateWeight(txtWeight.Text))
            {
                errors.Add(ValidatorsFormatters.GetWeightError(txtWeight.Text));
            }

            if (!ValidatorsFormatters.ValidatePrice(txtCostPrice.Text))
            {
                errors.Add(ValidatorsFormatters.GetPriceError(txtCostPrice.Text));
            }

            if (!ValidatorsFormatters.ValidatePrice(txtRetailPrice.Text))
            {
                errors.Add(ValidatorsFormatters.GetPriceError(txtRetailPrice.Text));
            }

            if (!ValidatorsFormatters.ValidateCaseSize(txtCaseSize.Text))
            {
                errors.Add(ValidatorsFormatters.GetCaseSizeError(txtCaseSize.Text));
            }

            if (cmbSupplier.SelectedValue == null || (int)cmbSupplier.SelectedValue == -1)
            {
                errors.Add("Please select a supplier");
            }

            if (errors.Any())
            {
                MessageBox.Show(string.Join("\n", errors), "Validation Errors",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void SaveNewItem()
        {
            try
            {
                var item = new Item
                {
                    Name = txtItemName.Text,
                    Sku = txtSKU.Text,
                    Category = cmbEditCategory.Text,
                    Weight = decimal.Parse(txtWeight.Text),
                    CaseSize = int.Parse(txtCaseSize.Text),
                    CostPrice = decimal.Parse(txtCostPrice.Text),
                    RetailPrice = decimal.Parse(txtRetailPrice.Text),
                    SupplierId = (int)cmbSupplier.SelectedValue,
                    Description = null,
                    Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0)
                };

                context.Items.Add(item);
                context.SaveChanges();

                var sites = context.Sites.Where(s => s.Active == 1).ToList();
                foreach (var site in sites)
                {
                    var inventory = new Inventory
                    {
                        ItemId = item.ItemId,
                        SiteId = site.SiteId,
                        Quantity = 0,
                        ItemLocation = "Stock",
                        ReorderThreshold = 0,
                        OptimumThreshold = 0
                    };
                    context.Inventories.Add(inventory);
                }
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving new item: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void UpdateExistingItem()
        {
            try
            {
                int itemId = int.Parse(lblItemId.Content.ToString());
                var item = context.Items.Find(itemId);

                if (item != null)
                {
                    item.Name = txtItemName.Text;
                    item.Sku = txtSKU.Text;
                    item.Category = cmbEditCategory.Text;
                    item.Weight = decimal.Parse(txtWeight.Text);
                    item.CaseSize = int.Parse(txtCaseSize.Text);
                    item.CostPrice = decimal.Parse(txtCostPrice.Text);
                    item.RetailPrice = decimal.Parse(txtRetailPrice.Text);
                    item.SupplierId = (int)cmbSupplier.SelectedValue;
                    item.Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0);
                }

                context.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating item: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void ApplyFilters()
        {
            var query = context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                .AsQueryable();

            string searchText = txtSearch.Text.ToLower();
            query = query.Where(i => i.Name.ToLower().Contains(searchText));

            if (cmbSearchCategory.SelectedIndex > 0)
            {
                string category = cmbSearchCategory.SelectedItem.ToString();
                query = query.Where(i => i.Category == category);
            }

            var result = query.Select(i => new
            {
                i.ItemId,
                ItemName = i.Name,
                i.Sku,
                i.Category,
                TotalQuantity = i.Inventories.Sum(inv => inv.Quantity),
                i.Weight,
                i.CaseSize,
                i.CostPrice,
                i.RetailPrice,
                i.SupplierId,
                SupplierName = i.Supplier.Name,
                Active = i.Active == 1 ? "Yes" : "No"
            }).ToList();

            dgvItems.ItemsSource = result;
        }

        private void PopulateAddEditInputs()
        {
            if (dgvItems.Items.Count > 0 && dgvItems.SelectedItem != null)
            {
                dynamic selectedItem = dgvItems.SelectedItem;

                try
                {
                    lblItemId.Content = selectedItem.ItemId.ToString();
                    txtItemName.Text = selectedItem.ItemName;
                    txtSKU.Text = selectedItem.Sku;
                    cmbEditCategory.Text = selectedItem.Category;
                    txtWeight.Text = selectedItem.Weight.ToString();
                    txtCostPrice.Text = selectedItem.CostPrice.ToString();
                    txtRetailPrice.Text = selectedItem.RetailPrice.ToString();
                    txtCaseSize.Text = selectedItem.CaseSize.ToString();
                    cmbSupplier.SelectedValue = selectedItem.SupplierId;
                    chkActive.IsChecked = selectedItem.Active == "Yes";
                    btnUpdate.IsEnabled = true;
                    btnClear.IsEnabled = true;
                    txtQuantity.Text = selectedItem.TotalQuantity.ToString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading item details: " + ex.Message);
                }
            }
            else
            {
                btnUpdate.IsEnabled = false;
                EnableInputs(false);
            }
        }

        private void SaveChanges()
        {
            MessageBoxResult result = MessageBox.Show(
                "The Item Will Be Added/Updated\nYES - Save Item\n" +
                "NO - Return To previous Page\nCANCEL - Continue Editing",
                "Confirmation Needed",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (!CheckInputs())
                {
                    return;
                }

                try
                {
                    if (string.IsNullOrEmpty(lblItemId.Content?.ToString()))
                    {
                        SaveNewItem();
                    }
                    else
                    {
                        UpdateExistingItem();
                    }

                    LoadItems();
                    DefaultLayout();
                    MessageBox.Show("Item saved successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving item: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (result == MessageBoxResult.No)
            {
                DefaultLayout();
            }
        }

        private void ChangeButtons()
        {
            if (dgvItems.ItemsSource == null)
            {
                MessageBox.Show("Please Click Refresh To Load The Data First", "Attention",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            EnableInputs(true);
            EnableSearchControls(false);
            btnRefresh.Visibility = Visibility.Collapsed;
            btnAdd.Visibility = Visibility.Collapsed;
            btnUpdate.Visibility = Visibility.Collapsed;
            btnClear.Visibility = Visibility.Collapsed;
            btnSave.Visibility = Visibility.Visible;
        }

        private void DefaultLayout()
        {
            ClearInputs();
            EnableInputs(false);
            EnableSearchControls(true);

            btnSave.Visibility = Visibility.Collapsed;
            btnAdd.Visibility = Visibility.Visible;
            btnUpdate.Visibility = Visibility.Visible;
            btnUpdate.IsEnabled = false;
            btnClear.Visibility = Visibility.Visible;
            btnClear.IsEnabled = false;
            btnRefresh.Visibility = Visibility.Visible;

            txtSearch.Clear();
            cmbSearchCategory.SelectedIndex = 0;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnClear.IsEnabled = !string.IsNullOrWhiteSpace(txtSearch.Text);
            ApplyFilters();
        }

        private void CmbSearchCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSearchCategory.SelectedIndex != 0)
            {
                btnClear.IsEnabled = true;
            }
            ApplyFilters();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            LoadItems();
            EnableSearchControls(true);
        }

        private void DgInventory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulateAddEditInputs();
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dgvItems.SelectedItem == null)
            {
                MessageBox.Show("Please select an item to update", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                ChangeButtons();
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            ChangeButtons();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (context != null)
            {
                context.Dispose();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }

        private void chkActive_Click(object sender, RoutedEventArgs e)
        {
            if (chkActive.IsChecked == false)
            {
                MessageBoxResult result = MessageBox.Show(
                    "You're about to deactivate this item!",
                    "Alert",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.OK)
                {
                    chkActive.IsChecked = false;
                }
                else
                {
                    chkActive.IsChecked = true;
                }
            }
        }
    }
}