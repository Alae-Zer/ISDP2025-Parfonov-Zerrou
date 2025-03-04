using ISDP2025_Parfonov_Zerrou.Models;

//ISDP Project
//Mohammed Alae-Zerrou, Serhii Parfonov
//NBCC, Winter 2025
//Completed By Mohammed with some changes from serhii
//Last Modified by Mohammed on Feb 13,2025
namespace ISDP2025_Parfonov_Zerrou.Functionality
{
    public static class AuditTransactions
    {
        //Create Record in the database
        //Sends Set Of Required Parameters
        //Returns Nothing
        public static void LogActivity(Employee employee, int txnId, string txnType, string status, int siteId, int? deliveryId = null, string? notes = null)
        {

            try
            {
                //Open database connection and assure it's properly closed
                using (BestContext context = new BestContext())
                {
                    //Check if transaction exists else throw an exeption
                    var transactionExists = context.Txns.Any(t => t.TxnId == txnId);
                    if (!transactionExists)
                    {
                        throw new Exception($"Transaction ID {txnId} does not exist");
                    }

                    //Create New Record instance
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

                    //Add and save record
                    context.Txnaudits.Add(auditRecord);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                //New exception Thrown
                throw new Exception($"Failed to create audit record: {ex.Message}");
            }
        }
    }
}

