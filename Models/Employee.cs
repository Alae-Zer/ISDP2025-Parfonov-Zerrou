using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("employee")]
[Index("PositionId", Name = "PositionID")]
[Index("SiteId", Name = "siteID")]
[Index("Username", Name = "username_constraint", IsUnique = true)]
public partial class Employee
{
    [Key]
    [Column("employeeID")]
    public int EmployeeId { get; set; }

    [StringLength(255)]
    public string Password { get; set; } = null!;

    [StringLength(20)]
    public string FirstName { get; set; } = null!;

    [StringLength(20)]
    public string LastName { get; set; } = null!;

    [StringLength(100)]
    public string? Email { get; set; }

    [Column("PositionID")]
    public int PositionId { get; set; }

    [Column("siteID")]
    public int SiteId { get; set; }

    [Column("username")]
    public string Username { get; set; } = null!;

    [Column("notes")]
    [StringLength(255)]
    public string? Notes { get; set; }

    [Column("locked")]
    public sbyte? Locked { get; set; }

    [Column("active")]
    public sbyte Active { get; set; }

    [ForeignKey("PositionId")]
    [InverseProperty("Employees")]
    public virtual Posn Position { get; set; } = null!;

    [ForeignKey("SiteId")]
    [InverseProperty("Employees")]
    public virtual Site Site { get; set; } = null!;

    [InverseProperty("Employee")]
    public virtual ICollection<Txnaudit> Txnaudits { get; set; } = new List<Txnaudit>();

    [InverseProperty("Employee")]
    public virtual ICollection<Txn> Txns { get; set; } = new List<Txn>();
}
