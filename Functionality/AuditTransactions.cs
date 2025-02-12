using ISDP2025_Parfonov_Zerrou.Models;

namespace ISDP2025_Parfonov_Zerrou.Functionality
{
    public static class AuditTransactions
    {
        public static void LogActivity(Employee employee, int txnId, string txnType, string status, int siteId, int? deliveryId = null, string? notes = null)
        {

            try
            {
                using (BestContext context = new BestContext())
                {
                    // Check if transaction exists
                    var transactionExists = context.Txns.Any(t => t.TxnId == txnId);
                    if (!transactionExists)
                    {
                        throw new Exception($"Transaction ID {txnId} does not exist");
                    }

                    var auditRecord = new Txnaudit
                    {
                        CreatedDate = DateTime.Now,
                        TxnId = txnId,
                        EmployeeId = employee.EmployeeID,
                        TxnType = txnType,
                        Status = status,
                        TxnDate = DateTime.Now,
                        SiteId = siteId,
                        DeliveryId = deliveryId,
                        Notes = notes
                    };

                    context.Txnaudits.Add(auditRecord);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create audit record: {ex.Message}");
            }
        }
    }
}

