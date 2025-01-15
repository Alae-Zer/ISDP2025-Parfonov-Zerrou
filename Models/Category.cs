using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISDP2025_Parfonov_Zerrou.Models;

[Table("category")]
public partial class Category
{
    [Key]
    [Column("categoryName")]
    [StringLength(32)]
    public string CategoryName { get; set; } = null!;

    [Column("active")]
    public sbyte Active { get; set; }

    [InverseProperty("CategoryNavigation")]
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
