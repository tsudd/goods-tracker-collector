using GoodsTracker.DataCollector.Models;

namespace GoodsTracker.DataCollector.Common.Trackers.Abstractions;

public interface IItemTracker
{
    Task FetchItemsAsync();
    IEnumerable<Item>? GetShopItems(string shopId);
    void ClearData();
}