using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("vehicle")]
public partial class Vehicle
{
    [Key]
    [Column("vehicleType")]
    [StringLength(20)]
    public string VehicleType { get; set; } = null!;

    [Column("maxWeight")]
    [Precision(10, 0)]
    public decimal MaxWeight { get; set; }

    [Precision(10, 2)]
    public decimal HourlyTruckCost { get; set; }

    [Column("costPerKm")]
    [Precision(10, 2)]
    public decimal CostPerKm { get; set; }

    [Column("notes")]
    [StringLength(255)]
    public string Notes { get; set; } = null!;

    [Column("active")]
    public sbyte Active { get; set; }

    [InverseProperty("VehicleTypeNavigation")]
    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
