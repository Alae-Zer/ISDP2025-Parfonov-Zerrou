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

namespace ISDP2025_Parfonov_Zerrou.Forms.FloorGuyUserControl
{
    public partial class FloorGuyPickupStoreOrder : UserControl
    {
        Employee currentUser;
        List<OrderViewModel> allOrders;
        List<OrderItemViewModel> orderItems;
        List<OrderItemViewModel> loadedItems;
        bool isSignatureProvided = false;
        int[] notStores = { 1, 2, 3, 9999, 10000 };
        private int currentDeliveryId = 0;

        public FloorGuyPickupStoreOrder(Employee employee)
        {
            InitializeComponent();
            currentUser = employee;
            loadedItems = new List<OrderItemViewModel>();
            InitializeControls(false);
            InitializeSignatureCanvas();
            PopulateSitesComboBox();
            cboStores.SelectedIndex = 0;
        }

        private void InitializeControls(bool isEnabled)
        {
            btnComplete.IsEnabled = false;
            btnMoveToLoaded.IsEnabled = false;
            btnMoveBack.IsEnabled = false;
            txtSearch.IsEnabled = isEnabled;
            cboStores.IsEnabled = isEnabled;
            dgvOrders.ItemsSource = null;
            dgvOrderItems.ItemsSource = null;
            dgvLoadedItems.ItemsSource = null;

            ResetSignature();
        }

        private void InitializeSignatureCanvas()
        {
            inkSignature.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = Colors.Blue,
                Width = 3,
                Height = 3,
                FitToCurve = true
            };
        }

        private void PopulateSitesComboBox()
        {
            try
            {
                List<Site> allSites = new List<Site>();
                allSites.Add(new Site { SiteId = 0, SiteName = "All Stores" });

                using (var context = new BestContext())
                {
                    var sites = context.Sites
                        .Where(s => s.Active == 1 && !notStores.Contains(s.SiteId))
                        .OrderBy(s => s.DayOfWeek)
                        .Select(s => new Site { SiteId = s.SiteId, SiteName = s.SiteName })
                        .ToList();

                    allSites.AddRange(sites);
                }

                cboStores.ItemsSource = allSites;
                cboStores.DisplayMemberPath = "SiteName";
                cboStores.SelectedValuePath = "SiteId";
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading stores", ex);
            }
        }

        private void LoadAssembledOrders()
        {
            try
            {
                using (var context = new BestContext())
                {
                    allOrders = context.Txns
                            .Include(t => t.SiteIdtoNavigation)
                            .Where(t => t.TxnStatus == "ASSEMBLED" &&
                                      (t.TxnType == "Store Order" ||
                                       t.TxnType == "Emergency Order" ||
                                       t.TxnType == "Back Order"))
                            .OrderBy(t => t.ShipDate)
                            .Select(o => new OrderViewModel
                            {
                                TxnId = o.TxnId,
                                StoreName = o.SiteIdtoNavigation.SiteName,
                                TxnType = o.TxnType,
                                ShipDate = o.ShipDate,
                                TxnStatus = o.TxnStatus,
                                SiteIdto = o.SiteIdto,
                                Notes = o.Notes
                            })
                            .ToList();

                    UpdateOrdersGrid();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading orders", ex);
            }
        }

        private List<OrderViewModel> GetFilteredOrders(int? siteId = null)
        {
            if (allOrders == null) return new List<OrderViewModel>();

            return siteId.HasValue && siteId.Value != 0
                ? allOrders.Where(o => o.SiteIdto == siteId.Value).ToList()
                : allOrders;
        }

        private void UpdateOrdersGrid()
        {
            int selectedSiteId = 0;
            if (cboStores.SelectedItem is Site selectedSite)
            {
                selectedSiteId = selectedSite.SiteId;
            }
            dgvOrders.ItemsSource = GetFilteredOrders(selectedSiteId);
        }

        private void LoadOrderItems(int txnId)
        {
            try
            {
                using (var context = new BestContext())
                {
                    orderItems = QueryOrderItems(context, txnId);
                    dgvOrderItems.ItemsSource = orderItems;
                    ResetLoadedItems();
                    ResetSignature();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error loading order items", ex);
            }
        }

        private List<OrderItemViewModel> QueryOrderItems(BestContext context, int txnId)
        {
            return (from ti in context.Txnitems
                    join i in context.Items on ti.ItemId equals i.ItemId
                    join inv in context.Inventories
                    on new { ti.ItemId, SiteId = 3 }
                    equals new { inv.ItemId, inv.SiteId }
                    where ti.TxnId == txnId
                    select new OrderItemViewModel
                    {
                        ItemId = ti.ItemId,
                        Name = i.Name,
                        Barcode = i.Sku,
                        Quantity = ti.Quantity,
                        CaseSize = i.CaseSize,
                        CurrentStock = inv.Quantity
                    }).ToList();
        }

        private void ResetLoadedItems()
        {
            loadedItems.Clear();
            dgvLoadedItems.ItemsSource = null;
            dgvLoadedItems.ItemsSource = loadedItems;
        }

        private void FilterItems(string searchText)
        {
            if (orderItems != null)
            {
                searchText = searchText.ToLower();
                var filteredItems = orderItems
                    .Where(i => i.Name.ToLower().Contains(searchText) ||
                            i.ItemId.ToString().Contains(searchText) ||
                            i.Barcode.ToLower().Contains(searchText))
                    .ToList();
                dgvOrderItems.ItemsSource = filteredItems;
            }
        }

        private void MoveItemToTruck(OrderItemViewModel item)
        {
            try
            {
                var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
                if (selectedOrder == null) return;

                if (!HasSufficientStock(item))
                    return;

                int quantityToMove = item.Quantity;
                AddToLoadedItems(item, quantityToMove);
                orderItems.Remove(item);

                ShowSuccessMessage($"Selected {quantityToMove} units of {item.Name} for loading");
                UpdateSignatureVisibility();
                RefreshGrids();
                CheckOrderCompletion();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error selecting item", ex);
            }
        }

        private bool HasSufficientStock(OrderItemViewModel item)
        {
            if (item.CurrentStock < item.Quantity)
            {
                ShowWarningMessage($"Insufficient stock for {item.Name}. Current stock: {item.CurrentStock}");
                return false;
            }
            return true;
        }

        private void AddToLoadedItems(OrderItemViewModel item, int quantity)
        {
            var loadedItem = loadedItems.FirstOrDefault(i => i.ItemId == item.ItemId);
            if (loadedItem != null)
            {
                loadedItem.Quantity += quantity;
            }
            else
            {
                loadedItems.Add(new OrderItemViewModel
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Barcode = item.Barcode,
                    Quantity = quantity,
                    CaseSize = item.CaseSize,
                    CurrentStock = item.CurrentStock
                });
            }
        }

        private void MoveItemBack(OrderItemViewModel selectedItem)
        {
            var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
            if (selectedOrder == null || selectedItem == null) return;

            int quantityToMoveBack = selectedItem.Quantity;

            AddBackToOrderItems(selectedItem, quantityToMoveBack);
            loadedItems.Remove(selectedItem);

            ShowSuccessMessage($"Removed {quantityToMoveBack} units of {selectedItem.Name} from selection");
            UpdateSignatureVisibility();
            RefreshGrids();
            CheckOrderCompletion();
        }

        private void AddBackToOrderItems(OrderItemViewModel item, int quantity)
        {
            var orderItem = orderItems.FirstOrDefault(i => i.ItemId == item.ItemId);
            if (orderItem == null)
            {
                orderItems.Add(new OrderItemViewModel
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Barcode = item.Barcode,
                    Quantity = quantity,
                    CaseSize = item.CaseSize,
                    CurrentStock = item.CurrentStock
                });
            }
            else
            {
                orderItem.Quantity += quantity;
            }
        }

        private void ResetSignature()
        {
            isSignatureProvided = false;
            inkSignature.Strokes.Clear();
            btnSign.IsEnabled = true;
            btnClearSignature.IsEnabled = false;
            signatureSection.Visibility = Visibility.Collapsed;
        }

        private void UpdateSignatureVisibility()
        {
            // Only show signature when left DataGrid is empty and right DataGrid has items
            if (orderItems.Count == 0 && loadedItems.Count > 0)
            {
                signatureSection.Visibility = Visibility.Visible;
            }
            else
            {
                signatureSection.Visibility = Visibility.Collapsed;
                isSignatureProvided = false;
                inkSignature.Strokes.Clear();
                btnSign.IsEnabled = true;
                btnClearSignature.IsEnabled = false;
            }
        }

        private void ProcessSignature()
        {
            if (inkSignature.Strokes.Count > 0)
            {
                isSignatureProvided = true;
                btnSign.IsEnabled = false;
                btnClearSignature.IsEnabled = true;
                CheckOrderCompletion();
                ShowSuccessMessage("Signature provided successfully");
            }
            else
            {
                ShowWarningMessage("Please provide a signature before confirming");
            }
        }

        private void ClearSignature()
        {
            inkSignature.Strokes.Clear();
            isSignatureProvided = false;
            btnSign.IsEnabled = true;
            btnClearSignature.IsEnabled = false;
            btnComplete.IsEnabled = false;
        }

        private byte[] ConvertSignatureToBytes(InkCanvas inkCanvas)
        {
            var rtb = new RenderTargetBitmap(
                (int)inkCanvas.ActualWidth,
                (int)inkCanvas.ActualHeight,
                96, 96,
                PixelFormats.Default);
            rtb.Render(inkCanvas);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        private void CheckOrderCompletion()
        {
            btnComplete.IsEnabled = loadedItems.Count > 0 && isSignatureProvided;
        }

        private void CompleteOrderLoading()
        {
            try
            {
                var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
                if (selectedOrder == null) return;

                if (!ValidateOrderCompletion())
                    return;

                int deliveryId = GetOrCreateDelivery(selectedOrder);
                currentDeliveryId = deliveryId;

                byte[] signatureData = ConvertSignatureToBytes(inkSignature);
                DeliveryManager.SaveSignature(currentDeliveryId, signatureData);

                if (MoveItemsToTruck())
                {
                    UpdateOrderStatus(selectedOrder, deliveryId);
                    Refresh();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error completing order loading", ex);
            }
        }

        private bool ValidateOrderCompletion()
        {
            if (!isSignatureProvided)
            {
                ShowWarningMessage("Driver signature is required to complete loading");
                return false;
            }

            if (loadedItems.Count == 0)
            {
                ShowWarningMessage("No items have been selected for loading");
                return false;
            }

            return true;
        }

        private int GetOrCreateDelivery(OrderViewModel order)
        {
            int existingDeliveryId = 0;
            using (var context = new BestContext())
            {
                var txn = context.Txns.Find(order.TxnId);
                if (txn?.DeliveryId > 0)
                    return txn.DeliveryId.Value;
            }

            return DeliveryManager.CreateDelivery(
                "Medium",
                $"Delivery for order #{order.TxnId} to {order.StoreName}",
                0
            );
        }

        private bool MoveItemsToTruck()
        {
            foreach (var item in loadedItems)
            {
                EnsureTruckInventoryExists(item.ItemId);

                if (!MoveInventory.Move(item.ItemId, item.Quantity, 3, 9999))
                {
                    ShowErrorMessage($"Failed to move {item.Name} to truck");
                    return false;
                }
            }
            return true;
        }

        private void EnsureTruckInventoryExists(int itemId)
        {
            using (var context = new BestContext())
            {
                var truckInventory = context.Inventories
                    .FirstOrDefault(i => i.ItemId == itemId && i.SiteId == 9999);

                if (truckInventory == null)
                {
                    context.Inventories.Add(new Inventory
                    {
                        ItemId = itemId,
                        SiteId = 9999,
                        ItemLocation = "TRUCK",
                        Quantity = 0,
                        OptimumThreshold = 0
                    });
                    context.SaveChanges();
                }
            }
        }

        private void UpdateOrderStatus(OrderViewModel order, int deliveryId)
        {
            using (var context = new BestContext())
            {
                var txn = context.Txns.Find(order.TxnId);
                if (txn != null)
                {
                    txn.TxnStatus = "IN TRANSIT";
                    txn.DeliveryId = deliveryId;
                    context.SaveChanges();

                    var totalItems = loadedItems.Sum(i => i.Quantity);

                    AuditTransactions.LogActivity(
                        currentUser,
                        order.TxnId,
                        order.TxnType,
                        "IN TRANSIT",
                        order.SiteIdto,
                        deliveryId,
                        $"Order loaded to truck. Total items loaded: {totalItems}"
                    );

                    ShowSuccessMessage($"Order marked as IN TRANSIT and assigned to delivery #{deliveryId}");
                }
            }
        }

        private void Refresh()
        {
            LoadAssembledOrders();
            loadedItems.Clear();
            dgvLoadedItems.ItemsSource = null;
            ResetSignature();
            btnComplete.IsEnabled = false;
            txtSearch.IsEnabled = true;
            cboStores.IsEnabled = true;
        }

        private void RefreshGrids()
        {
            dgvOrderItems.ItemsSource = null;
            dgvOrderItems.ItemsSource = orderItems;
            dgvLoadedItems.ItemsSource = null;
            dgvLoadedItems.ItemsSource = loadedItems;
        }

        private void ShowErrorMessage(string message, Exception ex = null)
        {
            string fullMessage = ex != null ? $"{message}: {ex.Message}" : message;
            HandyControl.Controls.MessageBox.Show(fullMessage, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowSuccessMessage(string message)
        {
            Growl.Success(new GrowlInfo
            {
                Message = message,
                ShowDateTime = false,
                WaitTime = 2
            });
        }

        private void ShowWarningMessage(string message)
        {
            Growl.Warning(new GrowlInfo
            {
                Message = message,
                ShowDateTime = false,
                WaitTime = 3
            });
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void cboStores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateOrdersGrid();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterItems(txtSearch.Text);
        }

        private void dgvOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvOrders.SelectedItem is OrderViewModel selectedOrder)
            {
                LoadOrderItems(selectedOrder.TxnId);
            }
        }

        private void dgvOrderItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvOrderItems.SelectedItem is OrderItemViewModel selectedItem)
            {
                btnMoveToLoaded.IsEnabled = selectedItem.CurrentStock >= selectedItem.CaseSize;
            }
        }

        private void dgvLoadedItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnMoveBack.IsEnabled = dgvLoadedItems.SelectedItem != null;
        }

        private void btnMoveToLoaded_Click(object sender, RoutedEventArgs e)
        {
            if (dgvOrderItems.SelectedItem is OrderItemViewModel selectedItem)
            {
                MoveItemToTruck(selectedItem);
            }
        }

        private void btnMoveBack_Click(object sender, RoutedEventArgs e)
        {
            if (dgvLoadedItems.SelectedItem is OrderItemViewModel selectedItem)
            {
                MoveItemBack(selectedItem);
            }
        }

        private void btnComplete_Click(object sender, RoutedEventArgs e)
        {
            CompleteOrderLoading();
        }

        private void btnSign_Click(object sender, RoutedEventArgs e)
        {
            ProcessSignature();
        }

        private void btnClearSignature_Click(object sender, RoutedEventArgs e)
        {
            ClearSignature();
        }
    }
}