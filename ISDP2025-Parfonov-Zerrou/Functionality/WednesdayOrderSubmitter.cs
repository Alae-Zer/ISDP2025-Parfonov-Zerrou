using System.Windows.Threading;
using ISDP2025_Parfonov_Zerrou.Models;

namespace ISDP2025_Parfonov_Zerrou.Functionality
{
    class WednesdayOrderSubmitter
    {
        private static DispatcherTimer THETimer;
        private static bool isStarted = false;
        private static readonly int SelectedHour = 16; // 4:00 PM

        public static void Initialize()
        {
            if (isStarted)
                return;

            // Create a timer that checks every hour
            THETimer = new DispatcherTimer();
            THETimer.Interval = TimeSpan.FromMinutes(60);
            THETimer.Tick += CheckIfWednesday4PM;
            THETimer.Start();

            // Also check immediately in case we're starting exactly at Wednesday 4 PM
            CheckIfWednesday4PM(null, null);

            isStarted = true;
            Console.WriteLine("Wednesday 4 PM order submitter initialized");
        }

        private static void CheckIfWednesday4PM(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            // Check if it's Wednesday AND it's 4 PM
            if (now.DayOfWeek == DayOfWeek.Wednesday && now.Hour == SelectedHour && now.Minute < 15)
            {
                Console.WriteLine("It's Wednesday 4 PM - submitting open orders");
                SubmitOpenOrders();
            }
        }

        private static void SubmitOpenOrders()
        {
            try
            {
                using (var context = new BestContext())
                {
                    // Get admin user for audit logs
                    var admin = context.Employees.FirstOrDefault(e => e.Username == "admin");
                    if (admin == null)
                        return;

                    // Find all NEW orders that have items
                    var orderIdsWithItems = context.Txnitems
                        .Select(ti => ti.TxnId)
                        .Distinct()
                        .ToList();

                    var ordersToSubmit = context.Txns
                        .Where(t => orderIdsWithItems.Contains(t.TxnId) &&
                               t.TxnStatus == "NEW" &&
                               (t.TxnType == "Store Order" || t.TxnType == "Emergency Order"))
                        .ToList();

                    // Submit each order
                    foreach (var order in ordersToSubmit)
                    {
                        order.TxnStatus = "SUBMITTED";

                        // Log the automatic submission
                        AuditTransactions.LogActivity(
                            admin,
                            order.TxnId,
                            order.TxnType,
                            "SUBMITTED",
                            order.SiteIdto,
                            order.DeliveryId,
                            "Automatically submitted on Wednesday at 4 PM"
                        );
                    }

                    // Save changes if any orders were updated
                    if (ordersToSubmit.Any())
                    {
                        context.SaveChanges();
                        Console.WriteLine($"Automatically submitted {ordersToSubmit.Count} orders");
                    }
                    else
                    {
                        Console.WriteLine("No orders to submit today");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Wednesday order submission: {ex.Message}");
            }
        }

        public static void Shutdown()
        {
            if (THETimer != null)
                THETimer.Stop();
        }
    }
}
