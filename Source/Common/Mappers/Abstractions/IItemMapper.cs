using GoodsTracker.DataCollector.Models;
using GoodsTracker.DataCollector.Models.Constants;

namespace GoodsTracker.DataCollector.Common.Mappers.Abstractions;

public interface IItemMapper
{
    const string CategoriesSeparator = "&";
    ItemModel MapItemFields(Dictionary<ItemFields, string> fields);
    ItemModel MapItemFields(Dictionary<ItemFields, object> fields);
}
