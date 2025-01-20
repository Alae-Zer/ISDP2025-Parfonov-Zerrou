using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("txnstatus")]
public partial class Txnstatus
{
    [Key]
    [Column("statusName")]
    [StringLength(20)]
    public string StatusName { get; set; } = null!;

    [Column("statusDescription")]
    [StringLength(100)]
    public string StatusDescription { get; set; } = null!;

    [Column("active")]
    public sbyte Active { get; set; }

    [InverseProperty("TxnStatusNavigation")]
    public virtual ICollection<Txn> Txns { get; set; } = new List<Txn>();
}
