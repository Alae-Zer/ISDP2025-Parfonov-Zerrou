using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("txntype")]
public partial class Txntype
{
    [Key]
    [Column("txnType")]
    [StringLength(20)]
    public string TxnType1 { get; set; } = null!;

    [Column("active")]
    public sbyte Active { get; set; }

    [InverseProperty("TxnTypeNavigation")]
    public virtual ICollection<Txnaudit> Txnaudits { get; set; } = new List<Txnaudit>();

    [InverseProperty("TxnTypeNavigation")]
    public virtual ICollection<Txn> Txns { get; set; } = new List<Txn>();
}
