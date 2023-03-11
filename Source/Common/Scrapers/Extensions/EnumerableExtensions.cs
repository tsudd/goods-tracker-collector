namespace GoodsTracker.DataCollector.Common.Scrapers.Extensions;

internal static class EnumerableExtensions
{
    public static IEnumerable<T> NotNulls<T>(this IEnumerable<T?> enumerable) where T : class
    {
        return enumerable.Where(e => e != null).Select(e => e!);
    }
}
