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

public sealed class YaNeighborsScraper : IScraper
{
    private const int requestAttemptsMaxAmount = 3;
    private const int delayBetweenRequests = 600;
    private const int maxWaitingTime = 20;

    private readonly static Regex productPublicIdPattern = new Regex(
        @"([0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12})\?placeSlug=(.*)$",
        RegexOptions.Compiled);

    private readonly IRequester _requester;
    private readonly IItemParser _parser;
    private readonly IItemMapper _mapper;
    private readonly IWebDriver _driver;
    private readonly ScraperConfig _config;
    private ILogger<YaNeighborsScraper> _logger;
    private readonly WebDriverWait _wait;

    public YaNeighborsScraper(
        ScraperConfig config, ILogger<YaNeighborsScraper> logger, IItemParser parser, IWebDriver driver,
        IItemMapper? mapper = null, IRequester? requester = null)
    {
        if (requester is null)
        {
            _requester = new BasicRequester();
        }
        else
        {
            _requester = requester;
        }

        if (mapper is null)
        {
            _mapper = new BasicMapper();
        }
        else
        {
            _mapper = mapper;
        }

        _logger = logger;
        _config = config;
        _parser = parser;
        _driver = driver;
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(maxWaitingTime));
    }

    public ScraperConfig GetConfig()
    {
        return _config;
    }

    public async Task<IList<ItemModel>> GetItemsAsync()
    {
        var categories = GetCategoryLinks();
        var items = new List<ItemModel>();

        foreach (var category in categories)
        {
            var categoryItems = await ProcessCategoryPageAsync(category)
                .ConfigureAwait(false);

            // TODO: optimize this post processing
            foreach (var item in categoryItems)
            {
                item.Categories.Add(category.CategoryName);
            }

            items.AddRange(categoryItems);
        }

        return items;
    }

    private IEnumerable<(string CategoryLink, string CategoryName)> GetCategoryLinks()
    {
        var links = new List<(string CategoryLink, string CategoryName)>();

        _driver.Navigate()
               .GoToUrl(new Uri(_config.ShopUrl + _config.ShopStartRecource));

        WaitForPageToLoad(By.XPath("//div[@class='UiKitShopMenu_root']/ul/li/a"));
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(_driver.PageSource);
        var rawLinks = htmlDoc.DocumentNode.SelectNodes("//div[@class='UiKitShopMenu_root']/ul/li/a");

        foreach (var raw in rawLinks)
        {
            links.Add(
                (raw.Attributes["href"]
                    .Value, raw.SelectSingleNode("div[@class='UiKitDesktopShopMenuItem_text']")
                               .InnerText));
        }

        return links;
    }

    private HtmlDocument GetCurrentHtmlDoc()
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(_driver.PageSource);

        return doc;
    }

    private async Task<IEnumerable<ItemModel>> ProcessCategoryPageAsync(
        (string CategoryLink, string CategoryName) categoryRecource)
    {
        _driver.Navigate()
               .GoToUrl(new Uri(_config.ShopUrl + categoryRecource.CategoryLink));

        WaitForPageToLoad(By.XPath("//ul[@class='DesktopGoodsList_list']/li/a"));
        var page = GetCurrentHtmlDoc();
        var parsedItems = new List<ItemModel>();
        var itemNodes = page.DocumentNode.SelectNodes("//li[@class='DesktopGoodsList_item']/a");

        if (itemNodes == null)
        {
            LoggerMessage.Define(
                LoggingLevel.Warning, 0, $"couldn't parse items from category: {categoryRecource.CategoryLink}")(
                this._logger, null);

            return parsedItems;
        }

        var results = (await Task.WhenAll(itemNodes.Select(itemNode => ProcessItemAsync(itemNode)))
                                 .ConfigureAwait(false)).Merge();

        LoggerMessage.Define(LoggingLevel.Error, 0, string.Join(",", results.Reasons.Select(r => r.Message)))(
            this._logger, null);

        return results.Value;
    }

    private async Task<Result<ItemModel>> ProcessItemAsync(HtmlNode itemNode)
    {
        var productLink = itemNode.Attributes["href"]
                                  .Value;

        var productGuidMatch = productPublicIdPattern.Match(productLink);

        if (!productGuidMatch.Success)
        {
            return Result.Fail($"couldn't match product recource to fetch info: {productLink}");
        }

        var requestProductResult = await RequestProductInfoAsyncWithMultipleAttempts(
                productGuidMatch.Groups[1]
                                .Value, productGuidMatch.Groups[2]
                                                        .Value)
            .ConfigureAwait(false);

        if (requestProductResult.IsFailed)
        {
            return Result.Fail($"couldn't fetch product from {productLink}: {requestProductResult.Errors}");
        }

        var parseItemResult = _parser.ParseItem(requestProductResult.Value);

        if (parseItemResult.IsFailed)
        {
            return Result.Fail($"couldn't parse product info from {productLink}: {parseItemResult.Errors}");
        }

        return _mapper.MapItemFields(parseItemResult.Value);
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
                return await _requester.PostAsync(
                                           productInfoUri, _config.Headers,
                                           GenerateContentBodyForProductFetch(productGuid, placeSlug))
                                       .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                LoggerMessage.Define(
                    LoggingLevel.Warning, 0,
                    $"couldn't fetch product info: {ex.Message}. Request will be retried after the delay")(
                    this._logger, ex);

                await Task.Delay(delayBetweenRequests)
                          .ConfigureAwait(false);
            }
        }
        while (attempts <= requestAttemptsMaxAmount);

        return Result.Fail("couldn't fetch product info after several attempts: " + productGuid);
    }

    private static string GenerateContentBodyForProductFetch(string productId, string placeSlug)
        => "{" +
           $"\"place_slug\":\"{placeSlug}\"," +
           $"\"product_public_id\":\"{productId}\"," +
           "\"with_categories\":true" +
           "}";

    private void WaitForPageToLoad(By condition)
    {
        try
        {
            _wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(condition));
        }
        catch (WebDriverTimeoutException ex)
        {
            LoggerMessage.Define(LoggingLevel.Critical, 0, $"loading of the page took to much: {_driver.Url}")(
                this._logger, ex);

            throw new InvalidOperationException("coudln't get category links after page loading.");
        }
    }
}
