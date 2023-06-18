using GoodsTracker.DataCollector.Common.Configuration;

using Microsoft.Extensions.Logging;

using GoodsTracker.DataCollector.Models;

using HtmlAgilityPack;

using GoodsTracker.DataCollector.Common.Parsers.Abstractions;
using GoodsTracker.DataCollector.Common.Scrapers.Abstractions;
using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Common.Mappers;
using GoodsTracker.DataCollector.Common.Requesters.Abstractions;
using GoodsTracker.DataCollector.Common.Requesters;

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

using SeleniumExtras.WaitHelpers;

using System.Text.RegularExpressions;

using LoggingLevel = Microsoft.Extensions.Logging.LogLevel;

using FluentResults;

namespace GoodsTracker.DataCollector.Common.Scrapers;

using GoodsTracker.DataCollector.Models.Constants;

internal sealed class YaNeighborsScraper : IScraper
{
    private const int requestAttemptsMaxAmount = 3;
    private const int delayBetweenRequests = 600;
    private const int maxWaitingTime = 40;

    private static readonly Regex productPublicIdPattern = new(
        @"([0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12})\?placeSlug=(.*)$",
        RegexOptions.Compiled);

    private readonly IRequester requester;
    private readonly IItemParser parser;
    private readonly IItemMapper mapper;
    private readonly IWebDriver driver;
    private readonly ScraperConfig config;
    private readonly ILogger<YaNeighborsScraper> logger;
    private readonly WebDriverWait wait;

    public YaNeighborsScraper(
        ScraperConfig config, ILogger<YaNeighborsScraper> logger, IItemParser parser, IWebDriver driver,
        IItemMapper? mapper = null, IRequester? requester = null)
    {
        this.requester = requester ?? new BasicRequester();
        this.mapper = mapper ?? new BasicMapper();
        this.logger = logger;
        this.config = config;
        this.parser = parser;
        this.driver = driver;
        this.wait = new WebDriverWait(this.driver, TimeSpan.FromSeconds(maxWaitingTime));
    }

    public ScraperConfig GetConfig()
    {
        return this.config;
    }

    public async Task<IList<ItemModel>> GetItemsAsync()
    {
        IEnumerable<(string CategoryLink, string CategoryName)> categories = this.GetCategoryLinks();
        var items = new List<ItemModel>();

        foreach ((string CategoryLink, string CategoryName) category in categories)
        {
            IEnumerable<ItemModel> categoryItems = await this.ProcessCategoryPageAsync(category)
                                                             .ConfigureAwait(false);

            // TODO: optimize this post processing
            foreach (ItemModel item in categoryItems)
            {
                item.Categories.Add(category.CategoryName);
            }

            items.AddRange(categoryItems);
        }

        return items;
    }

    private IEnumerable<(string CategoryLink, string CategoryName)> GetCategoryLinks()
    {
        this.driver.Navigate()
            .GoToUrl(new Uri(this.config.ShopUrl + this.config.ShopStartRecource));

        this.WaitForPageToLoad(By.XPath("//div[@class='UiKitShopMenu_root']/ul/li/a"));
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(this.driver.PageSource);
        HtmlNodeCollection? rawLinks = htmlDoc.DocumentNode.SelectNodes("//div[@class='UiKitShopMenu_root']/ul/li/a");

        return rawLinks.Select(
                           static raw => (raw.Attributes["href"]
                                             .Value, raw.SelectSingleNode("div[@class='UiKitDesktopShopMenuItem_text']")
                                                        .InnerText))
                       .ToList();
    }

    private HtmlDocument GetCurrentHtmlDoc()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(this.driver.PageSource);

        return doc;
    }

    private async Task<IEnumerable<ItemModel>> ProcessCategoryPageAsync(
        (string CategoryLink, string CategoryName) categoryResource)
    {
        this.driver.Navigate()
            .GoToUrl(new Uri(this.config.ShopUrl + categoryResource.CategoryLink));

        this.WaitForPageToLoad(By.XPath("//ul[@class='DesktopGoodsList_list']/li/a"));
        HtmlDocument page = this.GetCurrentHtmlDoc();
        var parsedItems = new List<ItemModel>();
        HtmlNodeCollection? itemNodes = page.DocumentNode.SelectNodes("//li[@class='DesktopGoodsList_item']/a");

        if (itemNodes == null)
        {
            LoggerMessage.Define(
                LoggingLevel.Warning, 0, $"couldn't parse items from category: {categoryResource.CategoryLink}")(
                this.logger, null);

            return parsedItems;
        }

        Result<IEnumerable<ItemModel>>? results = (await Task.WhenAll(itemNodes.Select(this.ProcessItemAsync))
                                                             .ConfigureAwait(false)).Merge();

        return results.Value;
    }

    private async Task<Result<ItemModel>> ProcessItemAsync(HtmlNode itemNode)
    {
        string? productLink = itemNode.Attributes["href"]
                                      .Value;

        Match productGuidMatch = productPublicIdPattern.Match(productLink);

        if (!productGuidMatch.Success)
        {
            return Result.Fail($"couldn't match product resource to fetch info: {productLink}");
        }

        Result<string> requestProductResult = await this.RequestProductInfoAsyncWithMultipleAttempts(
                                                            productGuidMatch.Groups[1]
                                                                            .Value, productGuidMatch.Groups[2]
                                                                .Value)
                                                        .ConfigureAwait(false);

        if (requestProductResult.IsFailed)
        {
            return Result.Fail($"couldn't fetch product from {productLink}: {requestProductResult.Errors}");
        }

        Result<Dictionary<ItemFields, string>> parseItemResult = this.parser.ParseItem(requestProductResult.Value);

        if (parseItemResult.IsFailed)
        {
            return Result.Fail($"couldn't parse product info from {productLink}: {parseItemResult.Errors}");
        }

        return this.mapper.MapItemFields(parseItemResult.Value);
    }

    private async Task<Result<string>> RequestProductInfoAsyncWithMultipleAttempts(string productGuid, string placeSlug)
    {
        var productInfoUri = new Uri("https://eda.yandex.by/api/v2/menu/product");
        var attempts = 0;

        do
        {
            attempts++;

            try
            {
                return await this.requester.PostAsync(
                                     productInfoUri, this.config.Headers,
                                     GenerateContentBodyForProductFetch(productGuid, placeSlug))
                                 .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                LoggerMessage.Define(
                    LoggingLevel.Warning, 0,
                    $"couldn't fetch product info: {ex.Message}. Request will be retried after the delay")(
                    this.logger, ex);

                await Task.Delay(delayBetweenRequests)
                          .ConfigureAwait(false);
            }
        }
        while (attempts <= requestAttemptsMaxAmount);

        return Result.Fail("couldn't fetch product info after several attempts: " + productGuid);
    }

    private static string GenerateContentBodyForProductFetch(string productId, string placeSlug)
    {
        return "{" +
               $"\"place_slug\":\"{placeSlug}\"," +
               $"\"product_public_id\":\"{productId}\"," +
               "\"with_categories\":true" +
               "}";
    }

    private void WaitForPageToLoad(By condition)
    {
        try
        {
            this.wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(condition));
        }
        catch (WebDriverTimeoutException ex)
        {
            LoggerMessage.Define(LoggingLevel.Critical, 0, $"loading of the page took to much: {this.driver.Url}")(
                this.logger, ex);

            throw new InvalidOperationException("couldn't get category links after page loading.");
        }
    }
}
