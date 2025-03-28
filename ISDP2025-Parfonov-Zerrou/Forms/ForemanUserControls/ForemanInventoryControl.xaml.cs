using HandyControl.Controls;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ISDP2025_Parfonov_Zerrou.Forms.ForemanUserControls
{
    public partial class ForemanInventoryControl : UserControl
    {
        // Global Variables
        BestContext context;
        List<string> categoriesList = new List<string>();
        List<Supplier> suppliersList = new List<Supplier>();
        string selectedImagePath;
        string imagePath;
        string fullPath;
        BitmapImage defaultImage;
        bool isNewItem = false;

        public ForemanInventoryControl()
        {
            // Load Defaults
            imagePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\Images\default.png"
            );
            fullPath = Path.GetFullPath(imagePath);

            InitializeComponent();
            context = new BestContext();
            LoadCategoriesToList();
            LoadSuppliersList();
            EnableSearchInputs(false);
            btnClear.IsEnabled = false;
            btnUpdate.IsEnabled = false;
            dgvItems.ItemsSource = null;
            LoadDefaultImage();
        }

        // Load suppliers for dropdown
        private void LoadSuppliersList()
        {
            try
            {
                suppliersList = context.Suppliers
                    .Where(s => s.Active == 1)
                    .OrderBy(s => s.Name)
                    .ToList();

                cmbSupplier.ItemsSource = suppliersList;
                cmbSupplier.DisplayMemberPath = "Name";
                cmbSupplier.SelectedValuePath = "SupplierId";
            }
            catch (Exception ex)
            {
                Growl.Error($"Error loading suppliers: {ex.Message}");
            }
        }

        // Loads Default Image
        private void LoadDefaultImage()
        {
            try
            {
                defaultImage = new BitmapImage();
                defaultImage.BeginInit();
                defaultImage.UriSource = new Uri(fullPath);
                defaultImage.CacheOption = BitmapCacheOption.OnLoad;
                defaultImage.EndInit();
                imgProduct.Source = defaultImage;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error loading default image: {ex.Message}");
                imgProduct.Source = null;
            }
        }

        // Loads Available Categories
        private void LoadCategoriesToList()
        {
            try
            {
                var itemCategories = context.Items
                    .Select(i => i.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                categoriesList.Clear();
                categoriesList.Add("All Categories");
                foreach (var category in itemCategories)
                {
                    categoriesList.Add(category);
                }

                cmbSearchCategory.ItemsSource = categoriesList;
                cmbSearchCategory.SelectedIndex = 0;

                cmbCategory.ItemsSource = itemCategories;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error loading categories: {ex.Message}");
            }
        }

        // Enables And Disables Search Inputs
        private void EnableSearchInputs(bool enableSearchInputs)
        {
            txtSearch.IsEnabled = enableSearchInputs;
            cmbSearchCategory.IsEnabled = enableSearchInputs;
        }

        // Enable editing controls
        private void EnableEditControls(bool enable)
        {
            txtItemName.IsEnabled = enable;
            cmbCategory.IsEnabled = enable;
            cmbSupplier.IsEnabled = enable;
            txtCaseSize.IsEnabled = enable;
            txtWeight.IsEnabled = enable;
            txtDescription.IsEnabled = enable;
            txtNotes.IsEnabled = enable;
            chkActive.IsEnabled = enable;
            btnBrowseImage.IsEnabled = enable;
            txtCostPrice.IsEnabled = enable;
            txtRetailPrice.IsEnabled = enable;

            txtSku.IsEnabled = isNewItem && enable;

            // Update button visibility
            if (enable)
            {
                btnBrowseImage.Visibility = Visibility.Visible;
                btnSave.Visibility = Visibility.Visible;
                btnCancel.Visibility = Visibility.Visible;
                btnUpdate.Visibility = Visibility.Collapsed;
                btnRefresh.Visibility = Visibility.Collapsed;
                btnClear.Visibility = Visibility.Collapsed;
                btnAddNew.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnBrowseImage.Visibility = Visibility.Collapsed;
                btnSave.Visibility = Visibility.Collapsed;
                btnCancel.Visibility = Visibility.Collapsed;
                btnUpdate.Visibility = Visibility.Visible;
                btnRefresh.Visibility = Visibility.Visible;
                btnClear.Visibility = Visibility.Visible;
                btnAddNew.Visibility = Visibility.Visible;
            }
        }

        // Prepares UI for adding a new item
        private void PrepareForNewItem()
        {
            isNewItem = true;
            ClearDetails();
            EnableEditControls(true);

            chkActive.IsChecked = true; // Active by default
            txtSku.IsEnabled = true;

            btnSave.Content = "Add Item";
            dgvItems.IsEnabled = false;

            txtItemName.Focus();
        }

        // Validates all fields for item using ValidatorsFormatters
        private bool ValidateItemFields()
        {
            List<string> errors = new List<string>();

            if (!ValidatorsFormatters.ValidateItemName(txtItemName.Text))
                errors.Add(ValidatorsFormatters.GetItemNameError(txtItemName.Text));

            if (isNewItem && !ValidatorsFormatters.ValidateSKU(txtSku.Text))
                errors.Add(ValidatorsFormatters.GetSKUError(txtSku.Text));

            if (cmbCategory.SelectedItem == null)
                errors.Add("Please select a category");

            if (cmbSupplier.SelectedItem == null)
                errors.Add("Please select a supplier");

            if (!ValidatorsFormatters.ValidateCaseSize(txtCaseSize.Text))
                errors.Add(ValidatorsFormatters.GetCaseSizeError(txtCaseSize.Text));

            if (!ValidatorsFormatters.ValidateWeight(txtWeight.Text))
                errors.Add(ValidatorsFormatters.GetWeightError(txtWeight.Text));

            // Validate cost price
            if (!ValidatorsFormatters.ValidatePrice(txtCostPrice.Text))
                errors.Add(ValidatorsFormatters.GetPriceError(txtCostPrice.Text));

            // Validate retail price
            if (!ValidatorsFormatters.ValidatePrice(txtRetailPrice.Text))
                errors.Add(ValidatorsFormatters.GetPriceError(txtRetailPrice.Text));

            // Validate retail price is higher than cost price
            if (decimal.TryParse(txtCostPrice.Text, out decimal costPrice) &&
                decimal.TryParse(txtRetailPrice.Text, out decimal retailPrice))
            {
                if (retailPrice <= costPrice)
                    errors.Add("Retail price must be higher than cost price");
            }

            if (errors.Count > 0)
            {
                Growl.Error(string.Join("\n", errors));
                return false;
            }

            return true;
        }

        // Loads Items To Data Grid
        private void LoadItemsToDGV()
        {
            try
            {
                var query = context.Items
                    .Include(i => i.Supplier)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    // Search by ID or name
                    query = query.Where(i => i.Name.ToLower().Contains(searchText) ||
                                            i.ItemId.ToString().Contains(searchText));
                }

                if (cmbSearchCategory.SelectedIndex > 0)
                {
                    string category = cmbSearchCategory.SelectedItem.ToString();
                    query = query.Where(i => i.Category == category);
                }

                var items = query.Select(i => new
                {
                    i.ItemId,
                    ItemName = i.Name,
                    i.Category,
                    SupplierName = i.Supplier.Name,
                    SupplierId = i.SupplierId,
                    i.Sku,
                    i.Description,
                    i.Notes,
                    i.ImageLocation,
                    i.CaseSize,
                    i.CostPrice,
                    i.RetailPrice,
                    i.Weight,
                    Active = i.Active == 1 ? "Yes" : "No"
                }).OrderBy(i => i.ItemName).ToList();

                dgvItems.ItemsSource = items;

                EnableSearchInputs(true);
                btnAddNew.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error loading items: {ex.Message}");
            }
        }

        // Add new item to the database
        private void AddNewItem()
        {
            if (!ValidateItemFields())
                return;

            try
            {
                string sku = txtSku.Text.Trim();

                if (context.Items.Any(i => i.Sku.ToLower() == sku.ToLower()))
                {
                    Growl.Warning("An item with this SKU already exists");
                    txtSku.Focus();
                    return;
                }

                Item newItem = new Item
                {
                    Name = txtItemName.Text.Trim(),
                    Sku = sku,
                    Category = cmbCategory.SelectedItem.ToString(),
                    SupplierId = (int)cmbSupplier.SelectedValue,
                    CaseSize = int.Parse(txtCaseSize.Text),
                    Weight = decimal.Parse(txtWeight.Text),
                    CostPrice = decimal.Parse(txtCostPrice.Text),
                    RetailPrice = decimal.Parse(txtRetailPrice.Text),
                    Description = txtDescription.Text.Trim(),
                    Notes = txtNotes.Text.Trim(),
                    Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0),
                    ImageLocation = !string.IsNullOrEmpty(selectedImagePath) ? selectedImagePath : fullPath
                };

                context.Items.Add(newItem);
                context.SaveChanges();

                int[] excludedSites = { 1, 3, 9999, 10000 };

                var sites = context.Sites
                    .Where(s => s.Active == 1 && !excludedSites.Contains(s.SiteId))
                    .ToList();

                foreach (var site in sites)
                {
                    var inventory = new Inventory
                    {
                        ItemId = newItem.ItemId,
                        SiteId = site.SiteId,
                        ItemLocation = "0",
                        Quantity = 0,
                        ReorderThreshold = 0,
                        OptimumThreshold = 0
                    };

                    context.Inventories.Add(inventory);
                }

                context.SaveChanges();

                Growl.Success("New product added successfully and added to all site inventories!");

                isNewItem = false;
                EnableEditControls(false);
                LoadItemsToDGV();
                dgvItems.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error adding new item: {ex.Message}");
            }
        }

        // Update SaveChanges method
        private void SaveChanges()
        {
            if (isNewItem)
            {
                AddNewItem();
                return;
            }

            if (!ValidateItemFields())
                return;

            var result = HandyControl.Controls.MessageBox.Show(
                "Do you want to save these changes?",
                "Confirmation",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (string.IsNullOrEmpty(txtItemId.Text))
                    {
                        Growl.Error("No item selected");
                        return;
                    }

                    int.TryParse(txtItemId.Text, out int itemId);
                    var item = context.Items.Find(itemId);

                    if (item != null)
                    {
                        item.Name = txtItemName.Text.Trim();
                        item.Category = cmbCategory.SelectedItem.ToString();
                        item.SupplierId = (int)cmbSupplier.SelectedValue;
                        item.CaseSize = int.Parse(txtCaseSize.Text);
                        item.Weight = decimal.Parse(txtWeight.Text);
                        item.CostPrice = decimal.Parse(txtCostPrice.Text);
                        item.RetailPrice = decimal.Parse(txtRetailPrice.Text);
                        item.Description = txtDescription.Text.Trim();
                        item.Notes = txtNotes.Text.Trim();
                        item.Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0);

                        if (!string.IsNullOrEmpty(selectedImagePath))
                        {
                            item.ImageLocation = selectedImagePath;
                        }

                        context.SaveChanges();
                        Growl.Success("Changes saved successfully!");

                        EnableEditControls(false);
                        LoadItemsToDGV();
                        dgvItems.IsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    Growl.Error($"Error saving changes: {ex.Message}");
                }
            }
            else if (result == MessageBoxResult.No)
            {
                EnableEditControls(false);
                ClearDetails();
                selectedImagePath = string.Empty;
                dgvItems.IsEnabled = true;
            }
        }

        // Populates TextBoxes With Selected row Details
        private void PopulateDetailFields(dynamic selectedItem)
        {
            try
            {
                if (selectedItem != null)
                {
                    txtItemId.Text = selectedItem.ItemId.ToString();
                    txtItemName.Text = selectedItem.ItemName;
                    txtSku.Text = selectedItem.Sku;

                    cmbCategory.SelectedItem = selectedItem.Category;

                    var supplier = suppliersList.FirstOrDefault(s => s.SupplierId == selectedItem.SupplierId);
                    if (supplier != null)
                    {
                        cmbSupplier.SelectedValue = supplier.SupplierId;
                    }

                    txtCaseSize.Text = selectedItem.CaseSize.ToString();
                    txtRetailPrice.Text = selectedItem.RetailPrice.ToString();
                    txtWeight.Text = selectedItem.Weight.ToString();
                    txtDescription.Text = selectedItem.Description;
                    txtNotes.Text = selectedItem.Notes;
                    txtCostPrice.Text = selectedItem.CostPrice.ToString();
                    txtRetailPrice.Text = selectedItem.RetailPrice.ToString();
                    chkActive.IsChecked = selectedItem.Active == "Yes";

                    if (!string.IsNullOrEmpty(selectedItem.ImageLocation))
                    {
                        try
                        {
                            var image = new BitmapImage();
                            image.BeginInit();
                            image.UriSource = new Uri(selectedItem.ImageLocation);
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.EndInit();
                            imgProduct.Source = image;
                        }
                        catch
                        {
                            imgProduct.Source = defaultImage;
                        }
                    }
                    else
                    {
                        imgProduct.Source = defaultImage;
                    }

                    btnClear.IsEnabled = true;
                    btnUpdate.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Growl.Error($"Error loading item details: {ex.Message}");
            }
        }

        // Reset Inputs and Set Default Image
        private void ClearDetails()
        {
            txtItemId.Text = string.Empty;
            txtItemName.Text = string.Empty;
            txtSku.Text = string.Empty;
            cmbCategory.SelectedItem = null;
            cmbSupplier.SelectedItem = null;
            txtCaseSize.Text = string.Empty;
            txtRetailPrice.Text = string.Empty;
            txtWeight.Text = string.Empty;
            txtDescription.Clear();
            txtCostPrice.Text = string.Empty;
            txtNotes.Clear();
            chkActive.IsChecked = false;
            imgProduct.Source = defaultImage;
            selectedImagePath = string.Empty;

            dgvItems.SelectedItem = null;
            btnClear.IsEnabled = false;
            btnUpdate.IsEnabled = false;
        }

        private void BrowseImage()
        {
            selectedImagePath = string.Empty;
            string defaultFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = defaultFolder,
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(openFileDialog.FileName);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    imgProduct.Source = image;
                    selectedImagePath = openFileDialog.FileName;
                }
                catch (Exception ex)
                {
                    Growl.Error($"Error loading image: {ex.Message}");
                    imgProduct.Source = defaultImage;
                    selectedImagePath = string.Empty;
                }
            }
        }

        private void PrepareForEdit()
        {
            isNewItem = false;
            EnableEditControls(true);
            txtSku.IsEnabled = false;
            btnSave.Content = "Save Changes";
            dgvItems.IsEnabled = false;
            txtItemName.Focus();
        }

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            BrowseImage();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadItemsToDGV();
            ClearDetails();
            EnableEditControls(false);
            EnableSearchInputs(true);
            dgvItems.IsEnabled = true;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearDetails();
        }

        private void DgInventory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvItems.SelectedItem != null)
            {
                PopulateDetailFields(dgvItems.SelectedItem);
                EnableEditControls(false);
            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dgvItems.ItemsSource == null)
            {
                Growl.Warning("Please click Refresh to load data first");
                return;
            }

            if (string.IsNullOrEmpty(txtItemId.Text))
            {
                Growl.Info("Please select an item first");
                return;
            }

            PrepareForEdit();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }

        private void BtnAddNew_Click(object sender, RoutedEventArgs e)
        {
            PrepareForNewItem();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = HandyControl.Controls.MessageBox.Show(
                "Cancel changes? Unsaved work will be lost.",
                "Confirm Cancel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                isNewItem = false;
                EnableEditControls(false);
                ClearDetails();
                dgvItems.IsEnabled = true;
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnClear.IsEnabled = !string.IsNullOrEmpty(txtSearch.Text);

            if (dgvItems.ItemsSource != null)
            {
                LoadItemsToDGV();
            }
        }

        private void CmbSearchCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnClear.IsEnabled = cmbSearchCategory.SelectedIndex > 0;

            if (dgvItems.ItemsSource != null)
            {
                LoadItemsToDGV();
            }
        }

        private void ChkActive_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chkActive.IsEnabled)
            {
                var result = HandyControl.Controls.MessageBox.Show(
                    "Are you sure you want to make this item inactive?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    chkActive.IsChecked = true;
                }
            }
        }
    }
}