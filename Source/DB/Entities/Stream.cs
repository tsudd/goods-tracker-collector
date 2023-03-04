namespace GoodsTracker.DataCollector.DB.Entities;

using System.ComponentModel.DataAnnotations.Schema;

public class Stream
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public DateTime FetchStartDate { get; set; }
    public DateTime FetchEndDate { get; set; }
}