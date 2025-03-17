using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class ModifyRecord : UserControl
    {
        private BestContext context;
        private Employee currentUser;
        private bool isEditMode = false;
        private Txn currentTransaction = null;

        public ModifyRecord(Employee employee)
        {
            InitializeComponent();
            currentUser = employee;
            context = new BestContext();

            // Initialize combo boxes
            LoadTxnStatusComboBox();
            LoadTxnTypeComboBox();
            LoadSitesToComboBox();
            LoadDeliveryIdsComboBox();
        }

        private void LoadTxnStatusComboBox()
        {
            try
            {
                var statuses = context.Txnstatuses
                    .Where(s => s.Active == 1)
                    .OrderBy(s => s.StatusName)
                    .ToList();

                cmbTxnStatus.ItemsSource = statuses;
                cmbEditTxnStatus.ItemsSource = statuses;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error loading transaction statuses: {ex.Message}");
            }
        }

        private void LoadTxnTypeComboBox()
        {
            try
            {
                var types = context.Txntypes
                    .Where(t => t.Active == 1)
                    .OrderBy(t => t.TxnType1)
                    .ToList();

                cmbTxnType.ItemsSource = types;
                cmbEditTxnType.ItemsSource = types;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error loading transaction types: {ex.Message}");
            }
        }

        private void LoadSitesToComboBox()
        {
            try
            {
                var sites = context.Sites
                    .Where(s => s.Active == 1)
                    .OrderBy(s => s.SiteName)
                    .Select(s => new { s.SiteId, s.SiteName })
                    .ToList();

                cmbEditSiteTo.ItemsSource = sites;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error loading sites: {ex.Message}");
            }
        }

        private void LoadDeliveryIdsComboBox()
        {
            try
            {
                var deliveries = context.Deliveries
                    .OrderByDescending(d => d.DeliveryDate)
                    .Take(100) // Limit to recent deliveries
                    .ToList();

                // Add a null option for no delivery
                var deliveryList = new List<Delivery>
                {
                    new Delivery { DeliveryId = 0, DeliveryDate = DateTime.Now, Notes = "No Delivery" }
                };
                deliveryList.AddRange(deliveries);

                cmbDeliveryId.ItemsSource = deliveryList;
            }
            catch (Exception ex)
            {
                Growl.Error($"Error loading deliveries: {ex.Message}");
            }
        }

        private void SearchTransactions()
        {
            try
            {
                // Start with base query
                var query = context.Txns
                    .Include(t => t.Employee)
                    .Include(t => t.SiteIdfromNavigation)
                    .Include(t => t.SiteIdtoNavigation)
                    .AsQueryable();

                // Filter by transaction ID if provided
                if (!string.IsNullOrWhiteSpace(txtSearchTxnId.Text) && int.TryParse(txtSearchTxnId.Text, out int txnId))
                {
                    query = query.Where(t => t.TxnId == txnId);
                }

                // Filter by status if selected
                if (cmbTxnStatus.SelectedValue != null)
                {
                    string status = cmbTxnStatus.SelectedValue.ToString();
                    query = query.Where(t => t.TxnStatus == status);
                }

                // Filter by type if selected
                if (cmbTxnType.SelectedValue != null)
                {
                    string type = cmbTxnType.SelectedValue.ToString();
                    query = query.Where(t => t.TxnType == type);
                }

                // Exclude completed/closed transactions
                var closedStatuses = new[] { "COMPLETE", "CLOSED", "CANCELLED", "REJECTED" };
                query = query.Where(t => !closedStatuses.Contains(t.TxnStatus));

                // Execute query and format results for display
                var transactions = query.Select(t => new
                {
                    t.TxnId,
                    t.TxnType,
                    t.TxnStatus,
                    SiteFrom = t.SiteIdfromNavigation.SiteName,
                    SiteTo = t.SiteIdtoNavigation.SiteName,
                    t.ShipDate,
                    t.CreatedDate,
                    EmployeeName = t.Employee.FirstName + " " + t.Employee.LastName,
                    EmergencyDelivery = t.EmergencyDelivery == 1 ? "Yes" : "No",
                    t.Notes
                })
                .OrderByDescending(t => t.CreatedDate)
                .ToList();

                dgvTransactions.ItemsSource = transactions;
                lblRecordCount.Text = $"Found {transactions.Count} transaction(s)";
            }
            catch (Exception ex)
            {
                Growl.Error($"Error searching transactions: {ex.Message}");
            }
        }

        private void LoadTransactionDetails(int txnId)
        {
            try
            {
                currentTransaction = context.Txns
                    .Include(t => t.Employee)
                    .Include(t => t.SiteIdfromNavigation)
                    .Include(t => t.SiteIdtoNavigation)
                    .Include(t => t.Delivery)
                    .FirstOrDefault(t => t.TxnId == txnId);

                if (currentTransaction != null)
                {
                    // Display transaction details
                    txtTxnId.Text = currentTransaction.TxnId.ToString();
                    txtEmployeeName.Text = $"{currentTransaction.Employee.FirstName} {currentTransaction.Employee.LastName}";
                    cmbEditTxnType.SelectedValue = currentTransaction.TxnType;
                    cmbEditTxnStatus.SelectedValue = currentTransaction.TxnStatus;
                    txtSiteFrom.Text = currentTransaction.SiteIdfromNavigation.SiteName;
                    cmbEditSiteTo.SelectedValue = currentTransaction.SiteIdto;
                    dpShipDate.SelectedDate = currentTransaction.ShipDate;
                    txtBarCode.Text = currentTransaction.BarCode;

                    // Handle nullable fields
                    if (currentTransaction.DeliveryId.HasValue)
                    {
                        cmbDeliveryId.SelectedValue = currentTransaction.DeliveryId;
                    }
                    else
                    {
                        cmbDeliveryId.SelectedValue = 0; // No delivery
                    }

                    chkEmergencyDelivery.IsChecked = currentTransaction.EmergencyDelivery == 1;
                    txtNotes.Text = currentTransaction.Notes;

                    // Enable edit button
                    btnEdit.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Growl.Error($"Error loading transaction details: {ex.Message}");
            }
        }

        private void EnterEditMode()
        {
            isEditMode = true;
            gridTransactionDetails.IsEnabled = true;
            btnEdit.Visibility = Visibility.Collapsed;
            btnSave.Visibility = Visibility.Visible;
            btnCancel.Visibility = Visibility.Visible;
            dgvTransactions.IsEnabled = false;
        }

        private void ExitEditMode()
        {
            isEditMode = false;
            gridTransactionDetails.IsEnabled = false;
            btnEdit.Visibility = Visibility.Visible;
            btnSave.Visibility = Visibility.Collapsed;
            btnCancel.Visibility = Visibility.Collapsed;
            dgvTransactions.IsEnabled = true;
        }

        private bool ValidateTransaction()
        {
            if (cmbEditTxnType.SelectedValue == null)
            {
                Growl.Warning("Please select a transaction type");
                return false;
            }

            if (cmbEditTxnStatus.SelectedValue == null)
            {
                Growl.Warning("Please select a transaction status");
                return false;
            }

            if (cmbEditSiteTo.SelectedValue == null)
            {
                Growl.Warning("Please select a destination site");
                return false;
            }

            if (dpShipDate.SelectedDate == null)
            {
                Growl.Warning("Please select a ship date");
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtBarCode.Text))
            {
                Growl.Warning("Please enter a barcode");
                return false;
            }

            return true;
        }

        private void SaveChanges()
        {
            try
            {
                if (!ValidateTransaction())
                {
                    return;
                }

                string oldStatus = currentTransaction.TxnStatus;
                string newStatus = cmbEditTxnStatus.SelectedValue.ToString();
                bool isCancelling = newStatus == "CANCELLED" && oldStatus != "CANCELLED";

                // Update transaction fields
                currentTransaction.TxnType = cmbEditTxnType.SelectedValue.ToString();
                currentTransaction.TxnStatus = newStatus;
                currentTransaction.SiteIdto = (int)cmbEditSiteTo.SelectedValue;
                currentTransaction.ShipDate = dpShipDate.SelectedDate.Value;
                currentTransaction.BarCode = txtBarCode.Text;

                // Handle nullable delivery ID
                if (cmbDeliveryId.SelectedValue != null && (int)cmbDeliveryId.SelectedValue != 0)
                {
                    currentTransaction.DeliveryId = (int)cmbDeliveryId.SelectedValue;
                }
                else
                {
                    currentTransaction.DeliveryId = null;
                }

                currentTransaction.EmergencyDelivery = (sbyte)(chkEmergencyDelivery.IsChecked == true ? 1 : 0);
                currentTransaction.Notes = txtNotes.Text;

                // Save changes
                context.SaveChanges();

                // If cancelling, handle inventory return
                if (isCancelling)
                {
                    HandleCancelledTransaction(currentTransaction);
                }

                // Log the activity
                AuditTransactions.LogActivity(
                    currentUser,
                    currentTransaction.TxnId,
                    currentTransaction.TxnType,
                    currentTransaction.TxnStatus,
                    currentTransaction.SiteIdto,
                    currentTransaction.DeliveryId,
                    $"Transaction modified by {currentUser.FirstName} {currentUser.LastName}"
                );

                // Refresh the UI
                ExitEditMode();
                SearchTransactions();
                Growl.Success("Transaction updated successfully");
            }
            catch (Exception ex)
            {
                Growl.Error($"Error saving changes: {ex.Message}");
            }
        }

        private void HandleCancelledTransaction(Txn transaction)
        {
            try
            {
                // Get the transaction items
                var txnItems = context.Txnitems
                    .Where(ti => ti.TxnId == transaction.TxnId)
                    .ToList();

                if (!txnItems.Any())
                {
                    return; // No items to return
                }

                // For each item, return inventory to the source
                foreach (var item in txnItems)
                {
                    // Determine current item location
                    // - If status is before ASSEMBLED, inventory is still at source
                    // - If status is ASSEMBLED, inventory is at warehouse bay
                    // - If status is IN TRANSIT, inventory is on truck
                    // - If status is DELIVERED, inventory is at destination

                    int fromSiteId;
                    int toSiteId;

                    switch (transaction.TxnStatus)
                    {
                        case "NEW":
                        case "SUBMITTED":
                        case "RECEIVED":
                            // Inventory not yet moved
                            continue; // Skip - no inventory movement needed
                        case "ASSEMBLING":
                            // Move from warehouse bay to warehouse
                            fromSiteId = 3; // Warehouse bay
                            toSiteId = 2;   // Warehouse
                            break;
                        case "ASSEMBLED":
                            // Move from warehouse bay to warehouse
                            fromSiteId = 3; // Warehouse bay
                            toSiteId = 2;   // Warehouse
                            break;
                        case "IN TRANSIT":
                            // Move from truck to warehouse
                            fromSiteId = 9999; // Truck
                            toSiteId = 2;      // Warehouse
                            break;
                        case "DELIVERED":
                            // Move from destination back to warehouse
                            fromSiteId = transaction.SiteIdto;
                            toSiteId = 2; // Warehouse
                            break;
                        default:
                            continue; // Skip for other statuses
                    }

                    // Use MoveInventory to return the items
                    bool success = MoveInventory.Move(
                        item.ItemId,
                        item.Quantity,
                        fromSiteId,
                        toSiteId
                    );

                    if (!success)
                    {
                        Growl.Warning($"Could not return item {item.ItemId} to inventory");
                    }
                }

                // Log the inventory return
                AuditTransactions.LogActivity(
                    currentUser,
                    transaction.TxnId,
                    transaction.TxnType,
                    "CANCELLED",
                    transaction.SiteIdto,
                    transaction.DeliveryId,
                    $"Transaction cancelled and inventory returned by {currentUser.FirstName} {currentUser.LastName}"
                );
            }
            catch (Exception ex)
            {
                Growl.Error($"Error handling cancelled transaction: {ex.Message}");
            }
        }

        #region Event Handlers
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTransactions();
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearchTxnId.Clear();
            cmbTxnStatus.SelectedIndex = 0;
            cmbTxnType.SelectedIndex = -1;
            dgvTransactions.ItemsSource = null;
            lblRecordCount.Text = string.Empty;
            btnEdit.IsEnabled = false;
        }

        private void DgvTransactions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvTransactions.SelectedItem != null && !isEditMode)
            {
                dynamic selectedItem = dgvTransactions.SelectedItem;
                int txnId = selectedItem.TxnId;
                LoadTransactionDetails(txnId);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (currentTransaction != null)
            {
                EnterEditMode();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentTransaction != null)
            {
                SaveChanges();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (isEditMode)
            {
                // Confirm cancellation
                Growl.Ask("Are you sure you want to cancel editing?", isConfirmed =>
                {
                    if (isConfirmed)
                    {
                        // Reload transaction details to reset any changes
                        LoadTransactionDetails(currentTransaction.TxnId);
                        ExitEditMode();
                    }
                    return true;
                });
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (context != null)
            {
                context.Dispose();
            }
        }
        #endregion
    }
}
