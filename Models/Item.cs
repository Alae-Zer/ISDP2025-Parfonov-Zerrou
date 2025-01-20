using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("item")]
[Index("Category", Name = "category")]
[Index("SupplierId", Name = "supplierID")]
public partial class Item
{
    [Key]
    [Column("itemID")]
    public int ItemId { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("sku")]
    [StringLength(20)]
    public string Sku { get; set; } = null!;

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("category")]
    [StringLength(32)]
    public string Category { get; set; } = null!;

    [Column("weight")]
    [Precision(10, 2)]
    public decimal Weight { get; set; }

    [Column("caseSize")]
    public int CaseSize { get; set; }

    [Column("costPrice")]
    [Precision(10, 2)]
    public decimal CostPrice { get; set; }

    [Column("retailPrice")]
    [Precision(10, 2)]
    public decimal RetailPrice { get; set; }

    [Column("supplierID")]
    public int SupplierId { get; set; }

    [Column("notes")]
    [StringLength(255)]
    public string? Notes { get; set; }

    [Column("active")]
    public sbyte Active { get; set; }

    [Column("imageLocation")]
    [StringLength(255)]
    public string? ImageLocation { get; set; }

    [ForeignKey("Category")]
    [InverseProperty("Items")]
    public virtual Category CategoryNavigation { get; set; } = null!;

    [InverseProperty("Item")]
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    [ForeignKey("SupplierId")]
    [InverseProperty("Items")]
    public virtual Supplier Supplier { get; set; } = null!;

    [InverseProperty("Item")]
    public virtual ICollection<Txnitem> Txnitems { get; set; } = new List<Txnitem>();
}
