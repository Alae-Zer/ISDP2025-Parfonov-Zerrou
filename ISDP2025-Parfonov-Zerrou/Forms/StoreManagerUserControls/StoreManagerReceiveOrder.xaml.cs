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

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Mohammed on March 18,2025

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
        private string userPermission;
        private bool isAdmin = false;
        private bool isInitialSelectionChange = true;

        // Constructor initializes the user control with employee information and permissions
        public StoreManagerReceiveOrder(Employee employee, string permission)
        {
            InitializeComponent();
            currentUser = employee;
            userPermission = permission;
            isAdmin = permission == "Admin";
            currentStoreId = employee.SiteId;
            acceptedItems = new List<OrderItemViewModel>();

            InitializeControls();
            InitializeSignatureCanvas();

            // Initialize site selector for admin users
            if (isAdmin)
            {
                InitializeSiteSelector();
            }
        }

        // Sets up initial UI state and ensures all controls are in default state
        private void InitializeControls()
        {
            btnComplete.IsEnabled = false;
            btnMoveToAccepted.IsEnabled = false;
            btnMoveBack.IsEnabled = false;
            txtSearch.IsEnabled = true;
            dgvOrders.ItemsSource = null;
            dgvDeliveredItems.ItemsSource = null;
            dgvStoreItems.ItemsSource = null;

            signatureSection.Visibility = Visibility.Collapsed;
            isSignatureProvided = false;
            inkSignature.Strokes.Clear();
            btnSign.IsEnabled = true;
            btnClearSignature.IsEnabled = false;

            // Set visibility of site selector based on user permission
            cmbSiteSelector.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            lblSiteSelector.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        // Configures the signature canvas with appropriate drawing attributes
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

        // Initializes site selector combobox for admin users, preventing initial order loading
        private void InitializeSiteSelector()
        {
            try
            {
                using (var context = new BestContext())
                {
                    var excludedSites = new[] { 1, 2, 3, 9999, 10000 };

                    // Get all sites except the excluded ones
                    var sites = context.Sites
                        .Where(s => !excludedSites.Contains(s.SiteId))
                        .OrderBy(s => s.SiteId)
                        .Select(s => new { s.SiteId, s.SiteName })
                        .ToList();

                    // Create a list with "All Sites" as the first option
                    var siteOptions = new List<object>
                    {
                        new { SiteId = 0, SiteName = "All Sites" }
                    };

                    // Add the sites from the database
                    siteOptions.AddRange(sites);

                    // Temporarily remove the event handler to prevent auto-loading
                    cmbSiteSelector.SelectionChanged -= cmbSiteSelector_SelectionChanged;

                    cmbSiteSelector.DisplayMemberPath = "SiteName";
                    cmbSiteSelector.SelectedValuePath = "SiteId";
                    cmbSiteSelector.ItemsSource = siteOptions;
                    cmbSiteSelector.SelectedIndex = 0;

                    // Reattach the event handler after setting the selected item
                    cmbSiteSelector.SelectionChanged += cmbSiteSelector_SelectionChanged;
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading sites: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Loads pending orders from the database with appropriate filtering based on user role
        private void LoadPendingOrders()
        {
            try
            {
                using (var context = new BestContext())
                {
                    var query = context.Txns
                        .Include(t => t.SiteIdtoNavigation)
                        .Where(t => (t.TxnStatus == "IN TRANSIT" || t.TxnStatus == "DELIVERED") &&
                                  (t.TxnType == "Store Order" ||
                                   t.TxnType == "Emergency Order" ||
                                   t.TxnType == "Back Order"));

                    // Apply site filter based on user role and selection
                    if (!isAdmin)
                    {
                        // Non-admin users can only see their own store's orders
                        query = query.Where(t => t.SiteIdto == currentStoreId);
                    }
                    else
                    {
                        // Admin users can see orders for all stores or a specific store
                        int selectedSiteId = (int)cmbSiteSelector.SelectedValue;
                        if (selectedSiteId != 0) // If not "All Sites"
                        {
                            query = query.Where(t => t.SiteIdto == selectedSiteId);
                        }
                    }

                    pendingOrders = query
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

        // Updates UI elements based on the order status (IN TRANSIT or DELIVERED)
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

        // Loads items for a selected delivery transaction
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

        // Retrieves the delivery ID and updates UI based on the order status
        private void LoadDeliveryIdAndUpdateUI(BestContext context, int txnId)
        {
            var order = context.Txns.Find(txnId);
            if (order != null && order.DeliveryId.HasValue)
            {
                currentDeliveryId = order.DeliveryId.Value;
                UpdateUIForOrderStatus(order.TxnStatus);

                // If Admin selects an order for a specific store, update currentStoreId
                if (isAdmin)
                {
                    currentStoreId = order.SiteIdto;
                }
            }
        }

        // Loads the specific items in an order including stock information
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

        // Clears accepted items list and resets signature information
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

        // Moves an item from the delivered list to the accepted list
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
                UpdateSignatureSectionVisibility();
                RefreshGrids();

                // Simply focus and select the first item if available
                if (deliveredItems.Count > 0)
                {
                    dgvDeliveredItems.SelectedItem = deliveredItems[0];
                    dgvDeliveredItems.Focus();
                }

                CheckOrderCompletion();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error accepting item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Moves an item from the accepted list back to the delivered list
        private void MoveItemBackToDelivered(OrderItemViewModel selectedItem)
        {
            int quantityToMoveBack = selectedItem.Quantity;

            AddItemBackToDeliveredList(selectedItem, quantityToMoveBack);
            acceptedItems.Remove(selectedItem);

            ShowMoveBackSuccessNotification(selectedItem, quantityToMoveBack);
            UpdateSignatureSectionAfterRemove();
            RefreshGrids();

            // Simply focus and select the first item if available
            if (acceptedItems.Count > 0)
            {
                dgvStoreItems.SelectedItem = acceptedItems[0];
                dgvStoreItems.Focus();
            }

            CheckOrderCompletion();
        }

        // Shows signature section when all items have been moved to acceptance
        private void UpdateSignatureSectionVisibility()
        {
            signatureSection.Visibility = deliveredItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // Validates that there is sufficient inventory for moving an item
        private bool ValidateInventoryForItem(OrderViewModel selectedOrder, OrderItemViewModel item)
        {
            bool hasEnoughStock = false;

            // Use the selected order's store ID when checking inventory
            int targetStoreId = selectedOrder.SiteIdto;

            if (selectedOrder.TxnStatus == "IN TRANSIT")
            {
                hasEnoughStock = CheckTruckInventory(item.ItemId, item.Quantity);
            }
            else if (selectedOrder.TxnStatus == "DELIVERED")
            {
                hasEnoughStock = CheckStoreroomInventory(item.ItemId, item.Quantity, targetStoreId);
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

        // Adds an item to the accepted list, combining quantities if already present
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

        // Displays a success notification after accepting an item
        private void ShowSuccessNotification(OrderItemViewModel item)
        {
            Growl.Success(new GrowlInfo
            {
                Message = $"Accepted {item.Quantity} units of {item.Name}",
                ShowDateTime = false,
                WaitTime = 2
            });
        }

        // Checks if there's enough inventory in the storeroom for an item
        private bool CheckStoreroomInventory(int itemId, int requiredQuantity, int storeId)
        {
            try
            {
                using (var context = new BestContext())
                {
                    var storeroomInventory = context.Inventories
                        .Where(i => i.ItemId == itemId &&
                                   i.SiteId == storeId &&
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

        // Refreshes both grids after item movement
        private void RefreshGrids()
        {
            dgvDeliveredItems.ItemsSource = null;
            dgvDeliveredItems.ItemsSource = deliveredItems;
            dgvStoreItems.ItemsSource = null;
            dgvStoreItems.ItemsSource = acceptedItems;
        }

        // Enables/disables the complete button based on items and signature status
        private void CheckOrderCompletion()
        {
            btnComplete.IsEnabled = acceptedItems.Count > 0 && isSignatureProvided;
        }

        // Refreshes the orders list when the refresh button is clicked
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            InitializeControls();
            LoadPendingOrders(); // Only load orders when refresh button is clicked
        }

        // Handles site selection changes for admin users, preventing initial loading
        private void cmbSiteSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitialSelectionChange)
            {
                isInitialSelectionChange = false;
                return;
            }

            if (cmbSiteSelector.SelectedValue != null)
            {
                int selectedSiteId = (int)cmbSiteSelector.SelectedValue;
                if (selectedSiteId != 0) // If not "All Sites"
                {
                    currentStoreId = selectedSiteId;
                }
                // Reload orders with the new site filter
                LoadPendingOrders();
            }
        }

        // Filters displayed items based on search text
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

        // Loads items when an order is selected
        private void dgvOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvOrders.SelectedItem is OrderViewModel selectedOrder)
            {
                LoadDeliveryItems(selectedOrder.TxnId);
            }
        }

        // Enables/disables the move to accepted button based on selection
        private void dgvDeliveredItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
            if (selectedOrder != null)
            {
                btnMoveToAccepted.IsEnabled = dgvDeliveredItems.SelectedItem != null;
            }
            else
            {
                btnMoveToAccepted.IsEnabled = false;
            }
        }

        // Enables/disables the move back button based on selection
        private void dgvStoreItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
            if (selectedOrder != null)
            {
                btnMoveBack.IsEnabled = dgvStoreItems.SelectedItem != null;
            }
            else
            {
                btnMoveBack.IsEnabled = false;
            }
        }

        // Moves the selected item to the accepted list
        private void btnMoveToAccepted_Click(object sender, RoutedEventArgs e)
        {
            if (dgvDeliveredItems.SelectedItem is OrderItemViewModel selectedItem)
            {
                MoveItemToStore(selectedItem);
            }
        }

        // Moves the selected item back to the delivered list
        private void btnMoveBack_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgvStoreItems.SelectedItem as OrderItemViewModel;
            if (selectedItem?.Quantity > 0)
            {
                MoveItemBackToDelivered(selectedItem);
            }
        }

        // Adds an item back to the delivered list, combining quantities if already present
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

        // Shows success notification after moving an item back
        private void ShowMoveBackSuccessNotification(OrderItemViewModel selectedItem, int quantityToMoveBack)
        {
            Growl.Success(new GrowlInfo
            {
                Message = $"Removed {quantityToMoveBack} units of {selectedItem.Name} from acceptance",
                ShowDateTime = false,
                WaitTime = 2
            });
        }

        // Updates signature section visibility after removing items
        private void UpdateSignatureSectionAfterRemove()
        {
            if (acceptedItems.Count == 0 || deliveredItems.Count == 0)
            {
                signatureSection.Visibility = Visibility.Collapsed;
                isSignatureProvided = false;
                inkSignature.Strokes.Clear();
                btnSign.IsEnabled = true;
                btnClearSignature.IsEnabled = false;
            }
        }

        // Completes the order receiving process with signature validation
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

                    // Update the currentStoreId to the order's destination for correct processing
                    currentStoreId = order.SiteIdto;

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

        // Validates that all required information is provided before completing action
        private bool ValidateCompleteAction()
        {
            if (!isSignatureProvided || acceptedItems.Count == 0) return false;
            var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
            return selectedOrder != null;
        }

        // Processes an IN TRANSIT order, moving items from truck to storeroom
        private void ProcessInTransitOrder(BestContext context, Txn order)
        {
            bool allMoved = MoveItemsFromTruckToStoreroom(context, order.SiteIdto);

            if (allMoved)
            {
                order.TxnStatus = "DELIVERED";
                context.SaveChanges();

                LogOrderReceived(order);
                Growl.Success(new GrowlInfo { Message = "Order received! Status: DELIVERED" });
                btnComplete.IsEnabled = false;
            }
        }

        // Moves items from truck inventory to storeroom inventory
        private bool MoveItemsFromTruckToStoreroom(BestContext context, int storeId)
        {
            foreach (var item in acceptedItems)
            {
                var truckInventory = context.Inventories
                    .FirstOrDefault(i => i.ItemId == item.ItemId && i.SiteId == 9999);

                if (truckInventory == null || truckInventory.Quantity < item.Quantity)
                    return false;

                truckInventory.Quantity -= item.Quantity;
                UpdateOrCreateStoreroomInventory(context, item, storeId);

                if (truckInventory.Quantity == 0)
                    context.Inventories.Remove(truckInventory);
            }
            return true;
        }

        // Updates or creates storeroom inventory records
        private void UpdateOrCreateStoreroomInventory(BestContext context, OrderItemViewModel item, int storeId)
        {
            var tempInventory = context.Inventories
                .FirstOrDefault(i => i.ItemId == item.ItemId &&
                                    i.SiteId == storeId &&
                                    i.ItemLocation == "STOREROOM");

            if (tempInventory == null)
            {
                tempInventory = new Inventory
                {
                    ItemId = item.ItemId,
                    SiteId = storeId,
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

        // Processes a DELIVERED order, moving items from storeroom to regular inventory
        private void ProcessDeliveredOrder(BestContext context, Txn order)
        {
            MoveItemsFromStoreroomToRegularInventory(context, order.SiteIdto);

            order.TxnStatus = "COMPLETE";
            context.SaveChanges();

            LogOrderCompleted(order);
            Growl.Success(new GrowlInfo { Message = "Order accepted! Status: COMPLETE" });
            btnComplete.IsEnabled = false;
        }

        // Moves all storeroom items to regular inventory for the store
        private void MoveItemsFromStoreroomToRegularInventory(BestContext context, int storeId)
        {
            var storeroomItems = context.Inventories
                .Where(i => i.SiteId == storeId && i.ItemLocation == "STOREROOM")
                .ToList();

            foreach (var item in storeroomItems)
            {
                var regularInventory = context.Inventories
                    .FirstOrDefault(i => i.ItemId == item.ItemId &&
                                        i.SiteId == storeId &&
                                        i.ItemLocation != "STOREROOM");

                if (regularInventory != null)
                {
                    regularInventory.Quantity += item.Quantity;
                    context.Inventories.Remove(item);
                }
            }
        }

        // Logs the order receiving activity for audit purposes
        private void LogOrderReceived(Txn order)
        {
            AuditTransactions.LogActivity(
                currentUser, order.TxnId, order.TxnType, "DELIVERED",
                order.SiteIdto, currentDeliveryId,
                $"Order received. Items in STOREROOM."
            );
        }

        // Logs the order completion activity for audit purposes
        private void LogOrderCompleted(Txn order)
        {
            AuditTransactions.LogActivity(
                currentUser, order.TxnId, order.TxnType, "COMPLETE",
                order.SiteIdto, order.DeliveryId ?? 0,
                $"Order accepted and finalized with signature by {currentUser.FirstName} {currentUser.LastName}"
            );
        }

        // Resets the UI after completing an order
        private void ResetUIAfterCompletion()
        {
            LoadPendingOrders();
            acceptedItems.Clear();
            dgvStoreItems.ItemsSource = null;
            ResetSignature();
        }

        // Resets the signature component
        private void ResetSignature()
        {
            isSignatureProvided = false;
            inkSignature.Strokes.Clear();
            signatureSection.Visibility = Visibility.Collapsed;
        }

        // Confirms the signature when the sign button is clicked
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

        // Clears the signature when the clear button is clicked
        private void btnClearSignature_Click(object sender, RoutedEventArgs e)
        {
            ClearSignature();
        }

        // Checks if there's enough inventory in the truck for an item
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

        // Clears the signature and resets related controls
        private void ClearSignature()
        {
            inkSignature.Strokes.Clear();
            isSignatureProvided = false;
            btnSign.IsEnabled = true;
            btnClearSignature.IsEnabled = false;
            btnComplete.IsEnabled = false;
        }

        // Converts the signature ink strokes to a byte array for storage
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