using GoodsTracker.DataCollector.Models;
using GoodsTracker.DataCollector.Common.Configuration;

namespace GoodsTracker.DataCollector.Common.Scrapers.Abstractions;

public interface IScraper
{
    Task<IEnumerable<ItemModel>> GetItemsAsync();
    ScraperConfig GetConfig();
}
