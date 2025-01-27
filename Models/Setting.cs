using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("settings")]
public partial class Setting
{
    [Key]
    [Column("setting_type", TypeName = "enum('global')")]
    public string SettingType { get; set; } = null!;

    public int LogoutTimeMinutes { get; set; }
}
