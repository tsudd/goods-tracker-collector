using GoodsTracker.DataCollector.Common.Trackers.Abstractions;

namespace GoodsTracker.DataCollector.Common.Adapters.Abstractions;
public interface IDataAdapter
{
    void SaveItems(IItemTracker tracker, IEnumerable<int> shopIds);
    Task SaveItemsAsync(IItemTracker tracker, IEnumerable<int> shopIds);
}
