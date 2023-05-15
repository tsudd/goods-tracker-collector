namespace GoodsTracker.DataCollector.Common.Adapters;

using System.Collections.Generic;

using GoodsTracker.DataCollector.Common.Adapters.Abstractions;
using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Trackers.Abstractions;
using GoodsTracker.DataCollector.DB.Context;
using GoodsTracker.DataCollector.Models;

using StreamRecord = GoodsTracker.DataCollector.DB.Entities.Stream;

using Microsoft.Extensions.Logging;
using GoodsTracker.DataCollector.DB.Entities;
using Microsoft.EntityFrameworkCore;
using FluentResults;
using GoodsTracker.DataCollector.Common.Adapters.Extensions;
using GoodsTracker.DataCollector.Common.Adapters.Results;
using GoodsTracker.DataCollector.Common.Adapters.Helpers;
using GoodsTracker.DataCollector.DB.Entities.Enumerators;

internal sealed class PostgresAdapter : IDataAdapter
{
    private readonly AdapterConfig _config;
    private readonly CollectorContext _context;
    private readonly ILogger _logger;
    public PostgresAdapter(
        AdapterConfig config,
        ILogger<PostgresAdapter> logger,
        CollectorContext context)
    {
        _config = config;
        _context = context;
        _logger = logger;
    }

    public async Task SaveItemsAsync(IItemTracker tracker, IEnumerable<int> shopIds)
    {
        Log(LogLevel.Information, "Starting saving items into PostgreSQL");
        Log(LogLevel.Information, "Fetching vendors with the provided list of ids...");
        var vendors = await GetVendorsAsync(shopIds).ConfigureAwait(false);
        Log(LogLevel.Information, $"Received {vendors.Count} vendor(s): {string.Join(',', vendors.Select(v => v.Name1))}");

        var stream = await CreateNewStreamAsync().ConfigureAwait(false);

        Log(LogLevel.Information, "New stream was created.");

        foreach (var vendor in vendors)
        {
            Log(LogLevel.Information, $"Saving items for {vendor.Name1}.");
            var items = tracker.GetShopItems(vendor.Id);
            if (items.Count == 0)
            {
                Log(LogLevel.Warning, $"No items to save for {vendor.Name2}.");
                continue;
            }

            var savedItems = 0;
            var updatedItems = 0;
            var rejectedItems = 0;
            var errors = new Dictionary<Result, ItemModel>();

            foreach (var item in items)
            {
                _context.Attach(vendor);

                if (item.DoesNotContainBasicInfo())
                {
                    rejectedItems++;
                    continue;
                }

                var saveResult = await UpsertItemAsync(item, vendor, stream).ConfigureAwait(false);

                if (saveResult.IsFailed)
                {
                    errors.Add(saveResult, item);
                }
                else if (saveResult.HasSuccess<UpdatedResult>())
                {
                    updatedItems++;
                }
                else if (saveResult.HasSuccess<CreatedResult>())
                {
                    savedItems++;
                }

                _context.ChangeTracker.Clear();
                _context.Attach(stream);
            }

            await SaveFailedResultsAsync(errors, stream).ConfigureAwait(false);

            Log(
                LogLevel.Information,
                $"Completed saving of items for {vendor.Name2}: "
                + $"{savedItems} were saved, {updatedItems} were updated, {errors.Count}"
                + $" failed to be saved, {rejectedItems} were rejected.");
        }
    }

    public void SaveItems(IItemTracker tracker, IEnumerable<int> shopIds)
    {
        this.SaveItemsAsync(tracker, shopIds).Wait();
    }

    // TODO: improve logging
    private void Log(LogLevel level, string message)
    {
        LoggerMessage.Define(level, 0, message)(_logger, null);
    }

    private async Task<StreamRecord> CreateNewStreamAsync()
    {
        var streamOfFetchedItems = new StreamRecord
        {
            FetchDate = DateTime.UtcNow,
        };

        _context.Add<StreamRecord>(streamOfFetchedItems);

        await _context.SaveChangesAsync().ConfigureAwait(false);

        return streamOfFetchedItems;
    }

    private async Task<Result> UpsertItemAsync(ItemModel model, Vendor vendor, StreamRecord stream)
    {
        ISuccess? upsertResult;
        if (TryGetItemOrCreateFromModel(model, vendor, out Item item))
        {
            UpdateItemIfRequired(ref item, model);

            var newCategories = await AddNewCategoriesAsync(model.Categories).ConfigureAwait(false);

            foreach (var category in newCategories)
            {
                item.Categories.Add(category);
            }

            upsertResult = new UpdatedResult();
        }
        else
        {
            item.Vendor = vendor;
            item.Categories = await AddCategoriesAsync(model.Categories).ConfigureAwait(false);
            item.Producer = await GetOrCreateProducerAsync(model).ConfigureAwait(false);
            await _context.AddAsync(item).ConfigureAwait(false);
            upsertResult = new CreatedResult();
        }

        await _context.AddAsync(model.ToItemRecord(stream, item)).ConfigureAwait(false);

        var saveResult = await Result.Try(
            () => _context.SaveChangesAsync(),
            ex => new FailedResult(ex.InnerException?.Message ?? ex.Message)).ConfigureAwait(false);

        return saveResult.IsSuccess ? Result.Ok().WithSuccess(upsertResult) : saveResult.ToResult();
    }

    private async Task<Producer?> GetOrCreateProducerAsync(ItemModel model)
    {
        if (string.IsNullOrEmpty(model.Producer))
        {
            return null;
        }

        var producerId = HashCodeGenerator.GetHashCode(model.Producer);

        var existingProducer = await _context.Producers
            .Select(static p => p.Id)
            .FirstOrDefaultAsync(p => p == producerId).ConfigureAwait(false);

        if (existingProducer != default)
        {
            var result = new Producer
            {
                Id = producerId,
            };

            _context.Attach(result);
            return result;
        }

        return new Producer
        {
            Id = producerId,
            Name = model.Producer,
            Country = model.Country,
        };
    }

    private bool TryGetItemOrCreateFromModel(ItemModel model, Vendor vendor, out Item item)
    {
        ArgumentNullException.ThrowIfNull(model.Name1);

        var result = _context.Items
            .Include(static i => i.Categories)
            .Select(static i => new
            {
                i.Id,
                i.Name1,
                i.VendorId,
                i.Fat,
                i.Protein,
                i.Carbo,
                i.ProducerId,
                i.Weight,
                i.ImageLink,
                i.Portion,
            })
            .FirstOrDefault(
                i => EF.Functions.ILike(i.Name1, model.Name1)
                && i.VendorId == vendor.Id
                && i.Weight == model.Weight);

        if (result == null)
        {
            item = model.ToEntity();

            return false;
        }

        item = new Item
        {
            Id = result.Id,
            Name1 = result.Name1,
            VendorId = result.VendorId,
            ProducerId = result.ProducerId,
            Weight = result.Weight,
            Portion = result.Portion,
            ImageLink = result.ImageLink,
            Fat = result.Fat,
            Protein = result.Protein,
            Carbo = result.Carbo,
        };
        _context.Attach(item);

        return true;
    }

    private async Task<IList<Vendor>> GetVendorsAsync(IEnumerable<int> ids)
    {
        var vendors = _context.Vendors.Where(vendor => ids.Contains(vendor.Id)).Select(static v => new
        {
            v.Id,
            v.Name2,
        });

        return await vendors.Select(static v => new Vendor
        {
            Id = v.Id,
            Name1 = v.Name2,
        }).ToListAsync().ConfigureAwait(false);
    }

    private void UpdateItemIfRequired(ref Item item, ItemModel model)
    {
        if (model.DoesItemRequireUpdate(item))
        {
            item.Adult = model.Adult ?? item.Adult;
            item.Carbo = model.Carbo;
            item.Fat = model.Fat;
            item.Protein = model.Protein;
            item.Portion = model.Portion;
            item.ImageLink = model.Link != null ? new Uri(model.Link) : item.ImageLink;
        }
    }

    private async Task<IList<Category>> AddNewCategoriesAsync(IList<string> modelCategories)
    {
        var distinctCategories = modelCategories.Distinct().ToList();
        var newCategories = new List<Category>();

        if (distinctCategories.Count == 0)
        {
            return newCategories;
        }

        var categoryCodes = distinctCategories.Select(static c => HashCodeGenerator.GetHashCode(c));

        var existingCategoriesCodes = await _context.Categories
                .Where(c => categoryCodes.Contains(c.Id))
                .Select(static c => c.Id)
                .ToListAsync().ConfigureAwait(false);

        if (existingCategoriesCodes.Count == categoryCodes.Distinct().Count())
        {
            return newCategories;
        }

        foreach (var modelCategory in distinctCategories)
        {
            var code = HashCodeGenerator.GetHashCode(modelCategory);
            if (!existingCategoriesCodes.Any(c => c == code))
            {
                newCategories.Add(new Category
                {
                    Id = code,
                    Name = modelCategory,
                });
            }
        }

        return newCategories;
    }

    private async Task<IList<Category>> AddCategoriesAsync(IList<string> modelCategories)
    {
        var distinctCategories = modelCategories.Distinct().ToList();
        var categoriesToAdd = new List<Category>();

        if (distinctCategories.Count == 0)
        {
            return categoriesToAdd;
        }

        var categoryCodes = distinctCategories.Select(static c => HashCodeGenerator.GetHashCode(c));

        categoriesToAdd.AddRange(
            await _context.Categories.Where(c => categoryCodes.Contains(c.Id)).ToListAsync().ConfigureAwait(false));

        foreach (var modelCategory in distinctCategories)
        {
            var code = HashCodeGenerator.GetHashCode(modelCategory);
            if (!categoriesToAdd.Any(c => c.Id == code))
            {
                categoriesToAdd.Add(new Category
                {
                    Id = code,
                    Name = modelCategory,
                });
            }
        }

        return categoriesToAdd;
    }

    private async Task SaveFailedResultsAsync(Dictionary<Result, ItemModel> failedResults, StreamRecord stream)
    {
        foreach (var result in failedResults)
        {
            var error = result.Key.Errors.FirstOrDefault();

            if (error == null)
            {
                continue;
            }

            ErrorType type = ErrorType.SaveFailed;

            if (error.Message.StartsWith("23505", StringComparison.InvariantCulture))
                type = ErrorType.DuplicateInTheStream;

            await _context.AddAsync(new ItemError
            {
                Stream = stream,
                ErrorType = type,
                Details = error.Message,
                SerialiedItem = type != ErrorType.DuplicateInTheStream ? result.Value.Serialize() : null,
            }).ConfigureAwait(false);
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}
