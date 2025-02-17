using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ISDP2025_Parfonov_Zerrou.Forms.FloorGuyUserControl
{
    public partial class FloorGuyFulfil : UserControl
    {
        private readonly Employee currentUser;
        private List<OrderItemViewModel> orderItems;
        private List<OrderItemViewModel> assembledItems;

        public FloorGuyFulfil(Employee employee)
        {
            InitializeComponent();
            currentUser = employee;
            LoadOpenOrders();
            InitializeControls();
        }

        private void InitializeControls()
        {
            assembledItems = new List<OrderItemViewModel>();
            txtBarcode.Focus();
            btnComplete.IsEnabled = false;
            btnMoveToAssembled.IsEnabled = false;
            btnMoveBack.IsEnabled = false;
        }

        private void LoadOpenOrders()
        {
            try
            {
                using (var context = new BestContext())
                {
                    var orders = context.Txns
                        .Include(t => t.SiteIdtoNavigation)
                        .Include(t => t.Employee)
                        .Where(t => t.TxnStatus == "ASSEMBLING")
                        .Select(t => new
                        {
                            t.TxnId,
                            t.SiteIdtoNavigation.SiteName,
                            t.CreatedDate,
                            t.ShipDate,
                            t.TxnStatus,
                            AssemblerName = $"{t.Employee.FirstName} {t.Employee.LastName}",
                            t.Notes
                        })
                        .ToList();

                    dgvOrders.ItemsSource = orders;
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Error($"Error loading orders: {ex.Message}", "Error");
            }
        }

        private void LoadOrderItems(int txnId)
        {
            try
            {
                using (var context = new BestContext())
                {
                    orderItems = context.Txnitems
                        .Include(ti => ti.Item)
                        .Where(ti => ti.TxnId == txnId)
                        .Select(ti => new OrderItemViewModel
                        {
                            ItemId = ti.ItemId,
                            Name = ti.Item.Name,
                            Required = ti.Quantity,
                            Remaining = ti.Quantity
                        })
                        .ToList();

                    dgvOrderItems.ItemsSource = orderItems;
                    assembledItems.Clear();
                    dgvAssembledItems.ItemsSource = null;
                    dgvAssembledItems.ItemsSource = assembledItems;
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Error($"Error loading order items: {ex.Message}", "Error");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.ToLower();
            if (orderItems != null)
            {
                var filteredItems = orderItems.Where(i =>
                    i.Name.ToLower().Contains(searchText) ||
                    i.ItemId.ToString().Contains(searchText)).ToList();
                dgvOrderItems.ItemsSource = filteredItems;
            }
        }

        private void Barcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    string barcode = txtBarcode.Text.Trim();
                    if (string.IsNullOrEmpty(barcode)) return;

                    var item = orderItems?.FirstOrDefault(i =>
                        i.ItemId.ToString() == barcode && i.Remaining > 0);

                    if (item != null)
                    {
                        MoveItemToAssembled(item);
                        HandyControl.Controls.MessageBox.Success($"Scanned: {item.Name}", "Success");
                    }
                    else
                    {
                        HandyControl.Controls.MessageBox.Warning("Item not found or already assembled", "Warning");
                    }

                    txtBarcode.Clear();
                    txtBarcode.Focus();
                }
                catch (Exception ex)
                {
                    HandyControl.Controls.MessageBox.Error($"Error processing barcode: {ex.Message}", "Error");
                }
            }
        }

        private void MoveItemToAssembled(OrderItemViewModel item)
        {
            if (MoveInventory.Move(item.ItemId, 1, 2, 3))
            {
                item.Remaining--;

                var assembledItem = assembledItems.FirstOrDefault(i => i.ItemId == item.ItemId);
                if (assembledItem != null)
                {
                    assembledItem.Assembled++;
                }
                else
                {
                    assembledItems.Add(new OrderItemViewModel
                    {
                        ItemId = item.ItemId,
                        Name = item.Name,
                        Required = item.Required,
                        Assembled = 1
                    });
                }

                RefreshGrids();
                CheckOrderCompletion();
            }
            else
            {
                HandyControl.Controls.MessageBox.Warning("Failed to move inventory", "Warning");
            }
        }

        private void RefreshGrids()
        {
            dgvOrderItems.Items.Refresh();
            dgvAssembledItems.ItemsSource = null;
            dgvAssembledItems.ItemsSource = assembledItems;
        }

        private void CheckOrderCompletion()
        {
            btnComplete.IsEnabled = orderItems?.All(i => i.Remaining == 0) ?? false;
        }

        private void btnComplete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgvOrders.SelectedItem != null)
                {
                    int txnId = (int)((dynamic)dgvOrders.SelectedItem).TxnId;
                    using (var context = new BestContext())
                    {
                        var order = context.Txns.Find(txnId);
                        if (order != null)
                        {
                            order.TxnStatus = "ASSEMBLED";
                            context.SaveChanges();

                            AuditTransactions.LogActivity(
                                currentUser,
                                txnId,
                                order.TxnType,
                                "ASSEMBLED",
                                order.SiteIdto);

                            HandyControl.Controls.MessageBox.Success("Order marked as assembled", "Success");

                            LoadOpenOrders();
                            ClearGrids();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Error($"Error completing order: {ex.Message}", "Error");
            }
        }
        private void ClearGrids()
        {
            dgvOrderItems.ItemsSource = null;
            dgvAssembledItems.ItemsSource = null;
            btnComplete.IsEnabled = false;
            btnMoveToAssembled.IsEnabled = false;
            btnMoveBack.IsEnabled = false;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadOpenOrders();
            ClearGrids();
        }

        private void dgvOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvOrders.SelectedItem != null)
            {
                int txnId = (int)((dynamic)dgvOrders.SelectedItem).TxnId;
                LoadOrderItems(txnId);
            }
        }

        private void dgvOrderItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnMoveToAssembled.IsEnabled = dgvOrderItems.SelectedItem != null &&
                ((OrderItemViewModel)dgvOrderItems.SelectedItem).Remaining > 0;
        }

        private void dgvAssembledItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnMoveBack.IsEnabled = dgvAssembledItems.SelectedItem != null &&
                ((OrderItemViewModel)dgvAssembledItems.SelectedItem).Assembled > 0;
        }

        private void btnMoveToAssembled_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgvOrderItems.SelectedItem as OrderItemViewModel;
            if (selectedItem?.Remaining > 0)
            {
                MoveItemToAssembled(selectedItem);
            }
        }

        private void btnMoveBack_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgvAssembledItems.SelectedItem as OrderItemViewModel;
            if (selectedItem?.Assembled > 0)
            {
                if (MoveInventory.Move(selectedItem.ItemId, 1, 3, 2))
                {
                    var orderItem = orderItems.First(i => i.ItemId == selectedItem.ItemId);
                    orderItem.Remaining++;
                    selectedItem.Assembled--;

                    if (selectedItem.Assembled == 0)
                    {
                        assembledItems.Remove(selectedItem);
                    }

                    RefreshGrids();
                    CheckOrderCompletion();
                }
                else
                {
                    HandyControl.Controls.MessageBox.Warning("Failed to move item back to warehouse", "Warning");
                }
            }
        }

        public class OrderItemViewModel
        {
            public int ItemId { get; set; }
            public string Name { get; set; }
            public int Required { get; set; }
            public int Assembled { get; set; }
            public int Remaining { get; set; }
        }
    }
}