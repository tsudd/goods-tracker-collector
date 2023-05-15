namespace GoodsTracker.DataCollector.Common.Adapters.Extensions;

using GoodsTracker.DataCollector.Common.Adapters.Helpers;
using GoodsTracker.DataCollector.DB.Entities;
using GoodsTracker.DataCollector.Models;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

using StreamRecord = GoodsTracker.DataCollector.DB.Entities.Stream;

internal static class ItemModelExtensions
{
    public static Item ToEntity(this ItemModel model)
    {
        return new Item
        {
            Name1 = model.Name1 ?? throw new ArgumentException("Bad model for entity mapping."),
            Name2 = model.Name2,
            Name3 = model.Name3,
            Portion = model.Portion,
            Fat = model.Fat,
            Carbo = model.Carbo,
            Protein = model.Protein,
            Adult = model.Adult ?? false,
            VendorCode = model.VendorCode,
            ImageLink = model.Link != null ? new Uri(model.Link) : null,
            PublicId = model.Guid,
            Compound = model.Compound,
            Weight = model.Weight,
            WeightUnit = model.WeightUnit,
            Categories = new List<Category>(),
        };
    }

    public static bool DoesNotContainBasicInfo(this ItemModel model)
    {
        return string.IsNullOrEmpty(model.Name1) && model.Price == null;
    }

    public static ItemRecord ToItemRecord(this ItemModel model, StreamRecord stream, Item item)
    {
        return new ItemRecord
        {
            Price = model.Price!.Value,
            CutPrice = model.Discount,
            OnDiscount = model.Discount != null,
            Stream = stream,
            Item = item,
        };
    }

    public static bool DoesItemRequireUpdate(this ItemModel model, Item entity)
    {
        return model.Link != entity.ImageLink?.AbsolutePath
            || model.Fat != entity.Fat
            || model.Carbo != entity.Carbo
            || model.Protein != entity.Protein
            || model.Portion != entity.Portion;
    }

    public static string Serialize(this ItemModel model)
    {
        var opt = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            WriteIndented = false,
        };
        return JsonSerializer.Serialize(model, opt);
    }
}
