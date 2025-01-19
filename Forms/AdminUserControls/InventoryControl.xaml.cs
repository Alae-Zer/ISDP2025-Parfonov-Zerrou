using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Serhii on January 18,2025
namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class InventoryControl : UserControl
    {
        //Declare Context and Global Lists for Storagin Data
        BestContext context;
        List<Site> locationsList = new List<Site>();
        List<dynamic> suppliersList = new List<dynamic>();
        List<string> categoriesList = new List<string>();

        public InventoryControl()
        {
            //Load Degfaults
            InitializeComponent();
            context = new BestContext();
            EnableSearchControls(false);
            LoadLocationsToList();
            LoadSuppliersToList();
            LoadCategoriesToList();
            LoadCategoriesToList();
            btnClear.IsEnabled = false;
            EnableInputs(false);
            dgvItems.ItemsSource = null;
        }

        //Reset Inputs
        //Send sNothing
        //Return Nothing
        private void ClearInputs()
        {
            lblItemId.Content = string.Empty;
            txtItemName.Clear();
            txtSKU.Clear();
            cmbEditCategory.SelectedIndex = -1;
            cmbEditCategory.Text = string.Empty;
            txtQuantity.Clear();
            txtWeight.Clear();
            txtCostPrice.Clear();
            txtRetailPrice.Clear();
            txtCaseSize.Clear();
            cmbLocation.SelectedIndex = 0;
            cmbSupplier.SelectedIndex = 0;
            chkActive.IsChecked = false;
            cmbSearchCategory.SelectedIndex = 0;
            cmbSearchLocation.SelectedIndex = 0;
            txtSearch.Clear();
        }

        //Populates List With Categories
        //Sends Nothing
        //Returns Nothing
        private void LoadCategoriesToList()
        {
            //QUERY
            var dbCategories = context.Items
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            //Reset List and Add Default Option
            categoriesList.Clear();
            categoriesList.Add("All Categories");

            //LOOP and ADD
            foreach (var category in dbCategories)
            {
                categoriesList.Add(category);
            }

            //Load Values To The CBOs
            cmbSearchCategory.ItemsSource = categoriesList;
            cmbSearchCategory.SelectedIndex = 0;
            //First Value Should Be Skipped as not Exists in DB
            cmbEditCategory.ItemsSource = categoriesList.Skip(1).ToList();
            cmbEditCategory.SelectedIndex = 0;
        }

        //Populate List With Locations
        //Sends Nothing
        //Returns Nothing
        // Change back to Site objects
        private void LoadLocationsToList()
        {
            //Reset List and Add Default Option
            locationsList.Clear();
            locationsList.Add(new Site
            {
                SiteId = -1,
                SiteName = "No Location Selected"
            });

            //QUERY
            var dbLocations = context.Sites
                .OrderBy(s => s.SiteName)
                .ToList();

            //LOOP and ADD
            foreach (var location in dbLocations)
            {
                locationsList.Add(location);
            }

            //Bind Values
            cmbSearchLocation.ItemsSource = locationsList;
            cmbSearchLocation.DisplayMemberPath = "SiteName";
            cmbSearchLocation.SelectedValuePath = "SiteId";
            cmbSearchLocation.SelectedIndex = 0;

            cmbLocation.ItemsSource = locationsList;
            cmbLocation.DisplayMemberPath = "SiteName";
            cmbLocation.SelectedValuePath = "SiteId";
            cmbLocation.SelectedIndex = 0;
        }

        //Populates List With Suppliers
        //Sends Nothing
        //Returns Nothing
        //Populates List With Suppliers
        //Sends Nothing
        //Returns Nothing
        private void LoadSuppliersToList()
        {

            //Add Default First Option
            suppliersList.Add(new Supplier
            {
                SupplierId = -1,
                Name = "No Supplier Selected",
                Active = 1
            });

            //QUERY To Get All Active Suppliers
            var dbSuppliers = context.Suppliers
                .Where(s => s.Active == 1)
                .OrderBy(s => s.Name)
                .ToList();

            //Loop Through Each Supplier and Add to List
            foreach (var supplier in dbSuppliers)
            {
                suppliersList.Add(supplier);
            }

            //Set Up ComboBox
            cmbSupplier.ItemsSource = suppliersList;
            cmbSupplier.DisplayMemberPath = "Name";
            cmbSupplier.SelectedValuePath = "SupplierId";
            cmbSupplier.SelectedIndex = 0;
        }

        //Populates DATAGRID with items
        //Sends Nothing
        //Returns Nothing
        private void LoadItems()
        {
            //QUERY
            var inventory = context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Site)
                .Include(i => i.Item.Supplier)
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
                    i.Item.SupplierId,
                    SupplierName = i.Item.Supplier.Name,
                    Active = i.Item.Active == 1 ? "Yes" : "No"
                })
                .ToList();

            //Bind Sources
            dgvItems.ItemsSource = inventory;
        }

        //Change State Of Inputs
        //Sends Nothing
        //Returns BOOL
        private void EnableInputs(bool isEnabled)
        {
            txtItemName.IsEnabled = isEnabled;
            txtSKU.IsEnabled = isEnabled;
            cmbEditCategory.IsEnabled = isEnabled;
            txtQuantity.IsEnabled = isEnabled;
            txtWeight.IsEnabled = isEnabled;
            txtCostPrice.IsEnabled = isEnabled;
            txtRetailPrice.IsEnabled = isEnabled;
            txtCaseSize.IsEnabled = isEnabled;
            cmbLocation.IsEnabled = isEnabled;
            chkActive.IsEnabled = isEnabled;
            cmbSupplier.IsEnabled = isEnabled;
        }

        //Change State Of Inpugts
        //Sends Nothing
        //Returns BOOL
        private void EnableSearchControls(bool isEnabled)
        {
            txtSearch.IsEnabled = isEnabled;
            cmbSearchCategory.IsEnabled = isEnabled;
            cmbSearchLocation.IsEnabled = isEnabled;
            dgvItems.IsEnabled = isEnabled;
        }

        //Verifies That All Required Fields Are Populated
        //Sends Nothinf
        //Returns BOOL
        private bool CheckInputs()
        {
            if (string.IsNullOrWhiteSpace(txtItemName.Text) ||
                string.IsNullOrWhiteSpace(txtSKU.Text) ||
                string.IsNullOrWhiteSpace(cmbEditCategory.Text) ||
                !decimal.TryParse(txtQuantity.Text, out _) ||
                !decimal.TryParse(txtWeight.Text, out _) ||
                !decimal.TryParse(txtCostPrice.Text, out _) ||
                !decimal.TryParse(txtRetailPrice.Text, out _) ||
                !int.TryParse(txtCaseSize.Text, out _) ||
                (int)cmbLocation.SelectedValue == -1 ||
                cmbSupplier.SelectedValue == null)
            {
                return false;
            }
            return true;
        }

        private void SaveNewItem()
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

            var inventory = new Inventory
            {
                ItemId = item.ItemId,
                SiteId = (int)cmbLocation.SelectedValue,
                Quantity = (int)decimal.Parse(txtQuantity.Text)
            };

            context.Inventories.Add(inventory);
        }

        private void UpdateExistingItem()
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

            var inventory = context.Inventories
                .FirstOrDefault(i => i.ItemId == itemId && i.SiteId == (int)cmbLocation.SelectedValue);

            if (inventory != null)
            {
                inventory.Quantity = (int)decimal.Parse(txtQuantity.Text);
            }
            else
            {
                inventory = new Inventory
                {
                    ItemId = itemId,
                    SiteId = (int)cmbLocation.SelectedValue,
                    Quantity = (int)decimal.Parse(txtQuantity.Text)
                };
                context.Inventories.Add(inventory);
            }
        }

        //Dynamic Filters
        //Sends Nothing
        //Returns Nothing
        private void ApplyFilters()
        {
            //Always Start With Fresh Data
            var query = context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Site)
                .Include(i => i.Item.Supplier)
                .AsQueryable();

            //Filter By Input - Apply Even If Empty
            string searchText = txtSearch.Text.ToLower();
            query = query.Where(i => i.Item.Name.ToLower().Contains(searchText));

            //Sort By Categories
            if (cmbSearchCategory.SelectedIndex > 0)
            {
                string category = cmbSearchCategory.SelectedItem.ToString();
                query = query.Where(i => i.Item.Category == category);
            }

            //Sort By Location
            if (cmbSearchLocation.SelectedIndex > 0)
            {
                int selectedSiteId = (int)cmbSearchLocation.SelectedValue;
                query = query.Where(i => i.SiteId == selectedSiteId);
            }

            //Create Result
            var result = query.Select(i => new
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
                i.Item.SupplierId,
                SupplierName = i.Item.Supplier.Name,
                Active = i.Item.Active == 1 ? "Yes" : "No"
            }).ToList();

            //Update DGV
            dgvItems.ItemsSource = result;
        }

        //Populates Inputs Based On Selected Item
        //Sends Nothing
        //Returns Nothing
        private void PopulateAddEditInputs()
        {
            if (dgvItems.Items.Count > 0 && dgvItems.SelectedItem != null)
            {
                //IDK how it works, Found Idea In Internet, But Works!!!
                dynamic selectedItem = dgvItems.SelectedItem;

                try
                {
                    //Populate Inputs Wioth Item From DataGrid
                    lblItemId.Content = selectedItem.ItemId.ToString();
                    txtItemName.Text = selectedItem.ItemName;
                    txtSKU.Text = selectedItem.Sku;
                    cmbEditCategory.Text = selectedItem.Category;
                    txtQuantity.Text = selectedItem.Quantity.ToString();
                    txtWeight.Text = selectedItem.Weight.ToString();
                    txtCostPrice.Text = selectedItem.CostPrice.ToString();
                    txtRetailPrice.Text = selectedItem.RetailPrice.ToString();
                    txtCaseSize.Text = selectedItem.CaseSize.ToString();
                    cmbLocation.SelectedValue = selectedItem.SiteId;
                    cmbSupplier.SelectedValue = selectedItem.SupplierId;
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

        //Saves Items When Changed or Added
        //Sends Nothing
        //Returns Nothing
        private void SaveChanges()
        {
            //Dialog Opened
            MessageBoxResult result = MessageBox.Show("The Item Will Be Added/Updated\n YES - Save Item\n " +
                "NO - Return To previous Page\n CANCEL - Continue Editing", "Confirmation Needed",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (!CheckInputs())
                {
                    MessageBox.Show("Please fill all required fields with valid values", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
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

                    context.SaveChanges();
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

        //Changes Buttons If DGV Populated
        //Sends Nothing
        //Returns Nothing
        private void ChanfeButtons()
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

        //Changes UI to Default condition
        //Sends Nothing
        //Returns Nothing
        private void DefaultLayout()
        {
            ClearInputs();
            EnableInputs(false);
            EnableSearchControls(true);

            //Hide Buttons
            btnSave.Visibility = Visibility.Collapsed;
            btnAdd.Visibility = Visibility.Visible;
            btnUpdate.Visibility = Visibility.Visible;
            btnUpdate.IsEnabled = false;
            btnClear.Visibility = Visibility.Visible;

            //Blank Searh 
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
            ChanfeButtons();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            ChanfeButtons();
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

        private void txtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

        }
    }
}