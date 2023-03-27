using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;
using CsvHelper;
using Microsoft.Extensions.Logging;
using GoodsTracker.DataCollector.Models;
using System.Globalization;
using GoodsTracker.DataCollector.Common.Adapters.Abstractions;

namespace GoodsTracker.DataCollector.Common.Adapters;

public class CsvAdapter : IDataAdapter
{
    private ILogger _logger;
    private AdapterConfig _config;

    public CsvAdapter(AdapterConfig config, ILogger<CsvAdapter> logger)
    {
        _config = config;
        _logger = logger;
    }

    public void SaveItems(IItemTracker tracker, IEnumerable<int> shopIds)
    {
        _logger.LogInformation("Writing items into CSV files...");
        foreach (var shop in shopIds)
        {
            var items = tracker.GetShopItems(shop);
            if (items is null || items.Count() == 0)
            {
                _logger.LogWarning($"No items to save for {shop}");
                continue;
            }
            var fileName = BuildCSVFileName(shop);
            try
            {
                using (var fs = new StreamWriter(fileName))
                using (var csvWriter = new CsvWriter(fs, CultureInfo.CurrentCulture))
                {
                    csvWriter.WriteHeader<ItemModel>();
                    csvWriter.NextRecord();
                    csvWriter.WriteRecords(items);
                    _logger.LogInformation(
                        $"{items.Count()} items from shop '{shop}' were writed to the {fileName}"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Couldn't write shop '{shop}' items to a CSV: {ex.Message}");
            }
        }
    }

    private string BuildCSVFileName(int shop, string timeLabel = "")
    {
        var timestamp = timeLabel;
        if (timestamp.Length == 0)
            timestamp = DateTime.Now.ToString("dd.MM.yyyy_HH-mm-ss");
        return $"shop_{shop}_{timestamp}.csv";
    }
}
