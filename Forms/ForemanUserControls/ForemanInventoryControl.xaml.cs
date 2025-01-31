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
        string selectedImagePath;
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

        //Assigns Path to String For Further Saving in The Database
        //Sends Nothing
        //Returns Nothing
        private void BrowseImage()
        {
            selectedImagePath = string.Empty;
            string defaultFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");

            //Create an Instance wit folder and filters
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = defaultFolder,
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

                    selectedImagePath = openFileDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading selected image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    imgProduct.Source = defaultImage;
                    selectedImagePath = string.Empty;
                }
            }
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
            chkActive.IsEnabled = enableEditFeatures;
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

        //Loads Items To Data Grid
        //Sends Nothing
        //Returns Nothing
        private void LoadItemsToDGV()
        {
            try
            {
                //QUERY
                var query = context.Items
                    .Include(i => i.Supplier)
                    .AsQueryable();

                //Apply Filters
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.ToLower();
                    query = query.Where(i => i.Name.ToLower().Contains(searchText));
                }

                //Apply Filters
                if (cmbSearchCategory.SelectedIndex > 0)
                {
                    string category = cmbSearchCategory.SelectedItem.ToString();
                    query = query.Where(i => i.Category == category);
                }

                //LOLOLOL When We Found It- We were Shocked how it works
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

                //Bund DataGrid
                dgvItems.ItemsSource = items;
            }
            //Cacth
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading items: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Populates Labels And TextBoxes With Selected row Details
        //Sends DYNAMIC Object
        //Returns Nothing
        private void PopulateDetailFields(dynamic selectedItem)
        {
            try
            {
                //If Item Is Identified- Display
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
                    chkActive.IsChecked = selectedItem.Active == "Yes";

                    //If Location is not set-Try to load the image using the path from the database
                    if (!string.IsNullOrEmpty(selectedItem.ImageLocation))
                    {
                        try
                        {
                            var image = new BitmapImage();
                            image.BeginInit();
                            image.UriSource = new Uri(selectedItem.ImageLocation);
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.EndInit();
                            //Set Image in the Alloted Area
                            imgProduct.Source = image;
                        }
                        catch
                        {
                            //Set Default Image if Wasn't able to download
                            imgProduct.Source = defaultImage;
                        }
                    }
                    else
                    {
                        //If Database doesn't contain Image
                        imgProduct.Source = defaultImage;
                    }

                    //Enable buttons
                    btnClear.IsEnabled = true;
                    btnUpdate.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading item details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Reset Inputs adn Set Default Image
        //Sends Nothing
        //Returns Nothing
        private void ClearDetails()
        {
            //Blank Inputs
            lblItemId.Content = string.Empty;
            lblItemName.Content = string.Empty;
            lblSku.Content = string.Empty;
            lblCategory.Content = string.Empty;
            lblSupplier.Content = string.Empty;
            lblCaseSize.Content = string.Empty;
            lblRetailPrice.Content = string.Empty;
            chkActive.IsChecked = false;
            txtDescription.Clear();
            txtNotes.Clear();
            imgProduct.Source = defaultImage;
            dgvItems.SelectedItem = null;
            btnClear.IsEnabled = false;
            btnUpdate.IsEnabled = false;
        }

        //Saves Changes 
        //Sends Nothing
        //Returns Nothing
        private void SaveChanges()
        {
            //Create Dialog
            MessageBoxResult result = MessageBox.Show(
                "Do you want to save these changes?\nYes - Save changes\nNo - Return without saving\nCancel - Continue editing",
                "Confirmation Needed",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            //If User Respondeed YES
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    //Verify If Item Id Is Not Empty
                    if (string.IsNullOrEmpty(lblItemId.Content.ToString()))
                    {
                        MessageBox.Show("No item selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    //Parse Label To Integer and QUERY
                    int.TryParse(lblItemId.Content.ToString(), out int itemId);
                    var item = context.Items.Find(itemId);

                    //If Item Is Found
                    if (item != null)
                    {
                        //Assign Values and Save Changes
                        item.Description = txtDescription.Text;
                        item.Notes = txtNotes.Text;
                        item.Active = (sbyte)(chkActive.IsChecked == true ? 1 : 0);

                        //If Image Was Selected
                        if (!string.IsNullOrEmpty(selectedImagePath))
                        {
                            item.ImageLocation = selectedImagePath;
                        }
                        //Save
                        context.SaveChanges();

                        //Success Confirmation
                        MessageBox.Show("Changes saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        //Refresh fields For the Next Step
                        EnableEditing(false);
                        LoadItemsToDGV();
                    }
                }
                //Catch Exeption
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            //Reset Inputs and DGV Selection
            else if (result == MessageBoxResult.No)
            {
                EnableEditing(false);
                ClearDetails();
                selectedImagePath = string.Empty;
            }
        }

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            BrowseImage();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadItemsToDGV();
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
            //If item Exists- Display
            if (dgvItems.SelectedItem != null)
            {
                PopulateDetailFields(dgvItems.SelectedItem);
                EnableEditing(false);
            }
        }

        private void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            //Verify that DGV already have an Item Source
            if (dgvItems.ItemsSource == null)
            {
                MessageBox.Show("Please click Refresh to load the data first", "Attention",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (lblItemId.Content == null || lblItemId.Content.ToString() == "")
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
            //Enable Clear Button if something in Search Field
            if (txtSearch.Text == "" || txtSearch.Text == null)
            {
                btnClear.IsEnabled = false;
            }
            else
            {
                btnClear.IsEnabled = true;
            }

            if (dgvItems.ItemsSource != null)
            {
                LoadItemsToDGV();
            }
        }

        private void CmbSearchCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Enable Clear Button If Selected index is Not Default
            if (cmbSearchCategory.SelectedIndex > 0)
            {
                btnClear.IsEnabled = true;
            }
            if (dgvItems.ItemsSource != null)
            {
                LoadItemsToDGV();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            //If Problem With Loading Has Occured - Disconnect Database
            if (context != null)
            {
                context.Dispose();
            }
        }

        private void ChkActive_Unchecked(object sender, RoutedEventArgs e)
        {
            if (chkActive.IsEnabled)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Are you sure you want to make this item inactive?",
                    "Confirmation Needed",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    chkActive.IsChecked = true;
                }
            }
        }
    }
}