using System.IO;
using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using HandyControl.Data;
using ISDP2025_Parfonov_Zerrou.Models;
using ISDP2025_Parfonov_Zerrou.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace ISDP2025_Parfonov_Zerrou.Forms.AdminUserControls
{
    public partial class ReportsControl : UserControl
    {
        private BestContext context;
        private List<Txntype> reportTypes;
        private Employee currentUser;
        AllReports reports;

        public ReportsControl(Employee employee, BestContext incontext)
        {
            InitializeComponent();
            context = incontext;
            currentUser = employee;
            LoadReportType();
            LoadSites();
            reports = new AllReports(context);

            // Set default date range (from now to 30 days ago)
            dpStartDate.SelectedDate = DateTime.Now.AddDays(-30);
            dpEndDate.SelectedDate = DateTime.Now;
        }

        public void LoadReportType()
        {
            // Load report types
            reportTypes = context.Txntypes.ToList();

            var customReportTypes = new List<object>
            {
                new { ReportName = "Delivery Report", ReportType = "Delivery" },
                new { ReportName = "Store Order Report", ReportType = "Store Order" },
                new { ReportName = "Shipping Receipt", ReportType = "Shipping" },
                new { ReportName = "Inventory Report", ReportType = "Inventory" },
                new { ReportName = "Orders Report", ReportType = "Orders" },
                new { ReportName = "Emergency Orders Report", ReportType = "Emergency Order" },
                new { ReportName = "Users Report", ReportType = "Users" },
                new { ReportName = "Backorders Report", ReportType = "Back Order" },
                new { ReportName = "Supplier Order Report", ReportType = "Supplier Order" }
            };

            cmbReportType.ItemsSource = customReportTypes;
            cmbReportType.DisplayMemberPath = "ReportName";
            cmbReportType.SelectedValuePath = "ReportType";
        }

        public void LoadSites()
        {
            currentUser = context.Employees
                        .Include(e => e.Position)
                        .FirstOrDefault(e => e.EmployeeID == currentUser.EmployeeID);

            var sitesList = new List<object>();
            sitesList.Add(new { SiteId = 0, SiteName = "All Sites" });

            // Add all active sites from the database
            var dbSites = context.Sites
                .Where(s => s.Active == 1)
                .OrderBy(s => s.SiteName)
                .Select(s => new { s.SiteId, s.SiteName })
                .ToList();

            foreach (var site in dbSites)
            {
                sitesList.Add(site);
            }

            // Set the ItemsSource for the ComboBox
            cmbSite.ItemsSource = sitesList;
            cmbSite.DisplayMemberPath = "SiteName";
            cmbSite.SelectedValuePath = "SiteId";

            if (currentUser.Position?.PermissionLevel == "Store Manager")
            {
                cmbSite.SelectedValue = currentUser.SiteId;
            }
            else
            {
                cmbSite.SelectedIndex = 0; // Select "All Sites"
            }
        }

        private void chkUseDateRange_CheckedChanged(object sender, RoutedEventArgs e)
        {
            dateRangePanel.IsEnabled = chkUseDateRange.IsChecked == true;
        }

        private void cmbReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbReportType.SelectedItem != null)
            {
                var reportType = cmbReportType.SelectedValue.ToString();

                rolePanel.Visibility = Visibility.Collapsed;
                supplierPanel.Visibility = Visibility.Collapsed;
                orderIdPanel.Visibility = Visibility.Collapsed;
                dayOfWeekPanel.Visibility = Visibility.Collapsed;

                cmbSite.IsEnabled = reportType != "Users";

                bool needsDateRange = reportType == "Delivery" ||
                                     reportType == "Store Order" ||
                                     reportType == "Emergency Order" ||
                                     reportType == "Orders" ||
                                     reportType == "Shipping" ||
                                     reportType == "Back Order";

                chkUseDateRange.IsEnabled = needsDateRange;

                // Show specific parameters based on report type
                switch (reportType)
                {
                    case "Delivery":
                        dayOfWeekPanel.Visibility = Visibility.Visible;

                        if (cmbDayOfWeek.SelectedIndex < 0)
                            cmbDayOfWeek.SelectedIndex = 0;
                        break;

                    case "Users":
                        rolePanel.Visibility = Visibility.Visible;
                        // Populate roles if not already done
                        if (cmbRole.Items.Count == 0)
                        {
                            cmbRole.Items.Add("All Roles");
                            foreach (var role in context.Posns.Select(p => p.PermissionLevel).OrderBy(p => p))
                            {
                                cmbRole.Items.Add(role);
                            }
                        }
                        // Select default
                        if (cmbRole.SelectedIndex < 0)
                            cmbRole.SelectedIndex = 0;
                        break;

                    case "Supplier Order":
                        supplierPanel.Visibility = Visibility.Visible;
                        // Populate suppliers if not already done
                        if (cmbSupplier.Items.Count == 0)
                        {
                            cmbSupplier.Items.Add("All Suppliers");
                            foreach (var supplier in context.Suppliers.Where(s => s.Active == 1).OrderBy(s => s.Name))
                            {
                                cmbSupplier.Items.Add(supplier.Name);
                            }
                        }
                        orderIdPanel.Visibility = Visibility.Visible;
                        // Select default
                        if (cmbSupplier.SelectedIndex < 0)
                            cmbSupplier.SelectedIndex = 0;
                        break;

                    case "Shipping":
                    case "Store Order":
                        orderIdPanel.Visibility = Visibility.Visible;
                        break;
                }

                // Uncheck and disable date range if not needed
                if (!needsDateRange)
                {
                    chkUseDateRange.IsChecked = false;
                    dateRangePanel.IsEnabled = false;
                }
            }
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (cmbReportType.SelectedItem == null)
            {
                HandyControl.Controls.MessageBox.Show("Please select a report type", "Report Generation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Get report parameters
                string reportType = cmbReportType.SelectedValue.ToString();
                int? siteId = null;

                if (cmbSite.SelectedValue != null && (int)cmbSite.SelectedValue != 0)
                {
                    siteId = (int)cmbSite.SelectedValue;
                }

                DateTime? startDate = null;
                DateTime? endDate = null;

                if (chkUseDateRange.IsChecked == true)
                {
                    startDate = dpStartDate.SelectedDate;
                    endDate = dpEndDate.SelectedDate;

                    if (!startDate.HasValue || !endDate.HasValue)
                    {
                        HandyControl.Controls.MessageBox.Show("Please select valid start and end dates", "Report Generation",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (startDate > endDate)
                    {
                        HandyControl.Controls.MessageBox.Show("Start date must be before end date", "Report Generation",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Generate the report data based on the selected type
                switch (reportType)
                {
                    case "Delivery":
                        DayOfWeek dayOfWeek = DayOfWeek.Sunday;
                        if (cmbDayOfWeek.SelectedIndex > 0) // Skip "All Days"
                        {
                            ComboBoxItem selectedItem = (ComboBoxItem)cmbDayOfWeek.SelectedItem;
                            string value = selectedItem.Content.ToString();
                            switch (value)
                            {
                                case "Monday":
                                    dayOfWeek = DayOfWeek.Monday;
                                    break;
                                case "Tuesday":
                                    dayOfWeek = DayOfWeek.Tuesday;
                                    break;
                                case "Wednesday":
                                    dayOfWeek = DayOfWeek.Wednesday;
                                    break;
                                case "Thursday":
                                    dayOfWeek = DayOfWeek.Thursday;
                                    break;
                                case "Friday":
                                    dayOfWeek = DayOfWeek.Friday;
                                    break;
                            }
                        }
                        UpdateUI(reports.GenerateDeliveryReport(startDate, endDate, siteId, dayOfWeek));
                        break;
                    case "Shipping":
                    case "Store Order":
                        if (!string.IsNullOrWhiteSpace(txtOrderId.Text) && int.TryParse(txtOrderId.Text, out int orderId))
                        {
                            // Show specific order
                            UpdateUI(reports.GenerateStoreOrderDetailReport(orderId));
                        }
                        else
                        {
                            // Show list of orders
                            UpdateUI(reports.GenerateStoreOrderReport(startDate, endDate, siteId));
                        }
                        break;

                    case "Inventory":
                        UpdateUI(reports.GenerateInventoryReport(siteId));
                        break;

                    case "Orders":
                        UpdateUI(reports.GenerateOrdersReport(startDate, endDate, siteId));
                        break;

                    case "Emergency Order":
                        UpdateUI(reports.GenerateEmergencyOrdersReport(startDate, endDate, siteId));
                        break;

                    case "Users":
                        string role = cmbRole.SelectedItem.ToString();
                        if (role == "All Roles") role = null;
                        UpdateUI(reports.GenerateUsersReport(role, siteId));
                        break;

                    case "Back Order":
                        UpdateUI(reports.GenerateBackordersReport(startDate, endDate, siteId));
                        break;

                    case "Supplier Order":
                        string supplier = cmbSupplier.SelectedItem.ToString();
                        if (supplier == "All Suppliers") supplier = null;
                        bool usePageBreaks = chkPageBreakBySupplier.IsChecked == true;

                        // If order ID is specified, show order details
                        if (!string.IsNullOrWhiteSpace(txtOrderId.Text) && int.TryParse(txtOrderId.Text, out int Id))
                        {
                            // Show specific supplier order details
                            UpdateUI(reports.GenerateSupplierOrderDetailReport(Id));
                        }
                        else
                        {
                            // Show list of supplier orders
                            UpdateUI(reports.GenerateSupplierOrdersReport(startDate, endDate));
                        }
                        break;

                    default:
                        HandyControl.Controls.MessageBox.Show("Report type not implemented yet", "Report Generation",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error generating report: {ex.Message}", "Report Generation",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateUI(object data)
        {
            if (data == null)
            {
                Growl.Warning(new GrowlInfo
                {
                    Message = "No Order with that ID",
                    ShowDateTime = false,
                    WaitTime = 2
                });
                return;
            }
            dgReportData.ItemsSource = (System.Collections.IEnumerable)data;
            var count = ((System.Collections.ICollection)data).Count;
            txtRecordCount.Text = $"Records: {count}";
            btnExportPDF.IsEnabled = count > 0;
        }


        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            btnGenerate_Click(sender, e);
        }

        private void btnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (dgReportData.Items.Count == 0)
            {
                HandyControl.Controls.MessageBox.Show("No data to export", "Export to PDF",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create a SaveFileDialog to let the user choose where to save the PDF
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    FileName = $"{GetReportTitle()}-{DateTime.Now:yyyy}-{DateTime.Now:MM}-{DateTime.Now:dd}_{DateTime.Now:hh}-{DateTime.Now:mm}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Create the PDF document
                    Document document = CreatePdfDocument();

                    // Render the document to PDF
                    PdfDocumentRenderer renderer = new PdfDocumentRenderer(true)
                    {
                        Document = document
                    };

                    renderer.RenderDocument();

                    // Save the PDF
                    renderer.PdfDocument.Save(saveFileDialog.FileName);

                    HandyControl.Controls.MessageBox.Show("PDF created successfully!", "Export to PDF",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Open the PDF file with the default PDF viewer
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"Error exporting to PDF: {ex.Message}", "Export to PDF",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetReportTitle()
        {
            if (cmbReportType.SelectedItem == null)
                return "Report";

            dynamic selectedItem = cmbReportType.SelectedItem;
            return selectedItem.ReportName;
        }

        private Document CreatePdfDocument()
        {
            // Create a new MigraDoc document
            var document = new Document();
            document.Info.Title = GetReportTitle();
            document.Info.Subject = "Bullseye Inventory System Report";
            document.Info.Author = "Bullseye Inventory System";

            // Clone the default page setup before modifying it
            PageSetup pageSetup = document.DefaultPageSetup.Clone();
            pageSetup.Orientation = MigraDoc.DocumentObjectModel.Orientation.Landscape;
            pageSetup.LeftMargin = Unit.FromCentimeter(2);
            pageSetup.RightMargin = Unit.FromCentimeter(2);
            pageSetup.TopMargin = Unit.FromCentimeter(2);
            pageSetup.BottomMargin = Unit.FromCentimeter(2);

            // Define styles
            DefineStyles(document);

            // Create the cover page
            CreateCoverPage(document);

            // Add the report data
            AddReportContent(document, pageSetup);

            return document;
        }

        private void DefineStyles(Document document)
        {
            // Get the predefined style Normal
            var style = document.Styles[StyleNames.Normal];
            style.Font.Name = "Arial";
            style.Font.Size = 10;

            // Heading1 style
            style = document.Styles[StyleNames.Heading1];
            style.Font.Name = "Arial";
            style.Font.Size = 18;
            style.Font.Bold = true;
            style.Font.Color = MigraDoc.DocumentObjectModel.Colors.DarkBlue;
            style.ParagraphFormat.SpaceAfter = 12;
            style.ParagraphFormat.Alignment = ParagraphAlignment.Center;

            // Heading2 style
            style = document.Styles[StyleNames.Heading2];
            style.Font.Size = 14;
            style.Font.Bold = true;
            style.Font.Color = MigraDoc.DocumentObjectModel.Colors.DarkBlue;
            style.ParagraphFormat.SpaceBefore = 6;
            style.ParagraphFormat.SpaceAfter = 6;

            // Header style
            style = document.Styles[StyleNames.Header];
            style.ParagraphFormat.AddTabStop("16cm", TabAlignment.Right);

            // Footer style
            style = document.Styles[StyleNames.Footer];
            style.ParagraphFormat.AddTabStop("10cm", TabAlignment.Center);

            // Create a new table style
            style = document.Styles.AddStyle("Table", StyleNames.Normal);
            style.Font.Name = "Arial";
            style.Font.Size = 11;
        }

        private void CreateCoverPage(Document document)
        {
            // Add a section to the document
            var section = document.AddSection();
            section.PageSetup.Orientation = MigraDoc.DocumentObjectModel.Orientation.Landscape;

            // Add Bullseye logo (you would need to add this to your project resources)
            var logoPath = "../../../Images/bullseye.png";
            if (File.Exists(logoPath))
            {
                var image = section.AddImage(logoPath);
                image.Width = "4cm";
                image.Height = "4cm";
                image.LockAspectRatio = true;
                image.RelativeVertical = RelativeVertical.Page;
                image.RelativeHorizontal = RelativeHorizontal.Page;
                image.Top = "2cm";
                image.Left = "13.5cm";
            }

            // Add spacer
            section.AddParagraph().Format.SpaceAfter = 100;

            // Add company name
            var paragraph = section.AddParagraph("Bullseye Sporting Goods");
            paragraph.Format.Font.Size = 24;
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.Format.SpaceAfter = 50;

            // Add report title
            paragraph = section.AddParagraph(GetReportTitle());
            paragraph.Format.Font.Size = 28;
            paragraph.Format.Font.Bold = true;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.Format.SpaceAfter = 40;

            // Add report parameters in a centered table
            var table = section.AddTable();
            table.Borders.Visible = false;
            table.Format.Alignment = ParagraphAlignment.Center;
            table.AddColumn("7cm");
            table.AddColumn("7cm");

            // Center the table
            table.LeftPadding = 0;
            table.RightPadding = 0;
            table.BottomPadding = 10;

            var row = table.AddRow();
            row.Cells[0].AddParagraph("Report Type:");
            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[1].AddParagraph(GetReportTitle());
            row.Cells[1].Format.Alignment = ParagraphAlignment.Left;

            if (cmbSite.SelectedItem != null && cmbSite.IsEnabled)
            {
                row = table.AddRow();
                row.Cells[0].AddParagraph("Site:");
                row.Cells[0].Format.Font.Bold = true;
                row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
                row.Cells[1].AddParagraph(cmbSite.Text);
                row.Cells[1].Format.Alignment = ParagraphAlignment.Left;
            }

            if (chkUseDateRange.IsChecked == true)
            {
                row = table.AddRow();
                row.Cells[0].AddParagraph("Date Range:");
                row.Cells[0].Format.Font.Bold = true;
                row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
                row.Cells[1].AddParagraph($"{dpStartDate.SelectedDate?.ToString("MM/dd/yyyy")} - {dpEndDate.SelectedDate?.ToString("MM/dd/yyyy")}");
                row.Cells[1].Format.Alignment = ParagraphAlignment.Left;
            }

            row = table.AddRow();
            row.Cells[0].AddParagraph("Generated By:");
            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[1].AddParagraph($"{currentUser.FirstName} {currentUser.LastName}");
            row.Cells[1].Format.Alignment = ParagraphAlignment.Left;

            row = table.AddRow();
            row.Cells[0].AddParagraph("Generated On:");
            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[1].AddParagraph(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
            row.Cells[1].Format.Alignment = ParagraphAlignment.Left;

            row = table.AddRow();
            row.Cells[0].AddParagraph("Records Count:");
            row.Cells[0].Format.Font.Bold = true;
            row.Cells[0].Format.Alignment = ParagraphAlignment.Right;
            row.Cells[1].AddParagraph(dgReportData.Items.Count.ToString());
            row.Cells[1].Format.Alignment = ParagraphAlignment.Left;
        }

        private void AddReportContent(Document document, PageSetup pageSetup)
        {
            // Add a section to the document
            var section = document.AddSection();

            // Apply the page setup
            section.PageSetup = pageSetup.Clone();

            // Add header and footer
            var header = section.Headers.Primary;
            var headerParagraph = header.AddParagraph();
            headerParagraph.AddText("Bullseye Sporting Goods");
            headerParagraph.AddTab();
            headerParagraph.AddText(GetReportTitle());

            var footer = section.Footers.Primary;
            var footerParagraph = footer.AddParagraph();
            footerParagraph.AddText("Generated on: " + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
            footerParagraph.AddTab();
            footerParagraph.AddText("Page ");
            footerParagraph.AddPageField();
            footerParagraph.AddText(" of ");
            footerParagraph.AddNumPagesField();

            // Add report title
            var paragraph = section.AddParagraph(GetReportTitle());
            paragraph.Style = StyleNames.Heading1;
            paragraph.Format.SpaceAfter = 10;

            // Handle supplier order with page breaks between suppliers
            if (cmbReportType.SelectedValue.ToString() == "Supplier Order" &&
                chkPageBreakBySupplier.IsChecked == true &&
                dgReportData.Items.Count > 0)
            {
                // Group items by supplier
                var data = (System.Collections.IEnumerable)dgReportData.ItemsSource;
                var supplierGroups = new Dictionary<string, List<object>>();

                // Group items by supplier name
                foreach (var item in data)
                {
                    var property = item.GetType().GetProperty("SupplierName");
                    if (property != null)
                    {
                        string supplierName = property.GetValue(item)?.ToString() ?? "Unknown";

                        if (!supplierGroups.ContainsKey(supplierName))
                        {
                            supplierGroups[supplierName] = new List<object>();
                        }

                        supplierGroups[supplierName].Add(item);
                    }
                }

                // Create a separate section for each supplier
                bool isFirstSupplier = true;
                foreach (var supplierGroup in supplierGroups)
                {
                    if (!isFirstSupplier)
                    {
                        // Add a new section with page break for subsequent suppliers
                        section = document.AddSection();
                        section.PageSetup = pageSetup.Clone();

                        // Add header and footer to the new section
                        header = section.Headers.Primary;
                        headerParagraph = header.AddParagraph();
                        headerParagraph.AddText("Bullseye Sporting Goods");
                        headerParagraph.AddTab();
                        headerParagraph.AddText(GetReportTitle());

                        footer = section.Footers.Primary;
                        footerParagraph = footer.AddParagraph();
                        footerParagraph.AddText("Generated on: " + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
                        footerParagraph.AddTab();
                        footerParagraph.AddText("Page ");
                        footerParagraph.AddPageField();
                        footerParagraph.AddText(" of ");
                        footerParagraph.AddNumPagesField();
                    }

                    // Add supplier heading
                    paragraph = section.AddParagraph($"Supplier: {supplierGroup.Key}");
                    paragraph.Style = StyleNames.Heading2;
                    paragraph.Format.SpaceAfter = 10;

                    // Create supplier-specific table
                    CreateReportTable(section, supplierGroup.Value);

                    isFirstSupplier = false;
                }
            }
            else
            {
                // Standard report without page breaks
                CreateReportTable(section, dgReportData.ItemsSource);
            }
        }

        private void CreateReportTable(Section section, System.Collections.IEnumerable data)
        {
            // Create a table for the data
            var table = section.AddTable();
            table.Style = "Table";
            table.Borders.Width = 0.5;
            table.Borders.Color = Colors.Gray;
            table.Shading.Color = Colors.White;
            table.TopPadding = 3;
            table.BottomPadding = 3;

            // Center the table in the section
            table.Format.Alignment = ParagraphAlignment.Center;

            // Add columns to the table based on the data
            if (dgReportData.Items.Count > 0)
            {
                // Get the column headers and calculate widths
                int columnCount = 0;
                foreach (var column in dgReportData.Columns)
                {
                    if (column.Visibility == System.Windows.Visibility.Visible)
                    {
                        columnCount++;
                    }
                }

                // Calculate column widths based on available page width
                // Use slightly less than the full width to account for margins
                double availableWidth = 24.0; // Typical usable page width in landscape mode in cm
                double columnWidth = availableWidth / columnCount;

                // Add columns with calculated widths
                foreach (var column in dgReportData.Columns)
                {
                    if (column.Visibility == System.Windows.Visibility.Visible)
                    {
                        // Adjust width for specific columns to improve readability
                        double width = columnWidth;
                        string head = column.Header.ToString();
                        // Make ID columns narrower, make Name columns wider
                        if (head.Contains("Id") || head.Contains("ID") || head.Contains("Active") || head.Contains("Locked"))
                            width = columnWidth * 0.7;
                        else if (head.Contains("Notes") || head.Contains("Description"))
                            width = columnWidth * 1.5;
                        else if (head.Contains("Email") || head.Contains("Name"))
                            width = columnWidth * 1.3;

                        table.AddColumn(Unit.FromCentimeter(width));
                    }
                }

                // Add header row
                var headerRow = table.AddRow();
                headerRow.HeadingFormat = true;
                headerRow.Shading.Color = Colors.LightGray;
                headerRow.Format.Font.Bold = true;
                headerRow.Format.Alignment = ParagraphAlignment.Center;
                headerRow.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;
                headerRow.Height = "0.7cm";

                int columnIndex = 0;
                foreach (var column in dgReportData.Columns)
                {
                    if (column.Visibility == System.Windows.Visibility.Visible)
                    {
                        Cell cell = headerRow.Cells[columnIndex];
                        cell.AddParagraph(column.Header.ToString());
                        cell.Format.Alignment = ParagraphAlignment.Center;
                        columnIndex++;
                    }
                }

                // Add data rows
                bool useAlternatingColor = true;
                foreach (var item in data)
                {
                    var dataRow = table.AddRow();
                    dataRow.VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment.Center;
                    dataRow.Height = "0.6cm";

                    // Apply alternating row colors
                    if (useAlternatingColor)
                        dataRow.Shading.Color = new Color(240, 240, 240);

                    useAlternatingColor = !useAlternatingColor;
                    columnIndex = 0;

                    foreach (var column in dgReportData.Columns)
                    {
                        if (column.Visibility == System.Windows.Visibility.Visible)
                        {
                            string head = column.Header.ToString();
                            var property = item.GetType().GetProperty(head);

                            if (property != null)
                            {
                                var value = property.GetValue(item);
                                var cell = dataRow.Cells[columnIndex];
                                cell.AddParagraph(value?.ToString() ?? "");

                                // Center align IDs, dates, statuses, and numeric fields
                                if (head.Contains("Id") || head.Contains("ID") ||
                                    head.Contains("Date") || head.Contains("Status") ||
                                    head.Contains("Emergency") || head.Contains("Active") ||
                                    head.Contains("Locked") || head.Contains("Quantity") ||
                                    head.Contains("CaseSize") || head.Contains("Price"))
                                    cell.Format.Alignment = ParagraphAlignment.Center;
                                else
                                    cell.Format.Alignment = ParagraphAlignment.Left;
                            }
                            columnIndex++;
                        }
                    }
                }
            }
            else
            {
                // If no data, display a message
                var paragraph = section.AddParagraph("No data available for this report.");
                paragraph.Format.Font.Italic = true;
                paragraph.Format.Alignment = ParagraphAlignment.Center;
            }
        }

        private System.Collections.IEnumerable FilterDataForSupplier(System.Collections.IEnumerable data, string supplier)
        {
            // Filter the data to only include items for the given supplier
            var filteredData = new List<object>();

            foreach (var item in data)
            {
                var property = item.GetType().GetProperty("Supplier");
                if (property != null)
                {
                    string itemSupplier = property.GetValue(item)?.ToString();
                    if (itemSupplier == supplier)
                    {
                        filteredData.Add(item);
                    }
                }
            }

            return filteredData;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            // Find the parent ContentControl and navigate back to the dashboard
            var parent = this.Parent as ContentControl;
            if (parent != null)
            {
                parent.Content = null;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Clean up resources when the control is unloaded
            if (context != null)
            {
                context.Dispose();
            }
        }
    }
}