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

        public object GenerateDeliveryReport(DateTime? startDate, DateTime? endDate, int? siteId, string dayOfWeek = null)
        {
            var query = context.Deliveries
                .Include(d => d.Txns)
                    .ThenInclude(t => t.SiteIdtoNavigation)
                .Include(d => d.VehicleTypeNavigation)
                .Where(d =>
                    (!startDate.HasValue || d.DeliveryDate >= startDate.Value) &&
                    (!endDate.HasValue || d.DeliveryDate <= endDate.Value) &&
                    (!siteId.HasValue || d.Txns.Any(t => t.SiteIdto == siteId.Value)) &&
                    (string.IsNullOrEmpty(dayOfWeek) || d.DeliveryDate.DayOfWeek.ToString() == dayOfWeek)
                )
                .Select(d => new
                {
                    DeliveryId = d.DeliveryId,
                    DeliveryDate = d.DeliveryDate,
                    DayOfWeek = d.DeliveryDate.DayOfWeek.ToString(),
                    SiteName = d.Txns.FirstOrDefault().SiteIdtoNavigation.SiteName,
                    SiteAddress = d.Txns.FirstOrDefault().SiteIdtoNavigation.Address + ", " +
                                  d.Txns.FirstOrDefault().SiteIdtoNavigation.City,
                    Distance = d.Txns.FirstOrDefault().SiteIdtoNavigation.DistanceFromWh,
                    VehicleType = d.VehicleType,
                    DistanceCost = d.DistanceCost,
                    MaxWeight = d.VehicleTypeNavigation.MaxWeight,
                    Notes = d.Notes
                });

            return query.ToList();
        }

        public object GenerateStoreOrderReport(DateTime? startDate, DateTime? endDate, int? siteId)
        {
            var query = context.Txns
                .Include(t => t.SiteIdfromNavigation)
                .Include(t => t.SiteIdtoNavigation)
                .Include(t => t.Employee)
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

        public object GenerateStoreOrderDetailReport(int orderId)
        {
            var query = context.Txns
                .Include(t => t.SiteIdfromNavigation)
                .Include(t => t.SiteIdtoNavigation)
                .Include(t => t.Employee)
                .Include(t => t.Txnitems)
                    .ThenInclude(ti => ti.Item)
                .Where(t => t.TxnId == orderId)
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
                    Notes = t.Notes,
                    Items = t.Txnitems.Select(i => new
                    {
                        ItemId = i.ItemId,
                        Name = i.Item.Name,
                        Quantity = i.Quantity,
                        CaseSize = i.Item.CaseSize,
                        Price = i.Item.RetailPrice,
                        Total = i.Quantity * i.Item.RetailPrice
                    }).ToList()
                });

            return query.ToList();
        }

        public object GenerateInventoryReport(int? siteId)
        {
            var query = context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Site)
                .Where(i => (!siteId.HasValue || i.SiteId == siteId.Value) &&
                            i.Item.Active == 1)
                .OrderBy(i => i.Site.SiteName)
                .ThenBy(i => i.Item.Name)
                .Select(i => new
                {
                    SiteName = i.Site.SiteName,
                    ItemId = i.ItemId,
                    ItemName = i.Item.Name,
                    Quantity = i.Quantity,
                    ReorderThreshold = i.ReorderThreshold ?? 0,
                    OptimumThreshold = i.OptimumThreshold,
                    BelowThreshold = i.Quantity < (i.ReorderThreshold ?? 0)
                });

            return query.ToList();
        }

        public object GenerateOrdersReport(DateTime? startDate, DateTime? endDate, int? siteId)
        {
            var query = context.Txns
                .Include(t => t.SiteIdfromNavigation)
                .Include(t => t.SiteIdtoNavigation)
                .Include(t => t.Employee)
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
        public object GenerateShippingReceiptReport(int txnId)
        {
            var query = context.Txns
                .Include(t => t.SiteIdfromNavigation)
                .Include(t => t.SiteIdtoNavigation)
                .Include(t => t.Employee)
                .Include(t => t.Txnitems)
                    .ThenInclude(ti => ti.Item)
                .Include(t => t.Delivery)
                    .ThenInclude(d => d.VehicleTypeNavigation)
                .Where(t => t.TxnId == txnId)
                .Select(t => new
                {
                    OrderId = t.TxnId,
                    BarCode = t.BarCode,
                    ShipDate = t.ShipDate,
                    VehicleType = t.Delivery.VehicleType,
                    FromSite = t.SiteIdfromNavigation.SiteName,
                    FromAddress = t.SiteIdfromNavigation.Address,
                    FromCity = t.SiteIdfromNavigation.City,
                    FromPostalCode = t.SiteIdfromNavigation.PostalCode,
                    FromPhone = t.SiteIdfromNavigation.Phone,
                    ToSite = t.SiteIdtoNavigation.SiteName,
                    ToAddress = t.SiteIdtoNavigation.Address,
                    ToCity = t.SiteIdtoNavigation.City,
                    ToPostalCode = t.SiteIdtoNavigation.PostalCode,
                    ToPhone = t.SiteIdtoNavigation.Phone,
                    CreatedBy = t.Employee.FirstName + " " + t.Employee.LastName,
                    EmergencyDelivery = t.EmergencyDelivery == 1 ? "Yes" : "No",
                    Notes = t.Notes,
                    Items = t.Txnitems.Select(i => new
                    {
                        ItemId = i.ItemId,
                        Name = i.Item.Name,
                        Quantity = i.Quantity,
                        CaseSize = i.Item.CaseSize,
                        Weight = i.Item.Weight,
                        TotalWeight = i.Quantity * i.Item.Weight
                    }).ToList()
                });

            return query.FirstOrDefault();
        }

        public object GenerateBackordersReport(DateTime? startDate, DateTime? endDate, int? siteId)
        {
            var query = context.Txns
                .Include(t => t.SiteIdfromNavigation)
                .Include(t => t.SiteIdtoNavigation)
                .Include(t => t.Employee)
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

        public object GenerateSupplierOrderReport(int? siteId, string supplierName = null, bool usePageBreaks = false)
        {
            var query = context.Items
                .Include(i => i.Supplier)
                .Include(i => i.Inventories)
                .Where(i =>
                    i.Active == 1 &&
                    (string.IsNullOrEmpty(supplierName) || i.Supplier.Name == supplierName)
                )
                .OrderBy(i => i.Supplier.Name)
                .ThenBy(i => i.Name)
                .Select(i => new
                {
                    ItemId = i.ItemId,
                    SKU = i.Sku,
                    Name = i.Name,
                    Description = i.Description,
                    CaseSize = i.CaseSize,
                    Category = i.Category,
                    CostPrice = i.CostPrice,
                    RetailPrice = i.RetailPrice,
                    Supplier = i.Supplier.Name,
                    WarehouseStock = i.Inventories.FirstOrDefault(inv => inv.SiteId == 2).Quantity,
                    ReorderThreshold = i.Inventories.FirstOrDefault(inv => inv.SiteId == 2).ReorderThreshold ?? 0,
                    BelowThreshold = i.Inventories.FirstOrDefault(inv => inv.SiteId == 2).Quantity <
                                    (i.Inventories.FirstOrDefault(inv => inv.SiteId == 2).ReorderThreshold ?? 0)
                });

            return query.ToList();
        }
    }
}
