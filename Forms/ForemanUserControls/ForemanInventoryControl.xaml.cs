using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace ISDP2025_Parfonov_Zerrou.Forms.ForemanUserControls
{
    public partial class ForemanInventoryControl : UserControl
    {
        private readonly BestContext context;
        private List<string> categoriesList = new List<string>();

        public ForemanInventoryControl()
        {
            InitializeComponent();
            context = new BestContext();
            LoadCategoriesToList();
            EnableEditing(false);
            LoadItems();
        }

        private void LoadCategoriesToList()
        {
            var dbCategories = context.Items
                .Select(i => i.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            categoriesList.Clear();
            categoriesList.Add("All Categories");
            categoriesList.AddRange(dbCategories);

            cmbSearchCategory.ItemsSource = categoriesList;
            cmbSearchCategory.SelectedIndex = 0;
        }

        private void EnableEditing(bool enable)
        {
            txtDescription.IsEnabled = enable;
            txtNotes.IsEnabled = enable;
            btnBrowseImage.IsEnabled = enable;
            btnSave.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            btnUpdate.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadItems()
        {
            var query = context.Items
                .Include(i => i.Supplier)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                string searchText = txtSearch.Text.ToLower();
                query = query.Where(i => i.Name.ToLower().Contains(searchText));
            }

            if (cmbSearchCategory.SelectedIndex > 0)
            {
                string category = cmbSearchCategory.SelectedItem.ToString();
                query = query.Where(i => i.Category == category);
            }

            var items = query.Select(i => new
            {
                i.ItemId,
                ItemName = i.Name,
                i.Category,
                SupplierName = i.Supplier.Name,
                i.Sku,
                i.Description,
                i.Notes,
                i.ImageLocation,
                i.CaseSize,
                i.CostPrice,
                i.RetailPrice
            }).Distinct().ToList();

            dgvItems.ItemsSource = items;
        }

        private void PopulateDetails(dynamic selectedItem)
        {
            if (selectedItem != null)
            {
                lblItemId.Content = selectedItem.ItemId.ToString();
                lblItemName.Content = selectedItem.ItemName;
                lblSku.Content = selectedItem.Sku;
                lblCategory.Content = selectedItem.Category;
                lblSupplier.Content = selectedItem.SupplierName;
                lblCaseSize.Content = selectedItem.CaseSize.ToString();
                lblCostPrice.Content = selectedItem.CostPrice.ToString("C");
                lblRetailPrice.Content = selectedItem.RetailPrice.ToString("C");
                txtDescription.Text = selectedItem.Description;
                txtNotes.Text = selectedItem.Notes;

                if (!string.IsNullOrEmpty(selectedItem.ImageLocation))
                {
                    try
                    {
                        imgProduct.Source = new BitmapImage(new Uri(selectedItem.ImageLocation));
                        txtImageLocation.Text = selectedItem.ImageLocation;
                    }
                    catch
                    {
                        imgProduct.Source = null;
                        txtImageLocation.Text = string.Empty;
                    }
                }
                else
                {
                    imgProduct.Source = null;
                    txtImageLocation.Text = string.Empty;
                }
            }
        }

        private void ClearDetails()
        {
            lblItemId.Content = string.Empty;
            lblItemName.Content = string.Empty;
            lblSku.Content = string.Empty;
            lblCategory.Content = string.Empty;
            lblSupplier.Content = string.Empty;
            lblCaseSize.Content = string.Empty;
            lblCostPrice.Content = string.Empty;
            lblRetailPrice.Content = string.Empty;
            txtDescription.Clear();
            txtNotes.Clear();
            txtImageLocation.Clear();
            imgProduct.Source = null;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ClearDetails();
            EnableEditing(false);
        }

        private void DgInventory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvItems.SelectedItem != null)
            {
                PopulateDetails(dgvItems.SelectedItem);
            }
        }

        private void SaveChanges()
        {
            try
            {
                if (string.IsNullOrEmpty(lblItemId.Content?.ToString()))
                {
                    MessageBox.Show("No item selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int itemId = int.Parse(lblItemId.Content.ToString());
                var item = context.Items.Find(itemId);

                if (item != null)
                {
                    item.Description = txtDescription.Text;
                    item.Notes = txtNotes.Text;
                    //item.ImageLocation = txtImageLocation.Text;

                    context.SaveChanges();
                    MessageBox.Show("Changes saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    EnableEditing(false);
                    LoadItems();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    txtImageLocation.Text = openFileDialog.FileName;
                    imgProduct.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
            ClearDetails();
            EnableEditing(false);
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(lblItemId.Content?.ToString()))
            {
                MessageBox.Show("Please select an item first.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            EnableEditing(true);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadItems();
        }

        private void CmbSearchCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadItems();
        }
    }
}