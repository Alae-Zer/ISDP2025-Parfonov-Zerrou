using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("delivery")]
[Index("VehicleType", Name = "vehicleType")]
public partial class Delivery
{
    [Key]
    [Column("deliveryID")]
    public int DeliveryId { get; set; }

    [Column("deliveryDate", TypeName = "datetime")]
    public DateTime DeliveryDate { get; set; }

    [Column("distanceCost")]
    [Precision(10, 2)]
    public decimal DistanceCost { get; set; }

    [Column("vehicleType")]
    [StringLength(20)]
    public string VehicleType { get; set; } = null!;

    [Column("notes")]
    [StringLength(255)]
    public string? Notes { get; set; }

    [InverseProperty("Delivery")]
    public virtual ICollection<Txnaudit> Txnaudits { get; set; } = new List<Txnaudit>();

    [InverseProperty("Delivery")]
    public virtual ICollection<Txn> Txns { get; set; } = new List<Txn>();

    [ForeignKey("VehicleType")]
    [InverseProperty("Deliveries")]
    public virtual Vehicle VehicleTypeNavigation { get; set; } = null!;
}
