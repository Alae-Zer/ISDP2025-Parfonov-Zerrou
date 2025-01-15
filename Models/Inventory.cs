using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[PrimaryKey("ItemId", "SiteId", "ItemLocation")]
[Table("inventory")]
[Index("SiteId", Name = "siteID")]
public partial class Inventory
{
    [Key]
    [Column("itemID")]
    public int ItemId { get; set; }

    [Key]
    [Column("siteID")]
    public int SiteId { get; set; }

    [Key]
    [Column("itemLocation")]
    [StringLength(9)]
    public string ItemLocation { get; set; } = null!;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("reorderThreshold")]
    public int? ReorderThreshold { get; set; }

    [Column("optimumThreshold")]
    public int OptimumThreshold { get; set; }

    [Column("notes")]
    [StringLength(255)]
    public string? Notes { get; set; }

    [ForeignKey("ItemId")]
    [InverseProperty("Inventories")]
    public virtual Item Item { get; set; } = null!;

    [ForeignKey("SiteId")]
    [InverseProperty("Inventories")]
    public virtual Site Site { get; set; } = null!;
}
