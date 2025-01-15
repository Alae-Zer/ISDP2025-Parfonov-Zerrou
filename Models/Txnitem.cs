using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[PrimaryKey("TxnId", "ItemId")]
[Table("txnitems")]
[Index("ItemId", Name = "ItemID")]
public partial class Txnitem
{
    [Key]
    [Column("txnID")]
    public int TxnId { get; set; }

    [Key]
    [Column("ItemID")]
    public int ItemId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("notes")]
    [StringLength(255)]
    public string? Notes { get; set; }

    [ForeignKey("ItemId")]
    [InverseProperty("Txnitems")]
    public virtual Item Item { get; set; } = null!;

    [ForeignKey("TxnId")]
    [InverseProperty("Txnitems")]
    public virtual Txn Txn { get; set; } = null!;
}
