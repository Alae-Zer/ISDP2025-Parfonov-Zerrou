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
    }
}