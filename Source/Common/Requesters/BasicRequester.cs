using GoodsTracker.DataCollector.Common.Requesters.Abstractions;
using HtmlAgilityPack;
namespace GoodsTracker.DataCollector.Common.Requesters;

public class BasicRequester : IRequester
{
    public BasicRequester(HttpClient? client = null)
    {
        if (client is null)
        {
            Client = new HttpClient();
        }
        else
        {
            Client = client;
        }
    }

    public HttpClient Client { get; private set; }

    public async Task<HtmlDocument> GetPageHtmlAsync(string url, Dictionary<string, string>? headers = null)
    {
        var request = CreateRequest(url, headers);
        var response = await Client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new HtmlWebException(
                $"couldn't get HTML page from {url}: {response.RequestMessage}");
        }
        var result = new HtmlDocument();
        result.LoadHtml(await response.Content.ReadAsStringAsync());
        return result;
    }

    public async Task<string> PostAsync(string url, Dictionary<string, string>? headers = null, string data = "")
    {
        var result = "";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if ((headers != null))
        {
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
        request.Content = new StringContent(data);
        using (var response = await Client.SendAsync(request))
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HtmlWebException(
                    $"couldn't perform POST to {url}: {response.RequestMessage}");
            }
            using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
            {
                result = await reader.ReadToEndAsync();
            }
        }
        return result;
    }

    public async Task<string> GetAsync(string url, Dictionary<string, string>? headers = null)
    {
        var request = CreateRequest(url, headers);
        using (var response = await Client.SendAsync(request))
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HtmlWebException(
                    $"couldn't perform GET to {url}: {response.RequestMessage}");
            }
            return await response.Content.ReadAsStringAsync();
        }
    }

    private HttpRequestMessage CreateRequest(string url, Dictionary<string, string>? headers = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
        return request;
    }
}