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
using GoodsTracker.DataCollector.Common.Parsers.Exceptions;
using GoodsTracker.DataCollector.Common.Scrapers.Extensions;

namespace GoodsTracker.DataCollector.Common.Scrapers;

public sealed class YaNeighborsScraper : IScraper
{
    private const int requestAttemptsMaxAmount = 3;
    private const int delayBetweenRequests = 400;
    private readonly static Regex productPublicIdPattern = new Regex(
        @"[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}",
        RegexOptions.Compiled
    );

    private readonly IRequester _requester;
    private readonly IItemParser _parser;
    private readonly IItemMapper _mapper;
    private readonly IWebDriver _driver;
    private readonly ScraperConfig _config;
    private ILogger<YaNeighborsScraper> _logger;
    private readonly WebDriverWait _wait;

    public YaNeighborsScraper(
        ScraperConfig config,
        ILogger<YaNeighborsScraper> logger,
        IItemParser parser,
        IWebDriver driver,
        IItemMapper? mapper = null,
        IRequester? requester = null
    )
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
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        _logger.LogInformation("Scraper was created");
    }

    public ScraperConfig GetConfig()
    {
        return _config;
    }

    public async Task<IEnumerable<ItemModel>> GetItemsAsync()
    {
        var categories = GetCategoryLinks();
        var items = new List<ItemModel>();

        foreach (var category in categories)
        {
            items.AddRange(await ProcessCategoryPageAsync(category).ConfigureAwait(false));
        }

        return items;
    }

    private IEnumerable<(string CategoryLink, string CategoryName)> GetCategoryLinks()
    {
        var links = new List<(string CategoryLink, string CategoryName)>();

        _driver.Navigate().GoToUrl(new Uri(_config.ShopUrl + _config.ShopStartRecource));
        WaitForPageToLoad(By.XPath("//div[@class='UiKitShopMenu_root']/ul/li/a"));

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(_driver.PageSource);

        var rawLinks = htmlDoc.DocumentNode.SelectNodes(
            "//div[@class='UiKitShopMenu_root']/ul/li/a"
        );

        foreach (var raw in rawLinks)
        {
            links.Add(
                (
                    raw.Attributes["href"].Value,
                    raw.SelectSingleNode("div[@class='UiKitDesktopShopMenuItem_text']").InnerText
                )
            );
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
        (string CategoryLink, string CategoryName) recourse
    )
    {
        _driver.Navigate().GoToUrl(new Uri(_config.ShopUrl + recourse.CategoryLink));
        WaitForPageToLoad(By.XPath("//ul[@class='DesktopGoodsList_list']/li/a"));
        var page = GetCurrentHtmlDoc();
        var itemRecourses = new List<string>();
        var parsedItems = new List<ItemModel>();

        var itemNodes = page.DocumentNode.SelectNodes("//li[@class='DesktopGoodsList_item']/a");

        if (itemNodes == null)
        {
            _logger.LogWarning($"couldn't parse items from category: {recourse.CategoryLink}");
            return parsedItems;
        }

        var results = await Task.WhenAll(itemNodes.Select(itemNode => ProcessItemAsync(itemNode))).ConfigureAwait(false);

        return results.NotNulls();
    }

    private async Task<ItemModel?> ProcessItemAsync(HtmlNode itemNode)
    {
        var productLink = itemNode.Attributes["href"].Value;
        var productGuidMatch = productPublicIdPattern.Match(productLink);

        if (!productGuidMatch.Success)
        {
            _logger.LogError("couldn't match product GUID to fetch info: " + productLink);
            return null;
        }
        try
        {
            var rawItem = await RequestProductInfoAsync(productGuidMatch.Value).ConfigureAwait(false);
            var parsedItemFields = _parser.ParseItem(rawItem);

            return _mapper.MapItemFields(parsedItemFields);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("couldn't fetch product from " + productLink + ":" + ex.Message);
        }
        catch (InvalidItemFormatException formatException)
        {
            _logger.LogError("couldn't parse product info from" + productLink + ":" + formatException.Message);
        }
        return null;
    }

    private async Task<string> RequestProductInfoAsync(string productGuid)
    {
        const string productInfo = "https://eda.yandex.by/api/v2/menu/product";

        var attempts = 0;
        do
        {
            attempts++;
            try
            {
                return await _requester.PostAsync(
                    productInfo,
                    _config.Headers,
                    GenerateContentBodyForProductFetch(productGuid))
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    "couldn't fetch product info: "
                        + ex.Message
                        + " request will be retried after the delay"
                );
                await Task.Delay(delayBetweenRequests).ConfigureAwait(false);
            }
        } while (attempts <= requestAttemptsMaxAmount);
        throw new HttpRequestException("couldn't fetch product info after several attempts: " + productGuid);
    }

    private static string GenerateContentBodyForProductFetch(string productId) => "{"
            + "\"place_slug\":\"sosedi_kaodz\","
            + $"\"product_public_id\":\"{productId}\","
            + "\"with_categories\":true"
            + "}";

    private void WaitForPageToLoad(By condition)
    {
        try
        {
            _wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(condition));
        }
        catch (WebDriverTimeoutException)
        {
            _logger.LogWarning($"loading of the page took to much: {_driver.Url}");
            throw new InvalidOperationException("coudln't get category links (CAPTCHA accured!!!)");
        }
    }
}
