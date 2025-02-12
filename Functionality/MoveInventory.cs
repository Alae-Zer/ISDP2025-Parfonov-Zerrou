using ISDP2025_Parfonov_Zerrou.Models;

namespace ISDP2025_Parfonov_Zerrou.Functionality
{
    public static class MoveInventory
    {
        public static bool Move(int itemId, int quantity, int fromSiteId, int toSiteId)
        {
            try
            {
                using (BestContext context = new BestContext())
                {

                    var source = context.Inventories.FirstOrDefault(i =>
                        i.ItemId == itemId &&
                        i.SiteId == fromSiteId);

                    var dest = context.Inventories.FirstOrDefault(i =>
                        i.ItemId == itemId &&
                        i.SiteId == toSiteId);


                    if (source == null || dest == null || source.Quantity < quantity)
                        return false;


                    source.Quantity -= quantity;
                    dest.Quantity += quantity;

                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}