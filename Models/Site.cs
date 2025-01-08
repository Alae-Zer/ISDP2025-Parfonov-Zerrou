using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("site")]
[Index("ProvinceId", Name = "provinceID")]
public partial class Site
{
    [Key]
    [Column("siteID")]
    public int SiteId { get; set; }

    [Column("siteName")]
    [StringLength(50)]
    public string SiteName { get; set; } = null!;

    [Column("address")]
    [StringLength(50)]
    public string Address { get; set; } = null!;

    [Column("address2")]
    [StringLength(50)]
    public string? Address2 { get; set; }

    [Column("city")]
    [StringLength(50)]
    public string City { get; set; } = null!;

    [Column("provinceID")]
    [StringLength(2)]
    public string ProvinceId { get; set; } = null!;

    [Column("country")]
    [StringLength(50)]
    public string Country { get; set; } = null!;

    [Column("postalCode")]
    [StringLength(14)]
    public string PostalCode { get; set; } = null!;

    [Column("phone")]
    [StringLength(14)]
    public string Phone { get; set; } = null!;

    [Column("dayOfWeek")]
    [StringLength(50)]
    public string? DayOfWeek { get; set; }

    [Column("distanceFromWH")]
    public int DistanceFromWh { get; set; }

    [Column("notes")]
    [StringLength(255)]
    public string? Notes { get; set; }

    [Column("active")]
    public sbyte Active { get; set; }

    [InverseProperty("Site")]
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    [InverseProperty("Site")]
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    [ForeignKey("ProvinceId")]
    [InverseProperty("Sites")]
    public virtual Province Province { get; set; } = null!;

    [InverseProperty("SiteIdfromNavigation")]
    public virtual ICollection<Txn> TxnSiteIdfromNavigations { get; set; } = new List<Txn>();

    [InverseProperty("SiteIdtoNavigation")]
    public virtual ICollection<Txn> TxnSiteIdtoNavigations { get; set; } = new List<Txn>();

    [InverseProperty("Site")]
    public virtual ICollection<Txnaudit> Txnaudits { get; set; } = new List<Txnaudit>();
}
