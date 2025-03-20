using ISDP2025_Parfonov_Zerrou.Models;

namespace ISDP_WEB.Pages.Small_Components
{
    public static class DeliveryCostCalculator
    {
        // Vehicle rates
        private static readonly Dictionary<string, (decimal MaxWeight, decimal CostPerKm, decimal HourlyRate)> VehicleRates =
            new Dictionary<string, (decimal, decimal, decimal)>
            {
                { "Van", (1000m, 0.75m, 10.00m) },
                { "Small", (5000m, 1.25m, 20.00m) },
                { "Medium", (10000m, 2.50m, 25.00m) },
                { "Heavy", (25000m, 3.50m, 35.00m) },
                { "Courier", (1000m, 0.00m, 50.00m) } // Special case for emergency deliveries
            };

        // Base hourly rate for a stop
        private const decimal BaseTimePerStop = 0.5m; // 30 minutes per stop

        // Calculate distance cost for a delivery
        public static decimal CalculateDistanceCost(List<Txn> orders, string vehicleType)
        {
            if (orders == null || !orders.Any())
                return 0;

            // Get vehicle cost per km
            if (!VehicleRates.TryGetValue(vehicleType, out var rates))
                throw new ArgumentException($"Unknown vehicle type: {vehicleType}");

            // We'll assume we're starting from the warehouse (site 2)
            const int WarehouseSiteId = 2;

            // Group orders by destination site
            var destinations = orders
                .Select(o => o.SiteIdtoNavigation)
                .Where(s => s != null)
                .DistinctBy(s => s.SiteId)
                .ToList();

            // Calculate total distance (assume round trip)
            decimal totalDistance = destinations.Sum(s => s.DistanceFromWh) * 2;

            // Calculate distance cost
            return totalDistance * rates.CostPerKm;
        }

        // Calculate time-based cost for a delivery
        public static decimal CalculateTimeCost(List<Txn> orders, string vehicleType)
        {
            if (orders == null || !orders.Any())
                return 0;

            // Get vehicle hourly rate
            if (!VehicleRates.TryGetValue(vehicleType, out var rates))
                throw new ArgumentException($"Unknown vehicle type: {vehicleType}");

            // Group orders by destination site
            var destinations = orders
                .GroupBy(o => o.SiteIdto)
                .Select(g => new { SiteId = g.Key, OrderCount = g.Count() })
                .ToList();

            // Calculate time for stops (30 mins per store plus 5 mins per order)
            decimal totalHours = destinations.Sum(d => BaseTimePerStop + (d.OrderCount * 0.08m));

            // Add drive time based on distance (assume 60 km/h average speed)
            var sites = orders
                .Select(o => o.SiteIdtoNavigation)
                .Where(s => s != null)
                .DistinctBy(s => s.SiteId)
                .ToList();

            decimal totalDistance = sites.Sum(s => s.DistanceFromWh) * 2;
            decimal driveHours = totalDistance / 60m;

            totalHours += driveHours;

            // Calculate time cost
            return totalHours * rates.HourlyRate;
        }

        // Calculate total delivery cost
        public static decimal CalculateTotalCost(List<Txn> orders, string vehicleType)
        {
            return CalculateDistanceCost(orders, vehicleType) + CalculateTimeCost(orders, vehicleType);
        }

        // Recommend best vehicle type based on weight
        public static string RecommendVehicleType(decimal totalWeight)
        {
            if (totalWeight <= 0)
                return "Van"; // Default for empty deliveries

            if (totalWeight <= 1000)
                return "Van";
            else if (totalWeight <= 5000)
                return "Small";
            else if (totalWeight <= 10000)
                return "Medium";
            else
                return "Heavy";
        }

        // Check if a vehicle can handle the weight
        public static bool CanVehicleHandleWeight(string vehicleType, decimal totalWeight)
        {
            if (!VehicleRates.TryGetValue(vehicleType, out var rates))
                return false;

            return totalWeight <= rates.MaxWeight;
        }

        // Get maximum weight for a vehicle type
        public static decimal GetVehicleMaxWeight(string vehicleType)
        {
            if (!VehicleRates.TryGetValue(vehicleType, out var rates))
                throw new ArgumentException($"Unknown vehicle type: {vehicleType}");

            return rates.MaxWeight;
        }
    }
}