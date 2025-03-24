using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Functionality
{
    public static class DeliveryManager
    {
        // Vehicle information model
        public class VehicleInfo
        {
            public string? Type { get; set; }
            public decimal MaxWeight { get; set; }
            public decimal CostPerKm { get; set; }
            public decimal HourlyRate { get; set; }
        }

        public static List<VehicleInfo> VehicleTypes = new List<VehicleInfo>
        {
            new VehicleInfo { Type = "Van", MaxWeight = 1000, CostPerKm = 0.75m, HourlyRate = 10.00m },
            new VehicleInfo { Type = "Small", MaxWeight = 5000, CostPerKm = 1.25m, HourlyRate = 20.00m },
            new VehicleInfo { Type = "Medium", MaxWeight = 10000, CostPerKm = 2.50m, HourlyRate = 25.00m },
            new VehicleInfo { Type = "Heavy", MaxWeight = 25000, CostPerKm = 3.50m, HourlyRate = 35.00m }
        };

        // Create a new delivery
        public static int CreateDelivery(string vehicleType, string? notes = null, decimal distanceCost = 0)
        {
            try
            {
                using (var context = new BestContext())
                {
                    var delivery = new Delivery
                    {
                        DeliveryDate = DateTime.Now,
                        VehicleType = vehicleType,
                        Notes = notes,
                        DistanceCost = distanceCost
                    };

                    context.Deliveries.Add(delivery);
                    context.SaveChanges();

                    return delivery.DeliveryId;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create delivery: {ex.Message}");
            }
        }

        // Get all deliveries with optional date filter
        public static List<Delivery> GetDeliveries(DateTime? date = null)
        {
            try
            {
                using (var context = new BestContext())
                {
                    // get all deliveries
                    var query = context.Deliveries.AsQueryable();
                    // and get the ones you need if nessesary
                    if (date.HasValue)
                    {
                        var startDate = date.Value.Date;
                        var endDate = startDate.AddDays(7);
                        query = query.Where(d => d.DeliveryDate >= startDate && d.DeliveryDate < endDate);
                    }

                    return query.OrderByDescending(d => d.DeliveryDate).ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get deliveries: {ex.Message}");
            }
        }

        // Assign an order to a delivery
        public static bool AssignOrderToDelivery(int txnId, int deliveryId, Employee employee)
        {
            try
            {
                using (var context = new BestContext())
                {
                    var order = context.Txns.Find(txnId);
                    if (order == null)
                    {
                        throw new Exception($"Order not found: {txnId}");
                    }

                    var delivery = context.Deliveries.Find(deliveryId);
                    if (delivery == null)
                    {
                        throw new Exception($"Delivery not found: {deliveryId}");
                    }

                    order.DeliveryId = deliveryId;
                    context.SaveChanges();

                    AuditTransactions.LogActivity(
                        employee,
                        order.TxnId,
                        order.TxnType,
                        order.TxnStatus,
                        order.SiteIdto,
                        deliveryId,
                        $"Order assigned to delivery #{deliveryId}"
                    );

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to assign order to delivery: {ex.Message}");
            }
        }

        // Get all orders for a specific delivery
        public static List<Txn> GetOrdersByDelivery(int deliveryId)
        {
            try
            {
                using (var context = new BestContext())
                {
                    return context.Txns
                        .Include(t => t.SiteIdtoNavigation)
                        .Include(t => t.Txnitems)
                            .ThenInclude(ti => ti.Item)
                        .Where(t => t.DeliveryId == deliveryId)
                        .OrderBy(t => t.ShipDate)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get orders by delivery: {ex.Message}");
            }
        }

        // Get all orders that don't have a delivery assigned yet
        public static List<Txn> GetUnassignedOrders()
        {
            try
            {
                using (var context = new BestContext())
                {
                    return context.Txns
                        .Include(t => t.SiteIdtoNavigation)
                        .Include(t => t.Txnitems)
                            .ThenInclude(ti => ti.Item)
                        .Where(t => t.TxnStatus == "ASSEMBLED" && t.DeliveryId == null &&
                               (t.TxnType == "Store Order" ||
                                t.TxnType == "Emergency Order" ||
                                t.TxnType == "Back Order"))
                        .OrderBy(t => t.ShipDate)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get unassigned orders: {ex.Message}");
            }
        }

        // Calculate the total weight of an order
        public static decimal CalculateOrderWeight(Txn order)
        {
            if (order == null)
                return 0;

            if (order.Txnitems == null || !order.Txnitems.Any())
            {
                try
                {
                    using (var context = new BestContext())
                    {
                        var items = context.Txnitems
                            .Include(i => i.Item)
                            .Where(i => i.TxnId == order.TxnId)
                            .ToList();

                        decimal totalWeight = 0;
                        foreach (var item in items)
                        {
                            totalWeight += item.Item.Weight * item.Quantity;
                        }
                        return totalWeight;
                    }
                }
                catch
                {
                    return 0;
                }
            }

            decimal weight = 0;
            foreach (var item in order.Txnitems)
            {
                weight += item.Item.Weight * item.Quantity;
            }
            return weight;
        }

        // Calculate distance cost for a delivery based on orders and vehicle type
        public static decimal CalculateDistanceCost(List<Txn> orders, string vehicleType)
        {
            decimal totalDistance = 0;
            if (orders == null || !orders.Any())
                return 0;

            // Get vehicle cost per km
            VehicleInfo vehicle = null;
            foreach (var v in VehicleTypes)
            {
                if (v.Type == vehicleType)
                {
                    vehicle = v;
                    break;
                }
            }

            // Group orders by destination site
            var destinations = orders
                .Select(o => o.SiteIdtoNavigation)
                .Where(s => s != null)
                .DistinctBy(s => s.SiteId)
                .ToList();

            // Calculate total distance (we will assume round trip to store and back)
            foreach (var destination in destinations)
            {
                totalDistance += destination.DistanceFromWh * 2;
            }

            // Calculate distance cost
            return totalDistance * vehicle.CostPerKm;
        }
    }
}