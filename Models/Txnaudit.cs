using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("txnaudit")]
[Index("SiteId", Name = "SiteID")]
[Index("DeliveryId", Name = "deliveryID")]
[Index("EmployeeId", Name = "employeeID")]
[Index("TxnType", Name = "txnType")]
public partial class Txnaudit
{
    [Key]
    [Column("txnAuditID")]
    public int TxnAuditId { get; set; }

    [Column("createdDate", TypeName = "datetime")]
    public DateTime CreatedDate { get; set; }

    [Column("txnID")]
    public int TxnId { get; set; }

    [Column("employeeID")]
    public int EmployeeId { get; set; }

    [Column("txnType")]
    [StringLength(20)]
    public string TxnType { get; set; } = null!;

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column("txnDate", TypeName = "datetime")]
    public DateTime TxnDate { get; set; }

    [Column("SiteID")]
    public int SiteId { get; set; }

    [Column("deliveryID")]
    public int? DeliveryId { get; set; }

    [Column("notes")]
    [StringLength(255)]
    public string? Notes { get; set; }

    [ForeignKey("DeliveryId")]
    [InverseProperty("Txnaudits")]
    public virtual Delivery? Delivery { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Txnaudits")]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey("SiteId")]
    [InverseProperty("Txnaudits")]
    public virtual Site Site { get; set; } = null!;

    [ForeignKey("TxnType")]
    [InverseProperty("Txnaudits")]
    public virtual Txntype TxnTypeNavigation { get; set; } = null!;
}
