namespace GoodsTracker.DataCollector.DB.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class Producer
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public uint Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Country { get; set; }
}
