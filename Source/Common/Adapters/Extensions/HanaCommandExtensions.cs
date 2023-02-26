using GoodsTracker.DataCollector.Models;
using Sap.Data.Hana;

namespace Common.Adapters.Extensions;

internal static class HanaCommandExtensions
{
    internal static void AddItemRecordToInsert(this HanaCommand cmd, Item item, object itemId)
    {
        cmd.Parameters.Add(CreateParameter("p0", itemId));
        cmd.Parameters.Add(CreateParameter("p1", item.Price));
        cmd.Parameters.Add(CreateParameter("p2", item.Discount));
        cmd.Parameters.Add(CreateParameter("p3", IsOnDiscount(item)));
    }

    internal static void AddItemRecordToInsert(this HanaCommand cmd, Item item)
    {
        cmd.Parameters.Add(CreateParameter("p0", item.Price));
        cmd.Parameters.Add(CreateParameter("p1", item.Discount));
        cmd.Parameters.Add(CreateParameter("p2", IsOnDiscount(item)));
    }

    internal static void AddItemToInsert(this HanaCommand cmd, Item item)
    {
        cmd.Parameters.Add(CreateParameter("p0", item.Name1));
        cmd.Parameters.Add(CreateParameter("p1", item.Name2));
        cmd.Parameters.Add(CreateParameter("p2", item.Name3));
        cmd.Parameters.Add(CreateParameter("p3", item.Link));
        cmd.Parameters.Add(CreateParameter("p4", item.Country));
        cmd.Parameters.Add(CreateParameter("p5", item.Producer));
        cmd.Parameters.Add(CreateParameter("p6", item.VendorCode));
        cmd.Parameters.Add(CreateParameter("p7", item.Wieght));
        cmd.Parameters.Add(CreateParameter("p8", item.WieghtUnit));
        cmd.Parameters.Add(CreateParameter("p9", item.Compound));
        cmd.Parameters.Add(CreateParameter("p10", item.Protein));
        cmd.Parameters.Add(CreateParameter("p11", item.Fat));
        cmd.Parameters.Add(CreateParameter("p12", item.Carbo));
        cmd.Parameters.Add(CreateParameter("p13", item.Portion));
    }

    internal static void AddItemImageLinkToUpdate(this HanaCommand cmd, Item item, object itemId)
    {
        cmd.Parameters.Add(CreateParameter("p0", item.Link));
        cmd.Parameters.Add(CreateParameter("p1", itemId));
    }

    internal static void AddCategoryToInsert(this HanaCommand cmd, string category)
    {
        cmd.Parameters.Add(CreateParameter("p0", category.GetHashCode()));
        cmd.Parameters.Add(CreateParameter("p1", category));
    }

    internal static void AddCatLinkToInsert(this HanaCommand cmd, int catHash)
    {
        cmd.Parameters.Add(CreateParameter("p0", catHash));
    }

    internal static bool TryExecuteCommand(this HanaCommand cmd)
    {
        try
        {
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (HanaException)
        {
            return false;
        }
    }

    private static int IsOnDiscount(Item item)
    => item.Discount is not null ? 1 : 0;

    private static HanaParameter CreateParameter(string pName, object? paramValue)
    {
        var param = new HanaParameter(pName, HanaDbType.VarChar);
        param.Value = paramValue;
        return param;
    }
}