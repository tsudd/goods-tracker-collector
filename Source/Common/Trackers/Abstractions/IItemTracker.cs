using GoodsTracker.DataCollector.Models;

namespace GoodsTracker.DataCollector.Common.Trackers.Abstractions;

public interface IItemTracker
{
    Task FetchItemsAsync();
    IEnumerable<ItemModel> GetShopItems(int shopId);
    void ClearData();
}
