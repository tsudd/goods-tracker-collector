namespace GoodsTracker.DataCollector.Common.Scrapers;

using System.Collections.ObjectModel;

using GoodsTracker.DataCollector.Common.Configuration;
using GoodsTracker.DataCollector.Common.Mappers;
using GoodsTracker.DataCollector.Common.Mappers.Abstractions;
using GoodsTracker.DataCollector.Common.Parsers.Abstractions;
using GoodsTracker.DataCollector.Common.Requesters;
using GoodsTracker.DataCollector.Common.Requesters.Abstractions;
using GoodsTracker.DataCollector.Common.Scrapers.Abstractions;
using GoodsTracker.DataCollector.Models;

using System.Drawing;
using System.Text.RegularExpressions;

using FluentResults;

using GoodsTracker.DataCollector.Models.Constants;

using Microsoft.Extensions.Logging;

using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

using SeleniumExtras.WaitHelpers;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

internal sealed class EvrooptScraper : IScraper
{
    private const int requestAttemptsMaxAmount = 3;
    private const int delayBetweenRequests = 600;
    private const int maxWaitingTime = 20;
    private const int delayBetweenItemsLoad = 3;
    private static readonly Regex productIdLinkPattern = new(@".*\/product\/(\d+)", RegexOptions.Compiled);

    private static readonly Regex dataSourceIdPattern = new(
        @"\/_next\/static\/([a-zA-Z0-9_-]+)\/_buildManifest\.js", RegexOptions.Compiled);

    private readonly IRequester requester;
    private readonly IItemParser parser;
    private readonly IItemMapper mapper;
    private readonly IWebDriver driver;
    private readonly ScraperConfig config;
    private readonly ILogger<EvrooptScraper> logger;
    private readonly WebDriverWait wait;
    private string? activeResourceUrl;

    internal EvrooptScraper(
        ScraperConfig config, ILogger<EvrooptScraper> logger, IItemParser parser, IWebDriver driver,
        IItemMapper? mapper = null)
    {
        this.requester = new BasicRequester();
        this.mapper = mapper ?? new BasicMapper();
        this.logger = logger;
        this.config = config;
        this.parser = parser;
        this.driver = driver;

        this.driver.Manage()
            .Window.Size = new Size(config.BrowserWidth, config.BrowserHeight);

        this.wait = new WebDriverWait(this.driver, TimeSpan.FromSeconds(maxWaitingTime));
    }

    public async Task<IList<ItemModel>> GetItemsAsync()
    {
        this.driver.Navigate()
            .GoToUrl(this.config.ShopUri);

        this.ClosePopupsIfAppeared();

        if (!this.TryExtractDataSource(out string dataSourceId))
        {
            throw new InvalidOperationException("Unable to extract data source id");
        }

        this.activeResourceUrl = dataSourceId;
        IEnumerable<(string CategoryLink, string CategoryName)> categories = this.GetCategoryLinks();
        var items = new List<ItemModel>();

        foreach ((string CategoryLink, string CategoryName) category in categories)
        {
            IEnumerable<ItemModel> categoryItems = await this.ProcessCategoryPageAsync(category)
                                                             .ConfigureAwait(false);

            items.AddRange(categoryItems);
        }

        return items;
    }

    private async Task<IEnumerable<ItemModel>> ProcessCategoryPageAsync(
        (string CategoryLink, string CategoryName) category)
    {
        const string productsContainerXPath = "//div[contains(@class,'products_products')]";
        const string productsPaginationNextXPath = "//li[contains(@class,'pagination_next')]/a";
        var productIds = new List<string>();

        this.driver.Navigate()
            .GoToUrl(new Uri(category.CategoryLink));

        this.WaitForPageToLoad(By.XPath(productsContainerXPath));

        do
        {
            this.LoadItems();

            ReadOnlyCollection<IWebElement> itemElements = this.driver.FindElements(
                By.XPath(productsContainerXPath + "/div/div/div[2]/a[contains(@class,'vertical_preview__link')]"));

            foreach (IWebElement itemElement in itemElements)
            {
                Match productIdMatch = productIdLinkPattern.Match(itemElement.GetAttribute("href"));

                if (!productIdMatch.Success)
                {
                    LoggerMessage.Define(
                        LogLevel.Warning, 0,
                        $"couldn't match product resource to fetch info: {itemElement.GetAttribute("href")}")(
                        this.logger, null);

                    continue;
                }

                productIds.Add(
                    productIdMatch.Groups[1]
                                  .Value);
            }

            try
            {
                this.driver.FindElement(By.XPath(productsPaginationNextXPath))
                    .Click();
            }
            catch (WebDriverException)
            {
                break;
            }
        }
        while (true);

        Result<IEnumerable<ItemModel>> results = (await Task.WhenAll(productIds.Select(this.ProcessItemAsync))
                                                            .ConfigureAwait(false)).Merge();

        return results.Value;
    }

    private IEnumerable<(string CategoryLink, string CategoryName)> GetCategoryLinks()
    {
        const string categoriesButtonXPath = "//div[contains(@class,'catalog_burger')]";
        const string categoriesXPath = "//ul[contains(@class,'navigation_categories')]/li";
        const string categoryLink = "//a[contains(@class,'desktop_subcategories__title')]";
        By categoryButtonPath = By.XPath(categoriesButtonXPath);
        var links = new List<(string CategoryLink, string CategoryName)>();

        this.driver.FindElement(categoryButtonPath)
            .Click();

        ReadOnlyCollection<IWebElement> categoryElements = this.driver.FindElements(By.XPath(categoriesXPath));
        var action = new Actions(this.driver);

        foreach (IWebElement element in categoryElements)
        {
            action.MoveToElement(element)
                  .Perform();

            IWebElement category = this.driver.FindElement(By.XPath(categoryLink));
            links.Add((category.GetAttribute("href"), category.Text));
        }

        return links;
    }

    public ScraperConfig GetConfig()
    {
        return this.config;
    }

    private async Task<Result<ItemModel>> ProcessItemAsync(string productId)
    {
        Result<string> requestProductResult = await this.RequestProductInfoAsyncWithMultipleAttempts(productId)
                                                        .ConfigureAwait(false);

        if (requestProductResult.IsFailed)
        {
            return Result.Fail($"couldn't fetch product info: {requestProductResult.Errors}");
        }

        Result<Dictionary<ItemFields, string>> parseItemResult = this.parser.ParseItem(requestProductResult.Value);

        if (parseItemResult.IsFailed)
        {
            return Result.Fail($"couldn't parse item info: {parseItemResult.Errors}");
        }

        return this.mapper.MapItemFields(parseItemResult.Value);
    }

    // TODO: move to abstract class
    private async Task<Result<string>> RequestProductInfoAsyncWithMultipleAttempts(string productId)
    {
        var productInfoUrl = $"https://edostavka.by/_next/data/{this.activeResourceUrl}/product/{productId}.json";
        var attempts = 0;

        do
        {
            attempts++;

            try
            {
                return await this.requester.GetAsync(productInfoUrl, this.config.Headers)
                                 .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                LoggerMessage.Define(
                    LogLevel.Warning, 0,
                    $"couldn't fetch product info: {ex.Message}. Request will be retried after the delay")(
                    this.logger, ex);

                await Task.Delay(delayBetweenRequests)
                          .ConfigureAwait(false);
            }
        }
        while (attempts <= requestAttemptsMaxAmount);

        return Result.Fail("couldn't fetch product info after several attempts: " + productId);
    }

    private void ClosePopupsIfAppeared()
    {
        const string cookiesPopupXPath = "//div[contains(@class,'cookies_actions')]";
        const string addressPopupXPath = "//div[contains(@class,'address_main')]";
        this.WaitForPageToLoad(By.XPath(cookiesPopupXPath));

        this.driver.FindElement(By.XPath(cookiesPopupXPath + "/button[2]"))
            ?.Click();

        this.WaitForPageToLoad(By.XPath(addressPopupXPath));

        this.driver.FindElement(By.XPath("//button[contains(@class,'closeButton')]"))
            ?.Click();
    }

    private void WaitForPageToLoad(By condition)
    {
        try
        {
            this.wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(condition));
        }
        catch (WebDriverTimeoutException ex)
        {
            LoggerMessage.Define(LogLevel.Critical, 0, $"loading of the page took to much: {this.driver.Url}")(
                this.logger, ex);

            throw new InvalidOperationException("couldn't get info after long page loading.");
        }
    }

    private void LoadItems()
    {
        const string itemLoaderXPath = "//div[contains(@class,'lazy-listing_loader')]";
        var jsExecutor = (IJavaScriptExecutor)this.driver;
        var waitForMore = new WebDriverWait(this.driver, TimeSpan.FromSeconds(delayBetweenItemsLoad));

        while (true)
        {
            try
            {
                waitForMore.Until(ExpectedConditions.ElementExists(By.XPath(itemLoaderXPath)));
                IWebElement element = this.driver.FindElement(By.XPath(itemLoaderXPath));
                jsExecutor.ExecuteScript("arguments[0].scrollIntoView(true)", element);
            }
            catch (WebDriverException)
            {
                return;
            }
        }
    }

    private bool TryExtractDataSource(out string dataSourceId)
    {
        const string scriptSelector = "script[src*='_buildManifest.js']";
        dataSourceId = string.Empty;
        IWebElement scriptElement = this.driver.FindElement(By.CssSelector(scriptSelector));
        Match sourceMatch = dataSourceIdPattern.Match(scriptElement.GetAttribute("src"));

        if (!sourceMatch.Success)
        {
            return false;
        }

        dataSourceId = sourceMatch.Groups[1]
                                  .Value;

        return true;
    }
}
