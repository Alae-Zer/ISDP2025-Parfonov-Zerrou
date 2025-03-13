using HandyControl.Controls;
using HandyControl.Data;
using ISDP2025_Parfonov_Zerrou.Forms.FloorGuyUserControl;
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
    public partial class StoreManagerReceiveOrder : UserControl
    {
        Employee currentUser;
        List<OrderViewModel> pendingOrders;
        List<OrderItemViewModel> deliveredItems;
        List<OrderItemViewModel> acceptedItems;
        bool isSignatureProvided = false;
        private int currentDeliveryId = 0;
        private int currentStoreId = 0;

        public StoreManagerReceiveOrder(Employee employee)
        {
            InitializeComponent();
            currentUser = employee;
            currentStoreId = employee.SiteId;
            acceptedItems = new List<OrderItemViewModel>();
            InitializeControls();
            InitializeSignatureCanvas();
        }

        private void InitializeControls()
        {
            btnComplete.IsEnabled = false;
            btnMoveToAccepted.IsEnabled = false;
            btnMoveBack.IsEnabled = false;
            txtSearch.IsEnabled = true;
            dgvOrders.ItemsSource = null;
            dgvDeliveredItems.ItemsSource = null;
            dgvStoreItems.ItemsSource = null;

            // Hide signature section initially
            signatureSection.Visibility = Visibility.Collapsed;
            isSignatureProvided = false;
            inkSignature.Strokes.Clear();
            btnSign.IsEnabled = true;
            btnClearSignature.IsEnabled = false;

            LoadPendingOrders();
        }

        private void InitializeSignatureCanvas()
        {
            // Initialize the InkCanvas for signatures
            inkSignature.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = Colors.Blue,
                Width = 3,
                Height = 3,
                FitToCurve = true
            };
        }

        private void LoadPendingOrders()
        {
            try
            {
                using (var context = new BestContext())
                {
                    // Include both IN TRANSIT and DELIVERED status
                    pendingOrders = context.Txns
                            .Include(t => t.SiteIdtoNavigation)
                            .Where(t => (t.TxnStatus == "IN TRANSIT" || t.TxnStatus == "DELIVERED") &&
                                      t.SiteIdto == currentStoreId &&
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
                                Notes = o.Notes,
                            })
                            .ToList();

                    dgvOrders.ItemsSource = pendingOrders;
                    grpStatus.Header = "Orders (In Transit and Delivered)";
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUIForOrderStatus(string status)
        {
            if (status == "IN TRANSIT")
            {
                grpDeliveredItems.Header = "Items in Truck";
                grpAcceptedItems.Header = "Items Accepted into Store";
                btnMoveToAccepted.IsEnabled = true;
                btnMoveBack.IsEnabled = true;
                btnComplete.Content = "Complete Receiving";
            }
            else if (status == "DELIVERED")
            {
                grpDeliveredItems.Header = "Items Delivered";
                grpAcceptedItems.Header = "Items to Accept";
                btnMoveToAccepted.IsEnabled = true;
                btnMoveBack.IsEnabled = true;
                btnComplete.Content = "Accept Delivery";
                btnComplete.Visibility = Visibility.Visible;
            }
        }

        private void LoadDeliveryItems(int txnId)
        {
            try
            {
                using (var context = new BestContext())
                {
                    LoadDeliveryIdAndUpdateUI(context, txnId);
                    LoadOrderItems(context, txnId);
                    ResetAcceptedItemsAndSignature();
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading order items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDeliveryIdAndUpdateUI(BestContext context, int txnId)
        {
            var order = context.Txns.Find(txnId);
            if (order != null && order.DeliveryId.HasValue)
            {
                currentDeliveryId = order.DeliveryId.Value;
                UpdateUIForOrderStatus(order.TxnStatus);
            }
        }

        private void LoadOrderItems(BestContext context, int txnId)
        {
            var query = from ti in context.Txnitems
                        join i in context.Items on ti.ItemId equals i.ItemId
                        where ti.TxnId == txnId
                        select new OrderItemViewModel
                        {
                            ItemId = ti.ItemId,
                            Name = i.Name,
                            Barcode = i.Sku,
                            Quantity = ti.Quantity,
                            CaseSize = i.CaseSize,
                            CurrentStock = context.Inventories
                                        .Where(inv => inv.ItemId == ti.ItemId && inv.SiteId == currentStoreId)
                                        .Sum(inv => inv.Quantity)
                        };

            deliveredItems = query.ToList();
            dgvDeliveredItems.ItemsSource = deliveredItems;
        }

        private void ResetAcceptedItemsAndSignature()
        {
            acceptedItems.Clear();
            dgvStoreItems.ItemsSource = null;
            dgvStoreItems.ItemsSource = acceptedItems;

            isSignatureProvided = false;
            inkSignature.Strokes.Clear();
            btnSign.IsEnabled = true;
            btnClearSignature.IsEnabled = false;
            signatureSection.Visibility = Visibility.Collapsed;
        }

        private void MoveItemToStore(OrderItemViewModel item)
        {
            try
            {
                var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
                if (selectedOrder == null) return;

                if (!ValidateInventoryForItem(selectedOrder, item)) return;

                AddItemToAcceptedList(item);
                deliveredItems.Remove(item);

                ShowSuccessNotification(item);

                // Use the visibility function that checks if deliveredItems is empty
                UpdateSignatureSectionVisibility();

                RefreshGrids();
                CheckOrderCompletion();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error accepting item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInventoryForItem(OrderViewModel selectedOrder, OrderItemViewModel item)
        {
            bool hasEnoughStock = false;

            if (selectedOrder.TxnStatus == "IN TRANSIT")
            {
                hasEnoughStock = CheckTruckInventory(item.ItemId, item.Quantity);
            }
            else if (selectedOrder.TxnStatus == "DELIVERED")
            {
                hasEnoughStock = CheckStoreroomInventory(item.ItemId, item.Quantity);
            }

            if (!hasEnoughStock)
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = $"Insufficient items for {item.Name}",
                    ShowDateTime = false,
                    WaitTime = 3,
                });
                return false;
            }

            return true;
        }

        private void AddItemToAcceptedList(OrderItemViewModel item)
        {
            int quantityToMove = item.Quantity;
            var acceptedItem = acceptedItems.FirstOrDefault(i => i.ItemId == item.ItemId);

            if (acceptedItem != null)
            {
                acceptedItem.Quantity += quantityToMove;
            }
            else
            {
                acceptedItems.Add(new OrderItemViewModel
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Barcode = item.Barcode,
                    Quantity = quantityToMove,
                    CaseSize = item.CaseSize,
                    CurrentStock = item.CurrentStock
                });
            }
        }

        private void ShowSuccessNotification(OrderItemViewModel item)
        {
            Growl.Success(new GrowlInfo
            {
                Message = $"Accepted {item.Quantity} units of {item.Name}",
                ShowDateTime = false,
                WaitTime = 2
            });
        }

        // This method should also keep signature hidden
        private void UpdateSignatureSectionVisibility()
        {
            // Only show signature if left DataGrid is empty
            signatureSection.Visibility = deliveredItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool CheckStoreroomInventory(int itemId, int requiredQuantity)
        {
            try
            {
                using (var context = new BestContext())
                {
                    var storeroomInventory = context.Inventories
                        .Where(i => i.ItemId == itemId &&
                                   i.SiteId == currentStoreId &&
                                   i.ItemLocation == "STOREROOM")
                        .Sum(i => i.Quantity);

                    return storeroomInventory >= requiredQuantity;
                }
            }
            catch
            {
                return false;
            }
        }

        private void RefreshGrids()
        {
            dgvDeliveredItems.ItemsSource = null;
            dgvDeliveredItems.ItemsSource = deliveredItems;
            dgvStoreItems.ItemsSource = null;
            dgvStoreItems.ItemsSource = acceptedItems;
        }

        private void CheckOrderCompletion()
        {
            // Enable Complete button only when items are accepted AND signature is provided
            btnComplete.IsEnabled = acceptedItems.Count > 0 && isSignatureProvided;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            InitializeControls();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.ToLower();
            if (deliveredItems != null)
            {
                var filteredItems = deliveredItems
                    .Where(i => i.Name.ToLower().Contains(searchText) ||
                               i.ItemId.ToString().Contains(searchText) ||
                               i.Barcode.ToLower().Contains(searchText))
                    .ToList();
                dgvDeliveredItems.ItemsSource = filteredItems;
            }
        }

        private void dgvOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvOrders.SelectedItem is OrderViewModel selectedOrder)
            {
                LoadDeliveryItems(selectedOrder.TxnId);
            }
        }

        private void dgvDeliveredItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
            if (selectedOrder != null)
            {
                // Enable if item is selected, regardless of order status
                btnMoveToAccepted.IsEnabled = dgvDeliveredItems.SelectedItem != null;
            }
            else
            {
                btnMoveToAccepted.IsEnabled = false;
            }
        }

        private void dgvStoreItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
            if (selectedOrder != null)
            {
                // Enable if item is selected, regardless of order status
                btnMoveBack.IsEnabled = dgvStoreItems.SelectedItem != null;
            }
            else
            {
                btnMoveBack.IsEnabled = false;
            }
        }

        private void btnMoveToAccepted_Click(object sender, RoutedEventArgs e)
        {
            if (dgvDeliveredItems.SelectedItem is OrderItemViewModel selectedItem)
            {
                MoveItemToStore(selectedItem);
            }
        }

        private void btnMoveBack_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgvStoreItems.SelectedItem as OrderItemViewModel;
            if (selectedItem?.Quantity > 0)
            {
                MoveItemBackToDelivered(selectedItem);
            }
        }

        private void MoveItemBackToDelivered(OrderItemViewModel selectedItem)
        {
            int quantityToMoveBack = selectedItem.Quantity;

            AddItemBackToDeliveredList(selectedItem, quantityToMoveBack);
            acceptedItems.Remove(selectedItem);

            ShowMoveBackSuccessNotification(selectedItem, quantityToMoveBack);
            UpdateSignatureSectionAfterRemove();
            RefreshGrids();
            CheckOrderCompletion();
        }

        private void AddItemBackToDeliveredList(OrderItemViewModel selectedItem, int quantityToMoveBack)
        {
            var deliveredItem = deliveredItems.FirstOrDefault(i => i.ItemId == selectedItem.ItemId);
            if (deliveredItem == null)
            {
                deliveredItem = new OrderItemViewModel
                {
                    ItemId = selectedItem.ItemId,
                    Name = selectedItem.Name,
                    Barcode = selectedItem.Barcode,
                    Quantity = quantityToMoveBack,
                    CaseSize = selectedItem.CaseSize,
                    CurrentStock = selectedItem.CurrentStock
                };
                deliveredItems.Add(deliveredItem);
            }
            else
            {
                deliveredItem.Quantity += quantityToMoveBack;
            }
        }

        private void ShowMoveBackSuccessNotification(OrderItemViewModel selectedItem, int quantityToMoveBack)
        {
            Growl.Success(new GrowlInfo
            {
                Message = $"Removed {quantityToMoveBack} units of {selectedItem.Name} from acceptance",
                ShowDateTime = false,
                WaitTime = 2
            });
        }

        private void UpdateSignatureSectionAfterRemove()
        {
            // Only clear signature if all items are moved from dgvDeliveredItems
            if (acceptedItems.Count == 0 || deliveredItems.Count == 0)
            {
                signatureSection.Visibility = Visibility.Collapsed;
                isSignatureProvided = false;
                inkSignature.Strokes.Clear();
                btnSign.IsEnabled = true;
                btnClearSignature.IsEnabled = false;
            }
        }

        private void btnComplete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateCompleteAction()) return;

                var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
                byte[] signatureData = ConvertSignatureToBytes(inkSignature);
                DeliveryManager.SaveSignature(currentDeliveryId, signatureData);

                using (var context = new BestContext())
                {
                    var order = context.Txns.Find(selectedOrder.TxnId);
                    if (order == null) return;

                    if (selectedOrder.TxnStatus == "IN TRANSIT")
                        ProcessInTransitOrder(context, order);
                    else if (selectedOrder.TxnStatus == "DELIVERED")
                        ProcessDeliveredOrder(context, order);
                }

                ResetUIAfterCompletion();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private bool ValidateCompleteAction()
        {
            if (!isSignatureProvided || acceptedItems.Count == 0) return false;
            var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
            return selectedOrder != null;
        }

        private void ProcessInTransitOrder(BestContext context, Txn order)
        {
            bool allMoved = MoveItemsFromTruckToStoreroom(context);

            if (allMoved)
            {
                order.TxnStatus = "DELIVERED";
                context.SaveChanges();

                LogOrderReceived(order);
                Growl.Success(new GrowlInfo { Message = "Order received! Status: DELIVERED" });
            }
        }

        private bool MoveItemsFromTruckToStoreroom(BestContext context)
        {
            foreach (var item in acceptedItems)
            {
                var truckInventory = context.Inventories
                    .FirstOrDefault(i => i.ItemId == item.ItemId && i.SiteId == 9999);

                if (truckInventory == null || truckInventory.Quantity < item.Quantity)
                    return false;

                truckInventory.Quantity -= item.Quantity;
                UpdateOrCreateStoreroomInventory(context, item);

                if (truckInventory.Quantity == 0)
                    context.Inventories.Remove(truckInventory);
            }
            return true;
        }

        private void UpdateOrCreateStoreroomInventory(BestContext context, OrderItemViewModel item)
        {
            var tempInventory = context.Inventories
                .FirstOrDefault(i => i.ItemId == item.ItemId &&
                                    i.SiteId == currentStoreId &&
                                    i.ItemLocation == "STOREROOM");

            if (tempInventory == null)
            {
                tempInventory = new Inventory
                {
                    ItemId = item.ItemId,
                    SiteId = currentStoreId,
                    ItemLocation = "STOREROOM",
                    Quantity = item.Quantity,
                    OptimumThreshold = 0
                };
                context.Inventories.Add(tempInventory);
            }
            else
            {
                tempInventory.Quantity += item.Quantity;
            }
        }

        private void ProcessDeliveredOrder(BestContext context, Txn order)
        {
            MoveItemsFromStoreroomToRegularInventory(context);

            order.TxnStatus = "COMPLETE";
            context.SaveChanges();

            LogOrderCompleted(order);
            Growl.Success(new GrowlInfo { Message = "Order accepted! Status: COMPLETE" });
        }

        private void MoveItemsFromStoreroomToRegularInventory(BestContext context)
        {
            var storeroomItems = context.Inventories
                .Where(i => i.SiteId == currentStoreId && i.ItemLocation == "STOREROOM")
                .ToList();

            foreach (var item in storeroomItems)
            {
                var regularInventory = context.Inventories
                    .FirstOrDefault(i => i.ItemId == item.ItemId &&
                                        i.SiteId == currentStoreId &&
                                        i.ItemLocation != "STOREROOM");

                if (regularInventory != null)
                {
                    regularInventory.Quantity += item.Quantity;
                    context.Inventories.Remove(item);
                }
            }
        }

        private void LogOrderReceived(Txn order)
        {
            AuditTransactions.LogActivity(
                currentUser, order.TxnId, order.TxnType, "DELIVERED",
                order.SiteIdto, currentDeliveryId,
                $"Order received. Items in STOREROOM."
            );
        }

        private void LogOrderCompleted(Txn order)
        {
            AuditTransactions.LogActivity(
                currentUser, order.TxnId, order.TxnType, "COMPLETE",
                order.SiteIdto, order.DeliveryId ?? 0,
                $"Order accepted and finalized with signature by {currentUser.FirstName} {currentUser.LastName}"
            );
        }

        private void ResetUIAfterCompletion()
        {
            LoadPendingOrders();
            acceptedItems.Clear();
            dgvStoreItems.ItemsSource = null;
            ResetSignature();
        }

        private void ResetSignature()
        {
            isSignatureProvided = false;
            inkSignature.Strokes.Clear();
            signatureSection.Visibility = Visibility.Collapsed;
        }

        private void btnSign_Click(object sender, RoutedEventArgs e)
        {
            if (inkSignature.Strokes.Count > 0)
            {
                isSignatureProvided = true;
                btnSign.IsEnabled = false;
                btnClearSignature.IsEnabled = true;

                CheckOrderCompletion();

                Growl.Success(new GrowlInfo
                {
                    Message = "Signature provided successfully",
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

        private void btnClearSignature_Click(object sender, RoutedEventArgs e)
        {
            ClearSignature();
        }

        private bool CheckTruckInventory(int itemId, int requiredQuantity)
        {
            try
            {
                using (var context = new BestContext())
                {
                    var truckInventory = context.Inventories
                        .Where(i => i.ItemId == itemId && i.SiteId == 9999)
                        .Sum(i => i.Quantity);

                    return truckInventory >= requiredQuantity;
                }
            }
            catch
            {
                return false;
            }
        }

        private void ClearSignature()
        {
            inkSignature.Strokes.Clear();
            isSignatureProvided = false;
            btnSign.IsEnabled = true;
            btnClearSignature.IsEnabled = false;

            // Update completion status
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
    }
}