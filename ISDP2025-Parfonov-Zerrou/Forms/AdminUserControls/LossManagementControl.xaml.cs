using HandyControl.Controls;
using HandyControl.Data;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
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
        string perm;

        public InventoryAdjustmentControl(Employee employee, string permission)
        {
            InitializeComponent();
            currentUser = employee;
            btnSubmit.IsEnabled = false;
            perm = permission;
        }

        private void SetUserStore()
        {
            try
            {
                using (var context = new BestContext())
                {
                    // Get the user's site
                    var userSite = context.Sites
                        .FirstOrDefault(s => s.SiteId == currentUser.SiteId);

                    if (userSite != null)
                    {
                        // Create a list with just the user's site
                        List<Site> userSiteList = new List<Site> { userSite };
                        cboStores.ItemsSource = userSiteList;
                        cboStores.DisplayMemberPath = "SiteName";
                        cboStores.SelectedValuePath = "SiteId";
                        cboStores.SelectedIndex = 0;
                    }
                }

                LoadInventoryData();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading store: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateStoresComboBox()
        {
            try
            {
                using (var context = new BestContext())
                {
                    // Get all valid store sites (excluding non-stores)
                    var sites = context.Sites
                        .Where(s => s.Active == 1 && !notStores.Contains(s.SiteId))
                        .OrderBy(s => s.SiteName)
                        .ToList();

                    cboStores.ItemsSource = sites;
                    cboStores.DisplayMemberPath = "SiteName";
                    cboStores.SelectedValuePath = "SiteId";

                    // Default to the current user's site
                    var userSite = sites.FirstOrDefault(s => s.SiteId == currentUser.SiteId);
                    if (userSite != null)
                    {
                        cboStores.SelectedValue = userSite.SiteId;
                    }
                    else if (sites.Count > 0)
                    {
                        cboStores.SelectedIndex = 0;
                    }
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
                if (cboStores.SelectedValue == null)
                    return;

                int siteId = (int)cboStores.SelectedValue;

                using (var context = new BestContext())
                {
                    if (currentAdjustmentType == "Return")
                    {
                        // For returns, show all active items regardless of inventory level
                        var items = context.Inventories
                            .Include(inv => inv.Item)
                            .Where(inv => inv.SiteId == siteId && inv.Item.Active == 1)
                            .Select(inv => new InventoryViewModel
                            {
                                ItemId = inv.ItemId,
                                Name = inv.Item.Name,
                                CurrentStock = inv.Quantity,
                                SiteId = siteId
                            })
                            .OrderBy(i => i.Name)
                            .ToList();

                        inventoryItems = items;
                    }
                    else
                    {
                        // For loss/damage, only show items with positive inventory
                        var items = context.Inventories
                            .Include(inv => inv.Item)
                            .Where(inv => inv.SiteId == siteId && inv.Quantity > 0 && inv.Item.Active == 1)
                            .Select(inv => new InventoryViewModel
                            {
                                ItemId = inv.ItemId,
                                Name = inv.Item.Name,
                                CurrentStock = inv.Quantity,
                                SiteId = siteId
                            })
                            .OrderBy(i => i.Name)
                            .ToList();

                        inventoryItems = items;
                    }

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

            // Check if the UI elements are initialized before accessing them
            if (txtSelectedItem != null)
            {
                txtSelectedItem.Text = "No item selected";
            }

            if (numQuantity != null)
            {
                numQuantity.Value = 1;
            }

            if (txtNotes != null)
            {
                txtNotes.Text = string.Empty;
            }

            if (btnSubmit != null)
            {
                btnSubmit.IsEnabled = false;
            }

            // Update UI based on type
            if (currentAdjustmentType == "Return" && chkGoodCondition != null)
            {
                chkGoodCondition.Visibility = Visibility.Visible;
                chkGoodCondition.IsChecked = true;
            }
            else if (chkGoodCondition != null)
            {
                chkGoodCondition.Visibility = Visibility.Collapsed;
                chkGoodCondition.IsChecked = false;
            }

            // Then load new data (this will reset the grid)
            LoadInventoryData();

            // After loading data, ensure nothing is selected
            if (dgvInventoryItems != null && dgvInventoryItems.Items != null)
            {
                dgvInventoryItems.UnselectAll();
            }
        }

        private void cboStores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadInventoryData();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Only admins can select different stores
            if (perm == "Administrator")
            {
                cboStores.IsEnabled = true;
                PopulateStoresComboBox();
            }
            else
            {
                // Non-admins can only work with their locations
                cboStores.IsEnabled = false;
                SetUserStore();
            }
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

                // Get the transaction type based on selected radio button
                string txnType;
                if (rbLoss.IsChecked == true)
                    txnType = "Loss";
                else if (rbDamage.IsChecked == true)
                    txnType = "Damage";
                else if (rbReturn.IsChecked == true)
                    txnType = "Return";
                else
                    return; // No valid selection

                // Handle returns in bad condition
                if (txnType == "Return" && !(chkGoodCondition.IsChecked == true))
                {
                    var result = HandyControl.Controls.MessageBox.Show(
                        "Item is not in good condition for resale. Would you like to create a LOSS record instead?",
                        "Item Condition",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                        txnType = "Loss";
                    else
                        return;
                }

                using (var context = new BestContext())
                {
                    // Create transaction record
                    var txn = new Txn
                    {
                        EmployeeId = currentUser.EmployeeID,
                        SiteIdto = selectedItem.SiteId,
                        SiteIdfrom = selectedItem.SiteId,
                        TxnStatus = "COMPLETE",
                        ShipDate = DateTime.Now,
                        TxnType = txnType,
                        BarCode = $"{txnType}-{DateTime.Now:yyyyMMddHHmmss}",
                        CreatedDate = DateTime.Now,
                        Notes = txtNotes.Text
                    };

                    context.Txns.Add(txn);
                    context.SaveChanges();

                    // Create transaction item record
                    var txnItem = new Txnitem
                    {
                        TxnId = txn.TxnId,
                        ItemId = selectedItem.ItemId,
                        Quantity = quantity,
                        Notes = txtNotes.Text
                    };

                    context.Txnitems.Add(txnItem);

                    // Update inventory
                    var inventory = context.Inventories
                        .FirstOrDefault(i => i.ItemId == selectedItem.ItemId && i.SiteId == selectedItem.SiteId);

                    if (inventory != null)
                    {
                        // Update quantity based on transaction type
                        if (txnType == "Loss" || txnType == "Damage")
                            inventory.Quantity -= quantity;
                        else if (txnType == "Return")
                            inventory.Quantity += quantity;

                        context.SaveChanges();

                        // Create audit record
                        AuditTransactions.LogActivity(
                            currentUser,
                            txn.TxnId,
                            txnType,
                            "COMPLETE",
                            selectedItem.SiteId,
                            null,
                            $"{(txnType == "Return" ? "Processed return" : "Recorded")} of {quantity} units of {selectedItem.Name}. Reason: {txtNotes.Text}"
                        );

                        Growl.Success(new GrowlInfo
                        {
                            Message = $"{txnType} recorded successfully",
                            ShowDateTime = false,
                            WaitTime = 3
                        });

                        // Reset form
                        txtSelectedItem.Text = "No item selected";
                        numQuantity.Value = 1;
                        txtNotes.Text = string.Empty;
                        btnSubmit.IsEnabled = false;
                        dgvInventoryItems.SelectedItem = null;
                        LoadInventoryData();
                    }
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(
                    $"Error creating record: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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