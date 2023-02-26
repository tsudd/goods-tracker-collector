using GoodsTracker.DataCollector.Models;
using GoodsTracker.DataCollector.Models.Constants;

namespace GoodsTracker.DataCollector.Common.Mappers.Abstractions;
public interface IItemMapper
{
    const string CATEGORIES_SEPARATOR = "&";
    Item MapItemFields(Dictionary<ItemFields, string> fields);
}