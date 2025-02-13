using ISDP2025_Parfonov_Zerrou.Models;


public class BackorderManager
{
    BestContext context;
    private readonly Employee currentUser;

    public BackorderManager(BestContext inputContext, Employee inputUser)
    {
        context = inputContext;
        currentUser = inputUser;
    }

    public bool AddToBackorder(Txn originalTransaction, int itemId, int quantity)
    {
        try
        {
            //Get the item for case size validation
            var item = context.Items.Find(itemId);
            if (item == null)
                throw new Exception("Item not found");

            if (quantity % item.CaseSize != 0)
                throw new Exception($"Quantity must be a multiple of case size ({item.CaseSize})");

            //Look for existing active backorder for this site
            var existingBackorder = context.Txns
                .Where(t => t.SiteIdto == originalTransaction.SiteIdto
                       && t.TxnType == "BACKORDER"
                       && t.TxnStatus == "NEW")
                .FirstOrDefault();

            if (existingBackorder != null)
            {
                //Add to existing backorder
                var existingItem = context.Txnitems
                    .FirstOrDefault(ti => ti.TxnId == existingBackorder.TxnId
                                     && ti.ItemId == itemId);

                if (existingItem != null)
                {
                    //Update quantity if item exists
                    existingItem.Quantity += quantity;
                    context.Txnitems.Update(existingItem);
                }
                else
                {
                    //Add new item to existing backorder
                    var newItem = new Txnitem
                    {
                        TxnId = existingBackorder.TxnId,
                        ItemId = itemId,
                        Quantity = quantity
                    };
                    context.Txnitems.Add(newItem);
                }
            }
            else
            {
                //Create new backorder
                var newBackorder = new Txn
                {
                    EmployeeId = currentUser.EmployeeID,
                    SiteIdto = originalTransaction.SiteIdto,
                    SiteIdfrom = 2,
                    TxnStatus = "NEW",
                    ShipDate = GetNextDeliveryDay(originalTransaction.SiteIdto),
                    TxnType = "BACKORDER",
                    BarCode = $"BO-{DateTime.Now:yyyyMMddHHmmss}",
                    CreatedDate = DateTime.Now
                };

                context.Txns.Add(newBackorder);
                context.SaveChanges();

                //Add item to new backorder
                var newItem = new Txnitem
                {
                    TxnId = newBackorder.TxnId,
                    ItemId = itemId,
                    Quantity = quantity
                };
                context.Txnitems.Add(newItem);
            }

            context.SaveChanges();
            return true;
        }
        catch
        {
            throw;
        }
    }

    private DateTime GetNextDeliveryDay(int siteId)
    {
        var site = context.Sites.Find(siteId);
        if (site == null || string.IsNullOrEmpty(site.DayOfWeek))
            throw new Exception("Site or delivery day not found");

        var today = DateTime.Today;

        //If today is not the delivery day, use today
        if (!today.DayOfWeek.ToString().Equals(site.DayOfWeek, StringComparison.OrdinalIgnoreCase))
        {
            return today;
        }

        //If today is delivery day, use tomorrow
        return today.AddDays(1);
    }
}