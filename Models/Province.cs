using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("province")]
public partial class Province
{
    [Key]
    [Column("provinceID")]
    [StringLength(2)]
    public string ProvinceId { get; set; } = null!;

    [Column("provinceName")]
    [StringLength(20)]
    public string ProvinceName { get; set; } = null!;

    [Column("countryCode")]
    [StringLength(50)]
    public string CountryCode { get; set; } = null!;

    [Column("active")]
    public sbyte Active { get; set; }

    [InverseProperty("Province")]
    public virtual ICollection<Deliverymethod> Deliverymethods { get; set; } = new List<Deliverymethod>();

    [InverseProperty("Province")]
    public virtual ICollection<Site> Sites { get; set; } = new List<Site>();

    [InverseProperty("ProvinceNavigation")]
    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
