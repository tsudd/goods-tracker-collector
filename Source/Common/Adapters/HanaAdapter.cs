using Sap.Data.Hana;
using Microsoft.Extensions.Logging;
using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;
using GoodsTracker.DataCollector.Common.Adapters.Exceptions;
using GoodsTracker.DataCollector.Common.Adapters.Abstractions;
using Common.Adapters.Extensions;

namespace GoodsTracker.DataCollector.Common.Adapters;
public class HanaAdapter : IDataAdapter
{
    public const string CREATE_STREAM_COMMAND =
        "INSERT INTO STREAM VALUES (STREAMSEQID.nextval, CURRENT_TIMESTAMP(0))";
    private const string ITEM_SEQUENCE_CUR = "ITEMSEQID.CURRVAL";
    private const string ITEM_SEQUENCE_NEXT = "ITEMSEQID.NEXTVAL";
    private ILogger _logger;
    private AdapterConfig _config;

    public HanaAdapter(AdapterConfig config, ILogger<HanaAdapter> logger)
    {
        _logger = logger;
        _config = config;
    }

    public void SaveItems(IItemTracker tracker, IEnumerable<string> shopIds)
    {
        try
        {
            using (var conn = new HanaConnection(_config.Arguments))
            {
                conn.Open();

                using (var cmd = new HanaCommand(CREATE_STREAM_COMMAND, conn))
                {
                    cmd.ExecuteNonQuery();
                    _logger.LogInformation("New stream was created. Starting saving items from shops into the DB");

                    foreach (var shop in shopIds)
                    {
                        _logger.LogInformation($"Saving items for {shop}");
                        cmd.Parameters.Clear();
                        var items = tracker.GetShopItems(shop);
                        if (items is null || items.Count() == 0)
                        {
                            _logger.LogWarning($"No items to save for {shop}");
                            continue;
                        }
                        try
                        {
                            var savedItems = 0;
                            var updatedItems = 0;
                            var rejectedItems = 0;
                            using (var insertRecordsCmd = new HanaCommand(GenerateItemRecordSaveCommand(), conn))
                            using (var insertItemCmd = new HanaCommand(GenerateItemSaveCommand(shop), conn))
                            {
                                foreach (var item in items)
                                {
                                    if (item.Name1 == null)
                                    {
                                        _logger.LogWarning("skiping item with empty name");
                                        rejectedItems++;
                                        continue;
                                    }
                                    cmd.CommandText = GenerateSelectItemCommand(item.Name1);
                                    using (var itemReader = cmd.ExecuteReader())
                                    {
                                        if (itemReader.HasRows)
                                        {
                                            while (itemReader.Read())
                                            {
                                                insertRecordsCmd.AddItemRecordToInsert(item, itemReader[0]);
                                                updatedItems++;
                                            }
                                            continue;
                                        }
                                    }
                                    insertItemCmd.Parameters.Clear();
                                    cmd.Parameters.Clear();

                                    insertItemCmd.AddItemToInsert(item);
                                    cmd.CommandText = GenerateItemRecordSaveCommand(ITEM_SEQUENCE_CUR);
                                    cmd.AddItemRecordToInsert(item);

                                    if (!insertItemCmd.TryExecuteCommand())
                                    {
                                        _logger.LogError($"coudln't save new item {item.Name1}");
                                        rejectedItems++;
                                        continue;
                                    }
                                    if (!cmd.TryExecuteCommand())
                                    {
                                        _logger.LogError($"coudln't save record for new item {item.Name1}");
                                    }
                                    savedItems++;

                                    if (item.Categories != null)
                                    {
                                        cmd.Parameters.Clear();

                                        cmd.CommandText = GenerateSelectExistingCategoriesCommand(item.Categories);
                                        var existingCategoryHashes = new List<int>();
                                        var existingCategoryNames = new List<string>();
                                        using (var reader = cmd.ExecuteReader())
                                        {
                                            while (reader.Read())
                                            {
                                                existingCategoryHashes.Add(reader.GetInt32(0));
                                                existingCategoryNames.Add(reader.GetString(1));
                                            }
                                        }

                                        var categoriesToCreate = from cat in item.Categories
                                                                 where !existingCategoryNames.Any(existing => cat == existing)
                                                                 select cat;

                                        if (categoriesToCreate.Count() > 0)
                                        {
                                            cmd.CommandText = GenerateCategorySaveCommand();
                                            foreach (var newCat in categoriesToCreate)
                                            {
                                                cmd.AddCategoryToInsert(newCat);
                                                existingCategoryHashes.Add(newCat.GetHashCode());
                                            }
                                            if (!cmd.TryExecuteCommand())
                                                _logger.LogError($"couldn't create new categories for {item.Categories}");
                                            cmd.Parameters.Clear();
                                        }

                                        if (existingCategoryHashes.Count > 0)
                                        {
                                            cmd.CommandText = GenerateCatLinkSaveCommand(ITEM_SEQUENCE_CUR);
                                            foreach (var catHash in existingCategoryHashes)
                                            {
                                                cmd.AddCatLinkToInsert(catHash);
                                            }
                                            if (!cmd.TryExecuteCommand())
                                                _logger.LogError($"couldn't link item to categories for {item.CategoriesEnum}");
                                            cmd.Parameters.Clear();
                                        }
                                    }
                                }
                                if (!insertRecordsCmd.TryExecuteCommand())
                                {
                                    _logger.LogError($"couldn't update items");
                                    throw new ApplicationException();
                                }

                            }
                            _logger.LogInformation(
                                $"Saving for {shop} completed. {updatedItems} items were updated, {savedItems} new were created.");
                        }
                        catch (HanaException ex)
                        {
                            _logger.LogError($"Error within communication with HANA: {ex.Message}");
                            throw new ApplicationException(ex.Message, ex);
                        }
                    }
                }
            }
        }
        catch (AdapterException ex)
        {
            _logger.LogError($"Error during items save from {ex.ShopId}: {ex.Message}");
            throw new ApplicationException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error within communication with HANA: {ex.Message}");
            throw new ApplicationException(ex.Message);
        }
        _logger.LogInformation("Saving to HANA has been completed. Check the logs for detailed info.");
    }

    private string GenerateItemSaveCommand(string shopId)
    => $"INSERT INTO ITEM VALUES({ITEM_SEQUENCE_NEXT},?,?,?,?,?,?,?,?,?,?,?,?,?,?,'{shopId}')";

    private string GenerateItemRecordSaveCommand(string itemId = "?")
    => $"INSERT INTO ITEMRECORD VALUES(STREAMSEQID.CURRVAL, {itemId}, ?, ?, ?)";
    private string GenerateSelectItemCommand(string itemName)
    => $"SELECT ID FROM ITEM WHERE NAME1 = '{itemName}'";
    private string GenerateCatLinkSaveCommand(string itemId = "?")
    => $"INSERT INTO CATLINK VALUES({itemId}, ?)";
    private string GenerateSelectExistingCategoriesCommand(List<string> categories)
    => $"SELECT ID, NAME FROM CATEGORY WHERE NAME in ({string.Join(",", categories.Select(cat => "'" + cat + "'"))})";
    private string GenerateCategorySaveCommand()
    => "INSERT INTO CATEGORY VALUES(?, ?)";
}
