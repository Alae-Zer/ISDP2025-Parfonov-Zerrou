using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Functionality
{
    public static class DeliveryManager
    {
        public static int CreateDelivery(string vehicleType, string notes = null, decimal distanceCost = 0)
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

        public static List<Delivery> GetDeliveries(DateTime? date = null)
        {
            try
            {
                using (var context = new BestContext())
                {
                    var query = context.Deliveries.AsQueryable();

                    if (date.HasValue)
                    {
                        var startDate = date.Value.Date;
                        var endDate = startDate.AddDays(1);
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

        public static bool SaveSignature(int deliveryId, byte[] signatureData)
        {
            try
            {
                using (var context = new BestContext())
                {
                    var delivery = context.Deliveries.Find(deliveryId);
                    if (delivery == null)
                    {
                        throw new Exception($"Delivery not found: {deliveryId}");
                    }

                    delivery.Signature = signatureData;
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save signature: {ex.Message}");
            }
        }

        public static List<Txn> GetOrdersByDelivery(int deliveryId)
        {
            try
            {
                using (var context = new BestContext())
                {
                    return context.Txns
                        .Include(t => t.SiteIdtoNavigation)
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

        public static List<Txn> GetUnassignedOrders()
        {
            try
            {
                using (var context = new BestContext())
                {
                    return context.Txns
                        .Include(t => t.SiteIdtoNavigation)
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
    }
}