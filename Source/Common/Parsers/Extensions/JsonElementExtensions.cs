namespace GoodsTracker.DataCollector.Common.Parsers.Extensions;

using System.Text.Json;

internal static class JsonElementExtensions
{
    public delegate bool TryGetValueDelegate<TValue>(out TValue value) where TValue : new();
    public static bool TryGetPropertyValue<TValue>(
        this JsonElement jsonElement,
        string propertyName,
        TryGetValueDelegate<TValue> valueTryGetter,
        out TValue propertyValue) where TValue : new()
    {
        propertyValue = new();
        if (jsonElement.TryGetProperty(propertyName, out JsonElement _))
        {
            if (valueTryGetter(out TValue value))
            {
                propertyValue = value;
                return true;
            }
        }
        return false;
    }

    public static bool TryGetPropertyValue<TValue>(
        this JsonElement jsonElement,
        string propertyName,
        Func<JsonElement, TValue> valueGetter,
        out TValue propertyValue) where TValue : new()
    {
        propertyValue = new();
        if (jsonElement.TryGetProperty(propertyName, out JsonElement element))
        {
            try
            {
                propertyValue = valueGetter(element);
                return true;
            }
            catch (InvalidOperationException) { }
        }
        return false;
    }

    public static bool TryGetPropertyValue(
        this JsonElement jsonElement,
        string propertyName,
        out string propertyValue)
    {
        propertyValue = "";
        if (jsonElement.TryGetProperty(propertyName, out JsonElement element))
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                return false;
            }
            var value = element.GetString();
            if (value != null)
            {
                propertyValue = value;
                return true;
            }
        }
        return false;
    }
}
