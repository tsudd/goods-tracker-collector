namespace GoodsTracker.DataCollector.DB.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

[PrimaryKey("ItemId", "UserId")]
public class FavoriteItem
{
    [Required]
    public int ItemId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    // FK
    [Required]
    public Item Item { get; set; } = null!;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime DateAdded { get; set; }
}