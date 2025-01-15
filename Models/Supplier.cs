using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("supplier")]
[Index("Province", Name = "province")]
public partial class Supplier
{
    [Key]
    [Column("supplierID")]
    public int SupplierId { get; set; }

    [Column("name")]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Column("address1")]
    [StringLength(50)]
    public string Address1 { get; set; } = null!;

    [Column("address2")]
    [StringLength(50)]
    public string? Address2 { get; set; }

    [Column("city")]
    [StringLength(50)]
    public string City { get; set; } = null!;

    [Column("country")]
    [StringLength(50)]
    public string Country { get; set; } = null!;

    [Column("province")]
    [StringLength(2)]
    public string Province { get; set; } = null!;

    [Column("postalcode")]
    [StringLength(11)]
    public string Postalcode { get; set; } = null!;

    [Column("phone")]
    [StringLength(14)]
    public string Phone { get; set; } = null!;

    [Column("contact")]
    [StringLength(100)]
    public string? Contact { get; set; }

    [Column("notes")]
    [StringLength(255)]
    public string? Notes { get; set; }

    [Column("active")]
    public sbyte Active { get; set; }

    [InverseProperty("Supplier")]
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    [ForeignKey("Province")]
    [InverseProperty("Suppliers")]
    public virtual Province ProvinceNavigation { get; set; } = null!;
}
