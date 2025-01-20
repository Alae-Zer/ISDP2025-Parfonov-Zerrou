using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("posn")]
public partial class Posn
{
    [Key]
    [Column("positionID")]
    public int PositionId { get; set; }

    [Column("permissionLevel")]
    [StringLength(20)]
    public string PermissionLevel { get; set; } = null!;

    [Column("active")]
    public sbyte Active { get; set; }

    [InverseProperty("Position")]
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
