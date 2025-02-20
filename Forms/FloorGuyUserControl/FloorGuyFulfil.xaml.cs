using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Mohammed on January 26,2025
namespace ISDP2025_Parfonov_Zerrou.Forms.FloorGuyUserControl
{
    public partial class FloorGuyFulfil : UserControl
    {
        Employee currentUser;
        List<OrderViewModel> allOrders;
        List<OrderItemViewModel> orderItems;
        List<OrderItemViewModel> assembledItems;
        int[] notStores = { 1, 2, 3, 9999, 10000 };

        public FloorGuyFulfil(Employee employee)
        {
            InitializeComponent();
            currentUser = employee;
            assembledItems = new List<OrderItemViewModel>();
            InitializeControls(false);
            PopulateSitesComboBox();
            cboStores.SelectedIndex = 0;
        }

        private void InitializeControls(bool isEnabled)
        {
            btnComplete.IsEnabled = false;
            btnMoveToAssembled.IsEnabled = false;
            btnMoveBack.IsEnabled = false;
            txtSearch.IsEnabled = isEnabled;
            cboStores.IsEnabled = isEnabled;
            dgvOrders.ItemsSource = null;
            dgvOrderItems.ItemsSource = null;
            dgvAssembledItems.ItemsSource = null;
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
                MessageBox.Show($"Error loading stores: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOpenOrders()
        {
            try
            {
                using (var context = new BestContext())
                {
                    allOrders = context.Txns
                            .Include(t => t.SiteIdtoNavigation)
                            .Where(t => t.TxnStatus == "ASSEMBLING" &&
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
                MessageBox.Show($"Error loading orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateOrdersGrid()
        {
            if (allOrders == null) return;

            var displayOrders = allOrders;
            if (cboStores.SelectedItem is Site selectedSite && selectedSite.SiteId != 0)
            {
                displayOrders = allOrders.Where(o => o.SiteIdto == selectedSite.SiteId).ToList();
            }

            dgvOrders.ItemsSource = displayOrders;
        }

        private void LoadOrderItems(int txnId)
        {
            try
            {
                using (var context = new BestContext())
                {
                    var query = from ti in context.Txnitems
                                join i in context.Items on ti.ItemId equals i.ItemId
                                join inv in context.Inventories
                                on new { ti.ItemId, SiteId = 2 }
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
                                };

                    orderItems = query.ToList();
                    dgvOrderItems.ItemsSource = orderItems;
                    assembledItems.Clear();
                    dgvAssembledItems.ItemsSource = null;
                    dgvAssembledItems.ItemsSource = assembledItems;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading order items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MoveItemToAssembled(OrderItemViewModel item)
        {
            try
            {
                var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
                if (selectedOrder == null) return;

                if (item.CurrentStock < item.CaseSize)
                {
                    MessageBox.Show($"Insufficient stock for {item.Name}. Current stock: {item.CurrentStock}",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new BestContext())
                {
                    var destInventory = context.Inventories
                        .FirstOrDefault(i => i.ItemId == item.ItemId && i.SiteId == 3);

                    if (destInventory == null)
                    {
                        destInventory = new Inventory
                        {
                            ItemId = item.ItemId,
                            SiteId = 3,
                            ItemLocation = "Stock",
                            Quantity = 0,
                            OptimumThreshold = 0
                        };
                        context.Inventories.Add(destInventory);
                        context.SaveChanges();
                    }
                }

                if (MoveInventory.Move(item.ItemId, item.CaseSize, 2, 3))
                {
                    item.CurrentStock -= item.CaseSize;
                    item.Quantity -= item.CaseSize;

                    var assembledItem = assembledItems.FirstOrDefault(i => i.ItemId == item.ItemId);
                    if (assembledItem != null)
                    {
                        assembledItem.Quantity += item.CaseSize;
                    }
                    else
                    {
                        assembledItems.Add(new OrderItemViewModel
                        {
                            ItemId = item.ItemId,
                            Name = item.Name,
                            Barcode = item.Barcode,
                            Quantity = item.CaseSize,
                            CaseSize = item.CaseSize,
                            CurrentStock = item.CurrentStock
                        });
                    }

                    var originalItem = orderItems.FirstOrDefault(i => i.ItemId == item.ItemId);
                    if (originalItem != null && originalItem.Quantity <= 0)
                    {
                        orderItems.Remove(originalItem);
                    }

                    var popup = new ItemToCartMovement($"Moving {item.CaseSize} units of {item.Name} to assembly area");
                    popup.Show();

                    RefreshGrids();
                    CheckOrderCompletion();
                }
                else
                {
                    MessageBox.Show($"Failed to move {item.Name} from warehouse to assembly area. Please check inventory levels.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshGrids()
        {
            dgvOrderItems.ItemsSource = null;
            dgvOrderItems.ItemsSource = orderItems;
            dgvAssembledItems.ItemsSource = null;
            dgvAssembledItems.ItemsSource = assembledItems;
        }

        private void CheckOrderCompletion()
        {
            btnComplete.IsEnabled = orderItems == null || !orderItems.Any();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            InitializeControls(true);
            LoadOpenOrders();
            txtSearch.IsEnabled = true;
        }

        private void cboStores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateOrdersGrid();
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
            if (dgvOrders.SelectedItem is OrderViewModel selectedOrder)
            {
                LoadOrderItems(selectedOrder.TxnId);
            }
        }

        private void dgvOrderItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvOrderItems.SelectedItem is OrderItemViewModel selectedItem)
            {
                btnMoveToAssembled.IsEnabled = selectedItem.CurrentStock >= selectedItem.CaseSize;
            }
        }

        private void dgvAssembledItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnMoveBack.IsEnabled = dgvAssembledItems.SelectedItem != null;
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
            var selectedItem = dgvAssembledItems.SelectedItem as OrderItemViewModel;
            var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;

            if (selectedItem?.Quantity > 0 && selectedOrder != null)
            {
                if (MoveInventory.Move(selectedItem.ItemId, selectedItem.CaseSize, 3, 2))
                {
                    var orderItem = orderItems.FirstOrDefault(i => i.ItemId == selectedItem.ItemId);
                    if (orderItem == null)
                    {
                        orderItem = new OrderItemViewModel
                        {
                            ItemId = selectedItem.ItemId,
                            Name = selectedItem.Name,
                            Barcode = selectedItem.Barcode,
                            Quantity = selectedItem.Quantity,
                            CaseSize = selectedItem.CaseSize,
                            CurrentStock = 0
                        };
                        orderItems.Add(orderItem);
                    }

                    orderItem.CurrentStock += selectedItem.CaseSize;
                    selectedItem.Quantity -= selectedItem.CaseSize;

                    var popup = new ItemToCartMovement($"Moving {selectedItem.CaseSize} units of {selectedItem.Name} back to warehouse");
                    popup.Show();

                    if (selectedItem.Quantity == 0)
                    {
                        assembledItems.Remove(selectedItem);
                    }

                    RefreshGrids();
                    CheckOrderCompletion();
                }
                else
                {
                    MessageBox.Show("Failed to move item back to warehouse", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void btnComplete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedOrder = dgvOrders.SelectedItem as OrderViewModel;
                if (selectedOrder != null)
                {
                    using (var context = new BestContext())
                    {
                        var order = context.Txns.Find(selectedOrder.TxnId);
                        if (order != null)
                        {
                            order.TxnStatus = "ASSEMBLED";
                            context.SaveChanges();

                            var totalItems = assembledItems.Sum(i => i.Quantity);

                            AuditTransactions.LogActivity(
                                currentUser,
                                order.TxnId,
                                order.TxnType,
                                "ASSEMBLED",
                                order.SiteIdto,
                                null,
                                $"Order assembly completed. Total items assembled: {totalItems}"
                            );

                            MessageBox.Show("Order marked as assembled", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadOpenOrders();
                            InitializeControls(true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error completing order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class OrderViewModel
    {
        public int TxnId { get; set; }
        public string StoreName { get; set; }
        public string TxnType { get; set; }
        public DateTime ShipDate { get; set; }
        public string TxnStatus { get; set; }
        public int SiteIdto { get; set; }
        public string Notes { get; set; }
    }

    public class OrderItemViewModel
    {
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public int Quantity { get; set; }
        public int CaseSize { get; set; }
        public int CurrentStock { get; set; }
        public bool InsufficientStock => CurrentStock < Quantity;
    }
}