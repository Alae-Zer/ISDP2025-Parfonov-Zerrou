using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Equal Share of Mohammed and Serhii
//Last Modified by Serhii on January 21,2025
namespace ISDP2025_Parfonov_Zerrou.Forms.ForemanUserControls
{
    public partial class ForemanInventoryControl : UserControl
    {
        //Global Variables and 
        BestContext context;
        List<string> categoriesList = new List<string>();
        string defaultImagePath = @"D:\WINTER 2025\ISDP\CODE\Images\default.png";
        BitmapImage defaultImage;

        public ForemanInventoryControl()
        {
            //Load Defaults
            InitializeComponent();
            context = new BestContext();
            LoadCategoriesToList();
            EnableEditing(false);
            EnableSearchInputs(false);
            btnClear.IsEnabled = false;
            dgvItems.ItemsSource = null;
            LoadDefaultImage();
        }

        //Loads Default Image
        //Sends Nothing
        //Returns Nothing
        private void LoadDefaultImage()
        {
            try
            {
                //Create and Initialize BitMap Object
                defaultImage = new BitmapImage();
                defaultImage.BeginInit();
                defaultImage.UriSource = new Uri(defaultImagePath);
                defaultImage.CacheOption = BitmapCacheOption.OnLoad;
                defaultImage.EndInit();
                //Set Control To The Loaded Object
                imgProduct.Source = defaultImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading default image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //Set Control to Null
                imgProduct.Source = null;
            }
        }

        //Loades Available Categories To 
        //sends Nothing
        //Returns Nothing
        private void LoadCategoriesToList()
        {
            try
            {
                //QUERY
                var itemCategories = context.Items
                    .Select(i => i.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                //Clear Previous List if Exists
                categoriesList.Clear();
                categoriesList.Add("All Categories");
                //LOOP to Add Not Default Categories
                for (int i = 0; i < itemCategories.Count; i++)
                {
                    categoriesList.Add(itemCategories[i]);
                }

                //Bind Source and Select First Indez
                cmbSearchCategory.ItemsSource = categoriesList;
                cmbSearchCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading categories: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Enables And Disables Search Inputs Based On received Value
        //Sends Boolean
        //Returns Nothing
        private void EnableSearchInputs(bool enablSearchInputs)
        {
            txtSearch.IsEnabled = enablSearchInputs;
            cmbSearchCategory.IsEnabled = enablSearchInputs;
        }

        //Enables And Disables Editing Fields
        //Sends Boolean
        //Returns Nothing
        private void EnableEditing(bool enableEditFeatures)
        {
            //Common Properties
            txtDescription.IsEnabled = enableEditFeatures;
            txtNotes.IsEnabled = enableEditFeatures;
            btnBrowseImage.IsEnabled = enableEditFeatures;

            //Make Selection Based On Function Input and Collaps Unnecessary Things
            if (enableEditFeatures)
            {
                btnBrowseImage.Visibility = Visibility.Visible;
                btnSave.Visibility = Visibility.Visible;
                btnUpdate.Visibility = Visibility.Collapsed;
                btnRefresh.Visibility = Visibility.Collapsed;
                btnClear.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnBrowseImage.Visibility = Visibility.Collapsed;
                btnSave.Visibility = Visibility.Collapsed;
                btnUpdate.Visibility = Visibility.Visible;
                btnRefresh.Visibility = Visibility.Visible;
                btnClear.Visibility = Visibility.Visible;
            }
        }

        //
        //
        //
        private void LoadItems()
        {
            try
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
                    i.RetailPrice,
                    Active = i.Active == 1 ? "Yes" : "No"
                }).OrderBy(i => i.ItemName).ToList();

                dgvItems.ItemsSource = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateDetails(dynamic selectedItem)
        {
            try
            {
                if (selectedItem != null)
                {
                    lblItemId.Content = selectedItem.ItemId.ToString();
                    lblItemName.Content = selectedItem.ItemName;
                    lblSku.Content = selectedItem.Sku;
                    lblCategory.Content = selectedItem.Category;
                    lblSupplier.Content = selectedItem.SupplierName;
                    lblCaseSize.Content = selectedItem.CaseSize.ToString();
                    lblRetailPrice.Content = selectedItem.RetailPrice.ToString("C");
                    txtDescription.Text = selectedItem.Description;
                    txtNotes.Text = selectedItem.Notes;

                    if (!string.IsNullOrEmpty(selectedItem.ImageLocation))
                    {
                        try
                        {
                            var image = new BitmapImage();
                            image.BeginInit();
                            image.UriSource = new Uri(selectedItem.ImageLocation);
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.EndInit();
                            imgProduct.Source = image;

                        }
                        catch
                        {
                            imgProduct.Source = defaultImage;
                        }
                    }
                    else
                    {
                        imgProduct.Source = defaultImage;
                    }

                    btnClear.IsEnabled = true;
                    btnUpdate.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading item details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            lblRetailPrice.Content = string.Empty;
            txtDescription.Clear();
            txtNotes.Clear();
            imgProduct.Source = defaultImage;
            dgvItems.SelectedItem = null;
            btnClear.IsEnabled = false;
            btnUpdate.IsEnabled = false;
        }

        private void SaveChanges()
        {
            MessageBoxResult result = MessageBox.Show(
                "Do you want to save these changes?\nYes - Save changes\nNo - Return without saving\nCancel - Continue editing",
                "Confirmation Needed",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
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
            else if (result == MessageBoxResult.No)
            {
                EnableEditing(false);
            }
        }

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            string initialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = initialDirectory,
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(openFileDialog.FileName);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    imgProduct.Source = image;

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading selected image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    imgProduct.Source = defaultImage;
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
            ClearDetails();
            EnableEditing(false);
            EnableSearchInputs(true);
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
                EnableEditing(false);
            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (dgvItems.ItemsSource == null)
            {
                MessageBox.Show("Please click Refresh to load the data first", "Attention",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

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
            btnClear.IsEnabled = !string.IsNullOrWhiteSpace(txtSearch.Text);
            if (dgvItems.ItemsSource != null)
            {
                LoadItems();
            }
        }

        private void CmbSearchCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSearchCategory.SelectedIndex > 0)
            {
                btnClear.IsEnabled = true;
            }
            if (dgvItems.ItemsSource != null)
            {
                LoadItems();
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