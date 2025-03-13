using HandyControl.Controls;
using HandyControl.Data;
using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Last Modified: March 2025
namespace ISDP2025_Parfonov_Zerrou.Forms.StoreManagerUserControls
{
    public partial class StroreManagerAcceptAndFulfilOnlineOrder : UserControl
    {
        private Employee currentUser;
        private List<OnlineOrderViewModel> allOrders;
        private List<OrderItemViewModel> orderItems;
        private List<OrderItemViewModel> preparedItems;

        public StroreManagerAcceptAndFulfilOnlineOrder(Employee employee)
        {
            InitializeComponent();
            currentUser = employee;
            preparedItems = new List<OrderItemViewModel>();
            InitializeControls();
            cboStatus.SelectedIndex = 0;
        }

        private void InitializeControls()
        {
            btnComplete.IsEnabled = false;
            txtSearch.IsEnabled = false;
            dgvOrders.ItemsSource = null;
            dgvOrderItems.ItemsSource = null;
            dgvPreparedItems.ItemsSource = null;
        }

        private void LoadOnlineOrders()
        {
            try
            {
                using (var context = new BestContext())
                {
                    // Base query to get online orders for the current store
                    var query = context.Txns
                        .Include(t => t.SiteIdtoNavigation)
                        .Where(t => t.TxnType == "Online" && t.SiteIdto == currentUser.SiteId)
                        .OrderByDescending(t => t.CreatedDate)
                        .AsQueryable();

                    // Apply status filter if selected
                    if (cboStatus.SelectedItem != null && cboStatus.SelectedIndex > 0)
                    {
                        string selectedStatus = ((ComboBoxItem)cboStatus.SelectedItem).Content.ToString();
                        query = query.Where(t => t.TxnStatus == selectedStatus);
                    }

                    // Transform to view model
                    allOrders = query.Select(o => new OnlineOrderViewModel
                    {
                        TxnId = o.TxnId,
                        CustomerName = o.Notes ?? "Unknown", // Customer info might be in Notes
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
                    // Get the current order
                    var txn = context.Txns.FirstOrDefault(t => t.TxnId == txnId);
                    if (txn == null) return;

                    // Query to get order items with current stock information
                    var query = from ti in context.Txnitems
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
                                    CurrentStock = inv.Quantity
                                };

                    orderItems = query.ToList();
                    dgvOrderItems.ItemsSource = orderItems;

                    // Clear prepared items if the order is NEW
                    if (txn.TxnStatus == "NEW")
                    {
                        preparedItems = new List<OrderItemViewModel>();
                        dgvPreparedItems.ItemsSource = preparedItems;

                        // Update Complete button for NEW orders
                        btnComplete.Content = "Accept Order";
                        btnComplete.IsEnabled = true;
                    }
                    // Load prepared items if order is already RECEIVED
                    else if (txn.TxnStatus == "RECEIVED")
                    {
                        // In a real implementation, you would load the reserved items
                        // For now, we'll just copy the order items to prepared items
                        preparedItems = orderItems.Select(i => new OrderItemViewModel
                        {
                            ItemId = i.ItemId,
                            Name = i.Name,
                            Barcode = i.Barcode,
                            Quantity = i.Quantity
                        }).ToList();

                        dgvPreparedItems.ItemsSource = preparedItems;

                        // Update Complete button for RECEIVED orders
                        btnComplete.Content = "Mark As Ready";
                        btnComplete.IsEnabled = true;
                    }
                    else if (txn.TxnStatus == "READY")
                    {
                        // Load prepared items for READY status
                        preparedItems = orderItems.Select(i => new OrderItemViewModel
                        {
                            ItemId = i.ItemId,
                            Name = i.Name,
                            Barcode = i.Barcode,
                            Quantity = i.Quantity
                        }).ToList();

                        dgvPreparedItems.ItemsSource = preparedItems;

                        // Update Complete button for READY orders
                        btnComplete.Content = "Complete Order";
                        btnComplete.IsEnabled = true;

                        // Show signature panel for READY orders
                        signaturePanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        // For completed or cancelled orders, just display items
                        preparedItems = orderItems.Select(i => new OrderItemViewModel
                        {
                            ItemId = i.ItemId,
                            Name = i.Name,
                            Barcode = i.Barcode,
                            Quantity = i.Quantity
                        }).ToList();

                        dgvPreparedItems.ItemsSource = preparedItems;

                        // Disable Complete button for completed orders
                        btnComplete.Content = "Complete Order";
                        btnComplete.IsEnabled = false;

                        // Hide signature panel
                        signaturePanel.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error loading order items: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CheckInventoryAvailability()
        {
            // Check if all items have sufficient stock
            return !orderItems.Any(i => i.CurrentStock < i.Quantity);
        }

        private void ReserveInventory(int txnId)
        {
            try
            {
                using (var context = new BestContext())
                {
                    // Update transaction status to RECEIVED
                    var txn = context.Txns.Find(txnId);
                    if (txn != null)
                    {
                        txn.TxnStatus = "RECEIVED";

                        // Update inventory by reserving items
                        foreach (var item in orderItems)
                        {
                            var inventory = context.Inventories.FirstOrDefault(
                                i => i.ItemId == item.ItemId && i.SiteId == currentUser.SiteId);

                            if (inventory != null)
                            {
                                // Reduce available inventory
                                inventory.Quantity -= item.Quantity;

                                // In a real implementation, you might move these to a "Reserved" location
                                // For simplicity, we're just reducing the available count
                            }
                        }

                        // Log the activity
                        AuditTransactions.LogActivity(
                            currentUser,
                            txn.TxnId,
                            txn.TxnType,
                            "RECEIVED",
                            txn.SiteIdto,
                            null,
                            $"Online order accepted. Items reserved."
                        );

                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error reserving inventory: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Re-throw to handle in the calling method
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            InitializeControls();
            LoadOnlineOrders();
        }

        private void cboStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (allOrders != null)
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
            }
        }

        private void dgvOrderItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Nothing specific needed here, but we'll keep the method for future functionality
        }

        private void dgvPreparedItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Nothing specific needed here, but we'll keep the method for future functionality
        }

        private void btnMoveToAssembled_Click(object sender, RoutedEventArgs e)
        {
            // Move selected item from order items to prepared items
            if (dgvOrderItems.SelectedItem is OrderItemViewModel selectedItem)
            {
                // Check if item already exists in prepared items
                var existingItem = preparedItems.FirstOrDefault(i => i.ItemId == selectedItem.ItemId);
                if (existingItem != null)
                {
                    // Update quantity if item already exists
                    existingItem.Quantity += 1;
                    // Decrease quantity in order items
                    selectedItem.Quantity -= 1;
                    // Remove if quantity becomes zero
                    if (selectedItem.Quantity <= 0)
                    {
                        orderItems.Remove(selectedItem);
                    }
                }
                else
                {
                    // Add new item to prepared items
                    preparedItems.Add(new OrderItemViewModel
                    {
                        ItemId = selectedItem.ItemId,
                        Name = selectedItem.Name,
                        Barcode = selectedItem.Barcode,
                        Quantity = 1
                    });
                    // Decrease quantity in order items
                    selectedItem.Quantity -= 1;
                    // Remove if quantity becomes zero
                    if (selectedItem.Quantity <= 0)
                    {
                        orderItems.Remove(selectedItem);
                    }
                }

                // Refresh data sources
                dgvOrderItems.ItemsSource = null;
                dgvOrderItems.ItemsSource = orderItems;
                dgvPreparedItems.ItemsSource = null;
                dgvPreparedItems.ItemsSource = preparedItems;
            }
        }

        private void btnMoveBack_Click(object sender, RoutedEventArgs e)
        {
            // Move selected item from prepared items back to order items
            if (dgvPreparedItems.SelectedItem is OrderItemViewModel selectedItem)
            {
                // Check if item already exists in order items
                var existingItem = orderItems.FirstOrDefault(i => i.ItemId == selectedItem.ItemId);
                if (existingItem != null)
                {
                    // Update quantity if item already exists
                    existingItem.Quantity += 1;
                    // Decrease quantity in prepared items
                    selectedItem.Quantity -= 1;
                    // Remove if quantity becomes zero
                    if (selectedItem.Quantity <= 0)
                    {
                        preparedItems.Remove(selectedItem);
                    }
                }
                else
                {
                    // Add new item to order items
                    orderItems.Add(new OrderItemViewModel
                    {
                        ItemId = selectedItem.ItemId,
                        Name = selectedItem.Name,
                        Barcode = selectedItem.Barcode,
                        Quantity = 1,
                        CurrentStock = selectedItem.CurrentStock
                    });
                    // Decrease quantity in prepared items
                    selectedItem.Quantity -= 1;
                    // Remove if quantity becomes zero
                    if (selectedItem.Quantity <= 0)
                    {
                        preparedItems.Remove(selectedItem);
                    }
                }

                // Refresh data sources
                dgvOrderItems.ItemsSource = null;
                dgvOrderItems.ItemsSource = orderItems;
                dgvPreparedItems.ItemsSource = null;
                dgvPreparedItems.ItemsSource = preparedItems;
            }
        }

        private void btnClearSignature_Click(object sender, RoutedEventArgs e)
        {
            // Clear the signature canvas
            inkSignature.Strokes.Clear();
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
                            // Handle different statuses
                            switch (txn.TxnStatus)
                            {
                                case "NEW":
                                    // Accept the order
                                    if (!CheckInventoryAvailability())
                                    {
                                        var result = HandyControl.Controls.MessageBox.Show(
                                            "Some items have insufficient stock. Do you still want to accept this order?",
                                            "Inventory Warning",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Warning);

                                        if (result == MessageBoxResult.No)
                                            return;
                                    }

                                    ReserveInventory(selectedOrder.TxnId);

                                    Growl.Success(new GrowlInfo
                                    {
                                        Message = "Order accepted and items reserved.",
                                        ShowDateTime = false,
                                        WaitTime = 2
                                    });
                                    break;

                                case "RECEIVED":
                                    // Mark as ready
                                    txn.TxnStatus = "READY";

                                    // Log the activity
                                    AuditTransactions.LogActivity(
                                        currentUser,
                                        txn.TxnId,
                                        txn.TxnType,
                                        "READY",
                                        txn.SiteIdto,
                                        null,
                                        $"Online order prepared and ready for pickup."
                                    );

                                    context.SaveChanges();

                                    Growl.Success(new GrowlInfo
                                    {
                                        Message = "Order marked as ready for pickup.",
                                        ShowDateTime = false,
                                        WaitTime = 2
                                    });
                                    break;

                                case "READY":
                                    // Check if signature is provided
                                    if (inkSignature.Strokes.Count == 0)
                                    {
                                        HandyControl.Controls.MessageBox.Show(
                                            "Customer signature is required to complete the order.",
                                            "Signature Required",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Warning);
                                        return;
                                    }

                                    // Complete the order
                                    txn.TxnStatus = "COMPLETE";

                                    // Log the activity
                                    AuditTransactions.LogActivity(
                                        currentUser,
                                        txn.TxnId,
                                        txn.TxnType,
                                        "COMPLETE",
                                        txn.SiteIdto,
                                        null,
                                        $"Online order completed and picked up by customer."
                                    );

                                    context.SaveChanges();

                                    Growl.Success(new GrowlInfo
                                    {
                                        Message = "Order completed successfully.",
                                        ShowDateTime = false,
                                        WaitTime = 2
                                    });

                                    // Hide signature panel after completion
                                    signaturePanel.Visibility = Visibility.Collapsed;
                                    break;
                            }

                            // Refresh the orders list and reload the current order
                            LoadOnlineOrders();
                            LoadOrderItems(selectedOrder.TxnId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error processing order: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        public string Barcode { get; set; } // Update to match Item.Sku
        public int Quantity { get; set; }
        public int CurrentStock { get; set; }
        public int CaseSize { get; set; }
        public bool InsufficientStock => CurrentStock < Quantity;
        public bool PickedUp { get; set; } = false;
    }
}