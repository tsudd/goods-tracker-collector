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
    private readonly AdapterConfig config;
    private readonly CollectorContext context;
    private readonly ILogger logger;

    public PostgresAdapter(AdapterConfig config, ILogger<PostgresAdapter> logger, CollectorContext context)
    {
        this.config = config;
        this.context = context;
        this.logger = logger;
    }

    public async Task SaveItemsAsync(IItemTracker tracker, IEnumerable<int> shopIds)
    {
        if (!tracker.IsThereAnythingToSave())
        {
            this.Log(LogLevel.Warning, "Nothing to save to save for.");

            return;
        }

        this.Log(LogLevel.Information, "Starting saving items into PostgreSQL");
        this.Log(LogLevel.Information, "Fetching vendors with the provided list of ids...");

        IList<Vendor> vendors = await this.GetVendorsAsync(shopIds)
                                          .ConfigureAwait(false);

        this.Log(
            LogLevel.Information,
            $"Received {vendors.Count} vendor(s): {string.Join(',', vendors.Select(static v => v.Name1))}");

        Stream stream = await this.CreateNewStreamAsync()
                                  .ConfigureAwait(false);

        this.Log(LogLevel.Information, "New stream was created.");

        foreach (Vendor vendor in vendors)
        {
            this.Log(LogLevel.Information, $"Saving items for {vendor.Name1}.");
            IList<ItemModel> items = tracker.GetShopItems(vendor.Id);

            if (items.Count == 0)
            {
                this.Log(LogLevel.Warning, $"No items to save for {vendor.Name2}.");

                continue;
            }

            var savedItems = 0;
            var updatedItems = 0;
            var rejectedItems = 0;
            var errors = new Dictionary<Result, ItemModel>();

            foreach (ItemModel item in items)
            {
                this.context.Attach(vendor);

                if (item.DoesNotContainBasicInfo())
                {
                    rejectedItems++;

                    continue;
                }

                Result saveResult = await this.UpsertItemAsync(item, vendor, stream)
                                              .ConfigureAwait(false);

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

                this.context.ChangeTracker.Clear();
                this.context.Attach(stream);
            }

            await this.SaveFailedResultsAsync(errors, stream)
                      .ConfigureAwait(false);

            this.Log(
                LogLevel.Information,
                $"Completed saving of items for {vendor.Name2}: " +
                $"{savedItems} were saved, {updatedItems} were updated, {errors.Count}" +
                $" failed to be saved, {rejectedItems} were rejected.");
        }
    }

    public void SaveItems(IItemTracker tracker, IEnumerable<int> shopIds)
    {
        this.SaveItemsAsync(tracker, shopIds)
            .Wait();
    }

    // TODO: improve logging
    private void Log(LogLevel level, string message)
    {
        LoggerMessage.Define(level, 0, message)(this.logger, null);
    }

    private async Task<StreamRecord> CreateNewStreamAsync()
    {
        var streamOfFetchedItems = new StreamRecord
        {
            FetchDate = DateTime.UtcNow,
        };

        this.context.Add(streamOfFetchedItems);

        await this.context.SaveChangesAsync()
                  .ConfigureAwait(false);

        return streamOfFetchedItems;
    }

    private async Task<Result> UpsertItemAsync(ItemModel model, Vendor vendor, StreamRecord stream)
    {
        ISuccess? upsertResult;

        if (this.TryGetItemOrCreateFromModel(model, vendor, out Item item))
        {
            UpdateItemIfRequired(ref item, model);

            IList<Category> newCategories = await this.AddNewCategoriesAsync(model.Categories)
                                                      .ConfigureAwait(false);

            foreach (Category category in newCategories)
            {
                item.Categories.Add(category);
            }

            upsertResult = new UpdatedResult();
        }
        else
        {
            item.Vendor = vendor;

            item.Categories = await this.AddCategoriesAsync(model.Categories)
                                        .ConfigureAwait(false);

            item.Producer = await this.GetOrCreateProducerAsync(model)
                                      .ConfigureAwait(false);

            await this.context.AddAsync(item)
                      .ConfigureAwait(false);

            upsertResult = new CreatedResult();
        }

        await this.context.AddAsync(model.ToItemRecord(stream, item))
                  .ConfigureAwait(false);

        Result<int>? saveResult = await Result.Try(
                                                  () => this.context.SaveChangesAsync(),
                                                  static ex => new FailedResult(
                                                      ex.InnerException?.Message ?? ex.Message))
                                              .ConfigureAwait(false);

        return saveResult.IsSuccess
            ? Result.Ok()
                    .WithSuccess(upsertResult)
            : saveResult.ToResult();
    }

    private async Task<Producer?> GetOrCreateProducerAsync(ItemModel model)
    {
        if (string.IsNullOrEmpty(model.Producer))
        {
            return null;
        }

        uint producerId = HashCodeGenerator.GetHashCode(model.Producer);

        uint existingProducer = await this.context.Producers.Select(static p => p.Id)
                                          .FirstOrDefaultAsync(p => p == producerId)
                                          .ConfigureAwait(false);

        if (existingProducer == default(uint))
        {
            return new Producer
            {
                Id = producerId,
                Name = model.Producer,
                Country = model.Country,
            };
        }

        var result = new Producer
        {
            Id = producerId,
        };

        this.context.Attach(result);

        return result;
    }

    private bool TryGetItemOrCreateFromModel(ItemModel model, Vendor vendor, out Item item)
    {
        ArgumentNullException.ThrowIfNull(model.Name1);

        var result = this.context.Items.Include(static i => i.Categories)
                         .Select(
                             static i => new
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
                             i => EF.Functions.ILike(i.Name1, model.Name1) &&
                                  i.VendorId == vendor.Id &&
                                  i.Weight == model.Weight);

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

        this.context.Attach(item);

        return true;
    }

    private async Task<IList<Vendor>> GetVendorsAsync(IEnumerable<int> ids)
    {
        var vendors = this.context.Vendors.Where(vendor => ids.Contains(vendor.Id))
                          .Select(
                              static v => new
                              {
                                  v.Id,
                                  v.Name2,
                              });

        return await vendors.Select(
                                static v => new Vendor
                                {
                                    Id = v.Id,
                                    Name1 = v.Name2,
                                })
                            .ToListAsync()
                            .ConfigureAwait(false);
    }

    private static void UpdateItemIfRequired(ref Item item, ItemModel model)
    {
        if (!model.DoesItemRequireUpdate(item))
        {
            return;
        }

        item.Adult = model.Adult ?? item.Adult;
        item.Carbo = model.Carbo;
        item.Fat = model.Fat;
        item.Protein = model.Protein;
        item.Portion = model.Portion;
        item.Compound = model.Compound;
        item.ImageLink = model.Link != null ? new Uri(model.Link) : item.ImageLink;
    }

    private async Task<IList<Category>> AddNewCategoriesAsync(IEnumerable<string> modelCategories)
    {
        List<string> distinctCategories = modelCategories.Distinct()
                                                         .ToList();

        var newCategories = new List<Category>();

        if (distinctCategories.Count == 0)
        {
            return newCategories;
        }

        IEnumerable<uint> categoryCodes = distinctCategories.Select(static c => HashCodeGenerator.GetHashCode(c));

        List<uint> existingCategoriesCodes = await this.context.Categories.Where(c => categoryCodes.Contains(c.Id))
                                                       .Select(static c => c.Id)
                                                       .ToListAsync()
                                                       .ConfigureAwait(false);

        if (existingCategoriesCodes.Count ==
            categoryCodes.Distinct()
                         .Count())
        {
            return newCategories;
        }

        foreach (var modelCategory in distinctCategories)
        {
            uint code = HashCodeGenerator.GetHashCode(modelCategory);

            if (!existingCategoriesCodes.Any(c => c == code))
            {
                newCategories.Add(
                    new Category
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
        List<string> distinctCategories = modelCategories.Distinct()
                                                         .ToList();

        var categoriesToAdd = new List<Category>();

        if (distinctCategories.Count == 0)
        {
            return categoriesToAdd;
        }

        IEnumerable<uint> categoryCodes = distinctCategories.Select(static c => HashCodeGenerator.GetHashCode(c));

        categoriesToAdd.AddRange(
            await this.context.Categories.Where(c => categoryCodes.Contains(c.Id))
                      .ToListAsync()
                      .ConfigureAwait(false));

        foreach (string modelCategory in distinctCategories)
        {
            uint code = HashCodeGenerator.GetHashCode(modelCategory);

            if (!categoriesToAdd.Any(c => c.Id == code))
            {
                categoriesToAdd.Add(
                    new Category
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
        foreach (KeyValuePair<Result, ItemModel> result in failedResults)
        {
            IError? error = result.Key.Errors.FirstOrDefault();

            if (error == null)
            {
                continue;
            }

            var type = ErrorType.SaveFailed;

            if (error.Message.StartsWith("23505", StringComparison.InvariantCulture))
            {
                type = ErrorType.DuplicateInTheStream;
            }

            await this.context.AddAsync(
                          new ItemError
                          {
                              Stream = stream,
                              ErrorType = type,
                              Details = error.Message,
                              SerialiedItem = type != ErrorType.DuplicateInTheStream ? result.Value.Serialize() : null,
                          })
                      .ConfigureAwait(false);
        }

        await this.context.SaveChangesAsync()
                  .ConfigureAwait(false);
    }
}
