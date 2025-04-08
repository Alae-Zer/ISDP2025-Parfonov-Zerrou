using HandyControl.Controls;
using HandyControl.Data;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.InventoryControls
{
    public partial class InventoryAdjustmentControl : UserControl
    {
        Employee currentUser;
        List<InventoryViewModel> inventoryItems;
        int[] notStores = { 1, 3, 9999, 10000 };
        private string currentAdjustmentType = "Loss";

        public InventoryAdjustmentControl(Employee employee, string permission)
        {
            InitializeComponent();
            currentUser = employee;
            btnSubmit.IsEnabled = false;

            // Simple permission check - admin can select stores, others can't
            if (permission == "Administrator")
            {
                cboStores.IsEnabled = true;
            }
            else
            {
                cboStores.IsEnabled = false;
            }

            PopulateStoresComboBox();
        }

        private void PopulateStoresComboBox()
        {
            try
            {
                List<Site> allSites = new List<Site>();

                using (var context = new BestContext())
                {
                    // Add "All Stores" option for admins
                    allSites.Add(new Site { SiteId = 0, SiteName = "All Stores" });

                    // Add all active stores
                    var sites = context.Sites
                        .Where(s => s.Active == 1 && !notStores.Contains(s.SiteId))
                        .OrderBy(s => s.SiteName)
                        .Select(s => new Site { SiteId = s.SiteId, SiteName = s.SiteName })
                        .ToList();

                    allSites.AddRange(sites);
                }

                cboStores.ItemsSource = allSites;
                cboStores.DisplayMemberPath = "SiteName";
                cboStores.SelectedValuePath = "SiteId";

                // For non-admins, select their site
                if (!cboStores.IsEnabled)
                {
                    // Find user's site in the list
                    var userSite = allSites.FirstOrDefault(s => s.SiteId == currentUser.SiteId);
                    if (userSite != null)
                    {
                        cboStores.SelectedValue = userSite.SiteId;
                    }
                }
                else
                {
                    // For admins, select "All Stores" by default
                    cboStores.SelectedIndex = 0;
                }

                LoadInventoryData();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading stores: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadInventoryData()
        {
            try
            {
                int selectedSiteId = 0;
                if (cboStores.SelectedItem is Site selectedSite)
                {
                    selectedSiteId = selectedSite.SiteId;
                }

                using (var context = new BestContext())
                {
                    IQueryable<InventoryViewModel> query;

                    if (currentAdjustmentType == "Return")
                    {
                        // For returns, show all items regardless of inventory
                        query = from i in context.Items
                                join inv in context.Inventories
                                    on new { ItemId = i.ItemId, SiteId = selectedSiteId == 0 ? currentUser.SiteId : selectedSiteId }
                                    equals new { ItemId = inv.ItemId, SiteId = inv.SiteId } into invJoin
                                from inventory in invJoin.DefaultIfEmpty()
                                where i.Active == 1
                                select new InventoryViewModel
                                {
                                    ItemId = i.ItemId,
                                    Name = i.Name,
                                    CurrentStock = inventory != null ? inventory.Quantity : 0,
                                    SiteId = selectedSiteId == 0 ? currentUser.SiteId : selectedSiteId
                                };
                    }
                    else
                    {
                        // For loss/damage, only show items with inventory
                        query = from inv in context.Inventories
                                join i in context.Items on inv.ItemId equals i.ItemId
                                where (selectedSiteId == 0 || inv.SiteId == selectedSiteId)
                                      && inv.Quantity > 0
                                      && i.Active == 1
                                select new InventoryViewModel
                                {
                                    ItemId = inv.ItemId,
                                    Name = i.Name,
                                    CurrentStock = inv.Quantity,
                                    SiteId = inv.SiteId
                                };
                    }

                    inventoryItems = query.ToList();
                    dgvInventoryItems.ItemsSource = inventoryItems;
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading inventory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void rbType_Checked(object sender, RoutedEventArgs e)
        {
            if (rbLoss.IsChecked == true)
                currentAdjustmentType = "Loss";
            else if (rbDamage.IsChecked == true)
                currentAdjustmentType = "Damage";
            else if (rbReturn.IsChecked == true)
                currentAdjustmentType = "Return";

            LoadInventoryData();
            UpdateFormBasedOnType();
        }

        private void UpdateFormBasedOnType()
        {
            // Clear selection when changing type
            dgvInventoryItems.SelectedItem = null;
            txtSelectedItem.Text = "No item selected";

            // Reset form
            numQuantity.Value = 1;
            txtNotes.Text = string.Empty;
            btnSubmit.IsEnabled = false;

            // Update UI based on type
            if (currentAdjustmentType == "Return")
            {
                chkGoodCondition.Visibility = Visibility.Visible;
                chkGoodCondition.IsChecked = true;
            }
            else
            {
                chkGoodCondition.Visibility = Visibility.Collapsed;
                chkGoodCondition.IsChecked = false;
            }
        }

        private void cboStores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadInventoryData();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadInventoryData();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.ToLower();
            if (inventoryItems != null)
            {
                var filteredItems = inventoryItems
                    .Where(i => i.Name.ToLower().Contains(searchText) ||
                               i.ItemId.ToString().Contains(searchText))
                    .ToList();
                dgvInventoryItems.ItemsSource = filteredItems;
            }
        }

        private void dgvInventoryItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvInventoryItems.SelectedItem is InventoryViewModel selectedItem)
            {
                txtSelectedItem.Text = $"{selectedItem.ItemId} - {selectedItem.Name}";

                // Set maximum for Loss/Damage based on current stock
                if (currentAdjustmentType == "Loss" || currentAdjustmentType == "Damage")
                {
                    numQuantity.Maximum = selectedItem.CurrentStock;
                    numQuantity.Value = Math.Min(1, selectedItem.CurrentStock);
                }
                else
                {
                    // For returns, no maximum necessary
                    numQuantity.Maximum = 999;
                    numQuantity.Value = 1;
                }

                btnSubmit.IsEnabled = true;
            }
            else
            {
                btnSubmit.IsEnabled = false;
            }
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (!(dgvInventoryItems.SelectedItem is InventoryViewModel selectedItem))
                return;

            try
            {
                int quantity = (int)numQuantity.Value;

                // Validate for returns
                if (currentAdjustmentType == "Return" && !(chkGoodCondition.IsChecked == true))
                {
                    // For returns in bad condition, prompt to create a loss instead
                    var result = HandyControl.Controls.MessageBox.Show(
                        "Item is not in good condition for resale. Would you like to create a LOSS record instead?",
                        "Item Condition",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Create a loss record instead
                        currentAdjustmentType = "Loss";
                        rbLoss.IsChecked = true;
                    }
                    else
                    {
                        return; // User cancelled
                    }
                }

                using (var context = new BestContext())
                {
                    // Create transaction record
                    var txn = new Txn
                    {
                        EmployeeId = currentUser.EmployeeID,
                        TxnType = currentAdjustmentType,
                        TxnStatus = "COMPLETE",
                        ShipDate = DateTime.Now,
                        BarCode = GenerateBarcode(),
                        CreatedDate = DateTime.Now,
                        Notes = txtNotes.Text,
                    };

                    // Set source/destination based on adjustment type
                    if (currentAdjustmentType == "Return")
                    {
                        // For return, we're adding to the store's inventory
                        txn.SiteIdto = selectedItem.SiteId;
                        txn.SiteIdfrom = 10000; // System source for a return
                    }
                    else // Loss or Damage
                    {
                        // For loss/damage, we're removing from the store's inventory
                        txn.SiteIdfrom = selectedItem.SiteId;
                        txn.SiteIdto = 10000; // Disposed to system
                    }

                    context.Txns.Add(txn);
                    context.SaveChanges();

                    // Create transaction item
                    var txnItem = new Txnitem
                    {
                        TxnId = txn.TxnId,
                        ItemId = selectedItem.ItemId,
                        Quantity = quantity
                    };

                    context.Txnitems.Add(txnItem);
                    context.SaveChanges();

                    bool success = false;

                    // Update inventory
                    if (currentAdjustmentType == "Loss" || currentAdjustmentType == "Damage")
                    {
                        // For loss/damage, decrement inventory
                        var inventory = context.Inventories.FirstOrDefault(i =>
                            i.ItemId == selectedItem.ItemId && i.SiteId == selectedItem.SiteId);

                        if (inventory != null)
                        {
                            inventory.Quantity -= quantity;
                            success = true;
                        }
                    }
                    else // Return
                    {
                        // For return, increment inventory
                        var inventory = context.Inventories.FirstOrDefault(i =>
                            i.ItemId == selectedItem.ItemId && i.SiteId == selectedItem.SiteId);

                        if (inventory != null)
                        {
                            inventory.Quantity += quantity;
                            success = true;
                        }
                        else
                        {
                            // Create new inventory entry if one doesn't exist
                            var newInventory = new Inventory
                            {
                                ItemId = selectedItem.ItemId,
                                SiteId = selectedItem.SiteId,
                                ItemLocation = "Stock",
                                Quantity = quantity,
                                OptimumThreshold = 5,
                                ReorderThreshold = 2
                            };
                            context.Inventories.Add(newInventory);
                            success = true;
                        }
                    }

                    if (success)
                    {
                        context.SaveChanges();
                    }
                    else
                    {
                        HandyControl.Controls.MessageBox.Show("Failed to update inventory", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Create audit trail
                    string auditMessage = "";
                    if (currentAdjustmentType == "Return")
                    {
                        auditMessage = $"Processed return of {quantity} units of {selectedItem.Name}";
                    }
                    else
                    {
                        auditMessage = $"Recorded {quantity} units of {selectedItem.Name} as {currentAdjustmentType}";
                    }

                    AuditTransactions.LogActivity(
                        currentUser,
                        txn.TxnId,
                        currentAdjustmentType,
                        "COMPLETE",
                        selectedItem.SiteId,
                        null,
                        auditMessage
                    );

                    Growl.Success(new GrowlInfo
                    {
                        Message = $"{currentAdjustmentType} record created successfully",
                        ShowDateTime = false,
                        WaitTime = 3
                    });

                    // Reset form and refresh data
                    txtSelectedItem.Text = "No item selected";
                    numQuantity.Value = 1;
                    txtNotes.Text = string.Empty;
                    btnSubmit.IsEnabled = false;
                    dgvInventoryItems.SelectedItem = null;
                    chkGoodCondition.IsChecked = true;
                    LoadInventoryData();
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error creating record: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateBarcode()
        {
            // Generate a unique barcode format: [TYPE]-[DATE/TIME]-[RANDOM]
            string type = currentAdjustmentType.Substring(0, 1).ToUpper(); // L, D, or R
            string datetime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string random = new Random().Next(1000, 9999).ToString();

            return $"{type}{datetime}{random}";
        }
    }

    public class InventoryViewModel
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public int CurrentStock { get; set; }
        public int SiteId { get; set; }
    }
}