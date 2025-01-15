using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("txn")]
[Index("DeliveryId", Name = "deliveryID")]
[Index("EmployeeId", Name = "employeeID")]
[Index("SiteIdfrom", Name = "siteIDFrom")]
[Index("SiteIdto", Name = "siteIDTo")]
[Index("TxnStatus", Name = "txnStatus")]
[Index("TxnType", Name = "txnType")]
public partial class Txn
{
    [Key]
    [Column("txnID")]
    public int TxnId { get; set; }

    [Column("employeeID")]
    public int EmployeeId { get; set; }

    [Column("siteIDTo")]
    public int SiteIdto { get; set; }

    [Column("siteIDFrom")]
    public int SiteIdfrom { get; set; }

    [Column("txnStatus")]
    [StringLength(20)]
    public string TxnStatus { get; set; } = null!;

    [Column("shipDate", TypeName = "datetime")]
    public DateTime ShipDate { get; set; }

    [Column("txnType")]
    [StringLength(20)]
    public string TxnType { get; set; } = null!;

    [Column("barCode")]
    [StringLength(50)]
    public string BarCode { get; set; } = null!;

    [Column("createdDate", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [Column("deliveryID")]
    public int? DeliveryId { get; set; }

    [Column("emergencyDelivery")]
    public sbyte? EmergencyDelivery { get; set; }

    [Column("notes")]
    [StringLength(255)]
    public string? Notes { get; set; }

    [ForeignKey("DeliveryId")]
    [InverseProperty("Txns")]
    public virtual Delivery? Delivery { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Txns")]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey("SiteIdfrom")]
    [InverseProperty("TxnSiteIdfromNavigations")]
    public virtual Site SiteIdfromNavigation { get; set; } = null!;

    [ForeignKey("SiteIdto")]
    [InverseProperty("TxnSiteIdtoNavigations")]
    public virtual Site SiteIdtoNavigation { get; set; } = null!;

    [ForeignKey("TxnStatus")]
    [InverseProperty("Txns")]
    public virtual Txnstatus TxnStatusNavigation { get; set; } = null!;

    [ForeignKey("TxnType")]
    [InverseProperty("Txns")]
    public virtual Txntype TxnTypeNavigation { get; set; } = null!;

    [InverseProperty("Txn")]
    public virtual ICollection<Txnitem> Txnitems { get; set; } = new List<Txnitem>();
}
