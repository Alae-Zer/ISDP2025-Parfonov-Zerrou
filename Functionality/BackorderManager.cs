using ISDP2025_Parfonov_Zerrou.Functionality;
using ISDP2025_Parfonov_Zerrou.Models;
using Microsoft.EntityFrameworkCore;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Mohammed with some changes from serhii
//Last Modified by Serhii on Feb 16,2025
namespace ISDP2025_Parfonov_Zerrou.Managers
{
    public class BackorderManager
    {
        // DB context and current user 
        BestContext context = new();
        Employee currentUser;

        // Constructor to set current user
        public BackorderManager(Employee inputUser)
        {
            currentUser = inputUser;
        }

        // Get all NEW backorders, ordered by creation date
        public List<Txn> GetAllBackorders()
        {
            try
            {
                using var context = new BestContext();
                return context.Txns
                    .Include(t => t.SiteIdtoNavigation)
                    .Where(t => t.TxnType == "Back Order" &&
                               (t.TxnStatus == "NEW" ||
                                t.TxnStatus == "RECEIVED"))
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving all backorders: {ex.Message}");
            }
        }

        // Get existing NEW backorder for a site
        public Txn? GetExistingBackorder(int siteId)
        {
            try
            {
                //Open Conn
                using var context = new BestContext();
                //Not Arguing for Possible Null
                return context.Txns
                    .FirstOrDefault(t => t.SiteIdto == siteId
                                    && t.TxnType == "Back Order"
                                    && (t.TxnStatus == "NEW" || t.TxnStatus == "RECEIVED"));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving existing backorder: {ex.Message}");
            }
        }

        public bool CreateNewBackorder(int siteId)
        {
            try
            {
                var newBackorder = new Txn
                {
                    EmployeeId = currentUser.EmployeeID,
                    SiteIdto = siteId,
                    SiteIdfrom = 2,
                    TxnStatus = "NEW",
                    ShipDate = GetNextDeliveryDay(siteId),
                    TxnType = "Back Order",
                    BarCode = $"BO-{DateTime.Now:yyyyMMddHHmmss}",
                    CreatedDate = DateTime.Now
                };

                context.Txns.Add(newBackorder);
                context.SaveChanges();

                //Audit log
                AuditTransactions.LogActivity(
                    currentUser,
                    newBackorder.TxnId,
                    "Back Order",
                    "NEW",
                    siteId,
                    null,
                    "New backorder created"
                );

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating new backorder: {ex.Message}");
            }
        }

        //Adds or Updates items in backorders
        public bool AddItemToBackorder(int txnId, int itemId, int quantity)
        {
            try
            {
                var item = context.Items.Find(itemId);
                if (item == null)
                    throw new Exception("Item not found");

                if (quantity % item.CaseSize != 0)
                    throw new Exception($"Quantity must be a multiple of case size ({item.CaseSize})");

                var existingItem = context.Txnitems
                    .FirstOrDefault(ti => ti.TxnId == txnId && ti.ItemId == itemId);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    context.Txnitems.Update(existingItem);
                }
                else
                {
                    var newItem = new Txnitem
                    {
                        TxnId = txnId,
                        ItemId = itemId,
                        Quantity = quantity
                    };
                    context.Txnitems.Add(newItem);
                }

                context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding item to backorder: {ex.Message}");
            }
        }

        //Add Item to backorder
        public bool AddToBackorder(Txn originalTransaction, int itemId, int quantity)
        {
            try
            {
                var item = context.Items.Find(itemId);
                if (item == null)
                    throw new Exception("Item not found");

                if (quantity % item.CaseSize != 0)
                    throw new Exception($"Quantity must be a multiple of case size ({item.CaseSize})");

                var existingBackorder = GetExistingBackorder(originalTransaction.SiteIdto);

                if (existingBackorder != null)
                {
                    return AddItemToBackorder(existingBackorder.TxnId, itemId, quantity);
                }
                else
                {
                    if (CreateNewBackorder(originalTransaction.SiteIdto))
                    {
                        var newBackorder = GetExistingBackorder(originalTransaction.SiteIdto);
                        return AddItemToBackorder(newBackorder.TxnId, itemId, quantity);
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding to backorder: {ex.Message}");
            }
        }

        public List<Txnitem> GetBackorderItems(int txnId)
        {
            try
            {
                List<Txnitem> backorderItems = context.Txnitems
                    .Include(ti => ti.Item)
                    .Where(ti => ti.TxnId == txnId)
                    .ToList();

                return backorderItems;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving backorder items for transaction {txnId}: {ex.Message}");
            }
        }

        public bool RemoveItemFromBackorder(int txnId, int itemId)
        {
            try
            {
                var txnItem = context.Txnitems
                    .FirstOrDefault(ti => ti.TxnId == txnId && ti.ItemId == itemId);

                if (txnItem != null)
                {
                    var txn = context.Txns.Find(txnId);
                    context.Txnitems.Remove(txnItem);
                    context.SaveChanges();

                    return true;
                }
                else
                {
                    throw new Exception("Item not found in backorder");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error removing item from backorder: {ex.Message}");
            }
        }

        public bool UpdateBackorderShipDate(int txnId, DateTime newShipDate)
        {
            //Find Backorder
            var backorder = context.Txns.Find(txnId);
            if (backorder == null)
                throw new Exception("Backorder not found");

            //Save New Date
            backorder.ShipDate = newShipDate;
            context.SaveChanges();

            //Audit Log
            AuditTransactions.LogActivity(
                currentUser,
                txnId,
                "Back Order",
                "UPDATED",
                backorder.SiteIdto,
                null,
                $"Updated ship date to {newShipDate:MM/dd/yyyy}"
            );

            return true;
        }

        //Determines Next Delivery Date
        private DateTime GetNextDeliveryDay(int siteId)
        {
            //Get site or throw error if not found
            var site = context.Sites.Find(siteId);
            if (site == null)
                throw new Exception("Site not found!");

            //Get delivery day (default Monday)
            var deliveryDay = site.DayOfWeek?.Trim().ToUpper() ?? "MONDAY";

            //Convert day name to number
            int deliveryDayNumber;
            switch (deliveryDay)
            {
                case "MONDAY": deliveryDayNumber = 1; break;
                case "TUESDAY": deliveryDayNumber = 2; break;
                case "WEDNESDAY": deliveryDayNumber = 3; break;
                case "THURSDAY": deliveryDayNumber = 4; break;
                case "FRIDAY": deliveryDayNumber = 5; break;
                default: deliveryDayNumber = 1; break;
            }

            //Get today's date and day number
            var today = DateTime.Now.Date;
            int todayNumber = (int)today.DayOfWeek;

            //Calculate days until next delivery
            int daysToWait;
            if (todayNumber < deliveryDayNumber)
                daysToWait = deliveryDayNumber - todayNumber;
            else if (todayNumber > deliveryDayNumber)
                daysToWait = 7 - (todayNumber - deliveryDayNumber);
            else
                daysToWait = 7;

            return today.AddDays(daysToWait);
        }
    }
}