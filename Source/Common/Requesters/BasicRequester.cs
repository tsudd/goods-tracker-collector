using System.Net.Http.Headers;

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

    public async Task<HtmlDocument> GetPageHtmlAsync(
        string url,
        Dictionary<string, string>? headers = null
    )
    {
        using var request = CreateRequest(url, headers);
        var response = await Client.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HtmlWebException($"couldn't get HTML page from {url}");
        }
        var result = new HtmlDocument();
        result.LoadHtml(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        return result;
    }

    public async Task<string> PostAsync(
        Uri uri,
        Dictionary<string, string>? headers = null,
        string data = ""
    )
    {
        var result = "";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        if ((headers != null))
        {
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
        request.Content = new StringContent(data);
        request.Content!.Headers.ContentType = MediaTypeHeaderValue.Parse(
            "application/json;charset=utf-8"
        );
        using (var response = await Client.SendAsync(request).ConfigureAwait(false))
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"couldn't perform POST to {uri.AbsoluteUri}");
            }
            using (
                var reader = new StreamReader(
                    await response.Content.ReadAsStreamAsync().ConfigureAwait(false)
                )
            )
            {
                result = await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
        return result;
    }

    public async Task<string> GetAsync(string url, Dictionary<string, string>? headers = null)
    {
        using var request = CreateRequest(url, headers);
        using (var response = await Client.SendAsync(request).ConfigureAwait(false))
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HtmlWebException($"couldn't perform GET to {url}");
            }
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }

    private static HttpRequestMessage CreateRequest(string url, Dictionary<string, string>? headers = null)
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
