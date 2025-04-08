using ISDP2025_Parfonov_Zerrou.Models;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class ItemDetailsPopup : Window
    {
        public ItemDetailsPopup(Item item)
        {
            InitializeComponent();
            LoadItemDetails(item);

            // Set up key bindings
            this.KeyDown += ItemDetailsPopup_KeyDown;
        }

        private void ItemDetailsPopup_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void LoadItemDetails(Item item)
        {
            // Basic item details
            txtItemId.Text = item.ItemId.ToString();
            txtName.Text = item.Name;
            txtSku.Text = item.Sku;
            txtDescription.Text = item.Description ?? "";
            txtCategory.Text = item.Category;
            txtWeight.Text = item.Weight.ToString();
            txtCaseSize.Text = item.CaseSize.ToString();
            txtCostPrice.Text = $"${item.CostPrice:F2}";
            txtRetailPrice.Text = $"${item.RetailPrice:F2}";


            // Get supplier name
            using (var context = new BestContext())
            {
                var supplier = context.Suppliers.Find(item.SupplierId);
                txtSupplier.Text = supplier?.Name ?? "";
            }

            // Load image
            try
            {
                if (!string.IsNullOrEmpty(item.ImageLocation))
                {
                    var image = new BitmapImage(new Uri(item.ImageLocation));
                    itemImage.Source = image;
                }
                else
                {
                    // Use a default image path
                    string defaultImagePath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        @"..\..\..\Images\default.png");
                    itemImage.Source = new BitmapImage(new Uri(defaultImagePath));
                }
            }
            catch
            {

            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}