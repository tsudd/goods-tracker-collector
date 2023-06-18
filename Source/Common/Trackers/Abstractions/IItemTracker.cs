using GoodsTracker.DataCollector.Models;

namespace GoodsTracker.DataCollector.Common.Trackers.Abstractions;

public interface IItemTracker
{
    Task FetchItemsAsync();
    IList<ItemModel> GetShopItems(int shopId);
    void ClearData();
    bool IsThereAnythingToSave();
}
