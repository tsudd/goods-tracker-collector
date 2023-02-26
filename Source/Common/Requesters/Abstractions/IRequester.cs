using HtmlAgilityPack;
namespace GoodsTracker.DataCollector.Common.Requesters.Abstractions;

public interface IRequester
{
    Task<string> PostAsync(string url, Dictionary<string, string>? headers = null, string data = "");
    Task<string> GetAsync(string url, Dictionary<string, string>? headers = null);
    Task<HtmlDocument> GetPageHtmlAsync(string url, Dictionary<string, string>? headers = null);
}