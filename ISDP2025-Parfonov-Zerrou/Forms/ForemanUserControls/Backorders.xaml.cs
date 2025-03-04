using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Managers;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Mohammed with some changes from serhii
//Last Modified by Serhii on Feb 16,2025
namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class Backorders : UserControl
    {
        //Declare All data for operations
        Employee currentUser;
        List<Txn> allBackorders;
        string[] allDays = { "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY" };
        BackorderManager backorderManager;
        string[] validStatuses = { "NEW", "RECEIVED", "ASSEMBLING" };

        public Backorders(Employee employee)
        {
            InitializeComponent();
            currentUser = employee;
            backorderManager = new BackorderManager(employee);
            InitializeControls(false);
            PopulateSitesComboBox();
            btnNewBackorder.IsEnabled = false;
            cboDeliveryDay.ItemsSource = allDays;
            txtSearch.IsEnabled = false;
        }

        private void UpdateControlsBasedOnStatus(string status)
        {
            switch (status)
            {
                case "NEW":
                    btnAdd.IsEnabled = true;
                    btnRemove.IsEnabled = true;
                    btnSave.IsEnabled = true;
                    btnSendToAssembly.IsEnabled = false;
                    cboDeliveryDay.IsEnabled = true;
                    dgvWarehouseItems.IsEnabled = true;
                    dgvBackItems.IsEnabled = true;
                    break;
                case "RECEIVED":
                    btnAdd.IsEnabled = true;
                    btnRemove.IsEnabled = true;
                    btnSave.IsEnabled = true;
                    btnSendToAssembly.IsEnabled = true;
                    cboDeliveryDay.IsEnabled = true;
                    dgvWarehouseItems.IsEnabled = true;
                    dgvBackItems.IsEnabled = true;
                    break;
                case "ASSEMBLING":
                    btnAdd.IsEnabled = false;
                    btnRemove.IsEnabled = false;
                    btnSave.IsEnabled = false;
                    btnSendToAssembly.IsEnabled = false;
                    cboDeliveryDay.IsEnabled = false;
                    dgvBackItems.IsEnabled = false;
                    dgvWarehouseItems.IsEnabled = false;

                    break;
                default:
                    btnAdd.IsEnabled = false;
                    btnRemove.IsEnabled = false;
                    btnSave.IsEnabled = false;
                    btnSendToAssembly.IsEnabled = false;
                    cboDeliveryDay.IsEnabled = false;
                    dgvBackItems.IsEnabled = false;
                    dgvWarehouseItems.IsEnabled = false;
                    break;
            }
        }

        //Enables/disables Controls based on received bool value
        private void InitializeControls(bool isEnable)
        {
            cboStores.IsEnabled = isEnable;
            btnAdd.IsEnabled = isEnable;
            btnSave.IsEnabled = isEnable;
            btnRemove.IsEnabled = isEnable;
            txtSearch.IsEnabled = isEnable;
        }

        //Populates combobox with stores
        private void PopulateSitesComboBox()
        {
            try
            {
                //Exclude NON-Store locations and Initialize list of objects
                int[] notStrores = { 1, 2, 3, 9999, 10000 };
                List<Site> allSites = new List<Site>();

                //Default value for combo, add to index 0
                allSites.Add(new Site { SiteId = 0, SiteName = "All Stores" });

                //Open connection using Python style making sure it will be closed gently (Thanks Steve)
                using (var context = new BestContext())
                {
                    //LOOP thorough context skipping inactive sites and containing NOT-Stores IDs
                    foreach (var site in context.Sites
                        .Where(s => s.Active == 1 && !notStrores.Contains(s.SiteId))
                        .Select(s => new { s.SiteId, s.SiteName }))
                    {
                        //Add site
                        allSites.Add(new Site { SiteId = site.SiteId, SiteName = site.SiteName });
                    }
                }

                //Bind Paramaetrs and select default value's index
                cboStores.ItemsSource = allSites;
                cboStores.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sites: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Loads Backorders to DGV
        private void LoadBackorders()
        {
            try
            {
                allBackorders = backorderManager.GetAllBackorders();
                dgvBackorders.ItemsSource = allBackorders;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading backorders: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Loads inventory and binds to DGV, Receives Optional Parameter
        private void LoadAvailableItems(string? searchText = null)
        {
            try
            {
                //Opens connection using Python style (Thanks Steve Again)
                using (var context = new BestContext())
                {
                    //QUERY
                    var query = context.Inventories
                        .Include(i => i.Item)
                        .Where(i => i.SiteId == 2 && i.Item.Active == 1);

                    //If Something in input
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        //Try to parse as a number
                        var search = searchText.ToLower();
                        bool isNumber = int.TryParse(searchText, out int searchID);

                        //Text Or Number Identified
                        query = query.Where(i =>
                            i.Item.Name.ToLower().Contains(search) ||
                            (isNumber && i.ItemId.ToString().Contains(searchText)));
                    }

                    //Bind Sourses
                    dgvWarehouseItems.ItemsSource = query
                        .Select(i => new BackorderItemViewModel
                        {
                            ItemId = i.ItemId,
                            Name = i.Item.Name,
                            Quantity = i.Quantity,
                            CaseSize = i.Item.CaseSize,
                            CurrentStock = i.Quantity,
                            OptimumThreshold = i.OptimumThreshold,
                            ReorderThreshold = i.ReorderThreshold ?? 0
                        })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Load Items From Backorder (Unfortunately Dynamic Won't work here)
        private void LoadBackorderItems(int txnId)
        {
            try
            {
                //Open connection - Thanks Steve
                using (var context = new BestContext())
                {
                    //JOIN TABLES
                    var backorderItems = (from ti in context.Txnitems
                                          join inv in context.Inventories
                                          on new { ti.ItemId, SiteId = ti.Txn.SiteIdto }
                                          equals new { inv.ItemId, inv.SiteId }
                                          where ti.TxnId == txnId
                                          select new BackorderItemViewModel
                                          {
                                              ItemId = ti.ItemId,
                                              Name = ti.Item.Name,
                                              Quantity = ti.Quantity,
                                              CaseSize = ti.Item.CaseSize,
                                              CurrentStock = inv.Quantity,
                                              OptimumThreshold = inv.OptimumThreshold,
                                              ReorderThreshold = inv.ReorderThreshold ?? 0
                                          }).ToList();

                    //Bind sourses
                    dgvBackItems.ItemsSource = backorderItems;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading backorder items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                //Search and load items based on results
                string searchText = txtSearch.Text;
                LoadAvailableItems(searchText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            //REfresh Functionality
            InitializeControls(true);
            LoadBackorders();
            LoadAvailableItems();
            txtSearch.IsEnabled = true;
        }

        private void StoreComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                //If backorders are empty
                if (allBackorders == null || !(cboStores.SelectedItem is Site selectedSite))
                {
                    dgvBackorders.ItemsSource = null;
                    btnNewBackorder.IsEnabled = false;
                }
                //Nothing Selected - Default state
                else if (selectedSite.SiteId == 0)
                {
                    dgvBackorders.ItemsSource = allBackorders;
                    btnNewBackorder.IsEnabled = false;
                }
                else
                {
                    var storeBackorders = allBackorders.Where(t => t.SiteIdto == selectedSite.SiteId).ToList();
                    dgvBackorders.ItemsSource = storeBackorders;
                    //If not Found - Enable Button
                    btnNewBackorder.IsEnabled = storeBackorders.Count == 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering backorders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgvBackorders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvBackorders.SelectedItem is Txn selectedBackorder)
            {
                //Show backorder details when a backorder is selected
                lblBackorderId.Content = selectedBackorder.TxnId.ToString();
                txtStatus.Text = selectedBackorder.TxnStatus;  // Add this line

                using (var context = new BestContext())
                {
                    //Get store delivery day from database
                    var store = context.Sites.FirstOrDefault(s => s.SiteId == selectedBackorder.SiteIdto);
                    string defaultDay = "MONDAY";

                    //Set delivery day if store has one
                    if (store != null && !string.IsNullOrEmpty(store.DayOfWeek))
                    {
                        defaultDay = store.DayOfWeek.Trim().ToUpper();
                    }

                    //Show and set delivery day combo box
                    cboDeliveryDay.Visibility = Visibility.Visible;
                    cboDeliveryDay.SelectedItem = defaultDay;
                }

                //Display shipment date and load items
                txtDeliveryDate.Text = selectedBackorder.ShipDate.ToString("MM/dd/yyyy");
                LoadBackorderItems(selectedBackorder.TxnId);
                UpdateControlsBasedOnStatus(selectedBackorder.TxnStatus);
            }
            else
            {
                //Clear and hide controls when no backorder is selected
                cboDeliveryDay.Visibility = Visibility.Collapsed;
                lblBackorderId.Content = "N/A";
                txtDeliveryDate.Text = "";
                txtStatus.Text = "";
                UpdateControlsBasedOnStatus("");
            }
        }



        private void btnNewBackorder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Verify store selection is valid
                if (cboStores.SelectedItem is Site selectedSite && selectedSite.SiteId != 0)
                {
                    //Create and display new backorder
                    backorderManager.CreateNewBackorder(selectedSite.SiteId);
                    LoadBackorders();
                    btnNewBackorder.IsEnabled = false;
                }
                else
                {
                    MessageBox.Show("Please select a valid store", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating backorder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgvBackorders.SelectedItem is Txn selectedBackorder)
                {
                    var selectedItem = dgvWarehouseItems.SelectedItem as BackorderItemViewModel;
                    if (selectedItem != null)
                    {
                        backorderManager.AddItemToBackorder(
                            selectedBackorder.TxnId,
                            selectedItem.ItemId,
                            selectedItem.CaseSize);

                        LoadBackorderItems(selectedBackorder.TxnId);
                    }
                    else
                    {
                        MessageBox.Show("Please select an item to add", "Warning",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a backorder first", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding item: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgvBackorders.SelectedItem is not Txn selectedBackorder)
                {
                    MessageBox.Show("Please select a backorder first", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dgvBackItems.SelectedItem is not BackorderItemViewModel selectedItem)
                {
                    MessageBox.Show("Please select an item to remove", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                backorderManager.RemoveItemFromBackorder(selectedBackorder.TxnId, selectedItem.ItemId);
                LoadBackorderItems(selectedBackorder.TxnId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing item: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgvBackorders.SelectedItem is not Txn selectedBackorder)
                {
                    MessageBox.Show("Please select a backorder first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var context = new BestContext())
                {
                    var updatedItems = dgvBackItems.ItemsSource as List<BackorderItemViewModel>;
                    if (updatedItems == null || !updatedItems.Any())
                    {
                        MessageBox.Show("No items to save", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var txn = context.Txns.Find(selectedBackorder.TxnId);
                    if (txn != null)
                    {
                        //Update quantities
                        foreach (var item in updatedItems)
                        {
                            var txnItem = context.Txnitems
                                .FirstOrDefault(ti => ti.TxnId == selectedBackorder.TxnId && ti.ItemId == item.ItemId);
                            if (txnItem != null)
                            {
                                txnItem.Quantity = item.Quantity;
                            }
                        }

                        // Update status based on items
                        if (txn.TxnStatus == "NEW" && updatedItems.Any())
                        {
                            txn.TxnStatus = "RECEIVED";
                        }
                        else if (txn.TxnStatus == "RECEIVED" && !updatedItems.Any())
                        {
                            txn.TxnStatus = "NEW";
                        }

                        context.SaveChanges();

                        // Parse the ship date from the textbox and update it using the manager
                        if (DateTime.TryParse(txtDeliveryDate.Text, out DateTime shipDate))
                        {
                            backorderManager.UpdateBackorderShipDate(selectedBackorder.TxnId, shipDate);
                        }

                        MessageBox.Show("Changes saved successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadBackorders();
                        LoadBackorderItems(selectedBackorder.TxnId);
                        UpdateControlsBasedOnStatus(txn.TxnStatus);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cboDeliveryDay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Check if both backorder and delivery day are selected
            if (dgvBackorders.SelectedItem is Txn selectedTxn &&
                cboDeliveryDay.SelectedItem is string selectedDay)
            {
                try
                {
                    DateTime currentDate = DateTime.Now.Date;
                    int targetDayNumber;

                    //Convert day names to numbers
                    switch (selectedDay)
                    {
                        case "MONDAY": targetDayNumber = 1; break;
                        case "TUESDAY": targetDayNumber = 2; break;
                        case "WEDNESDAY": targetDayNumber = 3; break;
                        case "THURSDAY": targetDayNumber = 4; break;
                        case "FRIDAY": targetDayNumber = 5; break;
                        default: targetDayNumber = 1; break;
                    }

                    int currentDayNumber = (int)currentDate.DayOfWeek;
                    int daysToAdd;

                    //Calculate how many days to add
                    if (currentDayNumber < targetDayNumber)
                    {
                        daysToAdd = targetDayNumber - currentDayNumber;
                    }
                    else if (currentDayNumber > targetDayNumber)
                    {
                        daysToAdd = 7 - (currentDayNumber - targetDayNumber);
                    }
                    else
                    {
                        daysToAdd = 7;
                    }

                    DateTime newShipDate = currentDate.AddDays(daysToAdd);
                    txtDeliveryDate.Text = newShipDate.ToString("MM/dd/yyyy");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error calculating delivery date: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        //CLASS FOR Displaying Backordered Items
        public class BackorderItemViewModel
        {
            public int ItemId { get; set; }
            public string Name { get; set; }
            public int Quantity { get; set; }
            public int CaseSize { get; set; }
            public int CurrentStock { get; set; }
            public int OptimumThreshold { get; set; }
            public int ReorderThreshold { get; set; }
            public bool BelowThreshold => CurrentStock < ReorderThreshold;
        }

        private void btnSendToAssembly_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgvBackorders.SelectedItem is Txn selectedBackorder)
                {
                    if (selectedBackorder.TxnStatus != "RECEIVED")
                    {
                        MessageBox.Show("Only RECEIVED orders can be sent to assembly",
                            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBoxResult confirmResult = MessageBox.Show("Are you sure you want to send this order to assembly?",
                            "Confirm Send", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (confirmResult == MessageBoxResult.Yes)
                        {
                            using (var context = new BestContext())
                            {
                                var txn = context.Txns.Find(selectedBackorder.TxnId);
                                if (txn != null)
                                {
                                    txn.TxnStatus = "ASSEMBLING";
                                    context.SaveChanges();

                                    //Audit LOG
                                    AuditTransactions.LogActivity(
                                        currentUser,
                                        txn.TxnId,
                                        "Back Order",
                                        "UPDATED",
                                        txn.SiteIdto,
                                        null,
                                        "Status changed to ASSEMBLING"
                                    );

                                    LoadBackorders();
                                    UpdateControlsBasedOnStatus("ASSEMBLING");
                                    MessageBox.Show("Order sent to assembly successfully",
                                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                        }
                        else if (confirmResult == MessageBoxResult.No)
                        {
                            MessageBox.Show("Order sending canceled.",
                                "Canceled", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Unexpected response.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending to assembly: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}