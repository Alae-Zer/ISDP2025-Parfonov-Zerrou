using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("deliverymethod")]
[Index("ProvinceId", Name = "provinceID")]
public partial class Deliverymethod
{
    [Key]
    [Column("deliveryMethodID")]
    public int DeliveryMethodId { get; set; }

    [Column("name")]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Column("address1")]
    [StringLength(50)]
    public string? Address1 { get; set; }

    [Column("address2")]
    [StringLength(50)]
    public string? Address2 { get; set; }

    [Column("city")]
    [StringLength(50)]
    public string? City { get; set; }

    [Column("country")]
    [StringLength(50)]
    public string? Country { get; set; }

    [Column("provinceID")]
    [StringLength(2)]
    public string ProvinceId { get; set; } = null!;

    [Column("postalcode")]
    [StringLength(11)]
    public string? Postalcode { get; set; }

    [Column("phone")]
    [StringLength(14)]
    public string? Phone { get; set; }

    [Column("contact")]
    [StringLength(100)]
    public string? Contact { get; set; }

    [Column("notes")]
    [StringLength(255)]
    public string? Notes { get; set; }

    [Column("active")]
    public sbyte Active { get; set; }

    [ForeignKey("ProvinceId")]
    [InverseProperty("Deliverymethods")]
    public virtual Province Province { get; set; } = null!;
}
