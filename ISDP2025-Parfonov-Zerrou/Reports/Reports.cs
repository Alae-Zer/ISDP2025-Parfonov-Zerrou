using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Reports
{
    public partial class AllReports
    {
        BestContext context;
        public AllReports(BestContext bestContext)
        {
            context = bestContext;
        }

        public object GenerateDeliveryReport(DateTime? startDate, DateTime? endDate, int? siteId, DayOfWeek dayOfWeek)
        {
            // Basic query without day of week filtering
            var query = context.Deliveries
                .Include(d => d.Txns)
                .Where(d =>
                    (!startDate.HasValue || d.DeliveryDate >= startDate.Value) &&
                    (!endDate.HasValue || d.DeliveryDate <= endDate.Value) &&
                    (!siteId.HasValue || d.Txns.Any(t => t.SiteIdto == siteId.Value)) &&
                    (dayOfWeek == DayOfWeek.Sunday || d.DeliveryDate.DayOfWeek == dayOfWeek)
                )
                .ToList() // Execute the query to get the results
                .Select(d => new // Process the results locally
                {
                    D_Id = d.DeliveryId,
                    D_Date = d.DeliveryDate,
                    DayOfWeek = d.DeliveryDate.DayOfWeek.ToString(),
                    Site_Name = d.Txns.FirstOrDefault()?.SiteIdtoNavigation.SiteName ?? "Unknown",
                    Address = d.Txns.FirstOrDefault()?.SiteIdtoNavigation.Address + ", " +
                     d.Txns.FirstOrDefault()?.SiteIdtoNavigation.City ?? "Unknown",
                    Distance = d.Txns.FirstOrDefault()?.SiteIdtoNavigation.DistanceFromWh ?? 0,
                    VehicleType = d.VehicleType,
                    Cost = d.DistanceCost,
                    MaxWeight = d.VehicleTypeNavigation.MaxWeight,
                    Notes = d.Notes
                });

            return query.ToList();
        }

        public object GenerateStoreOrderReport(DateTime? startDate, DateTime? endDate, int? siteId)
        {
            var query = context.Txns
                .Where(t => t.TxnType == "Store Order" &&
                      (!startDate.HasValue || t.CreatedDate >= startDate.Value) &&
                      (!endDate.HasValue || t.CreatedDate <= endDate.Value) &&
                      (!siteId.HasValue || t.SiteIdto == siteId.Value))
                .Select(t => new
                {
                    OrderId = t.TxnId,
                    CreatedDate = t.CreatedDate,
                    ShipDate = t.ShipDate,
                    Status = t.TxnStatus,
                    FromSite = t.SiteIdfromNavigation.SiteName,
                    ToSite = t.SiteIdtoNavigation.SiteName,
                    CreatedBy = t.Employee.FirstName + " " + t.Employee.LastName,
                    Notes = t.Notes
                });

            return query.ToList();
        }

        // show the items not just the list
        public object GenerateStoreOrderDetailReport(int orderId)
        {
            var order = context.Txns
            .FirstOrDefault(t => t.TxnId == orderId);

            if (order != null)
            {
                // Extract just the items with the properties you want to display
                var items = order.Txnitems.Select(i => new
                {
                    ItemId = i.ItemId,
                    Name = i.Item.Name,
                    Quantity = i.Quantity,
                    CaseSize = i.Item.CaseSize,
                    Price = i.Item.RetailPrice,
                    Total = i.Quantity * i.Item.RetailPrice,
                    Weight = i.Item.Weight,
                    TotalWeight = Math.Round(i.Quantity * i.Item.Weight, 2)
                }).ToList();

                return items;
            }
            else
            {
                return null;
            }
        }

        public object GenerateInventoryReport(int? siteId)
        {
            var query = context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Site)
                .Where(i => (!siteId.HasValue || i.SiteId == siteId.Value) &&
                            i.Item.Active == 1)
                .OrderBy(i => i.ItemId)
                .ThenBy(i => i.Site.SiteName)
                .ThenBy(i => i.Item.Name)
                .Select(i => new
                {
                    SiteName = i.Site.SiteName,
                    ItemId = i.ItemId,
                    ItemName = i.Item.Name,
                    Quantity = i.Quantity,
                    ReorderThreshold = i.ReorderThreshold ?? 0,
                    OptimumThreshold = i.OptimumThreshold,
                    BelowThreshold = (i.Quantity < i.ReorderThreshold) == true ? "Yes" : "No"
                });

            return query.ToList();
        }

        public object GenerateOrdersReport(DateTime? startDate, DateTime? endDate, int? siteId)
        {
            var query = context.Txns
                .Where(t => (!startDate.HasValue || t.CreatedDate >= startDate.Value) &&
                            (!endDate.HasValue || t.CreatedDate <= endDate.Value) &&
                            (!siteId.HasValue || t.SiteIdto == siteId.Value))
                .Select(t => new
                {
                    OrderId = t.TxnId,
                    OrderType = t.TxnType,
                    CreatedDate = t.CreatedDate,
                    ShipDate = t.ShipDate,
                    Status = t.TxnStatus,
                    FromSite = t.SiteIdfromNavigation.SiteName,
                    ToSite = t.SiteIdtoNavigation.SiteName,
                    CreatedBy = t.Employee.FirstName + " " + t.Employee.LastName,
                    IsEmergency = t.EmergencyDelivery == 1 ? "Yes" : "No",
                    Notes = t.Notes
                });

            return query.ToList();
        }

        public object GenerateEmergencyOrdersReport(DateTime? startDate, DateTime? endDate, int? siteId)
        {
            var query = context.Txns
                .Include(t => t.SiteIdfromNavigation)
                .Include(t => t.SiteIdtoNavigation)
                .Include(t => t.Employee)
                .Where(t => t.TxnType == "Emergency Order" &&
                            (!startDate.HasValue || t.CreatedDate >= startDate.Value) &&
                            (!endDate.HasValue || t.CreatedDate <= endDate.Value) &&
                            (!siteId.HasValue || t.SiteIdto == siteId.Value))
                .Select(t => new
                {
                    OrderId = t.TxnId,
                    CreatedDate = t.CreatedDate,
                    ShipDate = t.ShipDate,
                    Status = t.TxnStatus,
                    FromSite = t.SiteIdfromNavigation.SiteName,
                    ToSite = t.SiteIdtoNavigation.SiteName,
                    CreatedBy = t.Employee.FirstName + " " + t.Employee.LastName,
                    Notes = t.Notes
                });

            return query.ToList();
        }

        public object GenerateUsersReport(string role = null, int? siteId = null)
        {
            var query = context.Employees
                .Include(e => e.Position)
                .Include(e => e.Site)
                .Where(e =>
                    (string.IsNullOrEmpty(role) || e.Position.PermissionLevel == role) &&
                    (!siteId.HasValue || e.SiteId == siteId)
                )
                .Select(e => new
                {
                    EmployeeId = e.EmployeeID,
                    Username = e.Username,
                    Name = e.FirstName + " " + e.LastName,
                    Email = e.Email,
                    Position = e.Position.PermissionLevel,
                    Site = e.Site.SiteName,
                    Active = e.Active == 1 ? "Yes" : "No",
                    Locked = e.Locked == 1 ? "Yes" : "No",
                    Notes = e.Notes
                });

            return query.ToList();
        }

        public object GenerateBackordersReport(DateTime? startDate, DateTime? endDate, int? siteId)
        {
            var query = context.Txns
                .Where(t => t.TxnType == "Back Order" &&
                            (!startDate.HasValue || t.CreatedDate >= startDate.Value) &&
                            (!endDate.HasValue || t.CreatedDate <= endDate.Value) &&
                            (!siteId.HasValue || t.SiteIdto == siteId.Value))
                .Select(t => new
                {
                    OrderId = t.TxnId,
                    CreatedDate = t.CreatedDate,
                    ShipDate = t.ShipDate,
                    Status = t.TxnStatus,
                    FromSite = t.SiteIdfromNavigation.SiteName,
                    ToSite = t.SiteIdtoNavigation.SiteName,
                    CreatedBy = t.Employee.FirstName + " " + t.Employee.LastName,
                    Notes = t.Notes
                });

            return query.ToList();
        }

        // Method to list all supplier orders
        public object GenerateSupplierOrdersReport(DateTime? startDate, DateTime? endDate)
        {
            var query = context.Txns
                .Where(t => t.TxnType == "Supplier Order" &&
                      (!startDate.HasValue || t.CreatedDate >= startDate.Value) &&
                      (!endDate.HasValue || t.CreatedDate <= endDate.Value))
                .Select(t => new
                {
                    OrderId = t.TxnId,
                    CreatedDate = t.CreatedDate,
                    ShipDate = t.ShipDate,
                    Status = t.TxnStatus,
                    CreatedBy = t.Employee.FirstName + " " + t.Employee.LastName,
                    Notes = t.Notes,
                    ItemCount = t.Txnitems.Count,
                    TotalItems = t.Txnitems.Sum(i => i.Quantity),
                    TotalValue = t.Txnitems.Sum(i => i.Quantity * i.Item.CostPrice)
                })
                .OrderByDescending(t => t.CreatedDate);

            return query.ToList();
        }

        // Method to show detailed items within a specific supplier order
        public object GenerateSupplierOrderDetailReport(int orderId)
        {
            var order = context.Txns
                .FirstOrDefault(t => t.TxnId == orderId && t.TxnType == "Supplier Order");

            if (order != null)
            {
                // Get items in this order with supplier information
                var items = context.Txnitems
                    .Where(ti => ti.TxnId == orderId)
                    .Include(ti => ti.Item)
                    .ThenInclude(i => i.Supplier)
                    .Select(i => new
                    {
                        ItemId = i.ItemId,
                        SKU = i.Item.Sku,
                        Name = i.Item.Name,
                        Quantity = i.Quantity,
                        CaseSize = i.Item.CaseSize,
                        CasesOrdered = Math.Round((decimal)i.Quantity / i.Item.CaseSize, 2),
                        CostPrice = i.Item.CostPrice,
                        TotalCost = Math.Round(i.Quantity * i.Item.CostPrice, 2),
                        SupplierId = i.Item.SupplierId,
                        SupplierName = i.Item.Supplier.Name,
                        Category = i.Item.Category,
                        Notes = i.Notes
                    })
                    .OrderBy(i => i.SupplierName)
                    .ThenBy(i => i.Name)
                    .ToList();

                // Return the items collection directly
                return items;
            }
            else
            {
                return null;
            }
        }
    }
}
