using HandyControl.Controls;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class ModifyRecord : UserControl
    {
        private BestContext context;
        private Employee currentUser;
        private bool isEditMode = false;
        private Txn currentTransaction = null;
        private bool isInitializing = true;

        public ModifyRecord(Employee employee)
        {
            InitializeComponent();
            currentUser = employee;
            context = new BestContext();

            isInitializing = true;
            LoadTxnStatusComboBox();
            LoadTxnTypeComboBox();
            LoadSitesToComboBox();
            LoadDeliveryIdsComboBox();
            isInitializing = false;
            EnableSearch(false);
        }

        private void LoadTxnStatusComboBox()
        {
            try
            {
                var statuses = context.Txnstatuses
                    .Where(s => s.Active == 1)
                    .OrderBy(s => s.StatusName)
                    .ToList();

                var allStatuses = new List<Txnstatus>();
                allStatuses.Add(new Txnstatus
                {
                    StatusName = "All",
                    StatusDescription = "All Statuses",
                    Active = 1
                });
                allStatuses.AddRange(statuses);

                cmbTxnStatus.ItemsSource = allStatuses;
                cmbTxnStatus.SelectedIndex = 0;

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

                var allTypes = new List<Txntype>();
                allTypes.Add(new Txntype
                {
                    TxnType1 = "All",
                    Active = 1
                });
                allTypes.AddRange(types);

                cmbTxnType.ItemsSource = allTypes;
                cmbTxnType.SelectedIndex = 0;

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
                    .Take(100)
                    .ToList();

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
                var query = context.Txns
                    .Include(t => t.Employee)
                    .Include(t => t.SiteIdfromNavigation)
                    .Include(t => t.SiteIdtoNavigation)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(txtSearchTxnId.Text) && int.TryParse(txtSearchTxnId.Text, out int txnId))
                {
                    query = query.Where(t => t.TxnId == txnId);
                }

                if (cmbTxnStatus.SelectedIndex > 0 && cmbTxnStatus.SelectedItem is Txnstatus selectedStatus)
                {
                    query = query.Where(t => t.TxnStatus == selectedStatus.StatusName);
                }

                if (cmbTxnType.SelectedIndex > 0 && cmbTxnType.SelectedItem is Txntype selectedType)
                {
                    query = query.Where(t => t.TxnType == selectedType.TxnType1);
                }

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
                    txtTxnId.Text = currentTransaction.TxnId.ToString();
                    txtEmployeeName.Text = $"{currentTransaction.Employee.FirstName} {currentTransaction.Employee.LastName}";
                    cmbEditTxnType.SelectedValue = currentTransaction.TxnType;
                    cmbEditTxnStatus.SelectedValue = currentTransaction.TxnStatus;
                    txtSiteFrom.Text = currentTransaction.SiteIdfromNavigation.SiteName;
                    cmbEditSiteTo.SelectedValue = currentTransaction.SiteIdto;
                    dpShipDate.SelectedDate = currentTransaction.ShipDate;
                    txtBarCode.Text = currentTransaction.BarCode;

                    if (currentTransaction.DeliveryId.HasValue)
                    {
                        cmbDeliveryId.SelectedValue = currentTransaction.DeliveryId;
                    }
                    else
                    {
                        cmbDeliveryId.SelectedValue = 0;
                    }

                    chkEmergencyDelivery.IsChecked = currentTransaction.EmergencyDelivery == 1;
                    txtNotes.Text = currentTransaction.Notes;

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
            // Check if the transaction is in transit or complete - these statuses cannot be edited
            if (currentTransaction.TxnStatus == "IN TRANSIT")
            {
                Growl.Warning("Transactions in transit cannot be edited.");
                return;
            }

            if (currentTransaction.TxnStatus == "COMPLETE")
            {
                Growl.Warning("Completed transactions cannot be edited.");
                return;
            }

            isEditMode = true;
            gridTransactionDetails.IsEnabled = true;
            btnEdit.Visibility = Visibility.Collapsed;
            btnSave.Visibility = Visibility.Visible;
            btnCancel.Visibility = Visibility.Visible;
            dgvTransactions.IsEnabled = false;
            EnableSearch(false);

            // Disable the refresh button in edit mode
            btnSearch.IsEnabled = false;
        }

        private void ExitEditMode()
        {
            isEditMode = false;
            gridTransactionDetails.IsEnabled = false;
            btnEdit.Visibility = Visibility.Visible;
            btnSave.Visibility = Visibility.Collapsed;
            btnCancel.Visibility = Visibility.Collapsed;
            dgvTransactions.IsEnabled = true;
            EnableSearch(true);

            // Re-enable the refresh button when exiting edit mode
            btnSearch.IsEnabled = true;
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

                // Check if user is trying to cancel a non-online order with invalid status
                if (isCancelling && currentTransaction.TxnType != "Online")
                {
                    string[] allowedStatusesForCancel = { "NEW", "SUBMITTED", "ASSEMBLING", "ASSEMBLED" };
                    if (!allowedStatusesForCancel.Contains(oldStatus))
                    {
                        Growl.Warning("Non-online orders can only be cancelled if they are NEW, SUBMITTED, ASSEMBLING, or ASSEMBLED.");
                        return;
                    }
                }

                currentTransaction.TxnType = cmbEditTxnType.SelectedValue.ToString();
                currentTransaction.TxnStatus = newStatus;
                currentTransaction.SiteIdto = (int)cmbEditSiteTo.SelectedValue;
                currentTransaction.ShipDate = dpShipDate.SelectedDate.Value;
                currentTransaction.BarCode = txtBarCode.Text;

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

                context.SaveChanges();

                if (isCancelling)
                {
                    HandleCancelledTransaction(currentTransaction);
                }

                AuditTransactions.LogActivity(
                    currentUser,
                    currentTransaction.TxnId,
                    currentTransaction.TxnType,
                    currentTransaction.TxnStatus,
                    currentTransaction.SiteIdto,
                    currentTransaction.DeliveryId,
                    $"Transaction modified by {currentUser.FirstName} {currentUser.LastName}"
                );

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
                var txnItems = context.Txnitems
                    .Where(ti => ti.TxnId == transaction.TxnId)
                    .ToList();

                if (!txnItems.Any())
                {
                    return;
                }

                foreach (var item in txnItems)
                {
                    int fromSiteId;
                    int toSiteId;

                    bool isOnlineOrder = transaction.TxnType == "Online";

                    switch (transaction.TxnStatus)
                    {
                        case "NEW":
                        case "SUBMITTED":
                        case "RECEIVED":
                            continue;
                        case "ASSEMBLING":
                            fromSiteId = 3;
                            toSiteId = isOnlineOrder ? transaction.SiteIdfrom : 2;
                            break;
                        case "ASSEMBLED":
                            fromSiteId = 3;
                            toSiteId = isOnlineOrder ? transaction.SiteIdfrom : 2;
                            break;
                        case "IN TRANSIT":
                            fromSiteId = 9999;
                            toSiteId = isOnlineOrder ? transaction.SiteIdfrom : 2;
                            break;
                        case "DELIVERED":
                            fromSiteId = transaction.SiteIdto;
                            toSiteId = isOnlineOrder ? transaction.SiteIdfrom : 2;
                            break;
                        default:
                            continue;
                    }

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

                string transactionDescription = transaction.TxnType == "Online"
                    ? $"Online order cancelled and inventory returned to original store (Site ID: {transaction.SiteIdfrom})"
                    : "Transaction cancelled and inventory returned to warehouse";

                AuditTransactions.LogActivity(
                    currentUser,
                    transaction.TxnId,
                    transaction.TxnType,
                    "CANCELLED",
                    transaction.SiteIdto,
                    transaction.DeliveryId,
                    $"{transactionDescription} by {currentUser.FirstName} {currentUser.LastName}"
                );
            }
            catch (Exception ex)
            {
                Growl.Error($"Error handling cancelled transaction: {ex.Message}");
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isInitializing && dgvTransactions != null)
            {
                SearchTransactions();
            }
        }

        private void DetailsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEditMode && currentTransaction != null)
            {
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEditMode && currentTransaction != null)
            {
            }
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (isEditMode && currentTransaction != null)
            {
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            int statusIndex = cmbTxnStatus.SelectedIndex;
            int typeIndex = cmbTxnType.SelectedIndex;

            isInitializing = true;
            LoadTxnStatusComboBox();
            LoadTxnTypeComboBox();
            LoadSitesToComboBox();
            LoadDeliveryIdsComboBox();

            cmbTxnStatus.SelectedIndex = statusIndex;
            cmbTxnType.SelectedIndex = typeIndex;
            isInitializing = false;
            EnableSearch(true);
            SearchTransactions();
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearchTxnId.Clear();
            cmbTxnStatus.SelectedIndex = 0;
            cmbTxnType.SelectedIndex = 0;
            dgvTransactions.ItemsSource = null;
            lblRecordCount.Text = string.Empty;

            txtTxnId.Text = string.Empty;
            txtEmployeeName.Text = string.Empty;
            cmbEditTxnType.SelectedIndex = -1;
            cmbEditTxnStatus.SelectedIndex = -1;
            txtSiteFrom.Text = string.Empty;
            cmbEditSiteTo.SelectedIndex = -1;
            dpShipDate.SelectedDate = null;
            txtBarCode.Text = string.Empty;
            cmbDeliveryId.SelectedIndex = -1;
            chkEmergencyDelivery.IsChecked = false;
            txtNotes.Text = string.Empty;

            btnEdit.IsEnabled = false;
            currentTransaction = null;
            EnableSearch(false);
        }

        private void EnableSearch(bool isEnabled)
        {
            txtSearchTxnId.IsEnabled = isEnabled;
            cmbTxnStatus.IsEnabled = isEnabled;
            cmbTxnType.IsEnabled = isEnabled;
            btnClear.Visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
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
                Growl.Ask("Are you sure you want to cancel editing?", isConfirmed =>
                {
                    if (isConfirmed)
                    {
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
    }
}