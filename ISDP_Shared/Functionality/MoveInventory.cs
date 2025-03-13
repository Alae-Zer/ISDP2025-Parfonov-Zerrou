using ISDP2025_Parfonov_Zerrou.Models;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Mohammed with some changes from serhii
//Last Modified by Mohammed on Feb 13,2025
namespace ISDP2025_Parfonov_Zerrou.Functionality
{
    public static class MoveInventory
    {
        //Updates In the database
        //Sends set of integers
        //Returns Bool
        public static bool Move(int itemId, int quantity, int fromSiteId, int toSiteId)
        {
            try
            {
                //Open Database ANd Assure proper Closing
                using (BestContext context = new BestContext())
                {
                    //QUERY
                    var source = context.Inventories.FirstOrDefault(i =>
                        i.ItemId == itemId &&
                        i.SiteId == fromSiteId);

                    //QUERY
                    var dest = context.Inventories.FirstOrDefault(i =>
                        i.ItemId == itemId &&
                        i.SiteId == toSiteId);

                    //If Required piece is not Found - False
                    if (source == null || dest == null || source.Quantity < quantity)
                        return false;

                    //Add And Subtract
                    source.Quantity -= quantity;
                    dest.Quantity += quantity;

                    //Save And Return True
                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                //Don't want specify exceptions here
                return false;
            }
        }


        public static bool MoveExtended(int itemId, int quantity, int fromSiteId, int toSiteId, string toLocation)
        {
            try
            {
                using (var context = new BestContext())
                {
                    // Check source inventory
                    var sourceInventory = context.Inventories
                        .FirstOrDefault(i => i.ItemId == itemId && i.SiteId == fromSiteId);
                    if (sourceInventory == null || sourceInventory.Quantity < quantity)
                        return false;

                    // Reduce source quantity
                    sourceInventory.Quantity -= quantity;

                    // Find any inventory for this item at the destination site
                    var destInventory = context.Inventories
                        .FirstOrDefault(i => i.ItemId == itemId && i.SiteId == toSiteId);

                    if (destInventory == null)
                    {
                        // Create new record
                        destInventory = new Inventory
                        {
                            ItemId = itemId,
                            SiteId = toSiteId,
                            ItemLocation = toLocation,
                            Quantity = quantity,
                            OptimumThreshold = 0
                        };
                        context.Inventories.Add(destInventory);
                    }
                    else
                    {
                        destInventory.Quantity += quantity;
                    }

                    context.SaveChanges();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}