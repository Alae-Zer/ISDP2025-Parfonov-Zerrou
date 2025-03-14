using HandyControl.Controls;
using HandyControl.Data;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ISDP2025_Parfonov_Zerrou.Forms.StoreManagerUserControls
{
    public partial class StroreManagerAcceptAndFulfilOnlineOrder : UserControl
    {
        private Employee currentUser;
        private List<OnlineOrderViewModel> allOrders;
        private List<OrderItemViewModel> orderItems;
        private List<OrderItemViewModel> preparedItems;
        private bool isSignatureProvided = false;
        string[] statuses = { "All", "NEW", "SUBMITTED", "ASSEMBLING", "ASSEMBLED", "COMPLETE" };

        public StroreManagerAcceptAndFulfilOnlineOrder(Employee employee)
        {
            InitializeComponent();
            currentUser = employee;
            preparedItems = new List<OrderItemViewModel>();
            InitializeControls();
            PopulateStatusComboBox();
            InitializeSignatureControls();
        }

        private void PopulateStatusComboBox()
        {
            cboStatus.ItemsSource = statuses;
            cboStatus.SelectedIndex = 1;
        }

        private void InitializeControls()
        {
            btnComplete.IsEnabled = false;
            txtSearch.IsEnabled = false;
            dgvOrders.ItemsSource = null;
            dgvOrderItems.ItemsSource = null;
            dgvPreparedItems.ItemsSource = null;
            signaturePanel.Visibility = Visibility.Collapsed;
            ResetSignature();
        }

        private void InitializeSignatureControls()
        {
            inkSignature.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = Colors.Blue,
                Width = 3,
                Height = 3,
                FitToCurve = true
            };

            isSignatureProvided = false;
            btnConfirmSignature.IsEnabled = true;
            btnClearSignature.IsEnabled = false;
        }

        private void SetupForNewOrSubmittedStatus()
        {
            dgvOrderItems.ItemsSource = orderItems;
            dgvPreparedItems.ItemsSource = preparedItems;
            btnComplete.Content = "Start Assembling";
            signaturePanel.Visibility = Visibility.Collapsed;
            btnComplete.IsEnabled = false;
        }

        private void SetupForAssemblingStatus()
        {
            dgvOrderItems.ItemsSource = orderItems;
            dgvPreparedItems.ItemsSource = preparedItems;
            btnComplete.Content = "Mark As Assembled";
            signaturePanel.Visibility = Visibility.Collapsed;
            btnComplete.IsEnabled = orderItems.Count == 0 && preparedItems.Count > 0;
        }

        private void SetupForAssembledStatus()
        {
            dgvOrderItems.ItemsSource = orderItems;
            dgvPreparedItems.ItemsSource = preparedItems;
            btnComplete.Content = "Complete Order";
            signaturePanel.Visibility = Visibility.Visible;
            btnComplete.IsEnabled = false;
        }

        private void ProcessNewToSubmittedTransition(Txn txn)
        {
            txn.TxnStatus = "SUBMITTED";
            AuditTransactions.LogActivity(currentUser, txn.TxnId, txn.TxnType, "SUBMITTED", txn.SiteIdto, null, "Online order submitted for processing.");
        }

        private void ProcessSubmittedToAssemblingTransition(Txn txn)
        {
            txn.TxnStatus = "ASSEMBLING";
            AuditTransactions.LogActivity(currentUser, txn.TxnId, txn.TxnType, "ASSEMBLING", txn.SiteIdto, null, "Online order assembly started.");
        }

        private bool ProcessAssemblingToAssembledTransition(Txn txn)
        {
            if (orderItems.Count > 0)
            {
                HandyControl.Controls.MessageBox.Show("All items must be assembled before marking the order as assembled.", "Items Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            txn.TxnStatus = "ASSEMBLED";
            AuditTransactions.LogActivity(currentUser, txn.TxnId, txn.TxnType, "ASSEMBLED", txn.SiteIdto, null, "Online order assembly completed.");
            return true;
        }

        private bool ProcessAssembledToCompleteTransition(Txn txn, int txnId, BestContext context)
        {
            if (!isSignatureProvided || inkSignature.Strokes.Count == 0)
            {
                HandyControl.Controls.MessageBox.Show("Customer signature is required to complete the order.", "Signature Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            SaveSignature(txnId);
            txn.TxnStatus = "COMPLETE";

            foreach (var item in preparedItems)
            {
                var inventory = context.Inventories.FirstOrDefault(i => i.ItemId == item.ItemId && i.SiteId == currentUser.SiteId);
                if (inventory != null)
                {
                    inventory.Quantity -= item.Quantity;
                }
            }

            AuditTransactions.LogActivity(currentUser, txn.TxnId, txn.TxnType, "COMPLETE", txn.SiteIdto, null, "Online order completed with customer signature.");
            return true;
        }

        private void MoveItemToAssembled(OrderItemViewModel selectedItem)
        {
            var existingItem = preparedItems.FirstOrDefault(i => i.ItemId == selectedItem.ItemId);
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
                selectedItem.Quantity -= 1;
                if (selectedItem.Quantity <= 0)
                    orderItems.Remove(selectedItem);
            }
            else
            {
                preparedItems.Add(new OrderItemViewModel
                {
                    ItemId = selectedItem.ItemId,
                    Name = selectedItem.Name,
                    Barcode = selectedItem.Barcode,
                    Quantity = 1,
                    CurrentStock = selectedItem.CurrentStock,
                    CaseSize = selectedItem.CaseSize
                });
                selectedItem.Quantity -= 1;
                if (selectedItem.Quantity <= 0)
                    orderItems.Remove(selectedItem);
            }

            RefreshGrids();
            UpdateCompleteButtonState();
        }

        private void MoveItemBack(OrderItemViewModel selectedItem)
        {
            var existingItem = orderItems.FirstOrDefault(i => i.ItemId == selectedItem.ItemId);
            if (existingItem != null)
            {
                existingItem.Quantity += 1;
                selectedItem.Quantity -= 1;
                if (selectedItem.Quantity <= 0)
                    preparedItems.Remove(selectedItem);
            }
            else
            {
                orderItems.Add(new OrderItemViewModel
                {
                    ItemId = selectedItem.ItemId,
                    Name = selectedItem.Name,
                    Barcode = selectedItem.Barcode,
                    Quantity = 1,
                    CurrentStock = selectedItem.CurrentStock,
                    CaseSize = selectedItem.CaseSize
                });
                selectedItem.Quantity -= 1;
                if (selectedItem.Quantity <= 0)
                    preparedItems.Remove(selectedItem);
            }

            RefreshGrids();
            UpdateCompleteButtonState();
        }

        private void UpdateCompleteButtonState()
        {
            if (dgvOrders.SelectedItem is OnlineOrderViewModel selectedOrder)
            {
                if (selectedOrder.TxnStatus == "ASSEMBLED")
                {
                    btnComplete.IsEnabled = isSignatureProvided;
                }
                else
                {
                    btnComplete.IsEnabled = orderItems.Count == 0 && preparedItems.Count > 0;
                }
            }
        }

        private void RefreshGrids()
        {
            dgvOrderItems.ItemsSource = null;
            dgvOrderItems.ItemsSource = orderItems;
            dgvPreparedItems.ItemsSource = null;
            dgvPreparedItems.ItemsSource = preparedItems;
        }

        private void LoadOnlineOrders()
        {
            try
            {
                using (var context = new BestContext())
                {
                    var query = context.Txns
                        .Include(t => t.SiteIdtoNavigation)
                        .Where(t => t.TxnType == "Online" && t.SiteIdto == currentUser.SiteId)
                        .OrderByDescending(t => t.CreatedDate)
                        .AsQueryable();

                    if (cboStatus.SelectedItem != null && cboStatus.SelectedIndex > 0)
                    {
                        string selectedStatus = cboStatus.SelectedItem.ToString();
                        query = query.Where(t => t.TxnStatus == selectedStatus);
                    }

                    allOrders = query.Select(o => new OnlineOrderViewModel
                    {
                        TxnId = o.TxnId,
                        CustomerName = o.Notes ?? "Unknown",
                        OrderDate = o.CreatedDate,
                        PickupTime = o.ShipDate,
                        TxnStatus = o.TxnStatus,
                        Notes = o.Notes ?? ""
                    }).ToList();

                    dgvOrders.ItemsSource = allOrders;
                    txtSearch.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading online orders: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOrderItems(int txnId)
        {
            try
            {
                using (var context = new BestContext())
                {
                    var txn = context.Txns.FirstOrDefault(t => t.TxnId == txnId);
                    if (txn == null) return;

                    var allItems = from ti in context.Txnitems
                                   join i in context.Items on ti.ItemId equals i.ItemId
                                   join inv in context.Inventories
                                   on new { ti.ItemId, SiteId = currentUser.SiteId }
                                   equals new { inv.ItemId, inv.SiteId }
                                   where ti.TxnId == txnId
                                   select new OrderItemViewModel
                                   {
                                       ItemId = ti.ItemId,
                                       Name = i.Name,
                                       Barcode = i.Sku,
                                       Quantity = ti.Quantity,
                                       CurrentStock = inv.Quantity,
                                       CaseSize = i.CaseSize
                                   };

                    orderItems = allItems.ToList();
                    preparedItems = new List<OrderItemViewModel>();

                    ResetSignature();

                    switch (txn.TxnStatus)
                    {
                        case "NEW":
                        case "SUBMITTED":
                            SetupForNewOrSubmittedStatus();
                            break;

                        case "ASSEMBLING":
                            SetupForAssemblingStatus();
                            break;

                        case "ASSEMBLED":
                            SetupForAssembledStatus();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading order items: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSignature(int txnId)
        {
            try
            {
                string signatureDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Signatures");
                if (!Directory.Exists(signatureDirectory))
                {
                    Directory.CreateDirectory(signatureDirectory);
                }

                string filename = Path.Combine(signatureDirectory, $"OnlineOrder_{txnId}_Signature.png");

                var rtb = new RenderTargetBitmap(
                    (int)inkSignature.ActualWidth,
                    (int)inkSignature.ActualHeight,
                    96, 96,
                    PixelFormats.Default);
                rtb.Render(inkSignature);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                using (var ms = new FileStream(filename, FileMode.Create))
                {
                    encoder.Save(ms);
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error saving signature: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnMoveToAssembled_Click(object sender, RoutedEventArgs e)
        {
            if (dgvOrderItems.SelectedItem is OrderItemViewModel selectedItem)
            {
                MoveItemToAssembled(selectedItem);
            }
        }

        private void btnMoveBack_Click(object sender, RoutedEventArgs e)
        {
            if (dgvPreparedItems.SelectedItem is OrderItemViewModel selectedItem)
            {
                MoveItemBack(selectedItem);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            InitializeControls();
            LoadOnlineOrders();
        }

        private void cboStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allOrders != null || cboStatus.SelectedIndex >= 0)
            {
                LoadOnlineOrders();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.ToLower();
            if (orderItems != null)
            {
                var filteredItems = orderItems
                    .Where(i => i.Name.ToLower().Contains(searchText) ||
                           i.ItemId.ToString().Contains(searchText) ||
                           i.Barcode.ToLower().Contains(searchText))
                    .ToList();
                dgvOrderItems.ItemsSource = filteredItems;
            }
        }

        private void dgvOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvOrders.SelectedItem is OnlineOrderViewModel selectedOrder)
            {
                LoadOrderItems(selectedOrder.TxnId);
                UpdateCompleteButtonState();
            }
        }

        private void dgvOrderItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnMoveToAssembled.IsEnabled = dgvOrderItems.SelectedItem != null;
        }

        private void dgvPreparedItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnMoveBack.IsEnabled = dgvPreparedItems.SelectedItem != null;
        }

        private void btnClearSignature_Click(object sender, RoutedEventArgs e)
        {
            ResetSignature();
        }

        private void btnConfirmSignature_Click(object sender, RoutedEventArgs e)
        {
            if (inkSignature.Strokes.Count > 0)
            {
                isSignatureProvided = true;
                btnConfirmSignature.IsEnabled = false;
                btnClearSignature.IsEnabled = true;

                btnComplete.IsEnabled = true;

                Growl.Success(new GrowlInfo
                {
                    Message = "Signature confirmed",
                    ShowDateTime = false,
                    WaitTime = 2
                });
            }
            else
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "Please provide a signature before confirming",
                    ShowDateTime = false,
                    WaitTime = 2
                });
            }
        }

        private void btnComplete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgvOrders.SelectedItem is OnlineOrderViewModel selectedOrder)
                {
                    using (var context = new BestContext())
                    {
                        var txn = context.Txns.Find(selectedOrder.TxnId);
                        if (txn != null)
                        {
                            bool successful = false;

                            switch (txn.TxnStatus)
                            {
                                case "NEW":
                                    ProcessNewToSubmittedTransition(txn);
                                    successful = true;
                                    break;

                                case "SUBMITTED":
                                    ProcessSubmittedToAssemblingTransition(txn);
                                    successful = true;
                                    break;

                                case "ASSEMBLING":
                                    successful = ProcessAssemblingToAssembledTransition(txn);
                                    break;

                                case "ASSEMBLED":
                                    successful = ProcessAssembledToCompleteTransition(txn, selectedOrder.TxnId, context);
                                    break;
                            }

                            if (successful)
                            {
                                context.SaveChanges();

                                Growl.Success(new GrowlInfo
                                {
                                    Message = $"Order status updated to {txn.TxnStatus}.",
                                    ShowDateTime = false,
                                    WaitTime = 2
                                });

                                ResetToOriginalState();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error processing order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetSignature()
        {
            inkSignature.Strokes.Clear();
            isSignatureProvided = false;
            if (btnConfirmSignature != null) btnConfirmSignature.IsEnabled = true;
            if (btnClearSignature != null) btnClearSignature.IsEnabled = false;

            UpdateCompleteButtonState();
        }

        private void ResetToOriginalState()
        {
            btnComplete.IsEnabled = false;
            txtSearch.IsEnabled = false;
            dgvOrders.ItemsSource = null;
            dgvOrderItems.ItemsSource = null;
            dgvPreparedItems.ItemsSource = null;
            signaturePanel.Visibility = Visibility.Collapsed;
            ResetSignature();

            LoadOnlineOrders();
        }
    }

    public class OnlineOrderViewModel
    {
        public int TxnId { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime PickupTime { get; set; }
        public string TxnStatus { get; set; }
        public string Notes { get; set; }
    }

    public class OrderItemViewModel
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public int Quantity { get; set; }
        public int CurrentStock { get; set; }
        public int CaseSize { get; set; }
        public bool InsufficientStock => CurrentStock < Quantity;
        public bool PickedUp { get; set; }
    }
}