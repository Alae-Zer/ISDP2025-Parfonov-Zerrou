using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("permissions")]
public partial class Permission
{
    [Key]
    [Column("permissionID")]
    public int PermissionId { get; set; }

    [Column("permissionName")]
    [StringLength(50)]
    public string? PermissionName { get; set; }

    [ForeignKey("PermissionId")]
    [InverseProperty("Permissions")]
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
